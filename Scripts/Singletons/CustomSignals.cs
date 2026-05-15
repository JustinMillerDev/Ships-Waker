using Godot;

/// <summary>
/// Autoload singleton acting as a global event bus.
/// Nodes emit signals here; other nodes subscribe here instead of holding
/// direct references to the emitting node.
///
/// Usage (emit):
///   CustomSignals.Instance.EmitSignal(CustomSignals.SignalName.CargoSlotted, this);
///
/// Usage (subscribe):
///   CustomSignals.Instance.CargoSlotted += OnCargoSlotted;
/// </summary>
public partial class CustomSignals : Node
{
	public static CustomSignals Instance { get; private set; }

	/// <summary>Fired when any Cargo is successfully placed into a CargoSlot.</summary>
	[Signal] public delegate void CargoSlottedEventHandler(Cargo cargo);

	/// <summary>Fired when a Crew member is successfully placed into a CrewSlot.</summary>
	[Signal] public delegate void CrewSlottedEventHandler(Crew crew);

	/// <summary>Fired when an ability reduces a ShipComponent's cooldown.</summary>
	[Signal] public delegate void CooldownReducedEventHandler(ShipComponent component);

	/// <summary>Fired when a Ship's health or shields change.</summary>
	[Signal] public delegate void ShipHealthChangedEventHandler(Ship ship);

	public override void _Ready()
	{
		Instance = this;
	}
}
