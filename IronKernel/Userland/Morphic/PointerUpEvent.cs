using System.Drawing;

namespace IronKernel.Userland.Morphic;

public sealed class PointerUpEvent : MorphicEvent
{
	public Point Position { get; }

	public PointerUpEvent(Point position)
	{
		Position = position;
	}
}
