using System.Drawing;

namespace IronKernel.Userland.Morphic.Layout;

public sealed class VerticalStackMorph : Morph
{
	public int Padding { get; set; } = 4;
	public int Spacing { get; set; } = 2;

	protected override void UpdateLayout()
	{
		int y = Padding;

		foreach (var child in Submorphs)
		{
			child.Position = new Point(Padding, y);
			y += child.Size.Height + Spacing;
		}

		base.UpdateLayout();
	}
}