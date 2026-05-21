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
		_cargoContainer = GetParentNode("CanvasLayer/PlayerShip/Portrait/Pivot/Cargo");

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
				slot.IsReadied = false;
				_deepStorageSlots.Add(slot);

				SpawnCargoInSlot(slot);
			}
		}
	}

	private void SpawnReadiedSlots()
	{
		Node parent = GetParentNode("CanvasLayer/PlayerShip/Portrait/Pivot//CargoSlots/ReadiedSlots");
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
	/// Each turn:
	/// 1. Readied cargo shifts right into any open readied slot to its right.
	/// 2. Deep-storage cargo advances forward to fill any gaps left behind.
	/// Tweens are staggered for a wave effect; deep-storage tweens start after
	/// the readied shift tweens finish.
	/// </summary>
	private void AdvanceCargoToReadiedSlot()
	{
		const float stepDelay    = 0.04f;
		const float moveDuration = 0.2f;

		float tweenDelay = 0f;

		// ------------------------------------------------------------------
		// Phase 1 – shift readied cargo right into open readied slots.
		// Iterate right-to-left so each cargo only moves once per turn.
		// ------------------------------------------------------------------
		for (int i = _readiedSlots.Count - 1; i >= 0; i--)
		{
			CargoSlot current = _readiedSlots[i];
			if (!current.IsOccupied) continue;

			// Find the next open slot immediately to the right (one step only).
			if (i + 1 >= _readiedSlots.Count) continue;
			CargoSlot destination = _readiedSlots[i + 1].IsOccupied ? null : _readiedSlots[i + 1];

			if (destination == null) continue;

			Cargo cargo = current.OccupiedBy as Cargo;
			if (cargo == null) continue;

			current.Vacate();
			destination.OccupiedBy = cargo;
			cargo.CurrentSlot = destination;

			Vector2 targetGlobal = destination.GlobalPosition + new Vector2(8, 8);
			Tween tween = cargo.CreateTween();
			tween.TweenInterval(tweenDelay);
			tween.TweenProperty(cargo, "global_position", targetGlobal, moveDuration)
				 .SetTrans(Tween.TransitionType.Sine)
				 .SetEase(Tween.EaseType.Out);

			tweenDelay += stepDelay;
		}

		// ------------------------------------------------------------------
		// Phase 2 – advance deep-storage cargo into any open readied slots.
		// Deep-storage tweens are delayed so they start after phase 1 finishes.
		// ------------------------------------------------------------------
		float deepStorageBaseDelay = tweenDelay + moveDuration;
		float deepTweenDelay = deepStorageBaseDelay;

		// Find the first empty readied slot.
		CargoSlot targetReadiedSlot = null;
		foreach (CargoSlot slot in _readiedSlots)
		{
			if (!slot.IsOccupied) { targetReadiedSlot = slot; break; }
		}

		for (int i = 0; i < _deepStorageSlots.Count; i++)
		{
			CargoSlot current = _deepStorageSlots[i];
			if (!current.IsOccupied) continue;

			Cargo cargo = current.OccupiedBy as Cargo;
			if (cargo == null) continue;

			CargoSlot destination = i == 0
				? targetReadiedSlot
				: _deepStorageSlots[i - 1];

			if (destination == null || destination.IsOccupied) continue;

			current.Vacate();
			destination.OccupiedBy = cargo;
			cargo.CurrentSlot = destination;

			Vector2 targetGlobal = destination.GlobalPosition + new Vector2(8, 8);
			Tween tween = cargo.CreateTween();
			tween.TweenInterval(deepTweenDelay);
			tween.TweenProperty(cargo, "global_position", targetGlobal, moveDuration)
				 .SetTrans(Tween.TransitionType.Sine)
				 .SetEase(Tween.EaseType.Out);

			deepTweenDelay += stepDelay;
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
