/// <summary>
/// Implement on any node that can face a cardinal direction.
/// </summary>
public interface IOrientation
{
	/// <summary>The direction this node is currently facing.</summary>
	CardinalDirection Facing { get; set; }
}
