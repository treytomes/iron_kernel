using System.Drawing;

namespace IronKernel.Userland.Morphic.Events;

public sealed class PointerMoveEvent : MorphicEvent
{
	public Point Position { get; }

	public PointerMoveEvent(Point position)
	{
		Position = position;
	}
}
