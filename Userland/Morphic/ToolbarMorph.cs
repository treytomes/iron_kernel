using System.Drawing;
using Userland.Gfx;
using Userland.Morphic.Commands;
using Userland.Morphic.Layout;

namespace Userland.Morphic;

public sealed class ToolbarMorph : Morph
{
	#region Fields

	private readonly HorizontalStackMorph _stack;

	#endregion

	#region Constructors

	public ToolbarMorph()
	{
		_stack = new HorizontalStackMorph
		{
			ShouldClipToBounds = true
		};
		AddMorph(_stack);
	}

	#endregion

	#region Methods

	public void Clear()
	{
		while (_stack.Submorphs.Any())
		{
			_stack.RemoveMorph(_stack.Submorphs.First());
		}
	}

	public void AddItem(string label, ICommand command)
	{
		var button = new ButtonMorph(
			Point.Empty,
			new Size(48, 16),
			label)
		{
			Command = command
		};

		_stack.AddMorph(button);
	}

	protected override void UpdateLayout()
	{
		_stack.Position = Point.Empty;
		base.UpdateLayout();
		Size = _stack.Size;
	}

	protected override void DrawSelf(IRenderingContext rc)
	{
		var semantic = GetWorld().Style.Semantic;

		// Toolbar background
		rc.RenderFilledRect(
			new Rectangle(Point.Empty, Size),
			semantic.Surface
		);

		// Toolbar border
		rc.RenderRect(
			new Rectangle(Point.Empty, Size),
			semantic.Border
		);
	}

	#endregion
}