/// <summary>
/// Identifies the type of a cargo object.
/// Used by slot implementations to enforce compatibility (e.g. only Fuel in a FuelSlot).
/// </summary>
public enum CargoType
{
	Goods,
	FirstAid,
	Droid,
	Ammo,
	Fuel,
}
