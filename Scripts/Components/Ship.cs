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
	private static CompressedTexture2D _enemyPortraitTexture;

	// IHealth ------------------------------------------------------------------

	public int MaxHealth { get; private set; } = 50;
	public int CurrentHealth { get; set; } = 50;

	// Shields ------------------------------------------------------------------

	public int MaxShields { get; private set; } = 20;
	public int CurrentShields { get; set; } = 20;

	// Evasions -----------------------------------------------------------------

	public int MaxEvasions { get; private set; } = 0;
	public int CurrentEvasions { get; set; } = 0;

	// PDC ----------------------------------------------------------------------

	public int MaxPDC { get; private set; } = 6;
	public int CurrentPDC { get; set; } = 6;

	// Aliases matching existing lower-camel naming in some gameplay scripts.
	public int maxEvasions => MaxEvasions;
	public int currentEvasions
	{
		get => CurrentEvasions;
		set => CurrentEvasions = value;
	}

	public int maxPDC => MaxPDC;
	public int currentPDC
	{
		get => CurrentPDC;
		set => CurrentPDC = value;
	}

	public void ResetCurrentPDC()
	{
		CurrentPDC = MaxPDC;
	}

	public bool TryConsumeCurrentPDC()
	{
		if (CurrentPDC <= 0)
			return false;

		CurrentPDC--;
		return true;
	}

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
		UpdateMaxPDCFromScene();
		ResetCurrentPDC();

		if (_faction == ShipFaction.Enemy)
		{
			_enemyPortraitTexture ??= GD.Load<CompressedTexture2D>("res://Assets/GUI/EnemyShipBox.png");
			if (GetNodeOrNull<Sprite2D>("Portrait") is Sprite2D portrait && _enemyPortraitTexture != null)
				portrait.Texture = _enemyPortraitTexture;
		}

		if (_faction == ShipFaction.Enemy)
			SpawnExhaustTrails();
	}

	private void UpdateMaxPDCFromScene()
	{
		if (GetNodeOrNull<Node2D>("Portrait/Pivot/PDCs") is not Node2D pdcRoot)
			return;

		int pdcCount = 0;
		foreach (Node child in pdcRoot.GetChildren())
		{
			if (child is Pdc)
				pdcCount++;
		}

		if (pdcCount > 0)
			MaxPDC = pdcCount;
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
			GD.PushWarning("Ship: Could not find AnimatedSprite2D to attach exhaust trails.");
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

	private void _on_close_button_pressed()
	{
		Visible = false;
	}
}
