using IronKernel.Common.ValueObjects;

namespace IronKernel.Userland.Morphic;

public interface IMorphicCanvas
{
	void Clear(RadialColor color);
	void DrawPixel(int x, int y, RadialColor color);
	void DrawRect(int x, int y, int width, int height, RadialColor color);
}
