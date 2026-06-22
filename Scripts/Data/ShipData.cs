using System.Text.Json.Serialization;

/// <summary>
/// Represents a single ship entry deserialized from
/// Assets/Data/Ship's Waker Data - Ships.json.
/// </summary>
public class ShipData
{
	[JsonPropertyName("DataName")]
	public string DataName { get; set; }

	[JsonPropertyName("Name")]
	public string Name { get; set; }

	[JsonPropertyName("CurrentlyWorks")]
	public bool CurrentlyWorks { get; set; }

	[JsonPropertyName("ShipScene")]
	public string ShipScene { get; set; }

	[JsonPropertyName("ShipSmallScene")]
	public string ShipSmallScene { get; set; }
}
