using System.Text.Json.Serialization;

/// <summary>
/// Represents a single cargo entry deserialized from
/// Assets/Data/Ship's Waker Data - Cargo.json.
/// </summary>
public class CargoData
{
	[JsonPropertyName("DataName")]
	public string DataName { get; set; }

	[JsonPropertyName("Name")]
	public string Name { get; set; }

	[JsonPropertyName("CurrentlyWorks")]
	public bool CurrentlyWorks { get; set; }

	[JsonPropertyName("Price")]
	public int Price { get; set; }

	[JsonPropertyName("Ability")]
	[JsonConverter(typeof(JsonStringEnumConverter))]
	public CargoAbility Ability { get; set; }

	[JsonPropertyName("Image")]
	public string Image { get; set; }

	[JsonPropertyName("ImageTopDown")]
	public string ImageTopDown { get; set; }

	// Optional crew-like stats (Droid only)
	[JsonPropertyName("Health")]
	public int? Health { get; set; }

	[JsonPropertyName("Melee")]
	public int? Melee { get; set; }

	[JsonPropertyName("Repair")]
	public int? Repair { get; set; }

	[JsonPropertyName("Medical")]
	public int? Medical { get; set; }

	[JsonPropertyName("Weapons")]
	public int? Weapons { get; set; }

	[JsonPropertyName("Piloting")]
	public int? Piloting { get; set; }

	[JsonPropertyName("Science")]
	public int? Science { get; set; }

	[JsonPropertyName("Arcane")]
	public int? Arcane { get; set; }

	[JsonPropertyName("AbilityText")]
	public string AbilityText { get; set; }

	[JsonPropertyName("AbilityValue")]
	public int? AbilityValue { get; set; }
}
