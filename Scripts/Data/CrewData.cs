using System.Collections.Generic;
using System;
using System.Text.Json;
using System.Text.Json.Serialization;

/// <summary>
/// Converts JSON role strings (e.g. "First Mate") to the <see cref="CrewRole"/> enum.
/// </summary>
public class CrewRoleConverter : JsonConverter<CrewRole>
{
	public override CrewRole Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
	{
		string value = reader.GetString() ?? string.Empty;
		return value switch
		{
			"Captain"         => CrewRole.Captain,
			"First Mate"      => CrewRole.FirstMate,
			"Doctor"          => CrewRole.Doctor,
			"Chief Engineer"  => CrewRole.ChiefEngineer,
			"Weapons Officer" => CrewRole.WeaponsOfficer,
			"Oilmancer"       => CrewRole.Oilmancer,
			"Pilot"           => CrewRole.Pilot,
			_                 => CrewRole.Crewman,
		};
	}

	public override void Write(Utf8JsonWriter writer, CrewRole value, JsonSerializerOptions options)
	{
		string str = value switch
		{
			CrewRole.Captain         => "Captain",
			CrewRole.FirstMate       => "First Mate",
			CrewRole.Doctor          => "Doctor",
			CrewRole.ChiefEngineer   => "Chief Engineer",
			CrewRole.WeaponsOfficer  => "Weapons Officer",
			CrewRole.Oilmancer       => "Oilmancer",
			CrewRole.Pilot           => "Pilot",
			_                        => "Crewman",
		};
		writer.WriteStringValue(str);
	}
}

/// <summary>
/// Represents a single crew member entry deserialized from
/// Assets/Data/Ship's Waker Data - Crew.json.
/// </summary>
public class CrewData
{
	[JsonPropertyName("DataName")]
	public string DataName { get; set; }

	[JsonPropertyName("Name")]
	public string Name { get; set; }

	[JsonPropertyName("CurrentlyWorks")]
	public bool CurrentlyWorks { get; set; }

	[JsonPropertyName("Role")]
	[JsonConverter(typeof(CrewRoleConverter))]
	public CrewRole Role { get; set; }

	[JsonPropertyName("Health")]
	public int Health { get; set; }

	[JsonPropertyName("Melee")]
	public int Melee { get; set; }

	[JsonPropertyName("Repair")]
	public int Repair { get; set; }

	[JsonPropertyName("Medical")]
	public int Medical { get; set; }

	[JsonPropertyName("Weapons")]
	public int Weapons { get; set; }

	[JsonPropertyName("Piloting")]
	public int Piloting { get; set; }

	[JsonPropertyName("Science")]
	public int Science { get; set; }

	[JsonPropertyName("Arcane")]
	public int Arcane { get; set; }

	[JsonPropertyName("CrewAbility")]
	public string CrewAbility { get; set; }

	public int GetStat(CrewStat stat) => stat switch
	{
		CrewStat.Melee    => Melee,
		CrewStat.Repair   => Repair,
		CrewStat.Weapons  => Weapons,
		CrewStat.Piloting => Piloting,
		CrewStat.Science  => Science,
		CrewStat.Arcane   => Arcane,
		CrewStat.Medical  => Medical,
		_                 => 0,
	};

	[JsonPropertyName("ImageFrontView")]
	public string ImageFrontView { get; set; }

	[JsonPropertyName("ImageTopDown")]
	public string ImageTopDown { get; set; }
}
