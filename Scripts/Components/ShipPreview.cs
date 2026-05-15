using Godot;

/// <summary>
/// A clickable component that toggles selection of the enemy ship preview.
/// When selected, makes the EnemyShip node in the PlaySpace visible.
/// </summary>
public partial class ShipPreview : Node2D
{
	/// <summary>Path to the ShipView node this preview controls (set in the editor).</summary>
	[Export] public NodePath LinkedShipViewPath { get; set; }

	public bool IsSelected { get; private set; } = false;
	public bool IsHovered  { get; private set; } = false;

	private Node _tooltip;
	private Ship _enemyShip;

	public override void _Ready()
	{
		_tooltip = GetNodeOrNull("Tooltip");
		if (_tooltip is CanvasItem tooltipItem)
			tooltipItem.Visible = false;

		// Share the main viewport's World2D so the SubViewport renders the same scene.
		var subViewport = GetNodeOrNull<SubViewport>("Sprites/SubViewportContainer/SubViewport");
		if (subViewport != null)
			subViewport.World2D = GetViewport().World2D;

		// Position the sub-camera on the EnemyShip in PlaySpace.
		var camera = GetNodeOrNull<Camera2D>("Sprites/SubViewportContainer/SubViewport/Camera2D");
		_enemyShip = GetTree().Root.GetNodeOrNull<Ship>("PlaySpace/EnemyShip");
		if (camera != null && _enemyShip != null)
			camera.GlobalPosition = _enemyShip.GlobalPosition;

		CustomSignals.Instance.ShipHealthChanged += OnShipHealthChanged;
		UpdateBars();
	}

	public override void _ExitTree()
	{
		CustomSignals.Instance.ShipHealthChanged -= OnShipHealthChanged;
	}

	private void OnShipHealthChanged(Ship ship)
	{
		if (ship == _enemyShip)
			UpdateBars();
	}

	private void UpdateBars()
	{
		if (_enemyShip == null) return;

		if (GetNodeOrNull<Range>("Sprites/Bars/EnemyShipHealth") is Range healthBar)
		{
			healthBar.MinValue = 0;
			healthBar.MaxValue = _enemyShip.MaxHealth;
			healthBar.Value    = _enemyShip.CurrentHealth;
		}
		if (GetNodeOrNull<Label>("Sprites/Bars/EnemyShipHealth/Label") is Label healthLabel)
			healthLabel.Text = $"{_enemyShip.CurrentHealth}/{_enemyShip.MaxHealth}";

		if (GetNodeOrNull<Range>("Sprites/Bars/EnemyShipShields") is Range shieldsBar)
		{
			shieldsBar.MinValue = 0;
			shieldsBar.MaxValue = _enemyShip.MaxShields;
			shieldsBar.Value    = _enemyShip.CurrentShields;
		}
		if (GetNodeOrNull<Label>("Sprites/Bars/EnemyShipShields/Label") is Label shieldsLabel)
			shieldsLabel.Text = $"{_enemyShip.CurrentShields}/{_enemyShip.MaxShields}";
	}

	public void _on_focus_pressed()
	{
		IsSelected = !IsSelected;
		ApplySelection();
	}

	public void _on_focus_mouse_entered()
	{
		IsHovered = true;
		if (_tooltip is CanvasItem tooltipItem)
			tooltipItem.Visible = true;
	}

	public void _on_focus_mouse_exited()
	{
		IsHovered = false;
		if (_tooltip is CanvasItem tooltipItem)
			tooltipItem.Visible = false;
	}

	private void ApplySelection()
	{
		if (LinkedShipViewPath != null && !LinkedShipViewPath.IsEmpty)
		{
			var shipView = GetNodeOrNull<CanvasItem>(LinkedShipViewPath);
			if (shipView != null)
			{
				shipView.Visible = IsSelected;
				return;
			}
		}

		// Fallback: legacy EnemyShip visibility toggle.
		if (_enemyShip != null)
			_enemyShip.Visible = IsSelected;
	}
}
