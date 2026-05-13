using Godot;

/// <summary>
/// Describes which faction a ship belongs to.
/// </summary>
public enum ShipFaction
{
	Player,
	Ally,
	Enemy,
}

/// <summary>
/// A ship node. Tracks faction and hit points via <see cref="IHealth"/>.
/// </summary>
public partial class Ship : Node2D, IHealth
{
	// IHealth ------------------------------------------------------------------

	public int MaxHealth { get; private set; } = 50;
	public int CurrentHealth { get; set; } = 50;

	// Faction ------------------------------------------------------------------

	private ShipFaction _faction = ShipFaction.Player;

	/// <summary>Which faction this ship belongs to.</summary>
	[Export]
	public ShipFaction Faction
	{
		get => _faction;
		set
		{
			if (_faction == value) return;
			_faction = value;
		}
	}

	// Godot lifecycle ----------------------------------------------------------

	public override void _Ready()
	{
	}
}
