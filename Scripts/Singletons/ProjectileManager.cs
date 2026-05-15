using Godot;

/// <summary>
/// Singleton that spawns and animates projectiles.
/// Register as an Autoload in Project Settings → Autoload.
/// </summary>
public partial class ProjectileManager : Node
{
	public static ProjectileManager Instance { get; private set; }

	/// <summary>Travel time for a projectile in seconds.</summary>
	[Export] public float TravelTime { get; set; } = 2f;

	private Node _projectileContainer;
	private PackedScene _projectileScene;

	public override void _Ready()
	{
		Instance = this;
		_projectileScene = GD.Load<PackedScene>("res://Scenes/Projectile.tscn");

		// Projectiles live under the PlaySpace root so they share the game world.
		_projectileContainer = GetTree().Root.GetNodeOrNull("PlaySpace") ?? GetTree().Root;
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
		Tween tween = projectile.CreateTween();
		tween.TweenProperty(projectile, "global_position", destination, TravelTime)
			 .SetTrans(Tween.TransitionType.Linear)
			 .SetEase(Tween.EaseType.In);
		tween.TweenCallback(Callable.From(projectile.OnImpact));
	}
}
