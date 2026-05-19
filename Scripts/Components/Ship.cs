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

	// Shields ------------------------------------------------------------------

	public int MaxShields { get; private set; } = 20;
	public int CurrentShields { get; set; } = 20;

	/// <summary>
	/// Absorbs damage from shields first, then spills into health.
	/// Emits <see cref="CustomSignals.ShipHealthChanged"/> after applying damage.
	/// </summary>
	public void TakeDamage(int amount)
	{
		int shieldAbsorb = Mathf.Min(CurrentShields, amount);
		CurrentShields -= shieldAbsorb;
		int remainder = amount - shieldAbsorb;
		if (remainder > 0)
			CurrentHealth = Mathf.Max(0, CurrentHealth - remainder);
		CustomSignals.Instance.EmitSignal(CustomSignals.SignalName.ShipHealthChanged, this);
	}

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
		if (_faction == ShipFaction.Enemy)
			SpawnExhaustTrails();
	}

	// Exhaust trails -----------------------------------------------------------

	/// <summary>
	/// Spawns white-to-transparent downward Line2D exhaust trails on the six
	/// engine exhaust nodes that live under the enemy ship's animated sprite.
	/// </summary>
	private void SpawnExhaustTrails()
	{
		// Positions match the Node2D1-6 positions on the AnimatedSprite2D.
		Vector2[] exhaustPositions =
		{
			new Vector2(113, 14),
			new Vector2(190, 14),
			new Vector2(152, 17),
			new Vector2(450, 14),
			new Vector2(527, 14),
			new Vector2(489, 17),
		};

		// The animated sprite that owns the exhaust anchor positions.
		Node animSprite = GetNodeOrNull("Pivot/Sprites/Sprite2D2/AnimatedSprite2D");
		if (animSprite == null)
		{
			GD.PushWarning("Ship: Could not find AnimatedSprite2D to attach exhaust trails.");
			return;
		}

		// Shared gradient: white opaque → white transparent.
		var gradient = new Gradient();
		gradient.Colors = new Color[] { new Color(1, 1, 1, 1), new Color(1, 1, 1, 0) };

		foreach (Vector2 pos in exhaustPositions)
		{
			var anchor = new Node2D();
			anchor.Position = pos;
			animSprite.AddChild(anchor);

			var line = new Line2D();
			line.AddPoint(Vector2.Zero);
			line.AddPoint(new Vector2(0, 4500));
			line.Width = 10f;
			line.DefaultColor = new Color(1, 1, 1, 1);
			line.Gradient = gradient;
			anchor.AddChild(line);
		}
	}
}
