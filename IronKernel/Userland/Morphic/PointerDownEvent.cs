using System.Drawing;

namespace IronKernel.Userland.Morphic;

public sealed class PointerDownEvent : MorphicEvent
{
	public Point Position { get; }

	public PointerDownEvent(Point position)
	{
		Position = position;
	}
}
