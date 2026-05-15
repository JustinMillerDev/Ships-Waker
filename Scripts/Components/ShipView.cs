using Godot;

/// <summary>
/// Scales the ShipView node and its SubViewportContainer to match the
/// PlaySpace camera zoom so they stay visually consistent as the player zooms.
/// </summary>
public partial class ShipView : Node2D
{
	private SubViewportContainer _subViewportContainer;
	private PlaySpace _playSpace;

	public override void _Ready()
	{
		_subViewportContainer = GetNodeOrNull<SubViewportContainer>("Sprites/SubViewportContainer");

		// Share the main viewport's World2D so the SubViewport renders the same scene.
		var subViewport = GetNodeOrNull<SubViewport>("Sprites/SubViewportContainer/SubViewport");
		if (subViewport != null)
			subViewport.World2D = GetViewport().World2D;
		

		_playSpace = GetTree().Root.GetNodeOrNull<PlaySpace>("PlaySpace");
		if (_playSpace != null)
			_playSpace.ZoomChanged += OnZoomChanged;

		// Start at scale 1 with the position matching the default (0.75) zoom level.
		Scale = Vector2.One;
		Position = new Vector2(160f, 90f);
	}

	public override void _ExitTree()
	{
		if (_playSpace != null)
			_playSpace.ZoomChanged -= OnZoomChanged;
	}

	private void OnZoomChanged(Vector2 zoom)
	{
		// Camera zoom levels (0.25, 0.5, 0.75) map to preview zoom (0.5, 1.0, 1.5).
		Vector2 previewZoom = zoom * 2f;
		Scale = previewZoom;
		if (_subViewportContainer != null)
			_subViewportContainer.Scale = new Vector2(0.5f, 0.5f);

		Position = Mathf.RoundToInt(zoom.X * 100) switch
		{
			25  => new Vector2(240, 135),
			50  => new Vector2(160, 90),
			75  => new Vector2(80, 45),
			_   => Position,
		};
	}
}
