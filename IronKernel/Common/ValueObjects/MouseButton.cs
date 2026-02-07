namespace IronKernel.Common.ValueObjects;

public enum MouseButton
{
	/// <summary>
	///     The first button.
	/// </summary>
	Button1 = 0,

	/// <summary>
	///     The second button.
	/// </summary>
	Button2 = 1,

	/// <summary>
	///     The third button.
	/// </summary>
	Button3 = 2,

	/// <summary>
	///     The fourth button.
	/// </summary>
	Button4 = 3,

	/// <summary>
	///     The fifth button.
	/// </summary>
	Button5 = 4,

	/// <summary>
	///     The sixth button.
	/// </summary>
	Button6 = 5,

	/// <summary>
	///     The seventh button.
	/// </summary>
	Button7 = 6,

	/// <summary>
	///     The eighth button.
	/// </summary>
	Button8 = 7,

	/// <summary>
	///     The left mouse button. This corresponds to <see cref="Button1"/>.
	/// </summary>
	Left = Button1,

	/// <summary>
	///     The right mouse button. This corresponds to <see cref="Button2"/>.
	/// </summary>
	Right = Button2,

	/// <summary>
	///     The middle mouse button. This corresponds to <see cref="Button3"/>.
	/// </summary>
	Middle = Button3,

	/// <summary>
	///     The highest mouse button available.
	/// </summary>
	Last = Button8,

}

public static class MouseButtonExtensions
{
	public static MouseButton ToHost(this OpenTK.Windowing.GraphicsLibraryFramework.MouseButton @this)
	{
		return @this switch
		{
			OpenTK.Windowing.GraphicsLibraryFramework.MouseButton.Button1 => MouseButton.Button1,
			OpenTK.Windowing.GraphicsLibraryFramework.MouseButton.Button2 => MouseButton.Button2,
			OpenTK.Windowing.GraphicsLibraryFramework.MouseButton.Button3 => MouseButton.Button3,
			OpenTK.Windowing.GraphicsLibraryFramework.MouseButton.Button4 => MouseButton.Button4,
			OpenTK.Windowing.GraphicsLibraryFramework.MouseButton.Button5 => MouseButton.Button5,
			OpenTK.Windowing.GraphicsLibraryFramework.MouseButton.Button6 => MouseButton.Button6,
			OpenTK.Windowing.GraphicsLibraryFramework.MouseButton.Button7 => MouseButton.Button7,
			OpenTK.Windowing.GraphicsLibraryFramework.MouseButton.Button8 => MouseButton.Button8,
			_ => throw new NotImplementedException($"Unknown mouse button: {@this}"),
		};
	}
}