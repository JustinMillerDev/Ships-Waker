using System.Collections.Generic;
using Godot;

/// <summary>
/// A projectile fired by a ship component toward a target ship (or a specific component on that ship).
/// Created and launched exclusively by <see cref="ProjectileManager"/>.
/// </summary>
public partial class Projectile : Node2D
{
	private const int TrailLength = 24;
	private const int AttackTrailLength = 8;

	private int _activeTrailLength = TrailLength;

	/// <summary>The component that fired this projectile.</summary>
	public ShipComponent Source { get; set; }

	/// <summary>The ship this projectile is travelling toward.</summary>
	public Ship TargetShip { get; set; }

	/// <summary>The specific component to hit on the target ship, or null to hit the ship itself.</summary>
	public ShipComponent TargetComponent { get; set; }

	/// <summary>Amount of damage dealt on impact.</summary>
	public int Damage { get; set; }

	/// <summary>
	/// Calculates the world-space arrival position — the target component if set,
	/// otherwise the target ship's origin.
	/// </summary>
	public Vector2 ImpactPosition =>
		TargetComponent != null ? TargetComponent.GlobalPosition : TargetShip.GlobalPosition;

	private Line2D _trail;
	private readonly Queue<Vector2> _trailPoints = new();
	private bool _draining;

	public override void _Ready()
	{
		_trail = new Line2D
		{
			TopLevel     = true,   // world-space coordinates, unaffected by parent transform
			Width        = 3f,
			DefaultColor = new Color(1f, 0.85f, 0.3f, 0.7f),
			Gradient     = BuildTrailGradient(),
		};
		AddChild(_trail);
	}

	public override void _Process(double delta)
	{
		if (_draining)
		{
			if (_trailPoints.Count > 0)
				_trailPoints.Dequeue();

			if (_trailPoints.Count == 0)
			{
				QueueFree();
				return;
			}

			_trail.Points = [.. _trailPoints];
			return;
		}

		_trailPoints.Enqueue(GlobalPosition);
		if (_trailPoints.Count > _activeTrailLength)
			_trailPoints.Dequeue();

		_trail.Points = [.. _trailPoints];
	}

	/// <summary>Clears the trail history. Call this after a teleport to avoid a streak.</summary>
	public void ClearTrail()
	{
		_trailPoints.Clear();
		_trail.Points = [];
	}

	/// <summary>Switches to a shorter, skinnier trail for the attack run (stage 2).</summary>
	public void SetAttackTrail()
	{
		_activeTrailLength = AttackTrailLength;
		_trail.Width = 1.5f;
	}

	/// <summary>Called by ProjectileManager after the tween completes.</summary>
	public void OnImpact()
	{
		if (TargetComponent is IHealth componentHealth)
			componentHealth.TakeDamage(Damage);
		else if (TargetShip != null)
		{
			TargetShip.TakeDamage(Damage);
			GD.Print($"{Source?.Name ?? "Projectile"} dealt {Damage} damage to {TargetShip.Name}. HP: {TargetShip.CurrentHealth}/{TargetShip.MaxHealth}  Shields: {TargetShip.CurrentShields}/{TargetShip.MaxShields}");
		}

		// Hide the sprite so only the trail remains visible.
		GetNodeOrNull<Node2D>("Pivot")?.Hide();

		// Drain the trail tail-first toward the impact point, then free.
		_draining = true;
	}

	private static Gradient BuildTrailGradient()
	{
		var gradient = new Gradient();
		gradient.SetColor(1, new Color(1f, 0.85f, 0.3f, 0.9f)); // bright tail
		gradient.SetColor(0, new Color(1f, 0.4f,  0.1f, 0f));   // transparent head
		return gradient;
	}
}
