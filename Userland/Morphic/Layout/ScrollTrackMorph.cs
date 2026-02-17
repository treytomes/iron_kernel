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

	protected override void DrawSelf(IRenderingContext rc)
	{
		if (Style == null)
			return;

		var s = Style.Semantic;

		// Track background
		rc.RenderFilledRect(
			new Rectangle(Point.Empty, Size),
			s.Surface
		);

		// Optional outline (thin)
		rc.RenderRect(
			new Rectangle(Point.Empty, Size),
			s.Border
		);
	}
}