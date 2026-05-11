using Godot;

/// <summary>
/// Implement on any Node2D that acts as a slot another node can be dragged into.
/// Different slot types (e.g. CrewSlot, CargoSlot, BoarderSlot) implement this interface
/// to define what kinds of draggables they accept.
/// </summary>
public interface ISlottable
{
	/// <summary>The world-space position a draggable snaps to when slotted here.</summary>
	Vector2 SlotPosition { get; }

	/// <summary>
	/// The draggable node currently occupying this slot, or <c>null</c> if the slot is empty.
	/// </summary>
	IDraggable OccupiedBy { get; set; }

	/// <summary>Whether this slot is available to accept a draggable.</summary>
	bool IsOccupied { get; }

	/// <summary>
	/// Returns true if this slot is willing to accept the given draggable.
	/// Use this to enforce slot-type rules (e.g. only crew cards in a CrewSlot).
	/// </summary>
	bool CanAccept(IDraggable draggable);

	/// <summary>Slots the given draggable into this slot.</summary>
	void Accept(IDraggable draggable);

	/// <summary>Removes the currently slotted draggable, freeing the slot.</summary>
	void Vacate();

	/// <summary>
	/// Returns true if the given world-space point lies within this slot's drop area.
	/// Used by draggables to detect which slot the mouse was released over.
	/// </summary>
	bool ContainsPoint(Vector2 point);
}
