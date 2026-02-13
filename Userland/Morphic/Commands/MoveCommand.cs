namespace Userland.Morphic.Commands;

/// <summary>
/// Represents an intent to move a morph by a delta.
/// </summary>
public sealed class MoveCommand : MorphCommand
{
	/// <summary>
	/// Horizontal movement delta.
	/// </summary>
	public int DeltaX { get; }

	/// <summary>
	/// Vertical movement delta.
	/// </summary>
	public int DeltaY { get; }

	public MoveCommand(Morph target, int dx, int dy)
		: base(target)
	{
		DeltaX = dx;
		DeltaY = dy;
	}
}