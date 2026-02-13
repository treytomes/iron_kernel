using System.Drawing;
using IronKernel.Common.ValueObjects;

namespace Userland.Morphic.Events;

public sealed class PointerDownEvent(MouseButton button, Point position, KeyModifier modifiers) : MorphicEvent
{
	public MouseButton Button { get; init; } = button;
	public Point Position { get; init; } = position;
	public KeyModifier Modifiers { get; init; } = modifiers;
}
