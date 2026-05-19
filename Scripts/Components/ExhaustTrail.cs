using Godot;
using System.Collections.Generic;

/// <summary>
/// Animates a Line2D as a rocket exhaust trail that streams downward and
/// curves left/right to simulate high-speed turbulence, even when the
/// rocket itself is stationary.
/// </summary>
public partial class ExhaustTrail : Node2D
{
	/// <summary>How many points the trail holds.</summary>
	[Export] public int TrailLength { get; set; } = 1500;

	/// <summary>Downward speed of trail points in local pixels per second.</summary>
	[Export] public float FallSpeed { get; set; } = 720f;

	/// <summary>Maximum horizontal sway amplitude in pixels.</summary>
	[Export] public float SwayAmplitude { get; set; } = 4f;

	/// <summary>How quickly the sway oscillates (radians per second).</summary>
	[Export] public float SwayFrequency { get; set; } = 3f;

	/// <summary>Width of the line at its origin (thickest point).</summary>
	[Export] public float LineWidth { get; set; } = 12f;

	// -------------------------------------------------------------------------

	private Line2D _line;
	private readonly List<Vector2> _points = new();
	private float _time = 0f;
	// Per-point age used to offset sway so each point sways independently.
	private readonly List<float> _ages = new();

	public override void _Ready()
	{
		_line = new Line2D();
		_line.Width = LineWidth;
		_line.DefaultColor = new Color(1, 1, 1, 1);

		// Gradient: white opaque at origin, fully transparent at tail.
		var gradient = new Gradient();
		gradient.Colors = new Color[] { new Color(1, 1, 1, 0.9f), new Color(1, 1, 1, 0f) };
		_line.Gradient = gradient;

		AddChild(_line);

		// Seed the trail so it doesn't pop in from empty.
		for (int i = 0; i < TrailLength; i++)
		{
			float age = i * (1f / TrailLength) * (TrailLength / FallSpeed);
			_points.Add(SamplePoint(age));
			_ages.Add(age);
		}
	}

	public override void _Process(double delta)
	{
		float dt = (float)delta;
		_time += dt;

		// Age every existing point and shift them downward.
		for (int i = 0; i < _points.Count; i++)
		{
			_ages[i] += dt;
			_points[i] = SamplePoint(_ages[i]);
		}

		// Remove points that have fallen past the max trail length.
		float maxAge = TrailLength / FallSpeed;
		while (_ages.Count > 0 && _ages[_ages.Count - 1] > maxAge)
		{
			_points.RemoveAt(_points.Count - 1);
			_ages.RemoveAt(_ages.Count - 1);
		}

		// Always keep the origin point at (0, 0) at the front.
		_points.Insert(0, Vector2.Zero);
		_ages.Insert(0, 0f);

		// Trim to target length.
		while (_points.Count > TrailLength)
		{
			_points.RemoveAt(_points.Count - 1);
			_ages.RemoveAt(_ages.Count - 1);
		}

		// Write to the Line2D.
		_line.ClearPoints();
		foreach (var pt in _points)
			_line.AddPoint(pt);
	}

	/// <summary>
	/// Returns the local position of a trail point at <paramref name="age"/> seconds old.
	/// Points fall downward and sway horizontally with a sine wave.
	/// Two overlapping sine waves at different frequencies give an organic look.
	/// </summary>
	private Vector2 SamplePoint(float age)
	{
		float y = age * FallSpeed;

		// Two sine waves for a more turbulent, organic curve.
		float sway = Mathf.Sin(_time * SwayFrequency - age * 6f) * SwayAmplitude
		           + Mathf.Sin(_time * SwayFrequency * 1.7f - age * 9f) * SwayAmplitude * 0.4f;

		return new Vector2(sway, y);
	}
}
