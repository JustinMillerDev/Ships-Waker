using System.Collections.Generic;

/// <summary>
/// Read-only database of all crew entries loaded from JSON.
/// Access via DataManager.Crew.
/// </summary>
public class CrewDatabase
{
	public IReadOnlyList<CrewData> All { get; }
	public IReadOnlyDictionary<string, CrewData> ByDataName { get; }

	public CrewDatabase(List<CrewData> entries)
	{
		All = entries.AsReadOnly();

		var dict = new Dictionary<string, CrewData>(entries.Count);
		foreach (CrewData entry in entries)
			dict[entry.DataName] = entry;
		ByDataName = dict;
	}
}

/// <summary>
/// Read-only database of all cargo entries loaded from JSON.
/// Access via DataManager.Cargo.
/// </summary>
public class CargoDatabase
{
	public IReadOnlyList<CargoData> All { get; }
	public IReadOnlyDictionary<string, CargoData> ByDataName { get; }

	public CargoDatabase(List<CargoData> entries)
	{
		All = entries.AsReadOnly();

		var dict = new Dictionary<string, CargoData>(entries.Count);
		foreach (CargoData entry in entries)
			dict[entry.DataName] = entry;
		ByDataName = dict;
	}
}

/// <summary>
/// Read-only database of all ship component entries loaded from JSON.
/// Access via DataManager.Components.
/// </summary>
public class ComponentDatabase
{
	public IReadOnlyList<ComponentData> All { get; }
	public IReadOnlyDictionary<string, ComponentData> ByDataName { get; }

	public ComponentDatabase(List<ComponentData> entries)
	{
		All = entries.AsReadOnly();

		var dict = new Dictionary<string, ComponentData>(entries.Count);
		foreach (ComponentData entry in entries)
			dict[entry.DataName] = entry;
		ByDataName = dict;
	}
}

/// <summary>
/// Read-only database of all ship entries loaded from JSON.
/// Access via DataManager.Ships.
/// </summary>
public class ShipDatabase
{
	public IReadOnlyList<ShipData> All { get; }
	public IReadOnlyDictionary<string, ShipData> ByDataName { get; }

	public ShipDatabase(List<ShipData> entries)
	{
		All = entries.AsReadOnly();

		var dict = new Dictionary<string, ShipData>(entries.Count);
		foreach (ShipData entry in entries)
			dict[entry.DataName] = entry;
		ByDataName = dict;
	}
}
