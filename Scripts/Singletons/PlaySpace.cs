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
	public int CargoDeploysRemaining { get; private set; } = 1;

	/// <summary>True while the player is in a combat encounter.</summary>
	public bool IsInCombat { get; private set; } = false;

	public void EnterCombat() => IsInCombat = true;
	public void ExitCombat()  => IsInCombat = false;

	// Camera panning -----------------------------------------------------------

	private Camera2D _camera;
	private Node2D _playerShip;
	private Ship _playerShipData;
	private Ship _enemyShipData;
	private Node2D _playerShipSmall;
	private Node2D _enemyShipSmall;
	private Node2D _playerTargetIndicator;
	private Node2D _enemyTargetIndicator;
	private Label _playerTargetIndicatorName;
	private Label _playerTargetIndicatorHealth;
	private Label _enemyTargetIndicatorName;
	private Label _enemyTargetIndicatorHealth;
	private Vector2 _playerTargetIndicatorOffset;
	private Vector2 _enemyTargetIndicatorOffset;
	private bool _targetIndicatorOffsetsCaptured;
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

	// Combat range ----------------------------------------------------------
	public CombatRange CurrentRange { get; private set; } = CombatRange.Mid;
	private CombatRange _previousRange = CombatRange.Mid;

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

		_playerShipSmall = GetNodeOrNull<Node2D>("PlayerShipSmall");
		_enemyShipSmall = GetNodeOrNull<Node2D>("EnemyShipSmall");
		_playerShipData = GetNodeOrNull<Ship>("CanvasLayer/PlayerShip");
		_enemyShipData = GetNodeOrNull<Ship>("CanvasLayer/EnemyShip");
		_playerTargetIndicator = GetNodeOrNull<Node2D>("CanvasLayer/GUI/Gameplay/PlayerShipUI/TargetIndicator");
		_enemyTargetIndicator = GetNodeOrNull<Node2D>("CanvasLayer/GUI/Gameplay/EnemyShipUI/TargetIndicator");
		_playerTargetIndicatorName = GetNodeOrNull<Label>("CanvasLayer/GUI/Gameplay/PlayerShipUI/TargetIndicator/Name");
		_playerTargetIndicatorHealth = GetNodeOrNull<Label>("CanvasLayer/GUI/Gameplay/PlayerShipUI/TargetIndicator/Health");
		_enemyTargetIndicatorName = GetNodeOrNull<Label>("CanvasLayer/GUI/Gameplay/EnemyShipUI/TargetIndicator/Name");
		_enemyTargetIndicatorHealth = GetNodeOrNull<Label>("CanvasLayer/GUI/Gameplay/EnemyShipUI/TargetIndicator/Health");
		CaptureTargetIndicatorOffsets();
		UpdateTargetIndicatorsFromShips();
		UpdateTargetIndicatorStats();

		if (CustomSignals.Instance != null)
			CustomSignals.Instance.ShipHealthChanged += OnShipHealthChanged;

		// Initialise the attack radian for the first turn.
		ProjectileManager.Instance?.RandomizeAttackRadian(_enemyAnchor);

		// Share the main World2D with the sub-camera viewport so it renders scene content.
		var subVp = GetNodeOrNull<SubViewport>("CanvasLayer/GUI/Gameplay/ShipPreviews/SubViewportContainer/SubViewport");
		if (subVp != null)
			subVp.World2D = GetViewport().World2D;

		EnterCombat();
		StartTurn();
	}

	public override void _ExitTree()
	{
		if (CustomSignals.Instance != null)
			CustomSignals.Instance.ShipHealthChanged -= OnShipHealthChanged;
	}

	public override void _Process(double delta)
	{
		UpdateTargetIndicatorsFromShips();

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

		// Scroll zoom disabled.

		// W → pan to enemy ship
		if (@event is InputEventKey wKey && !wKey.IsEcho() && wKey.Pressed && wKey.Keycode == Key.W && !_viewingEnemy)
			PanCameraTo(_enemyShipPos, enemy: true);

		// S → pan back to player ship
		if (@event is InputEventKey sKey && !sKey.IsEcho() && sKey.Pressed && sKey.Keycode == Key.S && _viewingEnemy)
			PanCameraTo(_playerShipPos, enemy: false);

		// Debug range controls
		if (@event.IsActionPressed("ui_debug_0"))
		{
			foreach (var slot in ComponentManager.Instance.Slots)
			{
				if (slot.OccupiedBy is ShipComponent sc && sc.Data?.Cooldown.HasValue == true)
				{
					sc.CurrentCooldown = 0;
					CustomSignals.Instance.EmitSignal(CustomSignals.SignalName.CooldownReduced, sc);
				}
			}
		}
		if (@event.IsActionPressed("ui_debug_7")) SetRange(CombatRange.Close);
		if (@event.IsActionPressed("ui_debug_8")) SetRange(CombatRange.Mid);
		if (@event.IsActionPressed("ui_debug_9")) SetRange(CombatRange.Long);
	}

	public void SetRange(CombatRange range)
	{
		_previousRange = CurrentRange;
		CurrentRange = range;

		bool isLongCloseJump = (_previousRange == CombatRange.Long  && CurrentRange == CombatRange.Close)
							 || (_previousRange == CombatRange.Close && CurrentRange == CombatRange.Long);
		double cameraMultiplier = isLongCloseJump ? 2.0 : 1.0;

		UpdateRangeLabel();
		UpdateCameraZoomForRange(cameraMultiplier);
		UpdateShipSmallPositions(1.5);
	}

	/// <summary>Builds the range label text, marking the active range with ***.</summary>
	public string BuildRangeLabelText(CombatRange range)
	{
		static string Mark(string name, CombatRange value, CombatRange current)
			=> value == current ? $"*** {name} ***" : name;

		return $"{Mark("Long", CombatRange.Long, range)}\n{Mark("Mid", CombatRange.Mid, range)}\n{Mark("Close", CombatRange.Close, range)}";
	}

	private void UpdateRangeLabel()
	{
		var label = GetNodeOrNull<Label>("CanvasLayer/GUI/Gameplay/Stats/RangeIndicator/ColorRect/Label");
		if (label != null)
			label.Text = BuildRangeLabelText(CurrentRange);
	}

	private void UpdateCameraZoomForRange(double multiplier = 1.0)
	{
		if (_camera == null) return;

		float z = CurrentRange switch
		{
			CombatRange.Long  => 0.15f,
			CombatRange.Mid   => 0.25f,
			CombatRange.Close => 0.50f,
			_                 => 0.25f,
		};

		Tween tween = CreateTween();
		tween.TweenProperty(_camera, "zoom", new Vector2(z, z), 0.5 * multiplier)
			 .SetTrans(Tween.TransitionType.Sine)
			 .SetEase(Tween.EaseType.InOut);
	}

	private void UpdateShipSmallPositions(double multiplier = 1.0)
	{
		(float enemyX, float playerX) = CurrentRange switch
		{
			CombatRange.Close => (832f,    -193f),
			CombatRange.Mid   => (1366f,   -683f),
			CombatRange.Long  => (1983f,  -1353f),
			_                 => (1366f,   -683f),
		};

		Node2D enemy  = GetNodeOrNull<Node2D>("EnemyShipSmall");
		Node2D player = GetNodeOrNull<Node2D>("PlayerShipSmall");

		double duration = 0.5 * multiplier;

		if (enemy != null)
		{
			Tween t = CreateTween();
			t.TweenProperty(enemy, "position", new Vector2(enemyX, enemy.Position.Y), duration)
			 .SetTrans(Tween.TransitionType.Sine)
			 .SetEase(Tween.EaseType.InOut);
		}
		if (player != null)
		{
			Tween t = CreateTween();
			t.TweenProperty(player, "position", new Vector2(playerX, player.Position.Y), duration)
			 .SetTrans(Tween.TransitionType.Sine)
			 .SetEase(Tween.EaseType.InOut);
		}
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

		var cargo = GetNodeOrNull<Node2D>("CanvasLayer/PlayerShip/Portrait/Pivot//Cargo");
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

		var cargo = GetNodeOrNull<Node2D>("CanvasLayer/PlayerShip/Portrait/Pivot//Cargo");
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
		UpdateTargetIndicatorsFromShips();
	}

	private void CaptureTargetIndicatorOffsets()
	{
		if (_targetIndicatorOffsetsCaptured) return;
		if (_playerShipSmall == null || _enemyShipSmall == null || _playerTargetIndicator == null || _enemyTargetIndicator == null) return;

		_playerTargetIndicatorOffset = _playerTargetIndicator.Position - WorldToScreen(GetShipAnchor(_playerShipSmall));
		_enemyTargetIndicatorOffset = _enemyTargetIndicator.Position - WorldToScreen(GetShipAnchor(_enemyShipSmall));
		_targetIndicatorOffsetsCaptured = true;
	}

	private void UpdateTargetIndicatorsFromShips()
	{
		if (!_targetIndicatorOffsetsCaptured)
			CaptureTargetIndicatorOffsets();

		if (_playerShipSmall != null && _playerTargetIndicator != null)
			_playerTargetIndicator.Position = WorldToScreen(GetShipAnchor(_playerShipSmall)) + _playerTargetIndicatorOffset;

		if (_enemyShipSmall != null && _enemyTargetIndicator != null)
			_enemyTargetIndicator.Position = WorldToScreen(GetShipAnchor(_enemyShipSmall)) + _enemyTargetIndicatorOffset;
	}

	private void OnShipHealthChanged(Ship ship)
	{
		if (ship == _playerShipData || ship == _enemyShipData)
			UpdateTargetIndicatorStats();
	}

	private void UpdateTargetIndicatorStats()
	{
		UpdateSingleTargetIndicatorStats(_playerShipData, _playerTargetIndicatorName, _playerTargetIndicatorHealth, "Player Ship");
		UpdateSingleTargetIndicatorStats(_enemyShipData, _enemyTargetIndicatorName, _enemyTargetIndicatorHealth, "Enemy Ship");
	}

	private static void UpdateSingleTargetIndicatorStats(Ship ship, Label nameLabel, Label healthLabel, string fallbackName)
	{
		if (nameLabel != null)
			nameLabel.Text = ship?.Name ?? fallbackName;

		if (healthLabel != null)
			healthLabel.Text = ship != null
				? $"HP: {ship.CurrentHealth}/{ship.MaxHealth}"
				: "HP: 0/0";
	}

	private static Vector2 GetShipAnchor(Node2D ship)
	{
		if (ship.GetNodeOrNull<Node2D>("Pivot/Sprites/Sprite2D2/TargetIndicatorAnchor") is Node2D anchor)
			return anchor.GlobalPosition;

		if (ship.GetNodeOrNull<Node2D>("Pivot/Sprites/Sprite2D2") is Node2D sprite)
			return sprite.GlobalPosition;

		return ship.GlobalPosition;
	}

	private Vector2 WorldToScreen(Vector2 worldPosition)
	{
		// Convert world-space into canvas/screen-space using the active viewport transform.
		return GetViewport().GetCanvasTransform() * worldPosition;
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
		CargoDeploysRemaining = 1;
		ResetShipPDCs();
		UpdateDeployLabels();

		int enemyRangeIndex = GD.RandRange(0, 2);
		if (CustomSignals.Instance != null)
			CustomSignals.Instance.EmitSignal(CustomSignals.SignalName.EnemyRangeSelected, enemyRangeIndex);

		TurnStarted?.Invoke(CurrentTurn);
	}

	private void ResetShipPDCs()
	{
		GetNodeOrNull<Ship>("CanvasLayer/PlayerShip")?.ResetCurrentPDC();
		GetNodeOrNull<Ship>("CanvasLayer/EnemyShip")?.ResetCurrentPDC();
	}

	private void UpdateDeployLabels()
	{
		if (GetNodeOrNull("CanvasLayer/GUI/Gameplay/Stats/CargoDeploys") is Label cargoLabel)
			cargoLabel.Text = $"Cargo Deploys:{CargoDeploysRemaining}";
	}
}
