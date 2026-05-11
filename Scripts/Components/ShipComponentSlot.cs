using Godot;

/// <summary>
/// A slot that accepts ShipComponent draggables whose type matches this slot's type.
/// </summary>
public partial class ShipComponentSlot : Node2D, ISlottable
{
	/// <summary>Half-extents of the drop area. Adjust in the Inspector.</summary>
	[Export] public Vector2 HalfSize { get; set; } = new Vector2(32, 32);

	/// <summary>The category of ship component this slot accepts.</summary>
	[Export] public ShipComponentType SlotType { get; set; } = ShipComponentType.Internal;

	/// <summary>The orientation this component slot faces.</summary>
	[Export] public CardinalDirection Facing { get; set; } = CardinalDirection.Down;

	// ISlottable ----------------------------------------------------------------

	public Vector2 SlotPosition => GlobalPosition;

	public IDraggable OccupiedBy { get; set; }

	public bool IsOccupied => OccupiedBy != null;

	/// <summary>
	/// Accepts a ShipComponent whose ComponentType matches this slot's SlotType,
	/// and only when the slot is empty.
	/// </summary>
	public bool CanAccept(IDraggable draggable)
	{
		return !IsOccupied &&
			   draggable is ShipComponent component &&
			   component.ComponentType == SlotType;
	}

	public void Accept(IDraggable draggable)
	{
		if (!CanAccept(draggable))
			return;

		OccupiedBy = draggable;
		draggable.CurrentSlot = this;

		if (draggable is Node2D node2D)
			node2D.GlobalPosition = SlotPosition;
	}

	public void Vacate()
	{
		if (OccupiedBy != null)
		{
			OccupiedBy.CurrentSlot = null;
			OccupiedBy = null;
		}
	}

	public bool ContainsPoint(Vector2 point)
	{
		Rect2 bounds = new Rect2(GlobalPosition - HalfSize, HalfSize * 2);
		return bounds.HasPoint(point);
	}
}
