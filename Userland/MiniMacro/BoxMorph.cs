using System.Drawing;
using IronKernel.Common.ValueObjects;
using Userland.Gfx;
using Userland.Morphic;
using Color = IronKernel.Common.ValueObjects.Color;

namespace Userland.MiniMacro;

public sealed class BoxMorph : Morph
{
	public BoxMorph(Point position, Size size)
	{
		Position = position;
		Size = size;
		FillColor = Color.DarkGray;
		BorderColor = Color.Gray;
	}

	public Color FillColor { get; set; }
	public Color BorderColor { get; set; }

	public Size EditMe { get; set; } = new Size(128, 96);

	protected override void DrawSelf(IRenderingContext rc)
	{
		base.DrawSelf(rc);
		rc.RenderFilledRect(new Rectangle(new Point(0, 0), Size), FillColor);
		rc.RenderRect(new Rectangle(new Point(0, 0), Size), BorderColor);
	}
}
