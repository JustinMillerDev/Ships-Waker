using Godot;

/// <summary>
/// A HyperSleep casket slot. Only Crew members can be slotted into a Casket.
/// When a Crew member is accepted, their state is set to HyperSleep.
/// When a Crew member is vacated, their state is set to Idle.
/// </summary>
public partial class Casket : Node2D, ISlottable, IOrientation
{
	/// <summary>Half-extents of the slot's drop area. Adjust in the Inspector.</summary>
	[Export] public Vector2 HalfSize { get; set; } = new Vector2(32, 32);

	// IOrientation --------------------------------------------------------------

	/// <summary>The direction this casket faces.</summary>
	[Export] public CardinalDirection Facing { get; set; } = CardinalDirection.Left;

	// ISlottable ----------------------------------------------------------------

	public Vector2 SlotPosition => GlobalPosition;

	/// <summary>The Crew member currently inside this casket, or <c>null</c> if empty.</summary>
	public IDraggable OccupiedBy { get; set; }

	public bool IsOccupied => OccupiedBy != null;

	/// <summary>Accepts only Crew draggables into an empty casket.</summary>
	public bool CanAccept(IDraggable draggable)
	{
		return !IsOccupied && draggable is Crew;
	}

	public void Accept(IDraggable draggable)
	{
		if (!CanAccept(draggable))
			return;

		OccupiedBy = draggable;
		draggable.CurrentSlot = this;

		// Snap the crew member into the casket
		if (draggable is Node2D node2D)
			node2D.GlobalPosition = SlotPosition;

		// Put the crew member into HyperSleep
		if (draggable is Crew crew)
			crew.State = CrewState.HyperSleep;
	}

	public void Vacate()
	{
		if (OccupiedBy is Crew crew)
			crew.State = CrewState.Idle;

		if (OccupiedBy != null)
			OccupiedBy.CurrentSlot = null;

		OccupiedBy = null;
	}

	public bool ContainsPoint(Vector2 point)
	{
		Rect2 bounds = new Rect2(GlobalPosition - HalfSize, HalfSize * 2);
		return bounds.HasPoint(point);
	}
}
