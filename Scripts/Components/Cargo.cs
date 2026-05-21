using Godot;

/// <summary>
/// A draggable cargo object.
/// - Can be slotted into any CargoSlot (Goods, FirstAid, Droid, Ammo, Fuel).
/// - Ammo cargo can also slot into an AmmoSlot.
/// - Fuel cargo can also slot into a FuelSlot.
/// - Cargo can only be picked up by the player when it is sitting in a readied CargoSlot.
/// </summary>
public partial class Cargo : Node2D, IDraggable, IHealth
{
	private CargoType _cargoType = CargoType.Goods;

	/// <summary>The type of this cargo item. Determines which specialised slots will accept it.</summary>
	[Export]
	public CargoType CargoType
	{
		get => _cargoType;
		set
		{
			if (_cargoType == value) return;
			_cargoType = value;
			UpdateSprite();
		}
	}

	/// <summary>Half-extents of the clickable area. Adjust in the Inspector.</summary>
	[Export] public Vector2 HalfSize { get; set; } = new Vector2(32, 32);

	// IDraggable ----------------------------------------------------------------

	public Vector2 OriginalPosition { get; set; }
	public bool IsDragging { get; set; }
	public ISlottable CurrentSlot { get; set; }

	private Node _originalParent;
	private Vector2 _originalLocalPosition;
	private ISlottable _originalSlot;

	public bool IsHovered { get; private set; }

	/// <summary>True once this cargo has been deployed via a deploy charge. Further moves are free.</summary>
	public bool IsDeployed { get; private set; } = false;

	// IHealth ------------------------------------------------------------------
	public int MaxHealth { get; private set; } = 0;
	public int CurrentHealth { get; set; } = 0;

	/// <summary>The data record assigned to this cargo item on spawn.</summary>
	private CargoData _data;
	public CargoData Data
	{
		get => _data;
		set
		{
			_data = value;
			if (_data?.Ability == CargoAbility.Crew && _data.Health.HasValue)
			{
				MaxHealth = _data.Health.Value;
				CurrentHealth = _data.Health.Value;
			}
		}
	}

	private Node _tooltip;

	public override void _Ready()
	{
		OriginalPosition = GlobalPosition;
		_tooltip = GetNodeOrNull("Tooltip");
		if (_tooltip is CanvasItem tooltipItem)
			tooltipItem.Visible = false;
		UpdateSprite();
	}

	private void UpdateSprite()
	{
		if (GetNodeOrNull("Sprites/Sprite2D") is not Sprite2D sprite)
			return;

		string path = _cargoType switch
		{
			CargoType.Ammo     => "res://Assets/Characters/CargoAmmo.png",
			CargoType.Fuel     => "res://Assets/Characters/CargoFuel.png",
			CargoType.Droid    => "res://Assets/Characters/CargoDroid.png",
			CargoType.FirstAid => "res://Assets/Characters/CargoFirstAid.png",
			_                  => "res://Assets/Characters/CargoGoods.png",
		};

		sprite.Texture = GD.Load<CompressedTexture2D>(path);
	}

	public void _on_focus_pressed()
	{
		// Intentionally empty: drag is initiated exclusively via _Input so that
		// the TextureButton's "pressed" signal (which fires on mouse-UP) cannot
		// re-trigger OnDragStart after OnDragEnd has already run.
	}

	public void _on_focus_mouse_entered()
	{
		IsHovered = true;
		if (_tooltip is CanvasItem tooltipItem)
		{
			tooltipItem.Visible = true;
			if (_tooltip.GetNodeOrNull("PanelContainer/Label") is Label label)
				label.Text = Data?.Name ?? _cargoType.ToString();
		}

		if (Data != null)
			ShowFocusStats(Data);
	}

	public void _on_focus_mouse_exited()
	{
		IsHovered = false;
		if (_tooltip is CanvasItem tooltipItem)
			tooltipItem.Visible = false;

		HideFocusStats();
	}

	/// <summary>
	/// Returns true when this cargo is allowed to be dragged.
	/// Cargo may only be lifted from a readied CargoSlot (or when it has no slot at all).
	/// </summary>
	private bool IsSlotDraggable =>
		CurrentSlot is null ||
		CurrentSlot is FuelSlot ||
		CurrentSlot is AmmoSlot ||
		(CurrentSlot is CargoSlot cargoSlot && cargoSlot.IsReadied) ||
		(CurrentSlot is CrewSlot && _cargoType == CargoType.Droid);

	private bool HasCargoDeploysRemaining()
	{
		PlaySpace ps = GetTree().Root.GetNodeOrNull("PlaySpace") as PlaySpace;
		return ps == null || ps.CargoDeploysRemaining > 0;
	}

	private Tween _wiggleTween;

	private void PlayWiggle()
	{
		_wiggleTween?.Kill();
		Position = _originalLocalPosition;
		Vector2 origin = Position;
		const float dist = 4f;
		const float step = 0.05f;
		_wiggleTween = CreateTween();
		_wiggleTween.TweenProperty(this, "position", origin + new Vector2(dist, 0), step);
		_wiggleTween.TweenProperty(this, "position", origin + new Vector2(-dist, 0), step);
		_wiggleTween.TweenProperty(this, "position", origin + new Vector2(dist, 0), step);
		_wiggleTween.TweenProperty(this, "position", origin + new Vector2(-dist, 0), step);
		_wiggleTween.TweenProperty(this, "position", origin, step);
	}

