using Godot;

/// <summary>
/// A draggable ship component. Can only be slotted into a ShipComponentSlot
/// whose SlotType matches this component's ComponentType.
/// </summary>
public partial class ShipComponent : Node2D, IDraggable
{
	/// <summary>The type of this component. Must match the target slot's SlotType.</summary>
	[Export] public ShipComponentType ComponentType { get; set; } = ShipComponentType.Internal;

	/// <summary>The orientation this component faces.</summary>
	[Export] public CardinalDirection Facing { get; set; } = CardinalDirection.Down;

	/// <summary>Half-extents of the clickable area. Adjust in the Inspector.</summary>
	[Export] public Vector2 HalfSize { get; set; } = new Vector2(32, 32);

	// IDraggable ----------------------------------------------------------------

	public Vector2 OriginalPosition { get; set; }
	public bool IsDragging { get; set; }
	public ISlottable CurrentSlot { get; set; }

	public bool IsHovered { get; private set; }

	private Node _originalParent;
	private Vector2 _originalLocalPosition;
	private Node _tooltip;

	public override void _Ready()
	{
		OriginalPosition = GlobalPosition;
		_tooltip = GetNodeOrNull("Tooltip");
		if (_tooltip is CanvasItem tooltipItem)
			tooltipItem.Visible = false;
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
	}

	public void _on_focus_mouse_exited()
	{
		IsHovered = false;
		if (_tooltip is CanvasItem tooltipItem)
			tooltipItem.Visible = false;
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
