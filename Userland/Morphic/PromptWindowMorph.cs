using System.Drawing;
using IronKernel.Userland.Morphic.Commands;
using IronKernel.Userland.Morphic.Layout;

namespace IronKernel.Userland.Morphic;

public sealed class PromptWindowMorph : WindowMorph
{
	private string _value;

	public PromptWindowMorph(
		string message,
		string? defaultValue,
		Action<string?> onClose
	) : base(Point.Empty, new Size(420, 108), "Prompt")
	{
		Title = "Prompt";
		_value = defaultValue ?? string.Empty;

		var hStack = new HorizontalStackMorph();
		hStack.AddMorph(new LabelMorph { Text = message });
		hStack.AddMorph(new TextEditMorph(_value, v => _value = v));

		var toolbar = new ToolbarMorph();
		toolbar.AddItem("OK", new ActionCommand(() =>
		{
			Owner?.RemoveMorph(this);
			onClose(_value);
		}));
		toolbar.AddItem("Cancel", new ActionCommand(() =>
		{
			Owner?.RemoveMorph(this);
			onClose(null);
		}));

		var vStack = new VerticalStackMorph();
		vStack.AddMorph(hStack);
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