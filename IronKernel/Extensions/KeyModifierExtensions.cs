using IronKernel.Common.ValueObjects;

namespace IronKernel;

public static class KeyModifierExtensions
{
	public static KeyModifier ToHost(this OpenTK.Windowing.GraphicsLibraryFramework.KeyModifiers @this)
	{
		var modifiers = KeyModifier.None;
		if ((@this & OpenTK.Windowing.GraphicsLibraryFramework.KeyModifiers.Shift) != 0)
		{
			modifiers = modifiers | KeyModifier.Shift;
		}
		if ((@this & OpenTK.Windowing.GraphicsLibraryFramework.KeyModifiers.Control) != 0)
		{
			modifiers = modifiers | KeyModifier.Control;
		}
		if ((@this & OpenTK.Windowing.GraphicsLibraryFramework.KeyModifiers.Alt) != 0)
		{
			modifiers = modifiers | KeyModifier.Alt;
		}
		if ((@this & OpenTK.Windowing.GraphicsLibraryFramework.KeyModifiers.Super) != 0)
		{
			modifiers = modifiers | KeyModifier.Super;
		}
		if ((@this & OpenTK.Windowing.GraphicsLibraryFramework.KeyModifiers.CapsLock) != 0)
		{
			modifiers = modifiers | KeyModifier.CapsLock;
		}
		if ((@this & OpenTK.Windowing.GraphicsLibraryFramework.KeyModifiers.NumLock) != 0)
		{
			modifiers = modifiers | KeyModifier.NumLock;
		}
		return modifiers;
	}
}