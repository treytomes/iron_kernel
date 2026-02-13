using System.Drawing;

namespace Userland.Morphic.Events;

public sealed class PointerMoveEvent : MorphicEvent
{
	public Point Position { get; }

	public PointerMoveEvent(Point position)
	{
		Position = position;
	}
}
