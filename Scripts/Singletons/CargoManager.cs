using Godot;
using System.Collections.Generic;

/// <summary>
/// Singleton that manages all CargoSlots and Cargo items in the game.
/// Register this script as an Autoload in Project Settings → Autoload.
/// </summary>
public partial class CargoManager : Node
{
	public static CargoManager Instance { get; private set; }

	private static readonly CargoType[] AllCargoTypes =
	{
		CargoType.Goods, CargoType.FirstAid, CargoType.Droid, CargoType.Ammo, CargoType.Fuel,
	};

	// Deep storage: 5 columns × 4 rows, spaced 19 px apart.
	private const int DeepStorageCols    = 5;
	private const int DeepStorageRows    = 4;
	private const float DeepStorageSpacing = 19f;

	// Readied slots: 4 across, spaced 21 px apart.
	private const int ReadiedSlotCount   = 4;
	private const float ReadiedSlotSpacing = 19f;

	private readonly List<CargoSlot> _deepStorageSlots = new();
	private readonly List<CargoSlot> _readiedSlots     = new();

	/// <summary>Read-only view of all deep-storage cargo slots.</summary>
	public IReadOnlyList<CargoSlot> DeepStorageSlots => _deepStorageSlots;

	/// <summary>Read-only view of all readied cargo slots.</summary>
	public IReadOnlyList<CargoSlot> ReadiedSlots => _readiedSlots;

	private PackedScene _cargoSlotScene;
	private PackedScene _cargoScene;
	private Node _cargoContainer;

	public override void _Ready()
	{
		Instance = this;
		_cargoSlotScene = GD.Load<PackedScene>("res://Scenes/CargoSlot.tscn");
		_cargoScene     = GD.Load<PackedScene>("res://Scenes/Cargo.tscn");
		_cargoContainer = GetParentNode("PlayerShip/Pivot/Cargo");

		SpawnDeepStorageSlots();
		SpawnReadiedSlots();

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
		Node parent = GetParentNode("PlayerShip/Pivot/CargoSlots/DeepStorageSlots");
		if (parent == null) return;

		for (int row = 0; row < DeepStorageRows; row++)
		{
			for (int col = 0; col < DeepStorageCols; col++)
			{
				CargoSlot slot = InstantiateSlot(parent);
				if (slot == null) continue;

				slot.Position = new Vector2(col * DeepStorageSpacing, row * DeepStorageSpacing);
				slot.IsReadied = false;
				_deepStorageSlots.Add(slot);

				SpawnCargoInSlot(slot);
			}
		}
	}

	private void SpawnReadiedSlots()
	{
		Node parent = GetParentNode("PlayerShip/Pivot/CargoSlots/ReadiedSlots");
		if (parent == null) return;

		for (int i = 0; i < ReadiedSlotCount; i++)
		{
			CargoSlot slot = InstantiateSlot(parent);
			if (slot == null) continue;

			slot.Position = new Vector2(i * ReadiedSlotSpacing, 0f);
			slot.IsReadied = true;
			_readiedSlots.Add(slot);
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
		cargo.CargoType = AllCargoTypes[GD.RandRange(0, AllCargoTypes.Length - 1)];
		slot.Accept(cargo);
		cargo.GlobalPosition = slot.GlobalPosition + new Vector2(8, 8);
	}

	// ---------------------------------------------------------------------------
	// Turn handling
	// ---------------------------------------------------------------------------

	private void OnTurnStarted(int turn)
	{
		AdvanceCargoToReadiedSlot();
	}

	/// <summary>
	/// Each turn every piece of cargo advances one position forward like a conveyor belt.
	/// Slot 0 → first empty readied slot; slot N → slot N-1.
	/// Ownership is transferred front-to-back so each freed slot is immediately
	/// available for the cargo behind it. Tweens are staggered for a wave effect.
	/// </summary>
	private void AdvanceCargoToReadiedSlot()
	{
		// Find the first empty readied slot once up front.
		CargoSlot targetReadiedSlot = null;
		foreach (CargoSlot slot in _readiedSlots)
		{
			if (!slot.IsOccupied) { targetReadiedSlot = slot; break; }
		}

		// Process front-to-back. Vacating a slot immediately makes it available
		// as the destination for the cargo one index behind it.
		float tweenDelay = 0f;
		const float stepDelay = 0.04f;

		for (int i = 0; i < _deepStorageSlots.Count; i++)
		{
			CargoSlot current = _deepStorageSlots[i];
			if (!current.IsOccupied) continue;

			Cargo cargo = current.OccupiedBy as Cargo;
			if (cargo == null) continue;

			CargoSlot destination = i == 0
				? targetReadiedSlot
				: _deepStorageSlots[i - 1];

			// Destination still occupied (e.g. readied row full) — chain breaks here.
			if (destination == null || destination.IsOccupied) continue;

			// Transfer ownership immediately so the slot behind can move into this one.
			current.Vacate();
			destination.OccupiedBy = cargo;
			cargo.CurrentSlot = destination;

			// Cargo stays parented under PlayerShip/Cargo — just tween its global position.
			Vector2 targetGlobal = destination.GlobalPosition + new Vector2(8, 8);
			Tween tween = cargo.CreateTween();
			tween.TweenInterval(tweenDelay);
			tween.TweenProperty(cargo, "global_position", targetGlobal, 0.2)
				 .SetTrans(Tween.TransitionType.Sine)
				 .SetEase(Tween.EaseType.Out);

			tweenDelay += stepDelay;
		}
	}

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
