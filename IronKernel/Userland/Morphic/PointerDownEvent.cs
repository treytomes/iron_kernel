using System.Drawing;
using IronKernel.Common.ValueObjects;

namespace IronKernel.Userland.Morphic;

public sealed class PointerDownEvent(MouseButton button, Point position) : MorphicEvent
{
	public MouseButton Button { get; init; } = button;
	public Point Position { get; init; } = position;
}
