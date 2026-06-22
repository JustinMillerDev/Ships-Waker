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
		ApplyEnemySprite();
	}

	private void ApplyEnemySprite()
	{
		if (_faction != ShipFaction.Enemy) return;

		var sprite = GetNodeOrNull<Sprite2D>("Pivot/Sprites/Sprite2D2");
		if (sprite == null)
		{
			GD.PushWarning("ShipSmall: Could not find Pivot/Sprites/Sprite2D2 to apply enemy texture.");
			return;
		}

		sprite.Texture = GD.Load<Texture2D>("res://Assets/Backgrounds/SpaceShipGame_UI_Ship.png");
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
	/// When the focus button is pressed, show the corresponding full-size ship
	/// in CanvasLayer and hide the other one.
	/// </summary>
	public void _on_focus_pressed()
	{
		Node2D playerShip = GetTree().Root.GetNodeOrNull<Node2D>("PlaySpace/CanvasLayer/PlayerShip");
		Node2D enemyShip = GetTree().Root.GetNodeOrNull<Node2D>("PlaySpace/CanvasLayer/EnemyShip");

		if (playerShip == null && enemyShip == null) return;

		bool showPlayer = _faction == ShipFaction.Player;
		Node2D selectedShip = showPlayer ? playerShip : enemyShip;
		Node2D otherShip = showPlayer ? enemyShip : playerShip;

		// Clicking the currently visible ship hides it.
		if (selectedShip != null && selectedShip.Visible)
		{
			selectedShip.Visible = false;
			return;
		}

		if (selectedShip != null)
			selectedShip.Visible = true;

		if (otherShip != null)
			otherShip.Visible = false;
	}

	public void _on_focus_mouse_entered()
	{
		ShowFocusStats();
	}

	public void _on_focus_mouse_exited()
	{
		HideFocusStats();
	}

	private void ShowFocusStats()
	{
		Node root = GetTree().Root;
		if (root.GetNodeOrNull("PlaySpace/CanvasLayer/GUI/Gameplay/Stats/CurrentFocusStats") is not CanvasItem panel)
			return;

		panel.Visible = true;

		if (panel.GetNodeOrNull("Tooltip/Label") is not Label label)
			return;

		Ship ship = GetLinkedShip();
		if (ship == null)
		{
			label.Text = "Ship\nHP: 0/0\nEvasions: 0\nPDCs: 0";
			return;
		}

		label.Text =
			$"{ship.Name}\n" +
			$"HP: {ship.CurrentHealth}/{ship.MaxHealth}\n" +
			$"Evasions: {ship.CurrentEvasions}\n" +
			$"PDCs: {ship.CurrentPDC}/{ship.MaxPDC}";
	}

	private void HideFocusStats()
	{
		Node root = GetTree().Root;
		if (root.GetNodeOrNull("PlaySpace/CanvasLayer/GUI/Gameplay/Stats/CurrentFocusStats") is CanvasItem panel)
			panel.Visible = false;
	}

	private Ship GetLinkedShip()
	{
		string path = _faction == ShipFaction.Player
			? "PlaySpace/CanvasLayer/PlayerShip"
			: "PlaySpace/CanvasLayer/EnemyShip";

		return GetTree().Root.GetNodeOrNull<Ship>(path);
	}
}
