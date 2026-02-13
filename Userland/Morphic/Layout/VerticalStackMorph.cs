using System.Drawing;

namespace IronKernel.Userland.Morphic.Layout;

public sealed class VerticalStackMorph : Morph
{
	public int Padding { get; set; } = 4;
	public int Spacing { get; set; } = 2;

	protected override void UpdateLayout()
	{
		int maxWidth = 0;
		int y = Padding;

		for (var n = 0; n < Submorphs.Count; n++)
		{
			var child = Submorphs[n];
			if (child == null) continue;
			child.Position = new Point(Padding, y);
			y += child.Size.Height + Spacing;
			maxWidth = Math.Max(maxWidth, child.Size.Width);
		}

		Size = new Size(
			maxWidth + Padding * 2,
			y + Padding
		);

		base.UpdateLayout();
	}
}