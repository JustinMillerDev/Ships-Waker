using Godot;
using System;

public partial class PlaySpace : Node
{
	/// <summary>Current turn number, starting at 0.</summary>
	public int CurrentTurn { get; private set; } = 0;

	public event Action<int> TurnStarted;
	public event Action<int> TurnEnded;

	// Deploy budgets (reset to 1 at the start of every turn).
	public int CrewDeploysRemaining  { get; private set; } = 1;
	public int CargoDeploysRemaining { get; private set; } = 1;

	/// <summary>True while the player is in a combat encounter.</summary>
	public bool IsInCombat { get; private set; } = false;

	public void EnterCombat() => IsInCombat = true;
	public void ExitCombat()  => IsInCombat = false;

	// Camera panning -----------------------------------------------------------

	private Camera2D _camera;
	private Vector2 _playerShipPos;
	private Vector2 _enemyShipPos;
	private bool _viewingEnemy = false;
	private const float CameraPanDuration = 0.4f;

	public override void _Ready()
	{
		_camera = GetNodeOrNull<Camera2D>("Camera2D");

		Node2D playerShip = GetNodeOrNull<Node2D>("PlayerShip");
		Node2D enemyShip  = GetNodeOrNull<Node2D>("EnemyShip");

		if (playerShip != null) _playerShipPos = playerShip.GlobalPosition;
		if (enemyShip  != null) _enemyShipPos  = enemyShip.GlobalPosition;

		if (_camera != null) _camera.GlobalPosition = _playerShipPos;

		EnterCombat();
		StartTurn();
	}

	public override void _Input(InputEvent @event)
	{
		if (@event.IsActionPressed("ui_cancel"))
		{
			GetTree().Quit();
			return;
		}

		// Scroll wheel up  OR  W / Up arrow  → pan to enemy ship
		bool panUp = @event.IsActionPressed("ui_up") ||
		             (@event is InputEventKey key && !key.IsEcho() && key.Pressed &&
		              (key.Keycode == Key.W)) ||
		             (@event is InputEventMouseButton mb &&
		              mb.ButtonIndex == MouseButton.WheelUp && mb.Pressed);

		// Scroll wheel down  OR  S / Down arrow → pan back to player ship
		bool panDown = @event.IsActionPressed("ui_down") ||
		               (@event is InputEventKey key2 && !key2.IsEcho() && key2.Pressed &&
		                (key2.Keycode == Key.S)) ||
		               (@event is InputEventMouseButton mb2 &&
		                mb2.ButtonIndex == MouseButton.WheelDown && mb2.Pressed);

		if (panUp && !_viewingEnemy)
			PanCameraTo(_enemyShipPos, enemy: true);
		else if (panDown && _viewingEnemy)
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
			crewLabel.Text = $"CrDp:{CrewDeploysRemaining}";

		if (GetNodeOrNull("CanvasLayer/GUI/Gameplay/Stats/CargoDeploys") is Label cargoLabel)
			cargoLabel.Text = $"CaDp:{CargoDeploysRemaining}";
	}
}
