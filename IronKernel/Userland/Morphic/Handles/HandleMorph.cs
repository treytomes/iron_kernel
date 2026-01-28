using System.Drawing;
using IronKernel.Userland.Morphic.Events;

namespace IronKernel.Userland.Morphic.Handles;

public abstract class HandleMorph : Morph
{
	protected readonly Morph Target;

	protected Point StartMouse;
	protected Point StartPosition;
	protected Size StartSize;

	protected HandleMorph(Morph target)
	{
		Target = target;
		Visible = true;
		IsSelectable = false;
	}

	public override bool WantsKeyboardFocus => false;
	public override bool IsGrabbable => false;
	protected abstract MorphicStyle.HandleStyle? StyleForHandle { get; }

	public override void OnPointerDown(PointerDownEvent e)
	{
		if (!TryGetWorld(out var world)) return;

		StartMouse = e.Position;
		StartPosition = Target.Position;
		StartSize = Target.Size;

		world.CapturePointer(this);
		e.MarkHandled();
	}

	public override void OnPointerUp(PointerUpEvent e)
	{
		if (TryGetWorld(out var world)) world.ReleasePointer(this);
		e.MarkHandled();
	}
}
