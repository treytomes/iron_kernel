using System.Drawing;
using Userland.Morphic.Commands;
using Userland.Morphic.Layout;

namespace Userland.Morphic;

public sealed class ConfirmWindowMorph : WindowMorph
{
	public ConfirmWindowMorph(
		string message,
		Action<bool> onClose)
		: base(Point.Empty, new Size(352, 96), "Confirm")
	{
		var label = new LabelMorph { Text = message };

		var toolbar = new ToolbarMorph();
		toolbar.AddItem("OK", new ActionCommand(() =>
		{
			Owner?.RemoveMorph(this);
			onClose(true);
		}));
		toolbar.AddItem("Cancel", new ActionCommand(() =>
		{
			Owner?.RemoveMorph(this);
			onClose(false);
		}));

		var vStack = new VerticalStackMorph();
		vStack.AddMorph(label);
		vStack.AddMorph(toolbar);

		Content.AddMorph(vStack);
	}

	protected override void UpdateLayout()
	{
		base.UpdateLayout();
		Content.Size = Content.Submorphs.First().Size;
		Size = new Size(Content.Size.Width + Padding, Content.Size.Height + Padding + HeaderHeight);
	}
}