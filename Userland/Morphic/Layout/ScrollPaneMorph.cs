using System.Drawing;
using Userland.Morphic.Commands;
using Userland.Morphic.ValueObjects;

namespace Userland.Morphic.Layout;

public sealed class ScrollPaneMorph : DockPanelMorph
{
	#region Constants

	private const int ScrollStep = 16;
	private const int MinThumbSize = 12;

	#endregion

	#region Fields

	private Morph _content;
	private readonly ContainerMorph _viewport;

	private DockPanelMorph _hScrollBar;
	private DockPanelMorph _vScrollBar;

	private ScrollTrackMorph _hTrack = null!;
	private ScrollTrackMorph _vTrack = null!;

	private HorizontalScrollThumbMorph _hThumb = null!;
	private VerticalScrollThumbMorph _vThumb = null!;

	private Point _scrollOffset = Point.Empty;

	#endregion

	#region Constructors

	public ScrollPaneMorph(Morph content)
		: base()
	{
		ShouldClipToBounds = true;

		_content = content;

		_viewport = new ContainerMorph
		{
			ShouldClipToBounds = true
		};
		_viewport.AddMorph(_content);

		AddMorph(_viewport);
		SetDock(_viewport, Dock.Fill);

		_hScrollBar = BuildHorizontalScrollBar();
		AddMorph(_hScrollBar);
		SetDock(_hScrollBar, Dock.Bottom);

		_vScrollBar = BuildVerticalScrollBar();
		AddMorph(_vScrollBar);
		SetDock(_vScrollBar, Dock.Right);
	}

	#endregion

	#region Properties

	private int MaxScrollX => Math.Max(0, _content.Size.Width - _viewport.Size.Width);
	private int MaxScrollY => Math.Max(0, _content.Size.Height - _viewport.Size.Height);
	private int ScrollBarPadding => 4;

	#endregion

	#region Methods

	public void SetContent(Morph content)
	{
		if (content == null) throw new ArgumentNullException(nameof(content));

		if (_content != null)
		{
			_viewport.RemoveMorph(_content);
		}

		_content = content;

		// Reset scroll state
		_scrollOffset = Point.Empty;
		_content.Position = Point.Empty;

		_viewport.AddMorph(_content);

		// Ensure scrollbars and thumbs recompute
		InvalidateLayout();
	}

	#region Scrollbars

	private DockPanelMorph BuildHorizontalScrollBar()
	{
		var bar = new DockPanelMorph { Size = new Size(128, 12) };

		bar.AddMorph(new ButtonMorph(Point.Empty, new Size(12, 12), "<")
		{
			Command = new ActionCommand(() => ScrollBy(-ScrollStep, 0))
		});
		bar.SetDock(bar.Submorphs[^1], Dock.Left);

		bar.AddMorph(new ButtonMorph(Point.Empty, new Size(12, 12), ">")
		{
			Command = new ActionCommand(() => ScrollBy(ScrollStep, 0))
		});
		bar.SetDock(bar.Submorphs[^1], Dock.Right);

		_hTrack = new ScrollTrackMorph();
		bar.AddMorph(_hTrack);
		bar.SetDock(_hTrack, Dock.Fill);

		_hThumb = new HorizontalScrollThumbMorph(
			getMaxScroll: () => MaxScrollX,
			setScroll: x =>
			{
				_scrollOffset = new Point(x, _scrollOffset.Y);
				InvalidateLayout();
			});

		_hTrack.AddMorph(_hThumb);
		return bar;
	}

