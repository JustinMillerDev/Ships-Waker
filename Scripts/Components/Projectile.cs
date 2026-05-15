using Godot;

/// <summary>
/// A projectile fired by a ship component toward a target ship (or a specific component on that ship).
/// Created and launched exclusively by <see cref="ProjectileManager"/>.
/// </summary>
public partial class Projectile : Node2D
{
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

		QueueFree();
	}
}
