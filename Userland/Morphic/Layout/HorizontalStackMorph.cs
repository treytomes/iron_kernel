using System.Drawing;

namespace Userland.Morphic.Layout;

public class HorizontalStackMorph : Morph
{
	public int Padding { get; set; } = 4;
	public int Spacing { get; set; } = 2;

	protected override void UpdateLayout()
	{
		int x = Padding;
		int maxHeight = 0;

		foreach (var child in Submorphs.ToArray())
		{
			if (child == null) continue;
			child.Position = new Point(x, Padding);
			x += child.Size.Width + Spacing;
			maxHeight = Math.Max(maxHeight, child.Size.Height);
		}

		if (Visible)
		{
			Size = new Size(
				x + Padding,
				maxHeight + Padding * 2
			);
		}
		else
		{
			Size = Size.Empty;
		}

		base.UpdateLayout();
	}
}