	private DockPanelMorph BuildVerticalScrollBar()
	{
		var bar = new DockPanelMorph { Size = new Size(12, 128) };

		bar.AddMorph(new ButtonMorph(Point.Empty, new Size(12, 12), "^")
		{
			Command = new ActionCommand(() => ScrollBy(0, -ScrollStep))
		});
		bar.SetDock(bar.Submorphs[^1], Dock.Top);

		bar.AddMorph(new ButtonMorph(Point.Empty, new Size(12, 12), "v")
		{
			Command = new ActionCommand(() => ScrollBy(0, ScrollStep))
		});
		bar.SetDock(bar.Submorphs[^1], Dock.Bottom);

		_vTrack = new ScrollTrackMorph();
		bar.AddMorph(_vTrack);
		bar.SetDock(_vTrack, Dock.Fill);

		_vThumb = new VerticalScrollThumbMorph(
			getMaxScroll: () => MaxScrollY,
			getViewportHeight: () => _viewport.Size.Height,
			setScroll: y =>
			{
				_scrollOffset = new Point(_scrollOffset.X, y);
				InvalidateLayout();
			});

		_vTrack.AddMorph(_vThumb);
		return bar;
	}

	#endregion

	#region Layout

	protected override void UpdateLayout()
	{
		base.UpdateLayout();

		// Apply scroll transform
		_content.Position = new Point(-_scrollOffset.X, -_scrollOffset.Y);

		UpdateScrollbars();
	}

	private void UpdateScrollbars()
	{
		var style = Style ?? throw new NullReferenceException("Missing style.");

		bool canScrollX = MaxScrollX > 0;
		bool canScrollY = MaxScrollY > 0;

		_hScrollBar.Visible = canScrollX;
		_vScrollBar.Visible = canScrollY;

		_hScrollBar.Size = new Size(_hScrollBar.Size.Width, style.DefaultFontStyle.TileSize.Height + ScrollBarPadding);
		_vScrollBar.Size = new Size(style.DefaultFontStyle.TileSize.Width + ScrollBarPadding, _vScrollBar.Size.Height);

		if (canScrollX)
			UpdateHorizontalThumb();

		if (canScrollY)
			UpdateVerticalThumb();
	}

	private void UpdateHorizontalThumb()
	{
		if (MaxScrollX <= 0 || _content.Size.Width <= 0)
		{
			_hThumb.Visible = false;
			return;
		}

		var trackWidth = _hTrack.Size.Width;
		if (trackWidth <= MinThumbSize)
			return;

		_hThumb.Visible = true;

		var ratio = (float)_viewport.Size.Width / _content.Size.Width;
		var thumbWidth = Math.Clamp(
			(int)(trackWidth * ratio),
			MinThumbSize,
			trackWidth
		);

		_hThumb.Size = new Size(thumbWidth, _hThumb.Size.Height);

		var maxThumbX = trackWidth - thumbWidth;
		var x = (int)((float)_scrollOffset.X / MaxScrollX * maxThumbX);
		_hThumb.Position = new Point(x, _hThumb.Position.Y);
	}

	private void UpdateVerticalThumb()
	{
		if (MaxScrollY <= 0 || _content.Size.Height <= 0)
		{
			_vThumb.Visible = false;
			return;
		}

		var trackHeight = _hTrack.Size.Height;
		if (trackHeight <= MinThumbSize)
			return;

		_vThumb.Visible = true;

		var ratio = (float)_viewport.Size.Height / _content.Size.Height;
		var thumbHeight = Math.Clamp(
			(int)(trackHeight * ratio),
			MinThumbSize,
			trackHeight
		);

		_vThumb.Size = new Size(thumbHeight, _vThumb.Size.Height);

		var maxThumbY = trackHeight - thumbHeight;
		var y = (int)((float)_scrollOffset.Y / MaxScrollY * maxThumbY);
		_vThumb.Position = new Point(_vThumb.Position.X, y);
	}

	#endregion

	#region Scrolling helpers

	private void ScrollBy(int dx, int dy)
	{
		_scrollOffset = new Point(
			Math.Clamp(_scrollOffset.X + dx, 0, MaxScrollX),
			Math.Clamp(_scrollOffset.Y + dy, 0, MaxScrollY)
		);

		InvalidateLayout();
	}

	#endregion

	#endregion
}