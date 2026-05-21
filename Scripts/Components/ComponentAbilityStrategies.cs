using Godot;
using System.Collections.Generic;

// ---------------------------------------------------------------------------
// Strategy interface
// ---------------------------------------------------------------------------

public interface IComponentAbilityStrategy
{
	void EndOfTurnExecute(ShipComponent component);
	void OnFireExecute(ShipComponent component);
}

// ---------------------------------------------------------------------------
// Concrete strategies
// ---------------------------------------------------------------------------

public sealed class BaseEvasionStrategy      : IComponentAbilityStrategy { public void EndOfTurnExecute(ShipComponent c) => GD.Print($"{c.Name}: BaseEvasion end-of-turn.");    public void OnFireExecute(ShipComponent c) => GD.Print($"{c.Name}: BaseEvasion fired."); }
public sealed class NoSpeedStrategy        : IComponentAbilityStrategy { public void EndOfTurnExecute(ShipComponent c) => GD.Print($"{c.Name}: BaseSpeed end-of-turn.");     public void OnFireExecute(ShipComponent c) => GD.Print($"{c.Name}: BaseSpeed fired."); }
public sealed class BaseShieldsStrategy      : IComponentAbilityStrategy { public void EndOfTurnExecute(ShipComponent c) => GD.Print($"{c.Name}: BaseShields end-of-turn.");    public void OnFireExecute(ShipComponent c) => GD.Print($"{c.Name}: BaseShields fired."); }
public sealed class BaseCrewDeployStrategy   : IComponentAbilityStrategy { public void EndOfTurnExecute(ShipComponent c) => GD.Print($"{c.Name}: BaseCrewDeploy end-of-turn."); public void OnFireExecute(ShipComponent c) => GD.Print($"{c.Name}: BaseCrewDeploy fired."); }
public sealed class BaseCargoDeployStrategy  : IComponentAbilityStrategy { public void EndOfTurnExecute(ShipComponent c) => GD.Print($"{c.Name}: BaseCargoDeploy end-of-turn.");public void OnFireExecute(ShipComponent c) => GD.Print($"{c.Name}: BaseCargoDeploy fired."); }
public sealed class BaseCannonStrategy       : IComponentAbilityStrategy { public void EndOfTurnExecute(ShipComponent c) { GD.Print($"{c.Name}: BaseCannon end-of-turn. Cooldown unchanged ({c.CurrentCooldown})."); } public void OnFireExecute(ShipComponent c) => CannonFireHelper.Fire(c); }
public sealed class BaseScannersStrategy     : IComponentAbilityStrategy { public void EndOfTurnExecute(ShipComponent c) => GD.Print($"{c.Name}: BaseScanners end-of-turn.");  public void OnFireExecute(ShipComponent c) => GD.Print($"{c.Name}: BaseScanners fired."); }

