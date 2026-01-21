using IronKernel.Common;
using IronKernel.Common.ValueObjects;
using IronKernel.Modules.ApplicationHost;

namespace IronKernel.Userland.Morphic;

public sealed class FramebufferCanvas : IMorphicCanvas
{
	private readonly IApplicationBus _bus;

	public FramebufferCanvas(IApplicationBus bus)
	{
		_bus = bus;
	}

	public void Clear(RadialColor color)
	{
		_bus.Publish(new AppFbClear(color));
	}

	public void DrawPixel(int x, int y, RadialColor color)
	{
		_bus.Publish(new AppFbWriteSpan(x, y, [color]));
	}

	public void DrawRect(int x, int y, int width, int height, RadialColor color)
	{
		for (int iy = 0; iy < height; iy++)
		{
			var span = Enumerable.Repeat(color, width).ToArray();
			_bus.Publish(new AppFbWriteSpan(x, y + iy, span));
		}
	}
}
