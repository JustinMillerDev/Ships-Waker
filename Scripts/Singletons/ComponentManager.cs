using Godot;
using System.Collections.Generic;

/// <summary>
/// Singleton that manages all ShipComponentSlots on the player's ship.
/// Add this script as an Autoload in Project Settings → Autoload.
/// </summary>
public partial class ComponentManager : Node
{
	public static ComponentManager Instance { get; private set; }

	private readonly List<ShipComponentSlot> _slots = new();

	/// <summary>Read-only view of all collected ship component slots.</summary>
	public IReadOnlyList<ShipComponentSlot> Slots => _slots;

	private const int InitialExternalComponents = 2;

	private PackedScene _shipComponentScene;

	public override void _Ready()
	{
		Instance = this;
		_shipComponentScene = GD.Load<PackedScene>("res://Scenes/Component.tscn");
		CollectSlots();
		UpdateWorldSlotLabels();
		SpawnInitialRailGun();
		SpawnInitialExternalComponents();
		SpawnInitialMissileLaunchers();
		SpawnInitialInternalComponents();
		BindSidebarComponents();
	}

	// ---------------------------------------------------------------------------
	// Setup
	// ---------------------------------------------------------------------------

	private void CollectSlots()
	{
		Node playspace = GetTree().Root.GetNodeOrNull("PlaySpace");
		if (playspace == null)
		{
			GD.PushWarning("ComponentManager: Could not find PlaySpace root node.");
			return;
		}

		// --- Player ship: slots live at CanvasLayer/PlayerShip/Portrait/Pivot/ComponentSlots ---
		Node playerSlots = playspace.GetNodeOrNull("CanvasLayer/PlayerShip/Portrait/Pivot/ComponentSlots");
		if (playerSlots != null)
		{
			foreach (Node child in playerSlots.GetChildren())
			{
				if (child is not ShipComponentSlot slot) continue;
				_slots.Add(slot);
				UpdateSlotLabel(slot);
				AssignSlotData(slot);
			}
		}
		else
		{
			GD.PushWarning("ComponentManager: Could not find CanvasLayer/PlayerShip/Portrait/Pivot/ComponentSlots node.");
		}

		// --- Enemy ship: slots now live directly on the ship node ---
		Node enemyGui = playspace.GetNodeOrNull("CanvasLayer/EnemyShip/Portrait/Pivot/ComponentSlots");
		if (enemyGui != null)
		{
			foreach (Node child in enemyGui.GetChildren())
			{
				if (child is not ShipComponentSlot slot) continue;
				_slots.Add(slot);
				UpdateSlotLabel(slot);
				AssignSlotData(slot);
			}
		}
		else
		{
			GD.PushWarning("ComponentManager: Could not find CanvasLayer/EnemyShip/Portrait/Pivot/ComponentSlots node.");
		}
	}

	private static bool IsPlayerSlot(ShipComponentSlot slot)
	{
		if (slot == null) return false;
		if (slot.Owner == SlotOwner.Player) return true;
		return slot.GetPath().ToString().Contains("/CanvasLayer/PlayerShip/");
	}

	// Updates labels on world-space ComponentSlots for every ship in the PlaySpace.
	private void UpdateWorldSlotLabels()
	{
		Node playspace = GetTree().Root.GetNodeOrNull("PlaySpace");
		if (playspace == null) return;

		foreach (Node ship in playspace.GetChildren())
		{
			Node container = ship.GetNodeOrNull("Pivot/ComponentSlots");
			if (container == null) continue;

			foreach (Node child in container.GetChildren())
			{
				if (child is ShipComponentSlot slot)
					UpdateSlotLabel(slot);
			}
		}
	}

