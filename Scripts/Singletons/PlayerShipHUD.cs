using Godot;

/// <summary>
/// Manages the player ship's health and shields progress bars.
/// Attach this script to any persistent node in the PlaySpace scene.
/// Subscribes to <see cref="CustomSignals.ShipHealthChanged"/> and refreshes
/// the bars whenever the player ship's values change.
/// </summary>
public partial class PlayerShipHUD : Node
{
	private const string HealthBarPath  = "PlaySpace/CanvasLayer/GUI/Gameplay/Stats/Bars/PlayerShipHealth";
	private const string ShieldsBarPath = "PlaySpace/CanvasLayer/GUI/Gameplay/Stats/Bars/PlayerShipShields";

	private Ship _playerShip;

	public override void _Ready()
	{
		_playerShip = GetTree().Root.GetNodeOrNull<Ship>("PlaySpace/PlayerShip");
		CustomSignals.Instance.ShipHealthChanged += OnShipHealthChanged;
		UpdateBars();
	}

	public override void _ExitTree()
	{
		CustomSignals.Instance.ShipHealthChanged -= OnShipHealthChanged;
	}

	private void OnShipHealthChanged(Ship ship)
	{
		if (ship == _playerShip)
			UpdateBars();
	}

	private void UpdateBars()
	{
		if (_playerShip == null) return;

		Node root = GetTree().Root;

		if (root.GetNodeOrNull<Range>(HealthBarPath) is Range healthBar)
		{
			healthBar.MinValue = 0;
			healthBar.MaxValue = _playerShip.MaxHealth;
			healthBar.Value    = _playerShip.CurrentHealth;
		}
		if (root.GetNodeOrNull<Label>(HealthBarPath + "/Label") is Label healthLabel)
			healthLabel.Text = $"{_playerShip.CurrentHealth}/{_playerShip.MaxHealth}";

		if (root.GetNodeOrNull<Range>(ShieldsBarPath) is Range shieldsBar)
		{
			shieldsBar.MinValue = 0;
			shieldsBar.MaxValue = _playerShip.MaxShields;
			shieldsBar.Value    = _playerShip.CurrentShields;
		}
		if (root.GetNodeOrNull<Label>(ShieldsBarPath + "/Label") is Label shieldsLabel)
			shieldsLabel.Text = $"{_playerShip.CurrentShields}/{_playerShip.MaxShields}";
	}
}
