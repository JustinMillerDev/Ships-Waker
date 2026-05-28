using Godot;
using System.Collections.Generic;

/// <summary>
/// Singleton that manages all CargoSlots and Cargo items in the game.
/// Register this script as an Autoload in Project Settings → Autoload.
/// </summary>
public partial class CargoManager : Node
{
	public static CargoManager Instance { get; private set; }

	// Deep storage: 5 columns × 4 rows, spaced 19 px apart.
	private const int DeepStorageCols    = 5;
	private const int DeepStorageRows    = 5;
	private const float DeepStorageSpacing = 19f;

	private readonly List<CargoSlot> _deepStorageSlots = new();

	/// <summary>Read-only view of all deep-storage cargo slots.</summary>
	public IReadOnlyList<CargoSlot> DeepStorageSlots => _deepStorageSlots;

	private PackedScene _cargoSlotScene;
	private PackedScene _cargoScene;
	private Node _cargoContainer;

	public override void _Ready()
	{
		Instance = this;
		_cargoSlotScene = GD.Load<PackedScene>("res://Scenes/CargoSlot.tscn");
		_cargoScene     = GD.Load<PackedScene>("res://Scenes/Cargo.tscn");
		_cargoContainer = GetParentNode("CanvasLayer/PlayerShip/Portrait/Pivot/Cargo");

		SpawnDeepStorageSlots();

		// Subscribe to turn events from PlaySpace.
		Node playspace = GetTree().Root.GetNodeOrNull("PlaySpace");
		if (playspace is PlaySpace ps)
			ps.TurnStarted += OnTurnStarted;
	}

	// ---------------------------------------------------------------------------
	// Spawning
	// ---------------------------------------------------------------------------

	private void SpawnDeepStorageSlots()
	{
		Node parent = GetParentNode("CanvasLayer/PlayerShip/Portrait/Pivot//CargoSlots/DeepStorageSlots");
		if (parent == null) return;

		for (int row = 0; row < DeepStorageRows; row++)
		{
			for (int col = 0; col < DeepStorageCols; col++)
			{
				CargoSlot slot = InstantiateSlot(parent);
				if (slot == null) continue;

				// Odd rows run right-to-left so the slots snake back and forth.
				float x = (row % 2 == 0) ? col : (DeepStorageCols - 1 - col);
				slot.Position = new Vector2(x * DeepStorageSpacing, row * DeepStorageSpacing);
				slot.IsReadied = true;
				_deepStorageSlots.Add(slot);

				SpawnCargoInSlot(slot);
			}
		}
	}

	private void SpawnCargoInSlot(CargoSlot slot)
	{
		if (_cargoScene == null)
		{
			GD.PushWarning("CargoManager: Could not load Cargo.tscn.");
			return;
		}

		Cargo cargo = _cargoScene.Instantiate<Cargo>();
		Node container = _cargoContainer ?? (Node)slot;
		container.AddChild(cargo);

		var allCargo = DataManager.Cargo?.All;		
		if (allCargo != null && allCargo.Count > 0)
		{
			CargoData data = allCargo[GD.RandRange(0, allCargo.Count - 1)];
			cargo.Data = data;
			cargo.CargoType = data.Ability switch
			{
				CargoAbility.Ammo     => CargoType.Ammo,
				CargoAbility.Fuel     => CargoType.Fuel,
				CargoAbility.Crew     => CargoType.Droid,
				CargoAbility.FirstAid => CargoType.FirstAid,
				_                     => CargoType.Goods,
			};
		}
		else
		{
			cargo.CargoType = CargoType.Goods;
		}

		slot.OccupiedBy = cargo;
		cargo.CurrentSlot = slot;
		cargo.GlobalPosition = slot.SlotPosition;
	}

	// ---------------------------------------------------------------------------
	// Turn handling
	// ---------------------------------------------------------------------------

	private void OnTurnStarted(int turn) { }

	// ---------------------------------------------------------------------------
	// Helpers
	// ---------------------------------------------------------------------------

	private CargoSlot InstantiateSlot(Node parent)
	{
		if (_cargoSlotScene == null)
		{
			GD.PushWarning("CargoManager: Could not load CargoSlot.tscn.");
			return null;
		}

		CargoSlot slot = _cargoSlotScene.Instantiate<CargoSlot>();
		parent.AddChild(slot);
		return slot;
	}

	private Node GetParentNode(string nodePath)
	{
		Node playspace = GetTree().Root.GetNodeOrNull("PlaySpace");
		if (playspace == null)
		{
			GD.PushWarning("CargoManager: Could not find PlaySpace root node.");
			return null;
		}

		Node node = playspace.GetNodeOrNull(nodePath);
		if (node == null)
			GD.PushWarning($"CargoManager: Could not find node at path '{nodePath}'.");

		return node;
	}
}
