using Godot;

/// <summary>
/// Sidebar UI entry that displays a summary of an assigned ShipComponent.
/// </summary>
public partial class SideBarComponent : Control
{
	[Export] public ShipComponentSlot ComponentSlot { get; set; }

	public override void _Ready()
	{
		CustomSignals.Instance.CargoSlotted += OnCargoSlotted;
		CustomSignals.Instance.CooldownReduced += OnCooldownReduced;
        UpdateLabel();
	}

	public override void _ExitTree()
	{
		CustomSignals.Instance.CargoSlotted -= OnCargoSlotted;
		CustomSignals.Instance.CooldownReduced -= OnCooldownReduced;
	}

	private void OnCargoSlotted(Cargo cargo)
	{
		UpdateLabel();
	}

	private void OnCooldownReduced(ShipComponent component)
	{
		if (ComponentSlot?.OccupiedBy == component)
			UpdateLabel();
	}

	public void UpdateLabel()
	{
		if (GetNodeOrNull("Label") is not Label label)
			return;

		if (ComponentSlot?.OccupiedBy is not ShipComponent component)
		{
			label.Text = string.Empty;
			return;
		}

		string name = component.Data?.Name ?? component.Name;
        GD.Print("Updating label for component: " + name);
		string maxCooldown = component.Data?.Cooldown.HasValue == true ? component.Data.Cooldown.Value.ToString() : "?";
		label.Text = $"{name}\nCooldown: {component.CurrentCooldown}/{maxCooldown}\nAmmo: {component.Ammo}";
	}

	private void _on_button_pressed()
	{
		if (ComponentSlot?.OccupiedBy is not ShipComponent component) return;
		if (component.CurrentCooldown != 0) return;
		if (component.ActiveAbility is not ComponentAbility ability) return;
		ComponentAbilityRegistry.Instance.OnFireExecute(ability, component);
	}

	private void _on_focus_mouse_entered()
	{
		if (ComponentSlot != null)
			ComponentSlot._on_focus_mouse_entered();
	}

	private void _on_focus_mouse_exited()
	{
		if (ComponentSlot != null)
			ComponentSlot._on_focus_mouse_exited();
	}
}
