using IronKernel.Common.ValueObjects;

namespace IronKernel.Userland.Morphic.Events;

public static class KeyEventExtensions
{
	public static char? ToText(this KeyEvent e)
	{
		if (e.Action != InputAction.Press)
			return null;

		var shift = e.Modifiers.HasFlag(KeyModifier.Shift);
		var caps = e.Modifiers.HasFlag(KeyModifier.CapsLock);

		// Letters A–Z
		if (e.Key >= Key.A && e.Key <= Key.Z)
		{
			var c = (char)('a' + (e.Key - Key.A));
			var upper = shift ^ caps;
			return upper ? char.ToUpperInvariant(c) : c;
		}

		// Digits 0–9
		if (e.Key >= Key.D0 && e.Key <= Key.D9)
		{
			return shift
				? ShiftedDigit(e.Key)
				: (char)('0' + (e.Key - Key.D0));
		}

		// Whitespace
		if (e.Key == Key.Space)
			return ' ';

		// Punctuation
		return shift
			? ShiftedPunctuation(e.Key)
			: UnshiftedPunctuation(e.Key);
	}

	private static char? ShiftedDigit(Key key) => key switch
	{
		Key.D0 => ')',
		Key.D1 => '!',
		Key.D2 => '@',
		Key.D3 => '#',
		Key.D4 => '$',
		Key.D5 => '%',
		Key.D6 => '^',
		Key.D7 => '&',
		Key.D8 => '*',
		Key.D9 => '(',
		_ => null
	};

	private static char? UnshiftedPunctuation(Key key) => key switch
	{
		Key.Minus => '-',
		Key.Equal => '=',
		Key.LeftBracket => '[',
		Key.RightBracket => ']',
		Key.Backslash => '\\',
		Key.Semicolon => ';',
		Key.Apostrophe => '\'',
		Key.Comma => ',',
		Key.Period => '.',
		Key.Slash => '/',
		Key.GraveAccent => '`',
		_ => null
	};

	private static char? ShiftedPunctuation(Key key) => key switch
	{
		Key.D1 => '!',
		Key.D2 => '@',
		Key.D3 => '#',
		Key.D4 => '$',
		Key.D5 => '%',
		Key.D6 => '^',
		Key.D7 => '&',
		Key.D8 => '*',
		Key.D9 => '(',
		Key.D0 => ')',

		Key.Minus => '_',
		Key.Equal => '+',
		Key.LeftBracket => '{',
		Key.RightBracket => '}',
		Key.Backslash => '|',
		Key.Semicolon => ':',
		Key.Apostrophe => '"',
		Key.Comma => '<',
		Key.Period => '>',
		Key.Slash => '?',
		Key.GraveAccent => '~',

		_ => null
	};
}