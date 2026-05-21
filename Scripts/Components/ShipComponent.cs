using Godot;
using Godot.Collections;

/// <summary>
/// A draggable ship component. Can only be slotted into a ShipComponentSlot
/// whose SlotType matches this component's ComponentType.
/// </summary>
public partial class ShipComponent : Node2D, IDraggable, IHealth
{
	/// <summary>The type of this component. Must match the target slot's SlotType.</summary>
	[Export] public ShipComponentType ComponentType { get; set; } = ShipComponentType.Internal;

	/// <summary>The orientation this component faces.</summary>
	[Export] public CardinalDirection Facing { get; set; } = CardinalDirection.Down;

	/// <summary>Half-extents of the clickable area. Adjust in the Inspector.</summary>
	[Export] public Vector2 HalfSize { get; set; } = new Vector2(32, 32);

	/// <summary>Cargo slots belonging to this component's slot, used for ammo tracking.</summary>
	private Array<CargoSlot> _cargoSlots = new();
	[Export]
	public Array<CargoSlot> CargoSlots
	{
		get => _cargoSlots;
		set
		{
			_cargoSlots = value;
			foreach (CargoSlot slot in _cargoSlots)
				slot.SlotChanged += OnCargoSlotChanged;
		}
	}

	[Signal] public delegate void AmmoChangedEventHandler();

	private void OnCargoSlotChanged() => EmitSignal(SignalName.AmmoChanged);

	/// <summary>Turns remaining before this component can act again.</summary>
	public int CurrentCooldown { get; set; } = 6;

	// IHealth ------------------------------------------------------------------
	public int MaxHealth { get; private set; } = 10;
	public int CurrentHealth { get; set; } = 10;

	// IDraggable ----------------------------------------------------------------

	public Vector2 OriginalPosition { get; set; }
	public bool IsDragging { get; set; }
	public ISlottable CurrentSlot { get; set; }

	public bool IsHovered { get; private set; }

	/// <summary>
	/// The active ability tier determined by the current CrewSkill vs the tier
	/// requirements defined in Data. Returns null when Data is unset.
	/// </summary>
	public ComponentAbility? ActiveAbility
	{
		get
		{
			if (Data == null) return null;
			int skill = CrewSkill;
			if (skill <= Data.Tier0Req) return Data.Tier0Ability;
			if (skill <= Data.Tier1Req) return Data.Tier1Ability;
			if (skill <= Data.Tier2Req) return Data.Tier2Ability;
			if (!Data.Tier4Req.HasValue || skill <= Data.Tier3Req) return Data.Tier3Ability;
			return Data.Tier4Ability;
		}
	}

	/// <summary>
	/// Sum of the relevant stat across all crew stationed in this component's slot.
	/// </summary>
	public int CrewSkill
	{
		get
		{
			if (Data == null || CurrentSlot is not ShipComponentSlot componentSlot)
				return 0;

			int total = 0;
			foreach (CrewSlot crewSlot in componentSlot.CrewSlots)
			{
				if (crewSlot.OccupiedBy is Crew crew && crew.Data != null)
					total += crew.Data.GetStat(Data.RequiredSkill);
			}
			return total;
		}
	}

	/// <summary>
	/// Total ammo from all Ammo-ability cargo slotted in this component's CargoSlots.
	/// </summary>
	public int Ammo
	{
		get
		{
			int total = 0;
			foreach (CargoSlot cargoSlot in _cargoSlots)
			{
				if (cargoSlot.OccupiedBy is Cargo cargo && cargo.Data?.Ability == CargoAbility.Ammo)
					total += cargo.Data.AbilityValue ?? 0;
			}
			return total;
		}
	}

	/// <summary>The data record assigned to this component.</summary>
	private ComponentData _data;
	public ComponentData Data
	{
		get => _data;
		set
		{
			_data = value;
			if (_data != null)
			{
				MaxHealth = _data.Health;
				CurrentHealth = _data.Health;
				CurrentStrength = _data.Strength ?? 0;

				if (_data.Image != null)
				{
					var texture = GD.Load<Texture2D>("res://Assets/Characters/" + _data.Image + ".png");
					if (GetNodeOrNull("Sprites/Sprite2D") is Sprite2D sprite)
						sprite.Texture = texture;
				}
			}
		}
	}

