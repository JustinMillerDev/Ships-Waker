using Godot;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Singleton that tracks all Crew members in the game.
/// Register this script as an Autoload in the Godot Project Settings.
/// </summary>
public partial class CrewManager : Node
{
	public static CrewManager Instance { get; private set; }

	private readonly List<Crew> _allCrew = new();
	private readonly List<Casket> _allCaskets = new();

	/// <summary>Read-only view of every registered crew member.</summary>
	public IReadOnlyList<Crew> AllCrew => _allCrew;

	/// <summary>Read-only view of every spawned casket.</summary>
	public IReadOnlyList<Casket> AllCaskets => _allCaskets;

	[Export] public PackedScene CasketScene { get; set; }

	private const int CasketCount = 7;
	private const float CasketSpacing = 18f;

	public override void _Ready()
	{
		Instance = this;
		SpawnCaskets();
	}

	// ---------------------------------------------------------------------------
	// Casket spawning
	// ---------------------------------------------------------------------------

	private void SpawnCaskets()
	{
		if (CasketScene == null)
		{
			GD.PushWarning("CrewManager: CasketScene is not set. Assign Casket.tscn in the Inspector.");
			return;
		}

		// Playspace is the root node of the scene tree
		Node playspace = GetTree().Root.GetNodeOrNull("PlaySpace");
		if (playspace == null)
		{
			GD.PushWarning("CrewManager: Could not find PlaySpace root node.");
			return;
		}

		Node column1 = playspace.GetNodeOrNull("Caskets/Column1");
		Node column2 = playspace.GetNodeOrNull("Caskets/Column2");

		SpawnCasketColumn(column1, CardinalDirection.Left);
		SpawnCasketColumn(column2, CardinalDirection.Right);
	}

	private void SpawnCasketColumn(Node column, CardinalDirection facing)
	{
		if (column == null)
		{
			GD.PushWarning($"CrewManager: Casket column node not found.");
			return;
		}

		for (int i = 0; i < CasketCount; i++)
		{
			Casket casket = CasketScene.Instantiate<Casket>();
			column.AddChild(casket);
			casket.Position = new Vector2(0, i * CasketSpacing);
			casket.Facing = facing;
			_allCaskets.Add(casket);
		}
	}

	// ---------------------------------------------------------------------------
	// Registration
	// ---------------------------------------------------------------------------

	/// <summary>Registers a crew member. Called automatically by Crew._Ready if desired,
	/// or manually when spawning crew at runtime.</summary>
	public void Register(Crew crew)
	{
		if (!_allCrew.Contains(crew))
			_allCrew.Add(crew);
	}

	/// <summary>Unregisters a crew member (e.g. when they die or are removed).</summary>
	public void Unregister(Crew crew)
	{
		_allCrew.Remove(crew);
	}

	// ---------------------------------------------------------------------------
	// Queries
	// ---------------------------------------------------------------------------

	/// <summary>Returns all crew currently in the given state.</summary>
	public IEnumerable<Crew> GetCrewInState(CrewState state)
		=> _allCrew.Where(c => c.State == state);

	/// <summary>Returns the total number of registered crew.</summary>
	public int TotalCrew => _allCrew.Count;

	/// <summary>Returns how many crew are in the given state.</summary>
	public int CountInState(CrewState state)
		=> _allCrew.Count(c => c.State == state);

	// ---------------------------------------------------------------------------
	// Bulk operations
	// ---------------------------------------------------------------------------

	/// <summary>Sets all crew to the given state.</summary>
	public void SetAllCrewState(CrewState state)
	{
		foreach (Crew crew in _allCrew)
			crew.State = state;
	}

	/// <summary>Wakes all crew currently in HyperSleep, setting them to Idle.</summary>
	public void WakeAllCrew()
	{
		foreach (Crew crew in GetCrewInState(CrewState.HyperSleep).ToList())
			crew.State = CrewState.Idle;
	}
}
