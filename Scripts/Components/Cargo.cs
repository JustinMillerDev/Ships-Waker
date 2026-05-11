using Godot;

/// <summary>
/// A draggable cargo object.
/// - Can be slotted into any CargoSlot (Goods, FirstAid, Droid, Ammo, Fuel).
/// - Ammo cargo can also slot into an AmmoSlot.
/// - Fuel cargo can also slot into a FuelSlot.
/// - Cargo can only be picked up by the player when it is sitting in a readied CargoSlot.
/// </summary>
public partial class Cargo : Node2D, IDraggable
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

	public bool IsHovered { get; private set; }

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
		OnDragStart();
	}

	public void _on_focus_mouse_entered()
	{
		IsHovered = true;
		if (_tooltip is CanvasItem tooltipItem)
			tooltipItem.Visible = true;
	}

	public void _on_focus_mouse_exited()
	{
		IsHovered = false;
		if (_tooltip is CanvasItem tooltipItem)
			tooltipItem.Visible = false;
	}

	/// <summary>
	/// Returns true when this cargo is allowed to be dragged.
	/// Cargo may only be lifted from a readied CargoSlot (or when it has no slot at all).
	/// </summary>
	private bool IsDraggable =>
		CurrentSlot is null ||
		(CurrentSlot is CargoSlot cargoSlot && cargoSlot.IsReadied);

	public override void _Input(InputEvent @event)
	{
		if (@event is InputEventMouseButton mouseButton &&
			mouseButton.ButtonIndex == MouseButton.Left)
		{
			if (mouseButton.Pressed && (IsHovered || IsMouseOver()) && IsDraggable)
			{
				OnDragStart();
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

		// Reparent to the scene root so the position is not relative to a slot node.
		Node root = GetTree().Root;
		if (GetParent() != root)
		{
			_originalParent = GetParent();
			_originalLocalPosition = Position;
			Vector2 globalPos = GlobalPosition;
			GetParent().RemoveChild(this);
			root.AddChild(this);
			GlobalPosition = globalPos;
		}

		IsDragging = true;
	}

	public void OnDragEnd(ISlottable targetSlot)
	{
		IsDragging = false;

		// Deploying cargo into any slot costs a cargo deploy for this turn.
		if (targetSlot != null)
		{
			PlaySpace ps = GetTree().Root.GetNodeOrNull("PlaySpace") as PlaySpace;
			if (ps == null || !ps.TryUseCargoDeloy())
				targetSlot = null; // treat as a failed drop
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
		}
		else
		{
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
		GetTree().Root.GetNodeOrNull("PlaySpace/PlayerShip/Pivot/Cargo");

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
