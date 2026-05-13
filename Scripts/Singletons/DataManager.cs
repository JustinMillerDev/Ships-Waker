using Godot;
using System.Collections.Generic;
using System.Text.Json;

/// <summary>
/// Autoload singleton. Loads all JSON data files on startup and exposes
/// read-only databases for the rest of the game to query.
///
/// Usage:
///   DataManager.Crew.ByDataName["Cappy"]
///   DataManager.Cargo.All
///   DataManager.Components.ByDataName["Piloting"]
/// </summary>
public partial class DataManager : Node
{
	public static DataManager Instance { get; private set; }

	public static CrewDatabase      Crew       { get; private set; }
	public static CargoDatabase     Cargo      { get; private set; }
	public static ComponentDatabase Components { get; private set; }

	private static readonly JsonSerializerOptions JsonOptions = new()
	{
		PropertyNameCaseInsensitive = true,
	};

	public override void _Ready()
	{
		Instance = this;

		Crew       = new CrewDatabase(Load<CrewData>      ("res://Assets/Data/Ship's Waker Data - Crew.json"));
		Cargo      = new CargoDatabase(Load<CargoData>     ("res://Assets/Data/Ship's Waker Data - Cargo.json"));
		Components = new ComponentDatabase(Load<ComponentData>("res://Assets/Data/Ship's Waker Data - Components.json"));

		GD.Print($"[DataManager] Loaded {Crew.All.Count} crew, {Cargo.All.Count} cargo, {Components.All.Count} components.");
	}

	private static List<T> Load<T>(string resPath)
	{
		if (!FileAccess.FileExists(resPath))
		{
			GD.PrintErr($"[DataManager] File not found: {resPath}");
			return new List<T>();
		}

		string json = FileAccess.GetFileAsString(resPath);
		return JsonSerializer.Deserialize<List<T>>(json, JsonOptions) ?? new List<T>();
	}
}