public sealed class LowEvasionStrategy       : IComponentAbilityStrategy { public void EndOfTurnExecute(ShipComponent c) => GD.Print($"{c.Name}: LowEvasion end-of-turn.");    public void OnFireExecute(ShipComponent c) => GD.Print($"{c.Name}: LowEvasion fired."); }
public sealed class LowSpeedStrategy        : IComponentAbilityStrategy { public void EndOfTurnExecute(ShipComponent c) => GD.Print($"{c.Name}: LowSpeed end-of-turn.");     public void OnFireExecute(ShipComponent c) => GD.Print($"{c.Name}: LowSpeed fired."); }
public sealed class LowShieldsStrategy       : IComponentAbilityStrategy { public void EndOfTurnExecute(ShipComponent c) => GD.Print($"{c.Name}: LowShields end-of-turn.");    public void OnFireExecute(ShipComponent c) => GD.Print($"{c.Name}: LowShields fired."); }
public sealed class LowCrewDeployStrategy    : IComponentAbilityStrategy { public void EndOfTurnExecute(ShipComponent c) => GD.Print($"{c.Name}: LowCrewDeploy end-of-turn."); public void OnFireExecute(ShipComponent c) => GD.Print($"{c.Name}: LowCrewDeploy fired."); }
public sealed class LowCargoDeployStrategy   : IComponentAbilityStrategy { public void EndOfTurnExecute(ShipComponent c) => GD.Print($"{c.Name}: LowCargoDeploy end-of-turn.");public void OnFireExecute(ShipComponent c) => GD.Print($"{c.Name}: LowCargoDeploy fired."); }
public sealed class LowCannonStrategy        : IComponentAbilityStrategy { public void EndOfTurnExecute(ShipComponent c) { c.CurrentCooldown = Mathf.Max(0, c.CurrentCooldown - 1); GD.Print($"{c.Name}: LowCannon end-of-turn. Cooldown reduced to {c.CurrentCooldown}."); CustomSignals.Instance.EmitSignal(CustomSignals.SignalName.CooldownReduced, c); } public void OnFireExecute(ShipComponent c) => CannonFireHelper.Fire(c); }
public sealed class LowScannersStrategy      : IComponentAbilityStrategy { public void EndOfTurnExecute(ShipComponent c) => GD.Print($"{c.Name}: LowScanners end-of-turn.");  public void OnFireExecute(ShipComponent c) => GD.Print($"{c.Name}: LowScanners fired."); }

public sealed class MediumEvasionStrategy    : IComponentAbilityStrategy { public void EndOfTurnExecute(ShipComponent c) => GD.Print($"{c.Name}: MediumEvasion end-of-turn.");    public void OnFireExecute(ShipComponent c) => GD.Print($"{c.Name}: MediumEvasion fired."); }
public sealed class MediumSpeedStrategy     : IComponentAbilityStrategy { public void EndOfTurnExecute(ShipComponent c) => GD.Print($"{c.Name}: MediumSpeed end-of-turn.");     public void OnFireExecute(ShipComponent c) => GD.Print($"{c.Name}: MediumSpeed fired."); }
public sealed class MediumShieldsStrategy    : IComponentAbilityStrategy { public void EndOfTurnExecute(ShipComponent c) => GD.Print($"{c.Name}: MediumShields end-of-turn.");    public void OnFireExecute(ShipComponent c) => GD.Print($"{c.Name}: MediumShields fired."); }
public sealed class MediumCrewDeployStrategy : IComponentAbilityStrategy { public void EndOfTurnExecute(ShipComponent c) => GD.Print($"{c.Name}: MediumCrewDeploy end-of-turn."); public void OnFireExecute(ShipComponent c) => GD.Print($"{c.Name}: MediumCrewDeploy fired."); }
public sealed class MediumCargoDeployStrategy: IComponentAbilityStrategy { public void EndOfTurnExecute(ShipComponent c) => GD.Print($"{c.Name}: MediumCargoDeploy end-of-turn.");public void OnFireExecute(ShipComponent c) => GD.Print($"{c.Name}: MediumCargoDeploy fired."); }
public sealed class MediumCannonStrategy     : IComponentAbilityStrategy { public void EndOfTurnExecute(ShipComponent c) { c.CurrentCooldown = Mathf.Max(0, c.CurrentCooldown - 3); GD.Print($"{c.Name}: MediumCannon end-of-turn. Cooldown reduced to {c.CurrentCooldown}."); CustomSignals.Instance.EmitSignal(CustomSignals.SignalName.CooldownReduced, c); } public void OnFireExecute(ShipComponent c) => CannonFireHelper.Fire(c); }
public sealed class MediumScannersStrategy   : IComponentAbilityStrategy { public void EndOfTurnExecute(ShipComponent c) => GD.Print($"{c.Name}: MediumScanners end-of-turn.");  public void OnFireExecute(ShipComponent c) => GD.Print($"{c.Name}: MediumScanners fired."); }

