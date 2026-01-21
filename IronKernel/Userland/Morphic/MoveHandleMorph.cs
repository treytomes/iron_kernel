using System.Drawing;
using IronKernel.Common.ValueObjects;

namespace IronKernel.Userland.Morphic;

public sealed class MoveHandleMorph : HandleMorph
{
	public MoveHandleMorph(Morph target)
		: base(target)
	{
		Size = new Size(8, 8);
	}

	public override void Draw(IRenderingContext rc)
	{
		rc.RenderFilledRect(
			new Rectangle(Position, Size),
			IsHovered ? RadialColor.Orange : RadialColor.Orange.Lerp(RadialColor.White, 0.5f));
	}

	public override void OnPointerMove(PointerMoveEvent e)
	{
		var dx = e.Position.X - StartMouse.X;
		var dy = e.Position.Y - StartMouse.Y;

		Target.Position = new Point(
			StartPosition.X + dx,
			StartPosition.Y + dy);

		e.MarkHandled();
	}
}
