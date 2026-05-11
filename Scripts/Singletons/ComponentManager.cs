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

		Node container = playspace.GetNodeOrNull("PlayerShip/Pivot/ComponentSlots");
		if (container == null)
		{
			GD.PushWarning("ComponentManager: Could not find PlayerShip/Pivot/ComponentSlots node.");
			return;
		}

		foreach (Node child in container.GetChildren())
		{
			if (child is not ShipComponentSlot slot) continue;

			_slots.Add(slot);
			UpdateSlotLabel(slot);
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

		int spawned = 0;
		foreach (ShipComponentSlot slot in _slots)
		{
			if (spawned >= InitialExternalComponents) break;
			if (slot.SlotType != ShipComponentType.External || slot.IsOccupied) continue;

			ShipComponent component = _shipComponentScene.Instantiate<ShipComponent>();
			componentContainer.AddChild(component);
			component.ComponentType = ShipComponentType.External;
			component.Facing = slot.Facing;

			// Flip sprite horizontally for RIGHT-facing components.
			if (component.GetNodeOrNull("Sprites/Sprite2D") is Sprite2D sprite)
				sprite.FlipH = slot.Facing == CardinalDirection.Right;

			slot.Accept(component);

			// Position using the slot's directional anchor node.
			string anchorPath = slot.Facing == CardinalDirection.Right
				? "Pivot/Anchors/RightExternal"
				: "Pivot/Anchors/LeftExternal";
			component.GlobalPosition = slot.GetNodeOrNull(anchorPath) is Node2D anchor
				? anchor.GlobalPosition
				: slot.GlobalPosition;

			spawned++;
		}
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
