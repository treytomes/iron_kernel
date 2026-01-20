using System.Drawing;

namespace IronKernel.Morphic;

public sealed class PointerDownEvent : MorphicEvent
{
	public Point Position { get; }

	public PointerDownEvent(Point position)
	{
		Position = position;
	}
}
