using System.Drawing;
using Userland.Morphic.Halo;

namespace Userland.Morphic.Commands;

/// <summary>
/// Represents an intent to resize a morph from a specific handle.
/// </summary>
public sealed class ResizeCommand : MorphCommand
{
	#region Fields

	// Captured state (minimal)
	private Point _beforePosition;
	private Size _beforeSize;

	// Optional semantic payload (used only by morphs that care).
	private object? _beforeSemanticState;

	#endregion

	#region Constructors

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

	#endregion

	#region Properties

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

	#endregion

	#region Methods

	public override void Execute()
	{
		// Capture generic geometry
		_beforePosition = Target.Position;
		_beforeSize = Target.Size;

		// Allow morph to capture semantic state if needed
		if (Target is ISemanticResizeTarget semantic)
			_beforeSemanticState = semantic.CaptureResizeState();

		base.Execute();
	}

	public override void Undo()
	{
		// Restore semantic state first
		if (Target is ISemanticResizeTarget semantic && _beforeSemanticState != null)
			semantic.RestoreResizeState(_beforeSemanticState);

		// Restore geometry
		Target.Position = _beforePosition;
		Target.Size = _beforeSize;

		Target.Invalidate();
	}

	#endregion
}