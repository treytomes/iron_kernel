using System.Drawing;
using IronKernel.Common.ValueObjects;
using Userland.Gfx;

namespace Userland.Morphic.Inspector;

public sealed class RadialColorSwatchMorph : Morph
{
	private readonly Func<RadialColor?> _getColor;

	public RadialColorSwatchMorph(Func<RadialColor?> getColor)
	{
		_getColor = getColor;
		IsSelectable = false;
	}

	protected override void UpdateLayout()
	{
		int size = Style?.DefaultFontStyle.TileSize.Height ?? 8;
		Size = new Size(size, size);
		base.UpdateLayout();
	}

	protected override void DrawSelf(IRenderingContext rc)
	{
		if (Style == null)
			return;

		var s = Style.Semantic;
		var color = _getColor() ?? s.MutedText;

		rc.RenderFilledRect(
			new Rectangle(Point.Empty, Size),
			s.Border);

		rc.RenderFilledRect(
			new Rectangle(1, 1, Size.Width - 2, Size.Height - 2),
			color);
	}
}