using OpenTK.Mathematics;

namespace IronKernel.Modules.OpenTKHost;

public interface IHostWindow
{
	Vector2i ClientSize { get; }
	event Action<Vector2i> Resized;
	event Action Closed;
}
