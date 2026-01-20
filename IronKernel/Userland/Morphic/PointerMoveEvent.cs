using System.Drawing;

namespace IronKernel.Morphic;

public sealed class PointerMoveEvent : MorphicEvent
{
	public Point Position { get; }

	public PointerMoveEvent(Point position)
	{
		Position = position;
	}
}
