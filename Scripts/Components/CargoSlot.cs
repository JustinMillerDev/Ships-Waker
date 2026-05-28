using Godot;

/// <summary>
/// A slot that accepts any Cargo draggable regardless of its CargoType.
/// Can be marked as readied, which swaps its sprite to the ready variant.
/// </summary>
public partial class CargoSlot : Node2D, ISlottable
{
	/// <summary>Half-extents of the slot's drop area. Adjust in the Inspector.</summary>
	[Export] public Vector2 HalfSize { get; set; } = new Vector2(32, 32);

	/// <summary>The purpose of this cargo slot.</summary>
	[Export] public CargoSlotType SlotType { get; set; } = CargoSlotType.Unreadied;

	private static CompressedTexture2D _readyTexture;
	private static CompressedTexture2D _defaultTexture;

	private bool _isReadied;

	/// <summary>
	/// Whether this cargo slot is in the readied state.
	/// Setting this swaps the Sprites/Sprite2D texture accordingly.
	/// </summary>
	public bool IsReadied
	{
		get => _isReadied;
		set
		{
			if (_isReadied == value) return;
			_isReadied = value;
			UpdateSprite();
		}
	}

	public override void _Ready()
	{
		UpdateSprite();
	}

	private void UpdateSprite()
	{
		if (GetNodeOrNull("Sprites/Sprite2D") is not Sprite2D sprite)
			return;

		if (_isReadied)
		{
			_readyTexture ??= GD.Load<CompressedTexture2D>("res://Assets/Characters/ReadyCargoSlotSheet.png");
			sprite.Texture = _readyTexture;
		}
		else
		{
			_defaultTexture ??= GD.Load<CompressedTexture2D>("res://Assets/Characters/CargoSlotSheet.png");
			sprite.Texture = _defaultTexture;
		}
	}

	// ISlottable ----------------------------------------------------------------

	public Vector2 SlotPosition => GlobalPosition + new Vector2(4, 4);

	[Signal] public delegate void SlotChangedEventHandler();

	public IDraggable OccupiedBy { get; set; }

	public bool IsOccupied => OccupiedBy != null;

	/// <summary>
	/// Accepts cargo according to SlotType rules:
	/// Component slots accept only Ammo or Fuel; all other slots reject Ammo and Fuel.
	/// </summary>
	public bool CanAccept(IDraggable draggable)
	{
		if (draggable is not Cargo cargo) return false;
		if (IsOccupied && OccupiedBy != draggable) return false;
		return SlotType == CargoSlotType.Component
			? cargo.CargoType == CargoType.Ammo || cargo.CargoType == CargoType.Fuel
			: cargo.CargoType != CargoType.Ammo && cargo.CargoType != CargoType.Fuel;
	}

	public void Accept(IDraggable draggable)
	{
		if (!CanAccept(draggable))
			return;

		OccupiedBy = draggable;
		draggable.CurrentSlot = this;

		if (draggable is Node2D node2D)
			node2D.GlobalPosition = SlotPosition;

		EmitSignal(SignalName.SlotChanged);
	}

	public void Vacate()
	{
		if (OccupiedBy != null)
		{
			OccupiedBy.CurrentSlot = null;
			OccupiedBy = null;
			EmitSignal(SignalName.SlotChanged);
		}
	}

	public bool ContainsPoint(Vector2 point)
	{
		Rect2 bounds = new Rect2(GlobalPosition - HalfSize, HalfSize * 2);
		return bounds.HasPoint(point);
	}
}
