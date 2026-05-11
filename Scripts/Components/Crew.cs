using Godot;

/// <summary>
/// A draggable crew member node. Can only be slotted into a CrewSlot.
/// </summary>
public partial class Crew : Node2D, IDraggable, IOrientation
{
	public Vector2 OriginalPosition { get; set; }
	public bool IsDragging { get; set; }
	public ISlottable CurrentSlot { get; set; }

	// IOrientation --------------------------------------------------------------

	/// <summary>The direction this crew member is facing.</summary>
	[Export] public CardinalDirection Facing { get; set; } = CardinalDirection.Right;

	private CrewState _state = CrewState.HyperSleep;
	/// <summary>Current activity state of this crew member.</summary>
	public CrewState State
	{
		get => _state;
		set
		{
			if (_state == value) return;
			_state = value;
			EmitSignal(SignalName.StateChanged, (int)value);
		}
	}

	[Signal] public delegate void StateChangedEventHandler(int newState);

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

	public void _on_focus_pressed()
	{
		OnDragStart();
	}

	public void _on_focus_mouse_entered()
	{
		if (_tooltip is CanvasItem tooltipItem)
			tooltipItem.Visible = true;
	}

	public void _on_focus_mouse_exited()
	{
		if (_tooltip is CanvasItem tooltipItem)
			tooltipItem.Visible = false;
	}

	public override void _Input(InputEvent @event)
	{
		if (@event is InputEventMouseButton mouseButton)
		{
			if (mouseButton.ButtonIndex == MouseButton.Left)
			{
				if (mouseButton.Pressed && IsMouseOver())
				{
					OnDragStart();
				}
				else if (!mouseButton.Pressed && IsDragging)
				{
					ISlottable targetSlot = FindSlotUnderMouse();
					OnDragEnd(targetSlot);
				}
			}
		}
	}

	public override void _Process(double delta)
	{
		if (IsDragging)
		{
			OnDragUpdate(GetGlobalMousePosition());
		}
	}

	public void OnDragStart()
	{
		// Vacate the current slot before lifting the piece
		if (CurrentSlot != null)
		{
			CurrentSlot.Vacate();
			CurrentSlot = null;
		}

		IsDragging = true;
	}

	public void OnDragEnd(ISlottable targetSlot)
	{
		IsDragging = false;

		if (targetSlot != null && targetSlot.CanAccept(this))
		{
			targetSlot.Accept(this);
		}
		else
		{
			// Return to original position if no valid slot
			GlobalPosition = OriginalPosition;
		}
	}

	public void OnDragUpdate(Vector2 mousePosition)
	{
		GlobalPosition = mousePosition;
	}

	// ---------------------------------------------------------------------------
	// Helpers
	// ---------------------------------------------------------------------------

	private bool IsMouseOver()
	{
		// Assumes the node has a CollisionShape2D child via an Area2D or similar.
		// For a simple rect-based check we use the node's global position and a
		// configurable size exported below.
		Rect2 bounds = new Rect2(GlobalPosition - HalfSize, HalfSize * 2);
		return bounds.HasPoint(GetGlobalMousePosition());
	}

	/// <summary>Half-extents of the clickable area. Adjust in the Inspector.</summary>
	[Export] public Vector2 HalfSize { get; set; } = new Vector2(32, 32);

	private ISlottable FindSlotUnderMouse()
	{
		Vector2 mouse = GetGlobalMousePosition();
		// Walk siblings/cousins in the tree looking for an ISlottable whose
		// bounds contain the mouse cursor.
		return FindSlotIn(GetTree().Root, mouse);
	}

	private static ISlottable FindSlotIn(Node root, Vector2 point)
	{
		foreach (Node child in root.GetChildren())
		{
			if (child is ISlottable slottable && slottable.ContainsPoint(point))
				return slottable;

			ISlottable found = FindSlotIn(child, point);
			if (found != null)
				return found;
		}

		return null;
	}
}