public sealed class HighEvasionStrategy      : IComponentAbilityStrategy { public void EndOfTurnExecute(ShipComponent c) => GD.Print($"{c.Name}: HighEvasion end-of-turn.");    public void OnFireExecute(ShipComponent c) => GD.Print($"{c.Name}: HighEvasion fired."); }
public sealed class HighSpeedStrategy        : IComponentAbilityStrategy { public void EndOfTurnExecute(ShipComponent c) => GD.Print($"{c.Name}: HighSpeed end-of-turn.");     public void OnFireExecute(ShipComponent c) => GD.Print($"{c.Name}: HighSpeed fired."); }
public sealed class HighShieldsStrategy      : IComponentAbilityStrategy { public void EndOfTurnExecute(ShipComponent c) => GD.Print($"{c.Name}: HighShields end-of-turn.");    public void OnFireExecute(ShipComponent c) => GD.Print($"{c.Name}: HighShields fired."); }
public sealed class HighCrewDeployStrategy   : IComponentAbilityStrategy { public void EndOfTurnExecute(ShipComponent c) => GD.Print($"{c.Name}: HighCrewDeploy end-of-turn."); public void OnFireExecute(ShipComponent c) => GD.Print($"{c.Name}: HighCrewDeploy fired."); }
public sealed class HighCargoDeployStrategy  : IComponentAbilityStrategy { public void EndOfTurnExecute(ShipComponent c) => GD.Print($"{c.Name}: HighCargoDeploy end-of-turn.");public void OnFireExecute(ShipComponent c) => GD.Print($"{c.Name}: HighCargoDeploy fired."); }
public sealed class HighCannonStrategy       : IComponentAbilityStrategy { public void EndOfTurnExecute(ShipComponent c) { c.CurrentCooldown = Mathf.Max(0, c.CurrentCooldown - 6); GD.Print($"{c.Name}: HighCannon end-of-turn. Cooldown reduced to {c.CurrentCooldown}."); CustomSignals.Instance.EmitSignal(CustomSignals.SignalName.CooldownReduced, c); } public void OnFireExecute(ShipComponent c) => CannonFireHelper.Fire(c); }

public sealed class MaxShieldsStrategy              : IComponentAbilityStrategy { public void EndOfTurnExecute(ShipComponent c) => GD.Print($"{c.Name}: MaxShields end-of-turn.");    public void OnFireExecute(ShipComponent c) => GD.Print($"{c.Name}: MaxShields fired."); }

public sealed class BaseMissileLauncherStrategy  : IComponentAbilityStrategy { public void EndOfTurnExecute(ShipComponent c) { GD.Print($"{c.Name}: BaseMissileLauncher end-of-turn. Cooldown unchanged ({c.CurrentCooldown})."); } public void OnFireExecute(ShipComponent c) => CannonFireHelper.Fire(c); }
public sealed class LowMissileLauncherStrategy   : IComponentAbilityStrategy { public void EndOfTurnExecute(ShipComponent c) { c.CurrentCooldown = Mathf.Max(0, c.CurrentCooldown - 1); GD.Print($"{c.Name}: LowMissileLauncher end-of-turn. Cooldown reduced to {c.CurrentCooldown}."); CustomSignals.Instance.EmitSignal(CustomSignals.SignalName.CooldownReduced, c); } public void OnFireExecute(ShipComponent c) => CannonFireHelper.Fire(c); }
public sealed class MediumMissileLauncherStrategy: IComponentAbilityStrategy { public void EndOfTurnExecute(ShipComponent c) { c.CurrentCooldown = Mathf.Max(0, c.CurrentCooldown - 3); GD.Print($"{c.Name}: MediumMissileLauncher end-of-turn. Cooldown reduced to {c.CurrentCooldown}."); CustomSignals.Instance.EmitSignal(CustomSignals.SignalName.CooldownReduced, c); } public void OnFireExecute(ShipComponent c) => CannonFireHelper.Fire(c); }
public sealed class HighMissileLauncherStrategy  : IComponentAbilityStrategy { public void EndOfTurnExecute(ShipComponent c) { c.CurrentCooldown = Mathf.Max(0, c.CurrentCooldown - 6); GD.Print($"{c.Name}: HighMissileLauncher end-of-turn. Cooldown reduced to {c.CurrentCooldown}."); CustomSignals.Instance.EmitSignal(CustomSignals.SignalName.CooldownReduced, c); } public void OnFireExecute(ShipComponent c) => CannonFireHelper.Fire(c); }

