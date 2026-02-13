using IronKernel.Common.ValueObjects;

namespace Userland.Morphic.Events;

public sealed class KeyEvent : MorphicEvent
{
	public KeyEvent(InputAction action, KeyModifier modifiers, Key key)
	{
		Action = action;
		Modifiers = modifiers;
		Key = key;
	}

	public InputAction Action { get; }
	public KeyModifier Modifiers { get; }
	public Key Key { get; }
}
