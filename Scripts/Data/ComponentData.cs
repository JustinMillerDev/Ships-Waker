using System.Text.Json.Serialization;

/// <summary>
/// Represents a single ship component entry deserialized from
/// Assets/Data/Ship's Waker Data - Components.json.
/// </summary>
public class ComponentData
{
	[JsonPropertyName("DataName")]
	public string DataName { get; set; }

	[JsonPropertyName("Name")]
	public string Name { get; set; }

	[JsonPropertyName("CurrentlyWorks")]
	public bool CurrentlyWorks { get; set; }

	[JsonPropertyName("Position")]
	[JsonConverter(typeof(JsonStringEnumConverter))]
	public ComponentPosition Position { get; set; }

	[JsonPropertyName("Image")]
	public string Image { get; set; }

	[JsonPropertyName("Price")]
	public int Price { get; set; }

	[JsonPropertyName("Health")]
	public int Health { get; set; }

	[JsonPropertyName("Cooldown")]
	public int? Cooldown { get; set; }

	[JsonPropertyName("Strength")]
	public int? Strength { get; set; }

	[JsonPropertyName("RequiredSkill")]
	[JsonConverter(typeof(JsonStringEnumConverter))]
	public CrewStat RequiredSkill { get; set; }

	[JsonPropertyName("Tier0Ability")]
	[JsonConverter(typeof(JsonStringEnumConverter))]
	public ComponentAbility? Tier0Ability { get; set; }

	[JsonPropertyName("Tier0Req")]
	public int Tier0Req { get; set; }

	[JsonPropertyName("Tier1Ability")]
	[JsonConverter(typeof(JsonStringEnumConverter))]
	public ComponentAbility? Tier1Ability { get; set; }

	[JsonPropertyName("Tier1Req")]
	public int Tier1Req { get; set; }

	[JsonPropertyName("Tier1Text")]
	public string Tier1Text { get; set; }

	[JsonPropertyName("Tier2Ability")]
	[JsonConverter(typeof(JsonStringEnumConverter))]
	public ComponentAbility? Tier2Ability { get; set; }

	[JsonPropertyName("Tier2Req")]
	public int Tier2Req { get; set; }

	[JsonPropertyName("Tier2Text")]
	public string Tier2Text { get; set; }

	[JsonPropertyName("Tier3Ability")]
	[JsonConverter(typeof(JsonStringEnumConverter))]
	public ComponentAbility? Tier3Ability { get; set; }

	[JsonPropertyName("Tier3Req")]
	public int Tier3Req { get; set; }

	[JsonPropertyName("Tier3Text")]
	public string Tier3Text { get; set; }

	[JsonPropertyName("Tier4Ability")]
	[JsonConverter(typeof(JsonStringEnumConverter))]
	public ComponentAbility? Tier4Ability { get; set; }

	[JsonPropertyName("Tier4Req")]
	public int? Tier4Req { get; set; }

	[JsonPropertyName("Tier4Text")]
	public string Tier4Text { get; set; }
}
