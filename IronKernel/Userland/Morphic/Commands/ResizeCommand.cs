using IronKernel.Userland.Morphic.Handles;

namespace IronKernel.Userland.Morphic.Commands;

/// <summary>
/// Represents an intent to resize a morph from a specific handle.
/// </summary>
public sealed class ResizeCommand : MorphCommand
{
	/// <summary>
	/// The handle used to initiate the resize.
	/// </summary>
	public ResizeHandle Handle { get; }

	/// <summary>
	/// Horizontal resize delta.
	/// </summary>
	public int DeltaX { get; }

	/// <summary>
	/// Vertical resize delta.
	/// </summary>
	public int DeltaY { get; }

	public ResizeCommand(
		Morph target,
		ResizeHandle handle,
		int dx,
		int dy)
		: base(target)
	{
		Handle = handle;
		DeltaX = dx;
		DeltaY = dy;
	}
}