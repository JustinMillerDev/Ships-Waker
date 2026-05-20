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

	private const int CasketCount = 8;
	private const float CasketSpacing = 17f;

	private Queue<CrewData> _unassignedCrewData;

	private PackedScene _casketScene;
	private PackedScene _crewScene;
	private CompressedTexture2D _hyperSleepTexture;

	public override void _Ready()
	{
		Instance = this;
		_casketScene = GD.Load<PackedScene>("res://Scenes/Casket.tscn");
		_crewScene = GD.Load<PackedScene>("res://Scenes/Crew.tscn");
		_hyperSleepTexture = GD.Load<CompressedTexture2D>("res://Assets/Characters/CrewFrontView.png");

		_unassignedCrewData = DataManager.Crew != null
			? new Queue<CrewData>(DataManager.Crew.All)
			: new Queue<CrewData>();

		SpawnCaskets();

		Node playspace = GetTree().Root.GetNodeOrNull("PlaySpace");
		if (playspace is PlaySpace ps)
			ps.TurnEnded += OnTurnEnded;
	}

	// ---------------------------------------------------------------------------
	// Casket spawning
	// ---------------------------------------------------------------------------

	private void SpawnCaskets()
	{
		if (_casketScene == null)
		{
			GD.PushWarning("CrewManager: Could not load Casket.tscn.");
			return;
		}

		// Playspace is the root node of the scene tree
		Node playspace = GetTree().Root.GetNodeOrNull("PlaySpace");
		if (playspace == null)
		{
			GD.PushWarning("CrewManager: Could not find PlaySpace root node.");
			return;
		}

		Node column1 = playspace.GetNodeOrNull("PlayerShip/Pivot/Portrait//Caskets/Column1");
		Node column2 = playspace.GetNodeOrNull("PlayerShip/Pivot/Portrait//Caskets/Column2");

		SpawnCasketColumn(column1, CardinalDirection.Right, new Vector2(8, 8));
		SpawnCasketColumn(column2, CardinalDirection.Left, new Vector2(16, 8));

		AssignReadyTimers();
	}

	private void SpawnCasketColumn(Node column, CardinalDirection facing, Vector2 crewOffset)
	{
		if (column == null)
		{
			GD.PushWarning($"CrewManager: Casket column node not found.");
			return;
		}

		for (int i = 0; i < CasketCount; i++)
		{
			Casket casket = _casketScene.Instantiate<Casket>();
			column.AddChild(casket);
			casket.Position = new Vector2(0, i * CasketSpacing);
			casket.Facing = facing;
			_allCaskets.Add(casket);

			SpawnCrewInCasket(casket, crewOffset);
		}
	}

	private void SpawnCrewInCasket(Casket casket, Vector2 crewOffset)
	{
		if (_crewScene == null)
		{
			GD.PushWarning("CrewManager: Could not load Crew.tscn.");
			return;
		}

		Crew crew = _crewScene.Instantiate<Crew>();
		casket.AddChild(crew);
		crew.Facing = casket.Facing;
		crew.SlotOffset = crewOffset;

		if (_unassignedCrewData != null && _unassignedCrewData.Count > 0)
			crew.Data = _unassignedCrewData.Dequeue();

		// Set HyperSleep texture and rotation on the Sprite2D
		if (crew.GetNodeOrNull("Sprites/Sprite2D") is Sprite2D sprite)
		{
			sprite.Texture = _hyperSleepTexture;
			// LEFT = 90° counter-clockwise (-90°), RIGHT = 90° clockwise (+90°)
			sprite.RotationDegrees = casket.Facing == CardinalDirection.Left ? 90f : -90f;
		}

		// Slot the crew into the casket
		casket.Accept(crew);
		crew.Position = crewOffset;
		Register(crew);
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

	private void OnTurnEnded(int turn)
	{
		foreach (Crew crew in _allCrew)
		{
			crew.TurnsUntilReady = Mathf.Max(0, crew.TurnsUntilReady - 1);
			crew.IsReady = crew.TurnsUntilReady == 0;

			if (crew.CurrentSlot is Casket casket &&
				casket.GetNodeOrNull("ReadyLabel") is Label label)
			{
				if (crew.TurnsUntilReady == 0)
					casket.StartReadyFlicker();
				else
					label.Text = crew.TurnsUntilReady.ToString("D2");
			}
		}
	}

	/// <summary>
	/// Assigns each crew member a unique TurnsUntilReady value drawn from a
	/// shuffled range of [0, crewCount). Updates the ReadyLabel on their casket.
	/// </summary>
	private const int CrewReadyAtStart = 3;

	private void AssignReadyTimers()
	{
		int count = _allCrew.Count;

		// Three crew start at 0; the remaining crew get unique values 1..(count - CrewReadyAtStart).
		// This keeps the maximum counter lower to reflect the extra ready crew.
		List<int> values = new(count);
		for (int i = 0; i < CrewReadyAtStart; i++) values.Add(0);
		for (int i = 1; i <= count - CrewReadyAtStart; i++) values.Add(i);

		// Fisher-Yates shuffle.
		for (int i = count - 1; i > 0; i--)
		{
			int j = GD.RandRange(0, i);
			(values[i], values[j]) = (values[j], values[i]);
		}

		for (int i = 0; i < count; i++)
		{
			Crew crew = _allCrew[i];
			crew.TurnsUntilReady = values[i];
			crew.IsReady = values[i] == 0;

			if (crew.CurrentSlot is Casket casket &&
				casket.GetNodeOrNull("ReadyLabel") is Label label)
			{
				if (values[i] == 0)
					casket.StartReadyFlicker();
				else
					label.Text = values[i].ToString("D2");
			}
		}
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
