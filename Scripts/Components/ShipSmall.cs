using Godot;

/// <summary>
/// A lightweight ship node used for small/preview representations of a ship.
/// Unlike <see cref="Ship"/>, ShipSmall has no health or shields.
/// </summary>
public partial class ShipSmall : Node2D
{
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
			ApplyFactionToPdcs();
		}
	}

	// Godot lifecycle ----------------------------------------------------------

	public override void _Ready()
	{
		ApplyFactionToPdcs();
		SpawnExhaustTrails();
	}

	private void ApplyFactionToPdcs()
	{
		Node2D pdcRoot = GetNodeOrNull<Node2D>("Pivot/PDCs");
		if (pdcRoot == null) return;

		foreach (Node child in pdcRoot.GetChildren())
		{
			if (child is Pdc pdc)
				pdc.Faction = _faction;
		}
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
			new Vector2(113, 24),
			new Vector2(190, 24),
			new Vector2(152, 27),
			new Vector2(450, 24),
			new Vector2(527, 24),
			new Vector2(489, 27),
		};

		// The animated sprite that owns the exhaust anchor positions.
		Node animSprite = GetNodeOrNull("Pivot/Sprites/Sprite2D2/AnimatedSprite2D");
		if (animSprite == null)
		{
			GD.PushWarning("ShipSmall: Could not find AnimatedSprite2D to attach exhaust trails.");
			return;
		}

		foreach (Vector2 pos in exhaustPositions)
		{
			var trail = new ExhaustTrail();
			trail.Position = pos;
			animSprite.AddChild(trail);
		}
	}

	// UI callbacks -------------------------------------------------------------

	/// <summary>
	/// When the focus button is pressed on a player-faction ShipSmall,
	/// makes the full PlayerShip node visible.
	/// </summary>
	public void _on_focus_pressed()
	{
		if (_faction != ShipFaction.Player) return;

		if (GetTree().Root.GetNodeOrNull<Node2D>("PlaySpace/CanvasLayer/PlayerShip") is Node2D playerShip)
			playerShip.Visible = !playerShip.Visible;
	}
}
