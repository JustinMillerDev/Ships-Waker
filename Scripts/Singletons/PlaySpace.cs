using Godot;
using System;

public partial class PlaySpace : Node
{
	/// <summary>Current turn number, starting at 0.</summary>
	public int CurrentTurn { get; private set; } = 0;

	public event Action<int> TurnStarted;
	public event Action<int> TurnEnded;
	public event Action<Vector2> ZoomChanged;

	// Deploy budgets (reset to 1 at the start of every turn).
	public int CrewDeploysRemaining  { get; private set; } = 1;
	public int CargoDeploysRemaining { get; private set; } = 1;

	/// <summary>True while the player is in a combat encounter.</summary>
	public bool IsInCombat { get; private set; } = false;

	public void EnterCombat() => IsInCombat = true;
	public void ExitCombat()  => IsInCombat = false;

	// Camera panning -----------------------------------------------------------

	private Camera2D _camera;
	private Node2D _playerShip;
	private Vector2 _playerShipPos;
	private Vector2 _enemyShipPos;
	private bool _viewingEnemy = false;

	/// <summary>World-space anchor used to generate the player attack radian circle.</summary>
	private Vector2 _enemyAnchor;
	private const float CameraPanDuration = 0.2f;
	/// <summary>World-units per second the camera moves when holding an arrow key.</summary>
	private const float CameraMoveSpeed = 600f;

	private static readonly float[] ZoomLevels = { 0.75f, 0.5f };
	private int _zoomIndex = 0;

	public Vector2 CurrentZoom => new Vector2(ZoomLevels[_zoomIndex], ZoomLevels[_zoomIndex]);

	public override void _Ready()
	{
		_camera = GetNodeOrNull<Camera2D>("Camera2D");

		Node2D playerShip = GetNodeOrNull<Node2D>("PlayerShip");
		Node2D enemyShip  = GetNodeOrNull<Node2D>("EnemyShip");

		if (playerShip != null) { _playerShip = playerShip; _playerShipPos = playerShip.GlobalPosition; }
		if (enemyShip  != null)
		{
			_enemyShipPos = enemyShip.GlobalPosition;
			_enemyAnchor  = GetNodeOrNull<Node2D>("EnemyShipClipAndDraw/Pivot/Sprite2D/Anchors/TargetPoint")?.GlobalPosition ?? _enemyShipPos;
		}

		if (_camera != null) _camera.GlobalPosition = _playerShipPos;

		// Initialise the attack radian for the first turn.
		ProjectileManager.Instance?.RandomizeAttackRadian(_enemyAnchor);

		// Share the main World2D with the sub-camera viewport so it renders scene content.
		var subVp = GetNodeOrNull<SubViewport>("CanvasLayer/GUI/Gameplay/ShipPreviews/SubViewportContainer/SubViewport");
		if (subVp != null)
			subVp.World2D = GetViewport().World2D;

		EnterCombat();
		StartTurn();
	}

	public override void _Process(double delta)
	{
		if (_camera == null) return;

		Vector2 dir = Vector2.Zero;
		if (Input.IsActionPressed("ui_left"))  dir.X -= 1f;
		if (Input.IsActionPressed("ui_right")) dir.X += 1f;
		if (Input.IsActionPressed("ui_up"))    dir.Y -= 1f;
		if (Input.IsActionPressed("ui_down"))  dir.Y += 1f;

		if (dir != Vector2.Zero)
		{
			// Scale speed by zoom so movement feels the same at every zoom level.
			float zoom = ZoomLevels[_zoomIndex];
			_camera.GlobalPosition += dir.Normalized() * CameraMoveSpeed * (1f / zoom) * (float)delta;
			_viewingEnemy = false;
		}
	}

	public override void _Input(InputEvent @event)
	{
		if (@event.IsActionPressed("ui_cancel"))
		{
			GetTree().Quit();
			return;
		}

		// Scroll → cycle zoom
		if (@event is InputEventMouseButton mbZoom && mbZoom.Pressed)
		{
			if (mbZoom.ButtonIndex == MouseButton.WheelUp)
			{
				_zoomIndex = Mathf.Max(_zoomIndex - 1, 0);
				ApplyZoom();
				return;
			}
			else if (mbZoom.ButtonIndex == MouseButton.WheelDown)
			{
				_zoomIndex = Mathf.Min(_zoomIndex + 1, ZoomLevels.Length - 1);
				ApplyZoom();
				return;
			}
		}

		// W → pan to enemy ship
		if (@event is InputEventKey wKey && !wKey.IsEcho() && wKey.Pressed && wKey.Keycode == Key.W && !_viewingEnemy)
			PanCameraTo(_enemyShipPos, enemy: true);

		// S → pan back to player ship
		if (@event is InputEventKey sKey && !sKey.IsEcho() && sKey.Pressed && sKey.Keycode == Key.S && _viewingEnemy)
			PanCameraTo(_playerShipPos, enemy: false);
	}

