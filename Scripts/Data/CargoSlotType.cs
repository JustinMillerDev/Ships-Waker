/// <summary>
/// Determines what kind of cargo a CargoSlot is designed to hold.
/// </summary>
public enum CargoSlotType
{
	/// <summary>A standard slot that can hold readied cargo (not ammo or fuel).</summary>
	Readied,

	/// <summary>A standard slot that holds unreadied cargo (not ammo or fuel).</summary>
	Unreadied,

	/// <summary>A slot attached to a ship component that only accepts ammo or fuel.</summary>
	Component,
}