	public override void _Input(InputEvent @event)
	{
		if (@event is InputEventMouseButton mouseButton &&
			mouseButton.ButtonIndex == MouseButton.Left)
		{
			if (mouseButton.Pressed && IsHovered && IsSlotDraggable)
			{
				bool isCrewSlotSwap = _cargoType == CargoType.Droid && CurrentSlot is CrewSlot;
				if (!IsDeployed && !isCrewSlotSwap && !HasCargoDeploysRemaining())
				{
					PlayWiggle();
					GetViewport().SetInputAsHandled();
				}
				else
				{
					OnDragStart();
					GetViewport().SetInputAsHandled();
				}
			}
			else if (!mouseButton.Pressed && IsDragging)
			{
				OnDragEnd(FindSlotUnderMouse());
				GetViewport().SetInputAsHandled();
			}
		}
	}

	public override void _UnhandledInput(InputEvent @event)
	{
	}

	public override void _Process(double delta)
	{
		if (IsDragging)
			OnDragUpdate(GetGlobalMousePosition());
	}

	// IDraggable implementation -------------------------------------------------

	public void OnDragStart()
	{
		if (IsDragging) return;

		if (CurrentSlot != null)
		{
			_originalSlot = CurrentSlot;
			CurrentSlot.Vacate();
			CurrentSlot = null;
		}
		else
		{
			_originalSlot = null;
		}

		// Reparent to CanvasLayer so the position is not relative to a slot node.
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
		switch (_cargoType)
		{
			case CargoType.Droid:    CrewSlot.HighlightAll();      break;
			case CargoType.FirstAid: CrewSlot.HighlightOccupied(); break;
		}
	}

	public void OnDragEnd(ISlottable targetSlot)
	{
		IsDragging = false;
		CrewSlot.ResetAll();
		AmmoSlot.ResetAll();
		FuelSlot.ResetAll();

		// Deploying cargo costs a deploy only when placing into a valid, different, non-readied slot
		// and the cargo has not already been deployed this turn.
		// Droids moving between crew slots are always free (swap).
		bool targetIsReadied = targetSlot is CargoSlot targetCs && targetCs.IsReadied;
		bool originIsCrewSlot = _originalSlot is CrewSlot;
		if (targetSlot != null && targetSlot != _originalSlot && !targetIsReadied && !IsDeployed && !originIsCrewSlot && targetSlot.CanAccept(this))
		{
			PlaySpace ps = GetTree().Root.GetNodeOrNull("PlaySpace") as PlaySpace;
			if (ps == null || !ps.TryUseCargoDeloy())
				targetSlot = null; // treat as a failed drop
			else
				IsDeployed = true;
		}

		if (targetSlot != null && targetSlot.CanAccept(this))
		{
			// Reparent back under the shared cargo container (not the slot node).
			Node container = GetCargoContainer();
			if (container != null && GetParent() != container)
			{
				GetParent().RemoveChild(this);
				container.AddChild(this);
			}
			targetSlot.Accept(this);
			GlobalPosition = targetSlot.SlotPosition + new Vector2(8, 8);
			if (targetSlot is CargoSlot)
			{
				CustomSignals.Instance.EmitSignal(CustomSignals.SignalName.CargoSlotted, this);
			}
		}
		else
		{
			// Re-register with the original slot immediately so it is treated as occupied.
			// We set the fields directly rather than calling Accept() to avoid teleporting
			// the cargo before the snap-back tween has a chance to run.
			if (_originalSlot != null)
			{
				_originalSlot.OccupiedBy = this;
				CurrentSlot = _originalSlot;
			}

			// Tween back to original parent and local position.
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

	private Node GetCargoContainer() =>
		GetTree().Root.GetNodeOrNull("PlaySpace/CanvasLayer/PlayerShip/Portrait/Pivot//Cargo");

	private void ShowFocusStats(CargoData data)
	{
		Node root = GetTree().Root;
		if (root.GetNodeOrNull("PlaySpace/CanvasLayer/GUI/Gameplay/Stats/CurrentFocusStats") is not CanvasItem panel)
			return;

		panel.Visible = true;

		if (panel.GetNodeOrNull("Tooltip/Label") is Label label)
		{
			string text = $"{data.Name}\nPrice: {data.Price}\nAbility: {data.Ability}";
			if (!string.IsNullOrEmpty(data.AbilityText))
				text += $"\n{data.AbilityText}";

			if (MaxHealth > 0)
			{
				text += $"\nHP:       {CurrentHealth}/{MaxHealth}";
				if (data.Melee.HasValue)   text += $"\nMelee:    {Stars(data.Melee.Value)}";
				if (data.Repair.HasValue)  text += $"\nRepair:   {Stars(data.Repair.Value)}";
				if (data.Medical.HasValue) text += $"\nMedical:  {Stars(data.Medical.Value)}";
				if (data.Weapons.HasValue) text += $"\nWeapons:  {Stars(data.Weapons.Value)}";
				if (data.Piloting.HasValue) text += $"\nPiloting: {Stars(data.Piloting.Value)}";
				if (data.Science.HasValue) text += $"\nScience:  {Stars(data.Science.Value)}";
				if (data.Arcane.HasValue)  text += $"\nArcane:   {Stars(data.Arcane.Value)}";
			}

			label.Text = text;
		}
	}

	private void HideFocusStats()
	{
		Node root = GetTree().Root;
		if (root.GetNodeOrNull("PlaySpace/CanvasLayer/GUI/Gameplay/Stats/CurrentFocusStats") is CanvasItem panel)
			panel.Visible = false;
	}

	private static string Stars(int value) => new string('*', Mathf.Max(value, 0));

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