public sealed class BaseRailGunStrategy   : IComponentAbilityStrategy { public void EndOfTurnExecute(ShipComponent c) { GD.Print($"{c.Name}: BaseRailGun end-of-turn. Cooldown unchanged ({c.CurrentCooldown})."); } public void OnFireExecute(ShipComponent c) => CannonFireHelper.Fire(c); }
public sealed class LowRailGunStrategy    : IComponentAbilityStrategy { public void EndOfTurnExecute(ShipComponent c) { c.CurrentCooldown = Mathf.Max(0, c.CurrentCooldown - 1); GD.Print($"{c.Name}: LowRailGun end-of-turn. Cooldown reduced to {c.CurrentCooldown}."); CustomSignals.Instance.EmitSignal(CustomSignals.SignalName.CooldownReduced, c); } public void OnFireExecute(ShipComponent c) => CannonFireHelper.Fire(c); }
public sealed class MediumRailGunStrategy : IComponentAbilityStrategy { public void EndOfTurnExecute(ShipComponent c) { c.CurrentCooldown = Mathf.Max(0, c.CurrentCooldown - 3); GD.Print($"{c.Name}: MediumRailGun end-of-turn. Cooldown reduced to {c.CurrentCooldown}."); CustomSignals.Instance.EmitSignal(CustomSignals.SignalName.CooldownReduced, c); } public void OnFireExecute(ShipComponent c) => CannonFireHelper.Fire(c); }
public sealed class HighRailGunStrategy   : IComponentAbilityStrategy { public void EndOfTurnExecute(ShipComponent c) { c.CurrentCooldown = Mathf.Max(0, c.CurrentCooldown - 6); GD.Print($"{c.Name}: HighRailGun end-of-turn. Cooldown reduced to {c.CurrentCooldown}."); CustomSignals.Instance.EmitSignal(CustomSignals.SignalName.CooldownReduced, c); } public void OnFireExecute(ShipComponent c) => CannonFireHelper.Fire(c); }

// ---------------------------------------------------------------------------
// Shared cannon fire logic
// ---------------------------------------------------------------------------

public static class CannonFireHelper
{
	public static void Fire(ShipComponent c)
	{
		ShipSmall enemy = c.GetTree().Root.GetNodeOrNull<ShipSmall>("PlaySpace/EnemyShipSmall");
		if (enemy == null)
		{
			GD.PushWarning($"{c.Name}: CannonFire — no EnemyShipSmall found in scene tree.");
			return;
		}
		GD.Print($"{c.Data.Name}: Cannon fired at EnemyShipSmall.");
		int damage = c.Data?.Strength ?? 0;
		GD.Print("Damage calculated: " + damage);
		Node2D targetPoint = enemy.GetNodeOrNull<Node2D>("Pivot/Anchors/TargetPoint");
		if (targetPoint != null)
			ProjectileManager.Instance.Fire(c, enemy, targetPoint.GlobalPosition, damage);
		else
			ProjectileManager.Instance.Fire(c, enemy, null, damage);

		c.CurrentCooldown = c.Data?.Cooldown ?? c.CurrentCooldown;
		CustomSignals.Instance.EmitSignal(CustomSignals.SignalName.CooldownReduced, c);
		GD.Print($"{c.Name}: cannon fired. Cooldown reset to {c.CurrentCooldown}.");
	}
}

// ---------------------------------------------------------------------------
// Singleton registry — maps ComponentAbility → IComponentAbilityStrategy
// ---------------------------------------------------------------------------

public sealed class ComponentAbilityRegistry
{
	public static ComponentAbilityRegistry Instance { get; } = new();

