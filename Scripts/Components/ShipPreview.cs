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
		var enemyShip = GetTree().Root.GetNodeOrNull<Node2D>("PlaySpace/EnemyShip");
		if (camera != null && enemyShip != null)
			camera.GlobalPosition = enemyShip.GlobalPosition;
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
		Node2D enemyShip = GetTree().Root.GetNodeOrNull<Node2D>("PlaySpace/EnemyShip");
		if (enemyShip != null)
			enemyShip.Visible = IsSelected;
	}
}
