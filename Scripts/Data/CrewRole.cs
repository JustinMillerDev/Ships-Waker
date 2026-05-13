public enum CrewRole
{
	Captain,
	FirstMate,
	Doctor,
	ChiefEngineer,
	WeaponsOfficer,
	Oilmancer,
	Pilot,
	Crewman,
}

public static class CrewRoleExtensions
{
	public static string ToDisplayString(this CrewRole role) => role switch
	{
		CrewRole.FirstMate      => "First Mate",
		CrewRole.ChiefEngineer  => "Chief Engineer",
		CrewRole.WeaponsOfficer => "Weapons Officer",
		_                       => role.ToString(),
	};
}
