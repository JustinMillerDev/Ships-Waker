using Godot;

/// <summary>
/// Singleton that spawns and animates projectiles.
/// Register as an Autoload in Project Settings → Autoload.
/// </summary>
public partial class ProjectileManager : Node
{
	public static ProjectileManager Instance { get; private set; }

	/// <summary>Travel time for the second leg (attack run toward the enemy), in seconds.</summary>
	[Export] public float TravelTime { get; set; } = 2.5f;

	/// <summary>Travel time for the first leg (rise to staging point), in seconds. Defaults to half of TravelTime.</summary>
	[Export] public float StagingTravelTime { get; set; } = 2.5f;

	/// <summary>Radius of the circle around the enemy ship anchor used for attack radian positions.</summary>
	[Export] public float AttackRadianRadius { get; set; } = 330f;

	/// <summary>Delay in seconds between the projectile reaching the staging point and launching toward the enemy.</summary>
	[Export] public float StagingDelay { get; set; } = 0.25f;

	/// <summary>Radius (in world units) of the circle around the enemy sprite center used to pick a random impact point.</summary>
	[Export] public float AttackTargetRadius { get; set; } = 120f;

	/// <summary>The Y coordinate the projectile rises to before teleporting to the attack radian.</summary>
	[Export] public float StagingY { get; set; } = -2000f;

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
			"PlaySpace/EnemyShipSmall/Pivot/Sprites/Sprite2D2");
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
	public void Fire(ShipComponent source, ShipSmall targetShip, ShipComponent targetComponent, int damage)
	{
		if (source == null || targetShip == null) return;

		if (source.Data?.TrailType == TrailType.Missile)
		{
			FireMissileVolley(source, targetShip, targetComponent, null, damage);
			return;
		}

		Projectile projectile = SpawnProjectile(source, targetShip, targetComponent, damage, Vector2.Zero);
		Launch(projectile, projectile.ImpactPosition);
	}

	/// <summary>
	/// Fires a projectile from <paramref name="source"/> toward an explicit world-space position.
	/// </summary>
	public void Fire(ShipComponent source, ShipSmall targetShip, Vector2 targetPosition, int damage)
	{
		if (source == null || targetShip == null) return;

		if (source.Data?.TrailType == TrailType.Missile)
		{
			FireMissileVolley(source, targetShip, null, targetPosition, damage);
			return;
		}

		Projectile projectile = SpawnProjectile(source, targetShip, null, damage, Vector2.Zero);
		Launch(projectile, targetPosition);
	}

	private void FireMissileVolley(ShipComponent source, ShipSmall targetShip, ShipComponent targetComponent, Vector2? explicitTarget, int damage)
	{
		for (int i = 0; i < 4; i++)
		{
			int index = i;
			float xOffset = index * 5f;
			float stage2XOffset = (index - 1.5f) * 80f;    // spread: -120, -40, +40, +120
			float midYOffset = (index - 1.5f) * 300f;       // stagger: -450, -150, +150, +450
			GetTree().CreateTimer(index * 0.15f).Timeout += () =>
			{
				Projectile projectile = SpawnProjectile(source, targetShip, targetComponent, damage, new Vector2(xOffset, 0), stage2XOffset, midYOffset);
				Vector2 dest = explicitTarget ?? projectile.ImpactPosition;
				Launch(projectile, dest, stage2XOffset, midYOffset);
			};
		}
	}

	private Projectile SpawnProjectile(ShipComponent source, ShipSmall targetShip, ShipComponent targetComponent, int damage, Vector2 spawnOffset, float stage2XOffset = 0f, float midYOffset = 0f)
	{
		bool fromSmall = IsSourceFromShipSmall(source);

		// Choose container: CanvasLayer/Projectiles for PlayerShip, default container for ShipSmall.
		Node container = fromSmall
			? _projectileContainer
			: (GetTree().Root.GetNodeOrNull("PlaySpace/CanvasLayer/PlayerShip/Portrait/Projectiles") ?? _projectileContainer);

		Projectile projectile = _projectileScene.Instantiate<Projectile>();
		projectile.Source          = source;
		projectile.TargetShip      = targetShip;
		projectile.TargetComponent = targetComponent;
		projectile.Damage          = damage;

		container.AddChild(projectile);
		projectile.GlobalPosition = source.GlobalPosition + spawnOffset;

		if (source.Data?.ProjectileImage != null &&
			projectile.GetNodeOrNull<Sprite2D>("Pivot/Sprite2D") is Sprite2D sprite)
			sprite.Texture = GD.Load<Texture2D>("res://Assets/Effects/" + source.Data.ProjectileImage + ".png");

		// If fired from PlayerShip, also spawn a mirrored projectile on PlayerShipSmall.
		if (!fromSmall)
			SpawnSmallMirrorProjectile(source, targetShip, targetComponent, damage, spawnOffset, stage2XOffset, midYOffset);

		return projectile;
	}

	/// <summary>Spawns a duplicate projectile originating from the matching component on PlayerShipSmall.</summary>
	private void SpawnSmallMirrorProjectile(ShipComponent source, ShipSmall targetShip, ShipComponent targetComponent, int damage, Vector2 spawnOffset, float stage2XOffset = 0f, float midYOffset = 0f)
	{
		var smallShip = GetTree().Root.GetNodeOrNull<Node2D>("PlaySpace/PlayerShipSmall");
		if (smallShip == null) return;

		Projectile mirror = _projectileScene.Instantiate<Projectile>();
		mirror.Source             = source;
		mirror.TargetShip         = targetShip;
		mirror.TargetComponent    = targetComponent;
		mirror.Damage             = 0; // mirror does no additional damage
		mirror.IsSmallProjectile  = true;

		_projectileContainer.AddChild(mirror);
		mirror.GlobalPosition = smallShip.GlobalPosition + spawnOffset;

		if (source.Data?.ProjectileImage != null &&
			mirror.GetNodeOrNull<Sprite2D>("Pivot/Sprite2D") is Sprite2D sprite)
			sprite.Texture = GD.Load<Texture2D>("res://Assets/Effects/" + source.Data.ProjectileImage + ".png");

		var enemySmall = GetTree().Root.GetNodeOrNull<Node2D>("PlaySpace/EnemyShipSmall");
		Vector2 mirrorTarget = enemySmall != null ? enemySmall.GlobalPosition : targetShip.GlobalPosition;
		Launch(mirror, mirrorTarget, stage2XOffset, midYOffset);
	}

	/// <summary>
	/// Returns a travel-time multiplier based on the current combat range zoom ratio.
	/// Uses Mid (0.25) as the baseline — Close is faster, Long is slower.
	/// </summary>
	private float RangeTravelMultiplier()
	{
		const float baseZoom = 0.25f; // Mid zoom
		var playSpace = GetTree().Root.GetNodeOrNull<PlaySpace>("PlaySpace");
		float currentZoom = playSpace != null
			? playSpace.CurrentRange switch
			{
				CombatRange.Close => 0.50f,
				CombatRange.Mid   => 0.25f,
				CombatRange.Long  => 0.15f,
				_                 => 0.25f,
			}
			: 0.25f;
		return baseZoom / currentZoom;
	}

	private void Launch(Projectile projectile, Vector2 destination, float stage2XOffset = 0f, float midYOffset = 0f)
	{
		// Use the explicit flag rather than walking the source's parent tree.
		bool fromSmall = projectile.IsSmallProjectile;

		if (fromSmall)
		{
			GD.Print("Launching projectile from PlayerShipSmall toward " + destination);
			// --- ShipSmall source: single tween directly to EnemyShipSmall ---
			var enemySmall = GetTree().Root.GetNodeOrNull<Node2D>("PlaySpace/EnemyShipSmall");
			Vector2 target = enemySmall != null ? enemySmall.GlobalPosition : destination;
			bool isMissile = projectile.Source?.Data?.TrailType == TrailType.Missile;
			bool isRailGun = projectile.Source?.Data?.TrailType == TrailType.RailGun;
			float scaledTravelTime = TravelTime * RangeTravelMultiplier() / (isRailGun ? 3f : 1f);

			Tween tween = projectile.CreateTween();
			if (isMissile)
			{
				// Smooth quadratic Bézier arc: origin → control point (mid) → target.
				// Each missile gets a unique control point via stage2XOffset/midYOffset.
				var playerSmall = GetTree().Root.GetNodeOrNull<Node2D>("PlaySpace/PlayerShipSmall");
				Vector2 p0 = playerSmall != null ? playerSmall.GlobalPosition : projectile.GlobalPosition;
				Vector2 p1 = (p0 + target) * 0.5f + new Vector2(stage2XOffset, midYOffset); // control point
				Vector2 p2 = target;

				// TweenMethod drives t from 0→1; position is computed as B(t) = (1-t)²·p0 + 2(1-t)t·p1 + t²·p2
				tween.TweenMethod(
					Callable.From((float t) =>
					{
						float u = 1f - t;
						projectile.GlobalPosition = u * u * p0 + 2f * u * t * p1 + t * t * p2;
					}),
					0f, 1f, scaledTravelTime
				).SetTrans(Tween.TransitionType.Linear);
			}
			else
			{
				tween.TweenProperty(projectile, "global_position", target, scaledTravelTime)
					 .SetTrans(Tween.TransitionType.Linear)
					 .SetEase(Tween.EaseType.In);
			}
			tween.TweenCallback(Callable.From(projectile.OnImpact));
		}
		else
		{
			GD.Print("Launching projectile from PlayerShip toward " + destination);
			// --- PlayerShip source: Stage 1 rise, then remove projectile ---
			Vector2 stagingPoint = new Vector2(projectile.GlobalPosition.X, StagingY);
			float scaledStagingTime = StagingTravelTime * RangeTravelMultiplier();

			Tween tween = projectile.CreateTween();
			tween.TweenProperty(projectile, "global_position", stagingPoint, scaledStagingTime)
				 .SetTrans(Tween.TransitionType.Linear)
				 .SetEase(Tween.EaseType.In);

			// Brief pause at the staging point, then remove.
			tween.TweenInterval(StagingDelay);
			tween.TweenCallback(Callable.From(() =>
			{
				projectile.OnImpact();
			}));

			// --- Stage 2 commented out ---
			// tween.TweenCallback(Callable.From(() =>
			// {
			// 	var midRange = GetTree().Root.GetNodeOrNull<Node2D>("PlaySpace/Projectiles/MidRangePlayerAttacks");
			// 	if (midRange != null) projectile.Reparent(midRange);
			// 	Vector2 basePos = midRange?.GlobalPosition ?? PlayerAttackRadian;
			// 	projectile.GlobalPosition = basePos + new Vector2(stage2XOffset, 0);
			// 	projectile.Scale = new Vector2(0.2f, 0.2f);
			// 	projectile.ClearTrail();
			// 	projectile.SetAttackTrail();
			// }));
			// tween.TweenCallback(Callable.From(() =>
			// {
			// 	var sprite2D2 = GetTree().Root.GetNodeOrNull<Sprite2D>("PlaySpace/EnemyShipSmall/Pivot/Sprites/Sprite2D2");
			// 	Vector2 target = sprite2D2 != null
			// 		? sprite2D2.GlobalTransform * new Vector2(Mathf.Cos(GD.Randf() * Mathf.Tau), Mathf.Sin(GD.Randf() * Mathf.Tau)) * AttackTargetRadius * Mathf.Sqrt(GD.Randf())
			// 		: destination;
			// 	Tween flyTween = projectile.CreateTween();
			// 	flyTween.TweenProperty(projectile, "global_position", target, TravelTime).SetTrans(Tween.TransitionType.Linear).SetEase(Tween.EaseType.In);
			// 	flyTween.TweenCallback(Callable.From(projectile.OnImpact));
			// }));
		}
	}

	/// <summary>Returns true if the source component lives under PlayerShipSmall rather than PlayerShip.</summary>
	private static bool IsSourceFromShipSmall(ShipComponent source)
	{
		if (source == null) return false;
		Node node = source;
		while (node != null)
		{
			if (node is ShipSmall) return true;
			if (node is Ship)      return false;
			node = node.GetParent();
		}
		// Fall back: check node name path
		return source.GetPath().ToString().Contains("PlayerShipSmall");
	}
}
