using System.Drawing;
using System.Reflection;
using IronKernel.Userland.Morphic.Commands;
using IronKernel.Userland.Morphic.Layout;
using IronKernel.Userland.Morphic.ValueObjects;

namespace IronKernel.Userland.Morphic.Inspector;

/// <summary>
/// A live inspector for an arbitrary object.
/// Displays public instance properties in a PropertyListMorph.
/// </summary>
public sealed class InspectorMorph : WindowMorph
{
	#region Constants

	private const int ScrollStep = 16;

	#endregion

	#region Fields

	private readonly PropertyListMorph _propertyList;
	private readonly DockPanelMorph _layoutPanel;
	private readonly ContainerMorph _content;

	private Point _scrollOffset = Point.Empty;

	private VerticalScrollThumbMorph _vThumb = null!;
	private HorizontalScrollThumbMorph _hThumb = null!;

	#endregion

	#region Constructors

	public InspectorMorph(object target)
		: this(
			target,
			new Point(32, 32),
			new Size(240, 180))
	{
	}

	public InspectorMorph(object target, Point position, Size size)
		: base(position, size, GetTitle(target))
	{
		IsSelectable = true;

		_layoutPanel = new DockPanelMorph()
		{
			ShouldClipToBounds = true,
			Size = Content.Size
		};

		_propertyList = BuildPropertyList(target);
		_propertyList.Size = new Size(128, 128); // TODO: Sizing might not be needed.

		_content = new ContainerMorph()
		{
			Size = new Size(128, 128), // TODO: Sizing might not be needed.
			ShouldClipToBounds = true
		};
		_content.AddMorph(_propertyList);

		_layoutPanel.AddMorph(_content);
		_layoutPanel.SetDock(_content, Dock.Fill);

		var hScrollBar = BuildHorizontalScrollBar();
		_layoutPanel.AddMorph(hScrollBar);
		_layoutPanel.SetDock(hScrollBar, Dock.Bottom);

		var vScrollBar = BuildVerticalScrollBar();
		_layoutPanel.AddMorph(vScrollBar);
		_layoutPanel.SetDock(vScrollBar, Dock.Right);

		Content.AddMorph(_layoutPanel);
	}

	#endregion

	#region Helpers

	private Morph BuildHorizontalScrollBar()
	{
		var bar = new DockPanelMorph()
		{
			IsSelectable = true,
			Size = new Size(128, 12),
		};

		var scrollLeftButton = new ButtonMorph(Point.Empty, new Size(12, 12), "<")
		{
			Command = new ActionCommand(() => ScrollBy(-ScrollStep, 0))
		};
		bar.AddMorph(scrollLeftButton);
		bar.SetDock(scrollLeftButton, Dock.Left);

		var spacer = new ContainerMorph()
		{
			Size = new Size(12, 12)
		};
		bar.AddMorph(spacer);
		bar.SetDock(spacer, Dock.Right);

		var scrollRightButton = new ButtonMorph(Point.Empty, new Size(12, 12), ">")
		{
			Command = new ActionCommand(() => ScrollBy(ScrollStep, 0))
		};
		bar.AddMorph(scrollRightButton);
		bar.SetDock(scrollRightButton, Dock.Right);

		var track = new ScrollTrackMorph();
		bar.AddMorph(track);
		bar.SetDock(track, Dock.Fill);

		_hThumb = new HorizontalScrollThumbMorph(
			getMaxScroll: () => Math.Max(0, _propertyList.Size.Width - _content.Size.Width),
			setScroll: x =>
			{
				_scrollOffset = new Point(x, _scrollOffset.Y);
				InvalidateLayout();
			});

		track.AddMorph(_hThumb);
		return bar;
	}

	private Morph BuildVerticalScrollBar()
	{
		var bar = new DockPanelMorph()
		{
			IsSelectable = true,
			Size = new Size(12, 128)
		};

		var scrollUpButton = new ButtonMorph(Point.Empty, new Size(12, 12), "^")
		{
			Command = new ActionCommand(() => ScrollBy(0, -ScrollStep))
		};
		bar.AddMorph(scrollUpButton);
		bar.SetDock(scrollUpButton, Dock.Top);

		var scrollDownButton = new ButtonMorph(Point.Empty, new Size(12, 12), "v")
		{
			Command = new ActionCommand(() => ScrollBy(0, ScrollStep))
		};
		bar.AddMorph(scrollDownButton);
		bar.SetDock(scrollDownButton, Dock.Bottom);

		var track = new ScrollTrackMorph();
		bar.AddMorph(track);
		bar.SetDock(track, Dock.Fill);

		_vThumb = new VerticalScrollThumbMorph(
			getMaxScroll: () => Math.Max(0, _propertyList.Size.Height - _content.Size.Height),
			getViewportHeight: () => _content.Size.Height,
			setScroll: y =>
			{
				_scrollOffset = new Point(_scrollOffset.X, y);
				InvalidateLayout();
			});

		track.AddMorph(_vThumb);
		return bar;
	}

	private static string GetTitle(object target)
	{
		return target.GetType().Name;
	}

	private static PropertyListMorph BuildPropertyList(object target)
	{
		var properties = target
			.GetType()
			.GetProperties(BindingFlags.Instance | BindingFlags.Public)
			.Where(p => p.CanRead && p.GetIndexParameters().Length == 0);

		// This assumes you added FromProperties(IEnumerable<PropertyInfo>, object)
		return PropertyListMorph.FromProperties(properties, target);
	}

	protected override void UpdateLayout()
	{
		_layoutPanel.Size = Content.Size;
		base.UpdateLayout();

		_propertyList.Position = new Point(-_scrollOffset.X, -_scrollOffset.Y);

		UpdateThumbPositions();
	}

	private void UpdateThumbPositions()
	{
		if (_vThumb?.Owner != null)
		{
			var track = _vThumb.Owner;
			var maxScroll = Math.Max(0, _propertyList.Size.Height - _content.Size.Height);
			var maxThumb = Math.Max(0, track.Size.Height - _vThumb.Size.Height);

			if (maxScroll > 0 && maxThumb > 0)
			{
				var y = (int)((float)_scrollOffset.Y / maxScroll * maxThumb);
				_vThumb.Position = new Point(_vThumb.Position.X, y);
			}
		}

		if (_hThumb?.Owner != null)
		{
			var track = _hThumb.Owner;
			var maxScroll = Math.Max(0, _propertyList.Size.Width - _content.Size.Width);
			var maxThumb = Math.Max(0, track.Size.Width - _hThumb.Size.Width);

			if (maxScroll > 0 && maxThumb > 0)
			{
				var x = (int)((float)_scrollOffset.X / maxScroll * maxThumb);
				_hThumb.Position = new Point(x, _hThumb.Position.Y);
			}
		}
	}

	private void ScrollBy(int dx, int dy)
	{
		var child = _content.Submorphs[0];
		var maxX = Math.Max(0, child.Size.Width - _content.Size.Width);
		var maxY = Math.Max(0, child.Size.Height - _content.Size.Height);

		_scrollOffset = new Point(
			Math.Clamp(_scrollOffset.X + dx, 0, maxX),
			Math.Clamp(_scrollOffset.Y + dy, 0, maxY)
		);

		InvalidateLayout();
	}

	#endregion
}