using Godot;

/// <summary>
/// A slot that only accepts Crew draggables.
/// </summary>
public partial class CrewSlot : Node2D, ISlottable, IOrientation
{
	/// <summary>Half-extents of the slot's drop area. Adjust in the Inspector.</summary>
	[Export] public Vector2 HalfSize { get; set; } = new Vector2(32, 32);

	/// <summary>The direction crew will face when slotted here.</summary>
	[Export] public CardinalDirection Facing { get; set; } = CardinalDirection.Down;

	private static CompressedTexture2D _topDownTexture;

	// ISlottable ----------------------------------------------------------------

	public Vector2 SlotPosition => GlobalPosition;

	public IDraggable OccupiedBy { get; set; }

	public bool IsOccupied => OccupiedBy != null;

	/// <summary>
	/// Only accepts draggables that are Crew nodes and only when the slot is free.
	/// </summary>
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

		// Snap the crew member to this slot's position
		if (draggable is Node2D node2D)
			node2D.GlobalPosition = SlotPosition;

		// Match crew orientation and update texture
		if (draggable is Crew crew)
		{
			crew.Facing = Facing;

			_topDownTexture ??= GD.Load<CompressedTexture2D>("res://Assets/Characters/CrewTopDown.png");

			if (crew.GetNodeOrNull("Sprites/Sprite2D") is Sprite2D sprite)
			{
				sprite.Texture = _topDownTexture;
				sprite.RotationDegrees = Facing switch
				{
					CardinalDirection.Left  => -90f,
					CardinalDirection.Right => 90f,
					CardinalDirection.Up    => 0f,
					_                       => 180f,
				};
			}
		}
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

