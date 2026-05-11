using Godot;

/// <summary>
/// Implement on any Node2D that can be clicked and dragged around the screen.
/// When the mouse is released over an ISlottable, the node snaps to the slot's position.
/// Otherwise, the node returns to its original position.
/// </summary>
public interface IDraggable
{
	/// <summary>The position the node returns to when not slotted.</summary>
	Vector2 OriginalPosition { get; set; }

	/// <summary>Whether the node is currently being dragged.</summary>
	bool IsDragging { get; set; }

	/// <summary>The slot this node is currently occupying, if any.</summary>
	ISlottable CurrentSlot { get; set; }

	/// <summary>Called when the user begins dragging this node.</summary>
	void OnDragStart();

	/// <summary>
	/// Called when the user releases the drag.
	/// If <paramref name="targetSlot"/> is non-null, the node moves to that slot.
	/// Otherwise, it returns to <see cref="OriginalPosition"/>.
	/// </summary>
	void OnDragEnd(ISlottable targetSlot);

	/// <summary>Called every frame while the node is being dragged.</summary>
	void OnDragUpdate(Vector2 mousePosition);
}
