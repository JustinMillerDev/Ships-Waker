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
		SpawnInitialExternalComponents();
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

		// --- Player ship: slots live inside the ShipView GUI ---
		Node shipViews = playspace.GetNodeOrNull("CanvasLayer/GUI/Gameplay/ShipViews");
		if (shipViews != null)
		{
			foreach (Node shipView in shipViews.GetChildren())
			{
				Node container = shipView.GetNodeOrNull("ShipGUI/ComponentSlots");
				if (container == null) continue;

				foreach (Node child in container.GetChildren())
				{
					if (child is not ShipComponentSlot slot) continue;
					_slots.Add(slot);
					UpdateSlotLabel(slot);
					AssignSlotData(slot);
				}
			}
		}
		else
		{
			GD.PushWarning("ComponentManager: Could not find CanvasLayer/GUI/Gameplay/ShipViews node.");
		}

		// --- Enemy ship: slots now live directly on the ship node ---
		Node enemyGui = playspace.GetNodeOrNull("EnemyShip/Pivot/ShipGUI/ComponentSlots");
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
			GD.PushWarning("ComponentManager: Could not find EnemyShip/Pivot/ShipGUI/ComponentSlots node.");
		}
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

	private void SpawnInitialExternalComponents()
	{
		if (_shipComponentScene == null)
		{
			GD.PushWarning("ComponentManager: Could not load ShipComponent.tscn.");
			return;
		}

		Node playspace = GetTree().Root.GetNodeOrNull("PlaySpace");
		if (playspace == null) return;

		Node componentContainer = playspace.GetNodeOrNull("PlayerShip/Pivot/Components");
		if (componentContainer == null)
		{
			GD.PushWarning("ComponentManager: Could not find PlayerShip/Pivot/Components node.");
			return;
		}

		// Collect world-space slots from PlaySpace — their anchor GlobalPositions are valid world coords.
		var worldSlots = new List<ShipComponentSlot>();
		Node psSlotContainer = playspace.GetNodeOrNull("PlayerShip/Pivot/ComponentSlots");
		if (psSlotContainer != null)
		{
			foreach (Node child in psSlotContainer.GetChildren())
			{
				if (child is ShipComponentSlot ws)
					worldSlots.Add(ws);
			}
		}

		int spawned = 0;
		foreach (ShipComponentSlot slot in _slots)
		{
			if (spawned >= InitialExternalComponents) break;
			if (slot.SlotType != ShipComponentType.External || slot.IsOccupied) continue;

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

			slot.Accept(component);

			// Use the world-space PlaySpace slot's anchor for correct world positioning.
			// Match by SlotType + Facing to find the corresponding world-space slot.
			string anchorPath = slot.Facing == CardinalDirection.Right
				? "Pivot/Anchors/RightExternal"
				: "Pivot/Anchors/LeftExternal";

			ShipComponentSlot worldSlot = worldSlots.Find(
				ws => ws.SlotType == ShipComponentType.External && ws.Facing == slot.Facing && !ws.IsOccupied);

			if (worldSlot != null)
			{
				worldSlot.Accept(component); // mark world slot occupied so the next one isn't reused
				component.GlobalPosition = worldSlot.GetNodeOrNull(anchorPath) is Node2D anchor
					? anchor.GlobalPosition
					: worldSlot.GlobalPosition;
			}
			else
			{
				// Fallback: place at the component container's origin
				component.GlobalPosition = componentContainer is Node2D cn ? cn.GlobalPosition : Vector2.Zero;
			}

			spawned++;
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