	private void SpawnInitialInternalComponents()
	{
		if (_shipComponentScene == null) return;

		Node playspace = GetTree().Root.GetNodeOrNull("PlaySpace");
		if (playspace == null) return;

		Node componentContainer = playspace.GetNodeOrNull("CanvasLayer/PlayerShip/Portrait/Pivot/Components");
		if (componentContainer == null)
		{
			GD.PushWarning("ComponentManager: Could not find CanvasLayer/PlayerShip/Portrait/Pivot/Components node.");
			return;
		}

		// Build a lookup of world-space slots by type.
		var worldSlotsByType = new Dictionary<ShipComponentType, ShipComponentSlot>();
		Node psSlotContainer = playspace.GetNodeOrNull("CanvasLayer/PlayerShip/Portrait/Pivot/ComponentSlots");
		if (psSlotContainer != null)
		{
			foreach (Node child in psSlotContainer.GetChildren())
			{
				if (child is ShipComponentSlot ws && !worldSlotsByType.ContainsKey(ws.SlotType))
					worldSlotsByType[ws.SlotType] = ws;
			}
		}

		ShipComponentType[] internalTypes =
		{
			ShipComponentType.Piloting,
			ShipComponentType.Engines,
			ShipComponentType.Caskets,
			ShipComponentType.Hold,
			ShipComponentType.Shields,
		};

		foreach (ShipComponentType type in internalTypes)
		{
			// Find the first unoccupied GUI slot matching this type.
			ShipComponentSlot guiSlot = null;
			foreach (ShipComponentSlot slot in _slots)
			{
				if (!IsPlayerSlot(slot)) continue;
				if (slot.SlotType == type && !slot.IsOccupied) { guiSlot = slot; break; }
			}
			if (guiSlot == null) continue;

			// Look up data by the type name ("Piloting", "Engines", etc.).
			string dataName = type.ToString();
			if (DataManager.Components == null ||
				!DataManager.Components.ByDataName.TryGetValue(dataName, out ComponentData data))
			{
				GD.PushWarning($"ComponentManager: No ComponentData found for '{dataName}'.");
				continue;
			}

			ShipComponent component = _shipComponentScene.Instantiate<ShipComponent>();
			componentContainer.AddChild(component);
			component.ComponentType = type;
			component.Data = data;
			component.GetNodeOrNull<Sprite2D>("Sprites/Sprite2D").Visible = false;
			component.GetNodeOrNull<TextureButton>("Focus").Position = Vector2.Zero; // align focus button to slot pivot	
			component.GetNodeOrNull<Label>("Tooltip/PanelContainer/Label").Text = $"{data.Name}"; // set tooltip text
			guiSlot.Accept(component);

			// Position at the world-space slot; fall back to the container origin.
			if (worldSlotsByType.TryGetValue(type, out ShipComponentSlot worldSlot))
			{
				worldSlot.Accept(component);
				component.GlobalPosition = worldSlot.GlobalPosition;
			}
			else
			{
				component.GlobalPosition = componentContainer is Node2D cn ? cn.GlobalPosition : Vector2.Zero;
			}
		}
	}

