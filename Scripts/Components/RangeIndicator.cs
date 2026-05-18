using Godot;

public partial class RangeIndicator : Node2D
{
	private static readonly int[] RangeYPositions = { 1, 7, 13 };

	private Label _labelSelected;
	private Label _labelSelectedEnemy;

	private int _playerRangeIndex    = 1; // default: Mid
	private int _enemyRangeIndex     = 1; // default: Mid
	private int _playerEngineStrength = 0;

	/// <summary>Engine strength of the enemy ship. Defaults to 0.</summary>
	public int EnemyEngineStrength { get; set; } = 0;

	public override void _Ready()
	{
		_labelSelected      = GetNode<Label>("ColorRect/LabelSelected");
		_labelSelectedEnemy = GetNode<Label>("ColorRect/LabelSelectedEnemy");

		if (CustomSignals.Instance != null)
		{
			CustomSignals.Instance.EngineStrengthChanged += OnEngineStrengthChanged;
			CustomSignals.Instance.EnemyRangeSelected    += OnEnemyRangeSelected;
		}

		if (GetTree().Root.GetNodeOrNull("PlaySpace") is PlaySpace ps)
			ps.TurnEnded += OnTurnEnded;
	}

	public override void _ExitTree()
	{
		if (CustomSignals.Instance != null)
		{
			CustomSignals.Instance.EngineStrengthChanged -= OnEngineStrengthChanged;
			CustomSignals.Instance.EnemyRangeSelected    -= OnEnemyRangeSelected;
		}

		if (GetTree().Root.GetNodeOrNull("PlaySpace") is PlaySpace ps)
			ps.TurnEnded -= OnTurnEnded;
	}

	private void OnEngineStrengthChanged(int strength)
	{
		_playerEngineStrength = strength;
		_labelSelected.Text = $"E:{strength}";
	}

	private void OnEnemyRangeSelected(int rangeIndex)
	{
		_enemyRangeIndex = rangeIndex;
		_labelSelectedEnemy.Position = new Vector2(_labelSelectedEnemy.Position.X, RangeYPositions[rangeIndex]);
	}

	private void OnTurnEnded(int turn)
	{
		int winnerIndex;
		if (_playerEngineStrength > EnemyEngineStrength)
		{
			winnerIndex = _playerRangeIndex;
		}
		else if (EnemyEngineStrength > _playerEngineStrength)
		{
			winnerIndex = _enemyRangeIndex;
		}
		else
		{
			winnerIndex = GD.RandRange(0, 1) == 0 ? _playerRangeIndex : _enemyRangeIndex;
			GD.Print($"RangeIndicator: Engine strength tie ({_playerEngineStrength} vs {EnemyEngineStrength}). " +
					 $"Randomly chose {IndexToRange(winnerIndex)}.");
		}

		if (GetTree().Root.GetNodeOrNull("PlaySpace") is PlaySpace ps)
			ps.SetRange(IndexToRange(winnerIndex));
	}

	private static CombatRange IndexToRange(int index) => index switch
	{
		0 => CombatRange.Long,
		1 => CombatRange.Mid,
		2 => CombatRange.Close,
		_ => CombatRange.Mid,
	};

	public void _on_texture_button_long_pressed()
	{
		_playerRangeIndex = 0;
		_labelSelected.Position = new Vector2(_labelSelected.Position.X, RangeYPositions[0]);
	}

	public void _on_texture_button_mid_pressed()
	{
		_playerRangeIndex = 1;
		_labelSelected.Position = new Vector2(_labelSelected.Position.X, RangeYPositions[1]);
	}

	public void _on_texture_button_close_pressed()
	{
		_playerRangeIndex = 2;
		_labelSelected.Position = new Vector2(_labelSelected.Position.X, RangeYPositions[2]);
	}
}