	/// <summary>Runtime strength, initialised from Data and updated by ability tier changes.</summary>
	public int CurrentStrength { get; private set; }

	private ComponentAbility? _lastKnownAbility;

	private Node _originalParent;
	private Vector2 _originalLocalPosition;
	private Node _tooltip;

	public override void _Ready()
	{
		OriginalPosition = GlobalPosition;
		_tooltip = GetNodeOrNull("Tooltip");
		if (_tooltip is CanvasItem tooltipItem)
			tooltipItem.Visible = false;

		if (GetTree().Root.GetNodeOrNull("PlaySpace") is PlaySpace ps)
			ps.TurnEnded += OnEndOfTurn;

		if (CustomSignals.Instance != null)
			CustomSignals.Instance.CrewSlotted += OnCrewSlotted;
	}

	public override void _ExitTree()
	{
		if (GetTree().Root.GetNodeOrNull("PlaySpace") is PlaySpace ps)
			ps.TurnEnded -= OnEndOfTurn;

		if (CustomSignals.Instance != null)
			CustomSignals.Instance.CrewSlotted -= OnCrewSlotted;
	}

	private void OnCrewSlotted(Crew _) => RefreshEngineStrength();

	/// <summary>
	/// If this is an Engines component and the active ability tier has changed,
	/// updates CurrentStrength to match the new tier.
	/// </summary>
	private void RefreshEngineStrength()
	{
		if (ComponentType != ShipComponentType.Engines) return;

		ComponentAbility? ability = ActiveAbility;
		if (ability == _lastKnownAbility) return;
		_lastKnownAbility = ability;

		CurrentStrength = ability switch
		{
			ComponentAbility.NoSpeed     => 0,
			ComponentAbility.LowSpeed    => 1,
			ComponentAbility.MediumSpeed => 2,
			ComponentAbility.HighSpeed   => 3,
			_                            => CurrentStrength,
		};

		if (CustomSignals.Instance != null)
			CustomSignals.Instance.EmitSignal(CustomSignals.SignalName.EngineStrengthChanged, CurrentStrength);
	}

	/// <summary>Called at the end of every turn. Triggers the active ability.</summary>
	private void OnEndOfTurn(int turn)
	{
		if (ActiveAbility is not ComponentAbility ability) return;
		ComponentAbilityRegistry.Instance.EndOfTurnExecute(ability, this);
	}

	// ---------------------------------------------------------------------------
	// Focus / Hover
	// ---------------------------------------------------------------------------

	private bool CanDrag()
	{
		if (ComponentType != ShipComponentType.External) return true;
		PlaySpace ps = GetTree().Root.GetNodeOrNull("PlaySpace") as PlaySpace;
		return ps == null || !ps.IsInCombat;
	}

	public void _on_focus_pressed()
	{
		if (!CanDrag()) return;
		OnDragStart();
	}

	public void _on_focus_mouse_entered()
	{
		IsHovered = true;
		if (_tooltip is CanvasItem tooltipItem)
			tooltipItem.Visible = true;
		if (Data != null)
			ShowFocusStats();
	}

	public void _on_focus_mouse_exited()
	{
		IsHovered = false;
		if (_tooltip is CanvasItem tooltipItem)
			tooltipItem.Visible = false;
		HideFocusStats();
	}

	public override void _Input(InputEvent @event)
	{
		if (@event is InputEventMouseButton mouseButton &&
			mouseButton.ButtonIndex == MouseButton.Left)
		{
			if (mouseButton.Pressed && (IsHovered || IsMouseOver()))
			{
				if (CanDrag()) OnDragStart();
			}
			else if (!mouseButton.Pressed && IsDragging)
			{
				OnDragEnd(FindSlotUnderMouse());
			}
		}
	}

	public override void _Process(double delta)
	{
		if (IsDragging)
			OnDragUpdate(GetGlobalMousePosition());
	}

	// IDraggable implementation -------------------------------------------------

	public void OnDragStart()
	{
		if (CurrentSlot != null)
		{
			CurrentSlot.Vacate();
			CurrentSlot = null;
		}

		Node root = GetTree().Root;
		Node dragParent = root.GetNodeOrNull("PlaySpace/CanvasLayer") ?? root;
		if (GetParent() != dragParent)
		{
			_originalParent = GetParent();
			_originalLocalPosition = Position;
			Vector2 globalPos = GlobalPosition;
			GetParent().RemoveChild(this);
			dragParent.AddChild(this);
			GlobalPosition = globalPos;
		}

		IsDragging = true;
	}