	private void SpawnInitialRailGun()
	{
		if (_shipComponentScene == null) return;

		Node playspace = GetTree().Root.GetNodeOrNull("PlaySpace");
		if (playspace == null) return;

		Node componentContainer = playspace.GetNodeOrNull("CanvasLayer/PlayerShip/Portrait/Pivot/Components");
		if (componentContainer == null) return;

		if (DataManager.Components == null ||
			!DataManager.Components.ByDataName.TryGetValue("Rail Gun", out ComponentData railGunData))
		{
			GD.PushWarning("ComponentManager: No ComponentData found for 'Rail Gun'.");
			return;
		}

		// Find the first unoccupied external slot.
		ShipComponentSlot slot = null;
		foreach (ShipComponentSlot s in _slots)
		{
			if (!IsPlayerSlot(s)) continue;
			if (s.SlotType == ShipComponentType.External && !s.IsOccupied) { slot = s; break; }
		}
		if (slot == null) return;

		GD.Print("Spawning ship component: Rail Gun");
		ShipComponent component = _shipComponentScene.Instantiate<ShipComponent>();
		componentContainer.AddChild(component);
		component.ComponentType = ShipComponentType.External;
		component.Facing = slot.Facing;
		component.Data = railGunData;

		if (slot.GetNodeOrNull("Pivot/Sprites/Sprite2D") is Sprite2D slotSprite)
			slotSprite.Visible = false;

		if (component.GetNodeOrNull("Sprites/Sprite2D") is Sprite2D sprite)
			sprite.FlipH = slot.Facing == CardinalDirection.Right;

		string anchorPath = slot.Facing == CardinalDirection.Right
			? "Pivot/Anchors/RightExternal"
			: "Pivot/Anchors/LeftExternal";

		slot.Accept(component);
		component.GlobalPosition = (slot.GetNodeOrNull(anchorPath) is Node2D anchor
			? anchor.GlobalPosition
			: slot.GlobalPosition) + new Vector2(0, -24);

		if (component.GetNodeOrNull<TextureButton>("Focus") is TextureButton focusBtn)
			focusBtn.Position = new Vector2(focusBtn.Position.X, focusBtn.Position.Y + 24);

		if (component.GetNodeOrNull<Label>("Tooltip/PanelContainer/Label") is Label tooltip)
			tooltip.Text = railGunData.Name;
	}

	private void SpawnInitialExternalComponents()
	{
		if (_shipComponentScene == null)
		{
			GD.PushWarning("ComponentManager: Could not load ShipComponent.tscn.");
			return;
		}

		Node playspace = GetTree().Root.GetNodeOrNull("PlaySpace");
		if (playspace == null) return;

		Node componentContainer = playspace.GetNodeOrNull("CanvasLayer/PlayerShip/Portrait/Pivot/Components");
		if (componentContainer == null)
		{
			GD.PushWarning("ComponentManager: Could not find CanvasLayer/PlayerShip/Portrait/Pivot/Components node.");
			return;
		}

		int spawned = 0;
		foreach (ShipComponentSlot slot in _slots)
		{
			if (spawned >= InitialExternalComponents) break;
			if (!IsPlayerSlot(slot)) continue;
			if (slot.SlotType != ShipComponentType.External || slot.IsOccupied) continue;

			GD.Print("Spawning ship component: Cannon");
			ShipComponent component = _shipComponentScene.Instantiate<ShipComponent>();
			componentContainer.AddChild(component);
			component.ComponentType = ShipComponentType.External;
			component.Facing = slot.Facing;

			// Assign Cannon data.
			if (DataManager.Components != null &&
				DataManager.Components.ByDataName.TryGetValue("Cannon", out ComponentData cannonData))
				component.Data = cannonData;

			// Hide this slot's background sprite now that it's occupied by a cannon.
			if (slot.GetNodeOrNull("Pivot/Sprites/Sprite2D") is Sprite2D slotSprite)
				slotSprite.Visible = false;

			// Flip sprite horizontally for RIGHT-facing components.
			if (component.GetNodeOrNull("Sprites/Sprite2D") is Sprite2D sprite)
				sprite.FlipH = slot.Facing == CardinalDirection.Right;

			string anchorPath = slot.Facing == CardinalDirection.Right
				? "Pivot/Anchors/RightExternal"
				: "Pivot/Anchors/LeftExternal";

			slot.Accept(component);
			component.GlobalPosition = slot.GetNodeOrNull(anchorPath) is Node2D anchor
				? anchor.GlobalPosition
				: slot.GlobalPosition;

			spawned++;
		}
	}