	private void PanCameraTo(Vector2 target, bool enemy)
	{
		if (_camera == null) return;
		_viewingEnemy = enemy;

		Tween tween = CreateTween();
		tween.TweenProperty(_camera, "global_position", target, CameraPanDuration)
			 .SetTrans(Tween.TransitionType.Sine)
			 .SetEase(Tween.EaseType.InOut);
	}

	/// <summary>
	/// Called when a ShipPreview is selected. Shifts the player ship 320 px left
	/// and the camera 320 px right so both move out of the way for the preview panel.
	/// </summary>
	public void OnShipPreviewSelected()
	{
		if (_playerShip != null)
			_playerShip.GlobalPosition = _playerShipPos + new Vector2(-320f, 0f);

		var cargo = GetNodeOrNull<Node2D>("PlayerShip/Pivot/Cargo");
		// if (cargo != null)
		// 	cargo.Position = cargo.Position + new Vector2(-320f, 0f);

		//if (_camera != null)
			//_camera.GlobalPosition = _camera.GlobalPosition + new Vector2(160f, 0f);
	}

	/// <summary>
	/// Called when a ShipPreview is deselected. Restores the player ship to its
	/// original position and shifts the camera back 320 px.
	/// </summary>
	public void OnShipPreviewDeselected()
	{
		if (_playerShip != null)
			_playerShip.GlobalPosition = _playerShipPos;

		var cargo = GetNodeOrNull<Node2D>("PlayerShip/Pivot/Cargo");
		// if (cargo != null)
		// 	cargo.Position = cargo.Position - new Vector2(-320f, 0f);

		//if (_camera != null)
			//_camera.GlobalPosition = _camera.GlobalPosition - new Vector2(160f, 0f);
	}

	/// <summary>Sets the camera zoom to the nearest available zoom level.</summary>
	public void SetZoom(float zoom)
	{
		// Find the closest zoom level in the array.
		float bestDist = float.MaxValue;
		for (int i = 0; i < ZoomLevels.Length; i++)
		{
			float dist = Mathf.Abs(ZoomLevels[i] - zoom);
			if (dist < bestDist) { bestDist = dist; _zoomIndex = i; }
		}
		ApplyZoom();
	}

	private void ApplyZoom()
	{
		if (_camera == null) return;
		float z = ZoomLevels[_zoomIndex];
		_camera.Zoom = new Vector2(z, z);
		ZoomChanged?.Invoke(_camera.Zoom);
	}

	/// <summary>Connected to the End Turn button's pressed signal.</summary>
	public void _on_end_turn_button_pressed()
	{
		EndTurn();
	}

	/// <summary>
	/// Attempts to spend one crew deploy. Returns false (and does not decrement)
	/// if no crew deploys remain.
	/// </summary>
	public bool TryUseCrewDeploy()
	{
		if (CrewDeploysRemaining <= 0) return false;
		CrewDeploysRemaining--;
		UpdateDeployLabels();
		return true;
	}

	/// <summary>
	/// Attempts to spend one cargo deploy. Returns false (and does not decrement)
	/// if no cargo deploys remain.
	/// </summary>
	public bool TryUseCargoDeloy()
	{
		if (CargoDeploysRemaining <= 0) return false;
		CargoDeploysRemaining--;
		UpdateDeployLabels();
		return true;
	}

	private void EndTurn()
	{
		TurnEnded?.Invoke(CurrentTurn);
		ProjectileManager.Instance?.RandomizeAttackRadian(_enemyAnchor);
		CurrentTurn++;
		StartTurn();
	}

	private void StartTurn()
	{
		CrewDeploysRemaining  = 1;
		CargoDeploysRemaining = 1;
		UpdateDeployLabels();
		TurnStarted?.Invoke(CurrentTurn);
	}

	private void UpdateDeployLabels()
	{
		if (GetNodeOrNull("CanvasLayer/GUI/Gameplay/Stats/CrewDeploys") is Label crewLabel)
			crewLabel.Text = $"Crew Deploys:{CrewDeploysRemaining}";

		if (GetNodeOrNull("CanvasLayer/GUI/Gameplay/Stats/CargoDeploys") is Label cargoLabel)
			cargoLabel.Text = $"Cargo Deploys:{CargoDeploysRemaining}";
	}
}
