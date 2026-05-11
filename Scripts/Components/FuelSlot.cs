using Godot;

/// <summary>
/// A slot that only accepts Cargo draggables whose CargoType is <see cref="CargoType.Fuel"/>.
/// </summary>
public partial class FuelSlot : Node2D, ISlottable
{
	/// <summary>Half-extents of the slot's drop area. Adjust in the Inspector.</summary>
	[Export] public Vector2 HalfSize { get; set; } = new Vector2(32, 32);

	// ISlottable ----------------------------------------------------------------

	public Vector2 SlotPosition => GlobalPosition;

	public IDraggable OccupiedBy { get; set; }

	public bool IsOccupied => OccupiedBy != null;

	/// <summary>
	/// Only accepts Cargo draggables of type Fuel when the slot is free.
	/// </summary>
	public bool CanAccept(IDraggable draggable)
	{
		return !IsOccupied && draggable is Cargo cargo && cargo.CargoType == CargoType.Fuel;
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
