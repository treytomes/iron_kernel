using System.Drawing;
using IronKernel.Common.ValueObjects;
using Userland.Gfx;

namespace Userland.Morphic.Layout;

public sealed class ScrollTrackMorph : Morph
{
	public ScrollTrackMorph()
	{
		IsSelectable = false;
		ShouldClipToBounds = true;
	}

	public RadialColor FillColor => new RadialColor(3, 0, 0);

	protected override void DrawSelf(IRenderingContext rc)
	{
		rc.RenderFilledRect(new Rectangle(Point.Empty, Size), FillColor);
		base.DrawSelf(rc);
	}
}