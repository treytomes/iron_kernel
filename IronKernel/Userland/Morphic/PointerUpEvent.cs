using System.Drawing;

namespace IronKernel.Morphic;

public sealed class PointerUpEvent : MorphicEvent
{
	public Point Position { get; }

	public PointerUpEvent(Point position)
	{
		Position = position;
	}
}
