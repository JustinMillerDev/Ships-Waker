using Godot;

/// <summary>
/// Singleton that spawns and animates projectiles.
/// Register as an Autoload in Project Settings → Autoload.
/// </summary>
public partial class ProjectileManager : Node
{
	public static ProjectileManager Instance { get; private set; }

	/// <summary>Travel time for each leg of a projectile's journey, in seconds.</summary>
	[Export] public float TravelTime { get; set; } = .75f;

	/// <summary>Radius of the circle around the enemy ship anchor used for attack radian positions.</summary>
	[Export] public float AttackRadianRadius { get; set; } = 330f;

	/// <summary>Delay in seconds between the projectile reaching the staging point and launching toward the enemy.</summary>
	[Export] public float StagingDelay { get; set; } = 0.25f;

	/// <summary>The Y coordinate the projectile rises to before teleporting to the attack radian.</summary>
	[Export] public float StagingY { get; set; } = -280f;

	/// <summary>
	/// The current randomised launch position — a point on a circle of radius
	/// <see cref="AttackRadianRadius"/> around the enemy ship's target anchor.
	/// Re-randomised at game start and at the end of every turn.
	/// </summary>
	public Vector2 PlayerAttackRadian { get; private set; }

	private Node _projectileContainer;
	private PackedScene _projectileScene;
	private Node2D _targetPoint;

	public override void _Ready()
	{
		Instance = this;
		_projectileScene = GD.Load<PackedScene>("res://Scenes/Projectile.tscn");

		// Projectiles live under the PlaySpace root so they share the game world.
		_projectileContainer = GetTree().Root.GetNodeOrNull("PlaySpace") ?? GetTree().Root;

		_targetPoint = GetTree().Root.GetNodeOrNull<Node2D>(
			"PlaySpace/EnemyShipClipAndDraw/Pivot/Sprite2D/Anchors/TargetPoint");
	}

	/// <summary>
	/// Picks a new random position within a circle of <see cref="AttackRadianRadius"/>
	/// centred on <paramref name="enemyAnchor"/> and stores it as <see cref="PlayerAttackRadian"/>.
	/// Call this at game start and at the end of every turn.
	/// </summary>
	public void RandomizeAttackRadian(Vector2 enemyAnchor)
	{
		// Prefer the live world position of the TargetPoint node if available.
		Vector2 centre = _targetPoint != null ? _targetPoint.GlobalPosition : enemyAnchor;
		float angle = GD.Randf() * Mathf.Tau;
		PlayerAttackRadian = centre + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * AttackRadianRadius;
	}

	// ---------------------------------------------------------------------------
	// Public API
	// ---------------------------------------------------------------------------

	/// <summary>
	/// Fires a projectile from <paramref name="source"/> toward its current target.
	/// The target ship and optional target component must be provided.
	/// </summary>
	/// <param name="source">The component firing the projectile.</param>
	/// <param name="targetShip">The ship being targeted.</param>
	/// <param name="targetComponent">Optional specific component to hit; null hits the ship.</param>
	/// <param name="damage">Damage dealt on impact.</param>
	public void Fire(ShipComponent source, Ship targetShip, ShipComponent targetComponent, int damage)
	{
		if (source == null || targetShip == null) return;

		var projectile = _projectileScene.Instantiate<Projectile>();
		projectile.Source          = source;
		projectile.TargetShip      = targetShip;
		projectile.TargetComponent = targetComponent;
		projectile.Damage          = damage;

		_projectileContainer.AddChild(projectile);
		projectile.GlobalPosition = source.GlobalPosition;

		Vector2 destination = projectile.ImpactPosition;

		Launch(projectile, destination);
	}

	/// <summary>
	/// Fires a projectile from <paramref name="source"/> toward an explicit world-space position.
	/// </summary>
	public void Fire(ShipComponent source, Ship targetShip, Vector2 targetPosition, int damage)
	{
		if (source == null || targetShip == null) return;

		var projectile = _projectileScene.Instantiate<Projectile>();
		projectile.Source     = source;
		projectile.TargetShip = targetShip;
		projectile.Damage     = damage;

		_projectileContainer.AddChild(projectile);
		projectile.GlobalPosition = source.GlobalPosition;

		Launch(projectile, targetPosition);
	}

	private void Launch(Projectile projectile, Vector2 destination)
	{
		// --- Stage 1: rise straight up to the staging Y coordinate ---
		Vector2 stagingPoint = new Vector2(projectile.GlobalPosition.X, StagingY);

		Tween tween = projectile.CreateTween();
		tween.TweenProperty(projectile, "global_position", stagingPoint, TravelTime)
			 .SetTrans(Tween.TransitionType.Linear)
			 .SetEase(Tween.EaseType.In);

		// --- Brief pause at the staging point ---
		tween.TweenInterval(StagingDelay);

		// --- Teleport to MidRangePlayerAttacks node (instant) ---
		tween.TweenCallback(Callable.From(() =>
		{
			// Reparent under MidRangePlayerAttacks.
			var midRange = GetTree().Root.GetNodeOrNull<Node2D>("PlaySpace/Projectiles/MidRangePlayerAttacks");
			if (midRange != null)
				projectile.Reparent(midRange);

			// Teleport to that node's global position.
			projectile.GlobalPosition = midRange?.GlobalPosition ?? PlayerAttackRadian;
			projectile.Scale = new Vector2(0.2f, 0.2f);
		}));

		// --- Stage 2: fly toward Camera2D/EnemyShip24/Pivot/Sprites/Sprite2D2 ---
		tween.TweenCallback(Callable.From(() =>
		{
			var sprite2D2 = GetTree().Root.GetNodeOrNull<Node2D>(
				"PlaySpace/Camera2D/EnemyShip24/Pivot/Sprites/Sprite2D2");
			Vector2 target = sprite2D2?.GlobalPosition ?? destination;

			Tween flyTween = projectile.CreateTween();
			flyTween.TweenProperty(projectile, "global_position", target, TravelTime)
					.SetTrans(Tween.TransitionType.Linear)
					.SetEase(Tween.EaseType.In);
			flyTween.TweenCallback(Callable.From(projectile.OnImpact));
		}));
	}
}
