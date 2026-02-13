using System.Drawing;
using IronKernel.Userland.Morphic;
using IronKernel.Userland.Morphic.Events;

namespace IronKernel.Userland.MiniMacro;

public class DraggableMorph : Morph
{
	private bool _dragging;
	private Point _offset;

	public override void OnPointerDown(PointerDownEvent e)
	{
		base.OnPointerDown(e);

		_dragging = true;
		_offset = new Point(
			e.Position.X - Position.X,
			e.Position.Y - Position.Y);

		e.MarkHandled();
	}

	public override void OnPointerMove(PointerMoveEvent e)
	{
		base.OnPointerMove(e);

		if (_dragging)
		{
			Position = new Point(
				e.Position.X - _offset.X,
				e.Position.Y - _offset.Y);

			e.MarkHandled();
		}
	}

	public override void OnPointerUp(PointerUpEvent e)
	{
		base.OnPointerUp(e);

		_dragging = false;
		e.MarkHandled();
	}
}
