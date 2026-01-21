using System.Drawing;

namespace IronKernel.Userland.Morphic;

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
	}

	public override bool WantsKeyboardFocus => false;
	public override bool IsSelectable => false;

	public override void OnPointerDown(PointerDownEvent e)
	{
		StartMouse = e.Position;
		StartPosition = Target.Position;
		StartSize = Target.Size;

		(GetWorld() as WorldMorph)?.CapturePointer(this);
		e.MarkHandled();
	}

	public override void OnPointerUp(PointerUpEvent e)
	{
		(GetWorld() as WorldMorph)?.ReleasePointer(this);
		e.MarkHandled();
	}
}