	private void SpawnInitialMissileLaunchers()
	{
		if (_shipComponentScene == null) return;

		Node playspace = GetTree().Root.GetNodeOrNull("PlaySpace");
		if (playspace == null) return;

		Node componentContainer = playspace.GetNodeOrNull("CanvasLayer/PlayerShip/Portrait/Pivot/Components");
		if (componentContainer == null) return;

		if (DataManager.Components == null ||
			!DataManager.Components.ByDataName.TryGetValue("Missile Launcher", out ComponentData missileData))
		{
			GD.PushWarning("ComponentManager: No ComponentData found for 'Missile Launcher'.");
			return;
		}

		int spawned = 0;
		foreach (ShipComponentSlot slot in _slots)
		{
			if (spawned >= 2) break;
			if (!IsPlayerSlot(slot)) continue;
			if (slot.SlotType != ShipComponentType.External || slot.IsOccupied) continue;

			GD.Print("Spawning ship component: Missile Launcher");
			ShipComponent component = _shipComponentScene.Instantiate<ShipComponent>();
			componentContainer.AddChild(component);
			component.ComponentType = ShipComponentType.External;
			component.Facing = slot.Facing;
			component.Data = missileData;

			if (slot.GetNodeOrNull("Pivot/Sprites/Sprite2D") is Sprite2D slotSprite)
				slotSprite.Visible = false;

			if (component.GetNodeOrNull("Sprites/Sprite2D") is Sprite2D sprite)
				sprite.FlipH = slot.Facing == CardinalDirection.Right;

			string anchorPath = slot.Facing == CardinalDirection.Right
				? "Pivot/Anchors/RightExternal"
				: "Pivot/Anchors/LeftExternal";

			slot.Accept(component);
			component.GlobalPosition = slot.GetNodeOrNull(anchorPath) is Node2D anchor
				? anchor.GlobalPosition
				: slot.GlobalPosition;

			if (component.GetNodeOrNull<Label>("Tooltip/PanelContainer/Label") is Label tooltip)
				tooltip.Text = missileData.Name;

			spawned++;
		}
	}

	private void BindSidebarComponents()
	{
		Node playspace = GetTree().Root.GetNodeOrNull("PlaySpace");
		if (playspace == null) return;

		Node barContainer = playspace.GetNodeOrNull("CanvasLayer/GUI/Gameplay/SideBarComponents/HBoxContainer");
		if (barContainer == null) return;

		var weaponSlots = new List<ShipComponentSlot>();
		foreach (ShipComponentSlot slot in _slots)
		{
			if (!IsPlayerSlot(slot)) continue;
			if (slot.SlotType != ShipComponentType.External) continue;
			if (!slot.IsOccupied) continue;
			weaponSlots.Add(slot);
		}

		int index = 0;
		foreach (Node child in barContainer.GetChildren())
		{
			if (child is not SideBarComponent sideBarComponent) continue;

			sideBarComponent.ComponentSlot = index < weaponSlots.Count ? weaponSlots[index] : null;
			sideBarComponent.UpdateLabel();
			index++;
		}
	}

	private static void AssignSlotData(ShipComponentSlot slot)
	{
		if (DataManager.Components == null) return;

		string dataName = slot.SlotType switch
		{
			ShipComponentType.Piloting => "Piloting",
			ShipComponentType.Engines  => "Engines",
			ShipComponentType.Shields  => "Shields",
			ShipComponentType.Caskets  => "Caskets",
			ShipComponentType.Hold     => "Hold",
			_                          => null,
		};

		if (dataName != null && DataManager.Components.ByDataName.TryGetValue(dataName, out ComponentData data))
			slot.Data = data;
	}

	private static void UpdateSlotLabel(ShipComponentSlot slot)
	{
		if (slot.GetNodeOrNull("Pivot/Label") is not Label label) return;

		label.Text = slot.SlotType switch
		{
			ShipComponentType.Internal  => "I",
			ShipComponentType.External  => "X",
			ShipComponentType.Piloting  => "P",
			ShipComponentType.Shields   => "S",
			ShipComponentType.Engines   => "E",
			ShipComponentType.Caskets   => "C",
			ShipComponentType.Hold      => "H",
			_                           => "?",
		};
	}
}
