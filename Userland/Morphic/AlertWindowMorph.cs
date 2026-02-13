using System.Drawing;
using Userland.Morphic.Commands;
using Userland.Morphic.Layout;

namespace Userland.Morphic;

public sealed class AlertWindowMorph : WindowMorph
{
	public AlertWindowMorph(string message, Action onClose)
		: base(Point.Empty, new Size(352, 96), "Alert")
	{
		var vStack = new VerticalStackMorph();
		vStack.AddMorph(new LabelMorph { Text = message });
		vStack.AddMorph(new ButtonMorph(Point.Empty, new Size(24, 12), "OK")
		{
			Command = new ActionCommand(() =>
			{
				Owner?.RemoveMorph(this);
				onClose();
			}),
		});
		Content.AddMorph(vStack);
	}

	protected override void UpdateLayout()
	{
		base.UpdateLayout();
		Content.Size = Content.Submorphs.First().Size;
		Size = new Size(Content.Size.Width + Padding, Content.Size.Height + Padding + HeaderHeight);
	}
}