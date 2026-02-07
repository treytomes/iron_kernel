using System.Drawing;
using IronKernel.Common.ValueObjects;
using IronKernel.Userland.Gfx;

namespace IronKernel.Userland.Morphic.Layout;

public sealed class ScrollTrackMorph : Morph
{
	public ScrollTrackMorph()
	{
		IsSelectable = false;
		ShouldClipToBounds = true;
	}

	protected override void DrawSelf(IRenderingContext rc)
	{
		rc.RenderFilledRect(new Rectangle(Point.Empty, Size), new RadialColor(3, 0, 0));
		base.DrawSelf(rc);
	}
}