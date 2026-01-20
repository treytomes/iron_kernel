using System.Drawing;

namespace IronKernel.Morphic;

public sealed class WorldMorph : Morph
{
	public Size ScreenSize { get; }

	public WorldMorph(Size screenSize)
	{
		ScreenSize = screenSize;
		Position = Point.Empty;
		Size = screenSize;
	}
}
