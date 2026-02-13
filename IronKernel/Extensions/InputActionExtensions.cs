using IronKernel.Common.ValueObjects;

namespace IronKernel;

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