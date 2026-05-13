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

	public override void _Ready()
	{
		// Hide the background sprite for all named slots (Piloting, Shields, etc.).
		// External slots remain visible so empty cannon slots can be seen.
		if (GetNodeOrNull("Pivot/Sprites/Sprite2D") is Sprite2D sprite)
			sprite.Visible = false;

		// Style the label: white font with a black outline.
		if (GetNodeOrNull("Pivot/Label") is Label label)
		{
			label.AddThemeColorOverride("font_color", new Color(1, 1, 1));
			label.AddThemeColorOverride("font_outline_color", new Color(0, 0, 0));
			label.AddThemeConstantOverride("outline_size", 4);
		}
	}

	/// <summary>The component data record associated with this slot's type.</summary>
	public ComponentData Data { get; set; }

	public bool IsHovered { get; private set; }

	public void _on_focus_pressed()
	{
	}

	public void _on_focus_mouse_entered()
	{
		IsHovered = true;
		if (!IsOccupied)
			ShowFocusStats();
	}

	public void _on_focus_mouse_exited()
	{
		IsHovered = false;
		HideFocusStats();
	}

	private void ShowFocusStats()
	{
		Node root = GetTree().Root;
		if (root.GetNodeOrNull("PlaySpace/CanvasLayer/GUI/Gameplay/Stats/CurrentFocusStats") is not CanvasItem panel)
			return;

		panel.Visible = true;

		if (panel.GetNodeOrNull("Tooltip/Label") is not Label label)
			return;

		if (Data != null)
		{
			string hp = OccupiedBy is IHealth h ? $"{h.CurrentHealth}/{h.MaxHealth}" : $"{Data.Health}/{Data.Health}";
			label.Text = $"{Data.Name}\nHP: {hp}";
		}
		else
		{
			label.Text = SlotType switch
			{
				ShipComponentType.Internal => "Internal Slot",
				ShipComponentType.External => "External Slot",
				_                          => string.Empty,
			};
		}
	}

	private void HideFocusStats()
	{
		Node root = GetTree().Root;
		if (root.GetNodeOrNull("PlaySpace/CanvasLayer/GUI/Gameplay/Stats/CurrentFocusStats") is CanvasItem panel)
			panel.Visible = false;
	}

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