	public void OnDragEnd(ISlottable targetSlot)
	{
		IsDragging = false;

		if (targetSlot != null && targetSlot.CanAccept(this))
		{
			if (targetSlot is Node slotNode && GetParent() != slotNode)
			{
				GetParent().RemoveChild(this);
				slotNode.AddChild(this);
			}
			targetSlot.Accept(this);
			Position = new Vector2(8, 8);
		}
		else
		{
			if (_originalParent != null)
			{
				Node originalParent = _originalParent;
				Vector2 originalLocalPos = _originalLocalPosition;
				Vector2 currentGlobal = GlobalPosition;

				GetParent().RemoveChild(this);
				originalParent.AddChild(this);
				GlobalPosition = currentGlobal;

				Tween tween = CreateTween();
				tween.TweenProperty(this, "position", originalLocalPos, 0.125)
					 .SetTrans(Tween.TransitionType.Sine)
					 .SetEase(Tween.EaseType.Out);
			}
			else
			{
				Tween tween = CreateTween();
				tween.TweenProperty(this, "global_position", OriginalPosition, 0.125)
					 .SetTrans(Tween.TransitionType.Sine)
					 .SetEase(Tween.EaseType.Out);
			}
		}
	}

	public void OnDragUpdate(Vector2 mousePosition)
	{
		GlobalPosition = mousePosition;
	}

	// Helpers -------------------------------------------------------------------

	public void ShowFocusStats()
	{
		Node root = GetTree().Root;
		if (root.GetNodeOrNull("PlaySpace/CanvasLayer/GUI/Gameplay/Stats/CurrentFocusStats") is not CanvasItem panel)
			return;

		panel.Visible = true;

		if (panel.GetNodeOrNull("Tooltip/Label") is Label label)
		{
			string text = $"{Data.Name}\nHP: {CurrentHealth}/{MaxHealth}";
			if (Data.Cooldown.HasValue)
				text += $"\nCooldown: {CurrentCooldown}/{Data.Cooldown.Value}";
			if (ComponentType == ShipComponentType.Engines)
				text += $"\nStrength: {CurrentStrength}";
			text += $"\nRequired Skill: {Data.RequiredSkill}\nCurrent Skill: {new string('*', CrewSkill)}";
			text += $"\nTier 1: {new string('*', Data.Tier1Req)}\n{Data.Tier1Text}";
			text += $"\nTier 2: {new string('*', Data.Tier2Req)}\n{Data.Tier2Text}";
			text += $"\nTier 3: {new string('*', Data.Tier3Req)}\n{Data.Tier3Text}";
			if (Data.Tier4Req.HasValue)
				text += $"\nTier 4: {new string('*', Data.Tier4Req.Value)}\n{Data.Tier4Text}";
			label.Text = text;
		}
	}

	private void HideFocusStats()
	{
		Node root = GetTree().Root;
		if (root.GetNodeOrNull("PlaySpace/CanvasLayer/GUI/Gameplay/Stats/CurrentFocusStats") is CanvasItem panel)
			panel.Visible = false;
	}

	private bool IsMouseOver()
	{
		Rect2 bounds = new Rect2(GlobalPosition - HalfSize, HalfSize * 2);
		return bounds.HasPoint(GetGlobalMousePosition());
	}

	private ISlottable FindSlotUnderMouse()
	{
		Vector2 mouse = GetGlobalMousePosition();
		ISlottable best = null;
		float bestDist = float.MaxValue;
		CollectClosestSlot(GetTree().Root, mouse, ref best, ref bestDist);
		return best;
	}

	private void CollectClosestSlot(Node node, Vector2 point, ref ISlottable best, ref float bestDist)
	{
		foreach (Node child in node.GetChildren())
		{
			if (child is ISlottable slottable && child != this && slottable.ContainsPoint(point))
			{
				float dist = child is Node2D n2d ? n2d.GlobalPosition.DistanceTo(point) : 0f;
				if (dist < bestDist)
				{
					bestDist = dist;
					best = slottable;
				}
			}
			CollectClosestSlot(child, point, ref best, ref bestDist);
		}
	}
}
