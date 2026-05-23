using Godot;
using System.Collections.Generic;

public partial class Pdc : Node2D
{
	private ShipFaction _faction = ShipFaction.Enemy;

	[Export]
	public ShipFaction Faction
	{
		get => _faction;
		set => _faction = value;
	}

	[Export] public float FireIntervalSeconds { get; set; } = 0.2f;
	[Export] public float InterceptorSpeed { get; set; } = 1400f;
	[Export] public float InterceptRange { get; set; } = 1200f;
	[Export] public float InterceptHitRadius { get; set; } = 28f;

	private Node2D _emitPoint;
	private double _cooldown;

	public override void _Ready()
	{
		_emitPoint = GetNodeOrNull<Node2D>("Pivot/EmitPoint");
		if (_emitPoint == null)
			GD.PushWarning($"{Name}: missing Pivot/EmitPoint; interception disabled.");
	}

	public override void _Process(double delta)
	{
		if (!IsVisibleInTree() || _faction == ShipFaction.Player || _emitPoint == null || ProjectileManager.Instance == null) return;

		_cooldown -= delta;
		if (_cooldown > 0d) return;

		if (!TryAcquireIntercept(out Projectile missile, out Vector2 interceptPoint)) return;

		Projectile interceptor = ProjectileManager.Instance.FireDirect(
			_emitPoint.GlobalPosition,
			interceptPoint,
			InterceptorSpeed,
			ProjectileManager.ProjectileSourceKind.Cannon);

		if (interceptor == null) return;

		float travelTime = Mathf.Max(0.01f, _emitPoint.GlobalPosition.DistanceTo(interceptPoint) / Mathf.Max(1f, InterceptorSpeed));
		GetTree().CreateTimer(travelTime).Timeout += () => ResolveIntercept(missile, interceptor, interceptPoint);
		_cooldown = FireIntervalSeconds;
	}

	private bool TryAcquireIntercept(out Projectile missile, out Vector2 interceptPoint)
	{
		missile = null;
		interceptPoint = Vector2.Zero;

		List<ProjectileManager.InFlightProjectileInfo> inFlight = ProjectileManager.Instance.GetInFlightProjectiles();
		if (inFlight.Count == 0) return false;

		Vector2 shooterPos = _emitPoint.GlobalPosition;
		float bestTime = float.MaxValue;

		for (int i = 0; i < inFlight.Count; i++)
		{
			ProjectileManager.InFlightProjectileInfo info = inFlight[i];
			if (info.SourceKind != ProjectileManager.ProjectileSourceKind.Missile) continue;
			if (!GodotObject.IsInstanceValid(info.Projectile) || info.Projectile.IsQueuedForDeletion()) continue;

			Vector2 toShooter = shooterPos - info.Position;
			if (toShooter.Length() > InterceptRange) continue;
			if (info.Velocity.Dot(toShooter) <= 0f) continue; // only missiles moving toward this PDC

			if (!TryCalculateInterceptTime(shooterPos, info.Position, info.Velocity, InterceptorSpeed, out float t)) continue;
			if (t < 0f || t >= bestTime) continue;

			bestTime = t;
			missile = info.Projectile;
			interceptPoint = info.Position + info.Velocity * t;
		}

		return missile != null;
	}

	private void ResolveIntercept(Projectile missile, Projectile interceptor, Vector2 interceptPoint)
	{
		if (GodotObject.IsInstanceValid(interceptor) && !interceptor.IsQueuedForDeletion())
			interceptor.OnImpact();

		if (!GodotObject.IsInstanceValid(missile) || missile.IsQueuedForDeletion()) return;

		if (missile.GlobalPosition.DistanceTo(interceptPoint) <= InterceptHitRadius)
			missile.QueueFree();
	}

	private static bool TryCalculateInterceptTime(Vector2 shooterPos, Vector2 targetPos, Vector2 targetVelocity, float interceptorSpeed, out float time)
	{
		time = 0f;
		interceptorSpeed = Mathf.Max(1f, interceptorSpeed);

		Vector2 r = targetPos - shooterPos;
		float a = targetVelocity.Dot(targetVelocity) - interceptorSpeed * interceptorSpeed;
		float b = 2f * r.Dot(targetVelocity);
		float c = r.Dot(r);

		if (Mathf.IsZeroApprox(a))
		{
			if (Mathf.IsZeroApprox(b)) return false;
			time = -c / b;
			return time > 0f;
		}

		float discriminant = b * b - 4f * a * c;
		if (discriminant < 0f) return false;

		float sqrtDisc = Mathf.Sqrt(discriminant);
		float t1 = (-b - sqrtDisc) / (2f * a);
		float t2 = (-b + sqrtDisc) / (2f * a);

		bool t1Valid = t1 > 0f;
		bool t2Valid = t2 > 0f;

		if (!t1Valid && !t2Valid) return false;
		time = t1Valid && t2Valid ? Mathf.Min(t1, t2) : (t1Valid ? t1 : t2);
		return true;
	}
}
