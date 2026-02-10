using System.Drawing;
using IronKernel.Userland.Morphic.Commands;
using IronKernel.Userland.Morphic.Layout;

namespace IronKernel.Userland.Morphic;

public sealed class AlertWindowMorph : WindowMorph
{
	public AlertWindowMorph(string message, Action onClose)
		: base(Point.Empty, new Size(256, 64), "Alert")
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
}