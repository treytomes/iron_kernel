namespace IronKernel.Modules.OpenTKHost.ValueObjects;

public enum InputAction
{
	/// <summary>
	/// The key or mouse button was released.
	/// </summary>
	Release = 0,

	/// <summary>
	/// The key or mouse button was pressed.
	/// </summary>
	Press = 1,

	/// <summary>
	/// The key was held down until it repeated.
	/// </summary>
	Repeat = 2
}

public static class InputActionExtensions
{
	public static InputAction ToHost(this OpenTK.Windowing.GraphicsLibraryFramework.InputAction @this)
	{
		return @this switch
		{
			OpenTK.Windowing.GraphicsLibraryFramework.InputAction.Press => InputAction.Press,
			OpenTK.Windowing.GraphicsLibraryFramework.InputAction.Release => InputAction.Release,
			OpenTK.Windowing.GraphicsLibraryFramework.InputAction.Repeat => InputAction.Repeat,
			_ => throw new NotImplementedException($"Unknown input action: {@this}"),
		};
	}
}