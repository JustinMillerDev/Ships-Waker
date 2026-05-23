using Godot;

/// <summary>
/// Attach to the Camera2D node that owns Background1–Background9.
/// Scrolls all nine sprites downward and wraps them seamlessly so there
/// is never a visible gap.
/// </summary>
public partial class BackgroundScroller : Camera2D
{
	/// <summary>Scroll speed in pixels per second (Camera2D local space).</summary>
	[Export] public float ScrollSpeed { get; set; } = 40f;

	// The y-distance between adjacent rows of tiles.
	private const float TileHeight = 1440f;
	// 4 rows tall → total grid height used for wrapping.
	private const float TotalHeight = TileHeight * 4f; // 5760
	// Wrap a sprite back to the top once it reaches or passes this y.
	private const float WrapThreshold = 2306f; // 4320

	private Sprite2D[] _sprites;

	public override void _Ready()
	{
		// Sprites are direct children of this Camera2D node (Background1 – Background12).
		_sprites = new Sprite2D[12];
		for (int i = 0; i < 12; i++)
		{
			string name = $"Background{i + 1}";
			_sprites[i] = GetNodeOrNull<Sprite2D>(name);
			if (_sprites[i] == null)
				GD.PushWarning($"BackgroundScroller: could not find child node '{name}'.");
		}
	}

	public override void _Process(double delta)
	{
		float dy = ScrollSpeed * (float)delta;

		foreach (Sprite2D sprite in _sprites)
		{
			if (sprite == null) continue;

			sprite.Position += new Vector2(0f, dy);

			// When the sprite has scrolled past the bottom of the 4-row grid,
			// jump it back to the equivalent top position to loop seamlessly.
			if (sprite.Position.Y >= WrapThreshold)
				sprite.Position = new Vector2(sprite.Position.X, sprite.Position.Y - TotalHeight);
		}
	}
}
