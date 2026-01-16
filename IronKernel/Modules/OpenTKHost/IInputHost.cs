using OpenTK.Windowing.Common;

namespace IronKernel.Modules.OpenTKHost;

public interface IInputHost
{
	event Action<KeyboardKeyEventArgs> Key;
	event Action<MouseMoveEventArgs> MouseMove;
	event Action<MouseButtonEventArgs> MouseButton;
	event Action<MouseWheelEventArgs> MouseWheel;
	event Action<TextInputEventArgs> TextInput;
}
