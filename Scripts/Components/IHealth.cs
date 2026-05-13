/// <summary>
/// Implemented by any game object that has hit points.
/// </summary>
public interface IHealth
{
	/// <summary>Maximum hit points.</summary>
	int MaxHealth { get; }

	/// <summary>Current hit points. Should never exceed MaxHealth or go below 0.</summary>
	int CurrentHealth { get; set; }

	/// <summary>Whether the object has been reduced to zero hit points.</summary>
	bool IsDead => CurrentHealth <= 0;

	/// <summary>Apply damage, clamping CurrentHealth to a minimum of 0.</summary>
	void TakeDamage(int amount)
	{
		CurrentHealth = System.Math.Max(0, CurrentHealth - amount);
	}

	/// <summary>Restore health, clamping CurrentHealth to a maximum of MaxHealth.</summary>
	void Heal(int amount)
	{
		CurrentHealth = System.Math.Min(MaxHealth, CurrentHealth + amount);
	}
}
