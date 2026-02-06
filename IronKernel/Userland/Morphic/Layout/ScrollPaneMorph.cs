using System.Drawing;
using IronKernel.Userland.Morphic.Commands;
using IronKernel.Userland.Morphic.ValueObjects;

namespace IronKernel.Userland.Morphic.Layout;

public sealed class ScrollPaneMorph : Morph
{
	private readonly DockPanelMorph _layout;
	private readonly Morph _viewport;
	private readonly Morph _content;

	private Point _scrollOffset;

	private const int ScrollStep = 16;

	public ScrollPaneMorph(Morph content)
	{
		IsSelectable = false;

		_content = content;

		// Root layout
		_layout = new DockPanelMorph
		{
			ShouldClipToBounds = true
		};
		AddMorph(_layout);

		// Viewport
		_viewport = new ContainerMorph
		{
			ShouldClipToBounds = true,
			IsSelectable = false
		};
		_viewport.AddMorph(_content);

		_layout.AddMorph(_viewport);
		_layout.SetDock(_viewport, Dock.Fill);

		// Vertical scrollbar
		var vBar = CreateVerticalScrollBar();
		_layout.AddMorph(vBar);
		_layout.SetDock(vBar, Dock.Right);

		// Horizontal scrollbar
		var hBar = CreateHorizontalScrollBar();
		_layout.AddMorph(hBar);
		_layout.SetDock(hBar, Dock.Bottom);
	}

	private Morph CreateVerticalScrollBar()
	{
		var bar = new VerticalStackMorph
		{
			ShouldClipToBounds = true
		};

		bar.AddMorph(new ButtonMorph(Point.Empty, new Size(12, 12), "^")
		{
			Command = new ActionCommand(() => ScrollBy(0, -ScrollStep))
		});

		bar.AddMorph(new ButtonMorph(Point.Empty, new Size(12, 12), "v")
		{
			Command = new ActionCommand(() => ScrollBy(0, ScrollStep))
		});

		return bar;
	}

	private Morph CreateHorizontalScrollBar()
	{
		var bar = new HorizontalStackMorph
		{
			ShouldClipToBounds = true
		};

		bar.AddMorph(new ButtonMorph(Point.Empty, new Size(12, 12), "<")
		{
			Command = new ActionCommand(() => ScrollBy(-ScrollStep, 0))
		});

		bar.AddMorph(new ButtonMorph(Point.Empty, new Size(12, 12), ">")
		{
			Command = new ActionCommand(() => ScrollBy(ScrollStep, 0))
		});

		return bar;
	}

	private void ScrollBy(int dx, int dy)
	{
		var maxX = Math.Max(0, _content.Size.Width - _viewport.Size.Width);
		var maxY = Math.Max(0, _content.Size.Height - _viewport.Size.Height);

		_scrollOffset = new Point(
			Math.Clamp(_scrollOffset.X + dx, 0, maxX),
			Math.Clamp(_scrollOffset.Y + dy, 0, maxY)
		);

		InvalidateLayout();
	}

	protected override void UpdateLayout()
	{
		// Take all space offered by parent (ContentMorph)
		if (Owner != null)
		{
			Size = Owner.Size;
		}

		_content.Position = new Point(
			-_scrollOffset.X,
			-_scrollOffset.Y
		);

		base.UpdateLayout();
	}
}