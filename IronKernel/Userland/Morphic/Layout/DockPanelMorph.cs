using System.Drawing;
using IronKernel.Userland.Morphic.ValueObjects;

namespace IronKernel.Userland.Morphic.Layout;

public sealed class DockPanelMorph : Morph
{
	private readonly Dictionary<Morph, Dock> _dockMap = new();

	public int Padding { get; set; } = 1;

	public void RemoveDock(Morph morph)
	{
		if (morph.Owner != this)
			throw new InvalidOperationException("Morph is not a child of this DockPanelMorph.");

		_dockMap.Remove(morph);
		InvalidateLayout();
	}

	public void SetDock(Morph morph, Dock dock)
	{
		if (morph.Owner != this)
			throw new InvalidOperationException("Morph is not a child of this DockPanelMorph.");

		_dockMap[morph] = dock;
		InvalidateLayout();
	}

	public Dock GetDock(Morph morph)
	{
		return _dockMap.TryGetValue(morph, out var dock)
			? dock
			: Dock.Left; // or whatever your default is
	}

	protected override void UpdateLayout()
	{
		var remaining = new Rectangle(
			Padding,
			Padding,
			Size.Width - Padding * 2,
			Size.Height - Padding * 2);

		foreach (var child in Submorphs)
		{
			switch (GetDock(child))
			{
				case Dock.Left:
					child.Position = remaining.Location;
					child.Size = new Size(child.Size.Width, remaining.Height);
					remaining.X += child.Size.Width + Padding;
					remaining.Width -= child.Size.Width + Padding;
					break;

				case Dock.Right:
					child.Size = new Size(child.Size.Width, remaining.Height);
					child.Position = new Point(
						remaining.Right - child.Size.Width,
						remaining.Top);
					remaining.Width -= child.Size.Width + Padding;
					break;

				case Dock.Top:
					child.Position = remaining.Location;
					child.Size = new Size(remaining.Width, child.Size.Height);
					remaining.Y += child.Size.Height + Padding;
					remaining.Height -= child.Size.Height + Padding;
					break;

				case Dock.Bottom:
					child.Size = new Size(remaining.Width, child.Size.Height);
					child.Position = new Point(
						remaining.Left,
						remaining.Bottom - child.Size.Height);
					remaining.Height -= child.Size.Height + Padding;
					break;

				case Dock.Fill:
					child.Position = remaining.Location;
					child.Size = remaining.Size;
					break;
			}
		}

		base.UpdateLayout();
	}
}