	private readonly Dictionary<ComponentAbility, IComponentAbilityStrategy> _strategies = new()
	{
		{ ComponentAbility.BaseEvasion,       new BaseEvasionStrategy()       },
		{ ComponentAbility.NoSpeed,           new NoSpeedStrategy()        },
		{ ComponentAbility.BaseShields,       new BaseShieldsStrategy()       },
		{ ComponentAbility.BaseCrewDeploy,    new BaseCrewDeployStrategy()    },
		{ ComponentAbility.BaseCargoDeploy,   new BaseCargoDeployStrategy()   },
		{ ComponentAbility.BaseCannon,        new BaseCannonStrategy()        },
		{ ComponentAbility.BaseScanners,      new BaseScannersStrategy()      },
		{ ComponentAbility.LowEvasion,        new LowEvasionStrategy()        },
		{ ComponentAbility.LowSpeed,          new LowSpeedStrategy()         },
		{ ComponentAbility.LowShields,        new LowShieldsStrategy()        },
		{ ComponentAbility.LowCrewDeploy,     new LowCrewDeployStrategy()     },
		{ ComponentAbility.LowCargoDeploy,    new LowCargoDeployStrategy()    },
		{ ComponentAbility.LowCannon,         new LowCannonStrategy()         },
		{ ComponentAbility.LowScanners,       new LowScannersStrategy()       },
		{ ComponentAbility.MediumEvasion,     new MediumEvasionStrategy()     },
		{ ComponentAbility.MediumSpeed,       new MediumSpeedStrategy()      },
		{ ComponentAbility.MediumShields,     new MediumShieldsStrategy()     },
		{ ComponentAbility.MediumCrewDeploy,  new MediumCrewDeployStrategy()  },
		{ ComponentAbility.MediumCargoDeploy, new MediumCargoDeployStrategy() },
		{ ComponentAbility.MediumCannon,      new MediumCannonStrategy()      },
		{ ComponentAbility.MediumScanners,    new MediumScannersStrategy()    },
		{ ComponentAbility.HighEvasion,       new HighEvasionStrategy()       },
		{ ComponentAbility.HighSpeed,         new HighSpeedStrategy()        },
		{ ComponentAbility.HighShields,       new HighShieldsStrategy()       },
		{ ComponentAbility.HighCrewDeploy,    new HighCrewDeployStrategy()    },
		{ ComponentAbility.HighCargoDeploy,   new HighCargoDeployStrategy()   },
		{ ComponentAbility.HighCannon,        new HighCannonStrategy()        },
		{ ComponentAbility.MaxShields,             new MaxShieldsStrategy()             },
		{ ComponentAbility.BaseMissileLauncher,  new BaseMissileLauncherStrategy()  },
		{ ComponentAbility.LowMissileLauncher,   new LowMissileLauncherStrategy()   },
		{ ComponentAbility.MediumMissileLauncher,new MediumMissileLauncherStrategy() },
		{ ComponentAbility.HighMissileLauncher,  new HighMissileLauncherStrategy()  },
		{ ComponentAbility.BaseRailGun,          new BaseRailGunStrategy()          },
		{ ComponentAbility.LowRailGun,           new LowRailGunStrategy()           },
		{ ComponentAbility.MediumRailGun,        new MediumRailGunStrategy()        },
		{ ComponentAbility.HighRailGun,          new HighRailGunStrategy()          },
	};

	private ComponentAbilityRegistry() { }

	public void EndOfTurnExecute(ComponentAbility ability, ShipComponent component)
	{
		if (_strategies.TryGetValue(ability, out IComponentAbilityStrategy strategy))
			strategy.EndOfTurnExecute(component);
		else
			GD.PushWarning($"ComponentAbilityRegistry: No strategy registered for {ability}.");
	}

	public void OnFireExecute(ComponentAbility ability, ShipComponent component)
	{
		if (_strategies.TryGetValue(ability, out IComponentAbilityStrategy strategy))
			strategy.OnFireExecute(component);
		else
			GD.PushWarning($"ComponentAbilityRegistry: No strategy registered for {ability}.");
	}
}
