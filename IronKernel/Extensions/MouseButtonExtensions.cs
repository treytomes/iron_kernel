using IronKernel.Common.ValueObjects;

namespace IronKernel;

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