using System.Drawing;
using IronKernel.Common.ValueObjects;

namespace IronKernel.Userland.Morphic;

public sealed class ResizeHandleMorph : HandleMorph
{
	public ResizeHandleMorph(Morph target, ResizeHandle kind)
		: base(target)
	{
		Kind = kind;
		Size = new Size(6, 6);
	}

	public ResizeHandle Kind { get; }

	public override void Draw(IRenderingContext rc)
	{
		rc.RenderFilledRect(
			new Rectangle(Position, Size),
			IsHovered ? RadialColor.Cyan : RadialColor.Cyan.Lerp(RadialColor.White, 0.5f));
	}

	public override void OnPointerMove(PointerMoveEvent e)
	{
		var dx = e.Position.X - StartMouse.X;
		var dy = e.Position.Y - StartMouse.Y;

		var pos = StartPosition;
		var size = StartSize;

		switch (Kind)
		{
			case ResizeHandle.TopLeft:
				pos = new Point(pos.X + dx, pos.Y + dy);
				size = new Size(size.Width - dx, size.Height - dy);
				break;

			case ResizeHandle.TopRight:
				pos = new Point(pos.X, pos.Y + dy);
				size = new Size(size.Width + dx, size.Height - dy);
				break;

			case ResizeHandle.BottomLeft:
				pos = new Point(pos.X + dx, pos.Y);
				size = new Size(size.Width - dx, size.Height + dy);
				break;

			case ResizeHandle.BottomRight:
				size = new Size(size.Width + dx, size.Height + dy);
				break;
		}

		Target.Position = pos;
		Target.Size = new Size(
			Math.Max(1, size.Width),
			Math.Max(1, size.Height));

		e.MarkHandled();
	}
}
