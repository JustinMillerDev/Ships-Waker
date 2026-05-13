using Godot;
using System.Collections.Generic;

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
	private static CompressedTexture2D _droidTopDownTexture;

	// Frame indices (0-based for Sprite2D.Frame)
	private const int FrameBase      = 0;
	private const int FrameHighlight  = 1;
	private const int FrameSelected   = 2;

	// Global registry of all live CrewSlots and drag state
	private static readonly List<CrewSlot> _allSlots = new();
	private static bool _dragActive = false;
	private static bool _occupiedOnly = false;
	private static CrewSlot _hoveredSlot = null;

	public override void _Ready()
	{
		_allSlots.Add(this);
	}

	public override void _ExitTree()
	{
		_allSlots.Remove(this);
		if (_hoveredSlot == this) _hoveredSlot = null;
	}

	public override void _Process(double delta)
	{
		if (!_dragActive) return;
		// Only run the hover logic once per frame, from the first registered slot.
		if (_allSlots.Count == 0 || _allSlots[0] != this) return;

		// Find the single closest slot under the mouse.
		// In occupied-only mode (first aid), only slots occupied by a Crew (not a droid) qualify.
		Vector2 mouse = GetGlobalMousePosition();
		CrewSlot closest = null;
		float bestDist = float.MaxValue;
		foreach (var slot in _allSlots)
		{
			if (!slot.ContainsPoint(mouse)) continue;
			if (_occupiedOnly && !(slot.OccupiedBy is Crew)) continue;
			float dist = slot.GlobalPosition.DistanceTo(mouse);
			if (dist < bestDist)
			{
				bestDist = dist;
				closest = slot;
			}
		}

		if (closest != _hoveredSlot)
		{
			if (_hoveredSlot != null)
				_hoveredSlot.SetFrame(FrameHighlight);
			_hoveredSlot = closest;
			if (_hoveredSlot != null)
				_hoveredSlot.SetFrame(FrameSelected);
		}
	}

	private void SetFrame(int frame)
	{
		if (GetNodeOrNull("Sprites/Sprite2D") is Sprite2D sprite)
			sprite.Frame = frame;
	}

	/// <summary>Puts all slots into highlight mode when a compatible drag begins.</summary>
	public static void HighlightAll()
	{
		_dragActive = true;
		_occupiedOnly = false;
		_hoveredSlot = null;
		foreach (var slot in _allSlots)
			slot.SetFrame(FrameHighlight);
	}

	/// <summary>Highlights only occupied slots (e.g. for first aid targeting crew).</summary>
	public static void HighlightOccupied()
	{
		_dragActive = true;
		_occupiedOnly = true;
		_hoveredSlot = null;
		foreach (var slot in _allSlots)
			slot.SetFrame(slot.IsOccupied ? FrameHighlight : FrameBase);
	}

	/// <summary>Returns all slots to their base frame when a drag ends.</summary>
	public static void ResetAll()
	{
		_dragActive = false;
		_occupiedOnly = false;
		_hoveredSlot = null;
		foreach (var slot in _allSlots)
			slot.SetFrame(FrameBase);
	}

	// ISlottable ----------------------------------------------------------------

	public Vector2 SlotPosition => GlobalPosition;

	public IDraggable OccupiedBy { get; set; }

	public bool IsOccupied => OccupiedBy != null;

	/// <summary>
	/// Only accepts draggables that are Crew nodes and only when the slot is free.
	/// </summary>
	public bool CanAccept(IDraggable draggable)
	{
		if (IsOccupied) return false;
		if (draggable is Crew) return true;
		if (draggable is Cargo cargo && cargo.CargoType == CargoType.Droid) return true;
		return false;
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
		else if (draggable is Cargo droid && droid.CargoType == CargoType.Droid)
		{
			_droidTopDownTexture ??= GD.Load<CompressedTexture2D>("res://Assets/Characters/CargoDroidTopDown.png");

			if (droid.GetNodeOrNull("Sprites/Sprite2D") is Sprite2D droidSprite)
			{
				droidSprite.Texture = _droidTopDownTexture;
				droidSprite.RotationDegrees = Facing switch
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
