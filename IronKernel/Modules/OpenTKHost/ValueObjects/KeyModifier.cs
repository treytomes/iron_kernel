namespace IronKernel.Modules.OpenTKHost.ValueObjects;

/// <summary>
/// Key modifiers, such as Shift or CTRL.
/// </summary>
[Flags]
public enum KeyModifier
{
	None = 0x0000,

	/// <summary>
	/// if one or more Shift keys were held down.
	/// </summary>
	Shift = 0x0001,

	/// <summary>
	/// If one or more Control keys were held down.
	/// </summary>
	Control = 0x0002,

	/// <summary>
	/// If one or more Alt keys were held down.
	/// </summary>
	Alt = 0x0004,

	/// <summary>
	/// If one or more Super keys were held down.
	/// </summary>
	Super = 0x0008,

	/// <summary>
	///     If caps lock is enabled.
	/// </summary>
	CapsLock = 0x0010,

	/// <summary>
	///     If num lock is enabled.
	/// </summary>
	NumLock = 0x0020,
}

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