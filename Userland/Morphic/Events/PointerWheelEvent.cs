using System.Drawing;

namespace IronKernel.Userland.Morphic.Events;

public sealed class PointerWheelEvent(Point delta) : MorphicEvent
{
	public Point Delta { get; init; } = delta;
}
