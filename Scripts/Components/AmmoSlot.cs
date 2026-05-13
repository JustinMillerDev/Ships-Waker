using Godot;
using System.Collections.Generic;

/// <summary>
/// A slot that only accepts Cargo draggables whose CargoType is <see cref="CargoType.Ammo"/>.
/// </summary>
public partial class AmmoSlot : Node2D, ISlottable
{
	/// <summary>Half-extents of the slot's drop area. Adjust in the Inspector.</summary>
	[Export] public Vector2 HalfSize { get; set; } = new Vector2(32, 32);

	private const int FrameBase     = 0;
	private const int FrameHighlight = 1;
	private const int FrameSelected  = 2;

	private static readonly List<AmmoSlot> _allSlots = new();
	private static bool _dragActive = false;
	private static AmmoSlot _hoveredSlot = null;

	public override void _Ready()  => _allSlots.Add(this);
	public override void _ExitTree()
	{
		_allSlots.Remove(this);
		if (_hoveredSlot == this) _hoveredSlot = null;
	}

	public override void _Process(double delta)
	{
		if (!_dragActive) return;
		if (_allSlots.Count == 0 || _allSlots[0] != this) return;

		Vector2 mouse = GetGlobalMousePosition();
		AmmoSlot closest = null;
		float bestDist = float.MaxValue;
		foreach (var slot in _allSlots)
		{
			if (!slot.ContainsPoint(mouse)) continue;
			float dist = slot.GlobalPosition.DistanceTo(mouse);
			if (dist < bestDist) { bestDist = dist; closest = slot; }
		}

		if (closest != _hoveredSlot)
		{
			if (_hoveredSlot != null) _hoveredSlot.SetFrame(FrameHighlight);
			_hoveredSlot = closest;
			if (_hoveredSlot != null) _hoveredSlot.SetFrame(FrameSelected);
		}
	}

	private void SetFrame(int frame)
	{
		if (GetNodeOrNull("Sprites/Sprite2D") is Sprite2D sprite)
			sprite.Frame = frame;
	}

	public static void HighlightAll()
	{
		_dragActive = true;
		_hoveredSlot = null;
		foreach (var slot in _allSlots)
			slot.SetFrame(FrameHighlight);
	}

	public static void ResetAll()
	{
		_dragActive = false;
		_hoveredSlot = null;
		foreach (var slot in _allSlots)
			slot.SetFrame(FrameBase);
	}

	public Vector2 SlotPosition => GlobalPosition;

	public IDraggable OccupiedBy { get; set; }

	public bool IsOccupied => OccupiedBy != null;

	/// <summary>
	/// Only accepts Cargo draggables of type Ammo when the slot is free.
	/// </summary>
	public bool CanAccept(IDraggable draggable)
	{
		return !IsOccupied && draggable is Cargo cargo && cargo.CargoType == CargoType.Ammo;
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
