using System.Drawing;
using IronKernel.Common.ValueObjects;
using Userland.Gfx;
using Color = IronKernel.Common.ValueObjects.Color;

namespace Userland.Morphic.Inspector;

public sealed class ColorSwatchMorph : Morph
{
	private readonly Func<Color?> _getColor;

	public ColorSwatchMorph(Func<Color?> getColor)
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
