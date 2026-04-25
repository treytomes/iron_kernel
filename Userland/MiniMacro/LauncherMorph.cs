using System.Drawing;
using IronKernel.Common.ValueObjects;
using Userland.Gfx;
using Userland.Morphic;
using Userland.Morphic.Commands;
using Userland.Morphic.Layout;
using Microsoft.Extensions.DependencyInjection;
using Color = IronKernel.Common.ValueObjects.Color;

namespace Userland.MiniMacro;

public sealed class LauncherMorph : WindowMorph
{
	#region Inner types

	private sealed class DividerMorph : Morph
	{
		public DividerMorph()
		{
			Size = new Size(0, 5);
			IsSelectable = false;
		}

		protected override void DrawSelf(IRenderingContext rc)
		{
			if (Style == null) return;
			var y = Size.Height / 2;
			rc.RenderHLine(new Point(2, y), Size.Width - 4, Style.Semantic.Border);
		}
	}

	#endregion

	#region Fields

	private readonly IServiceProvider _services;
	private readonly VerticalStackMorph _stack;
	private bool _hasApps = false;
	private bool _hasActions = false;

	#endregion

	#region Constructor

	public LauncherMorph(IServiceProvider services, Point position)
		: base(position, new Size(120, 32), "Launcher")
	{
		_services = services;
		_stack = new VerticalStackMorph
		{
			Padding = 2,
			Spacing = 1,
			ShouldClipToBounds = false,
		};
		Content = _stack;
	}

	#endregion

	#region Layout

	protected override void UpdateLayout()
	{
		// First pass: let buttons size naturally to their labels.
		base.UpdateLayout();

		// Stretch all children to the widest button width and fit the window.
		var stackSize = _stack.Size;
		if (stackSize.Width <= 0 || stackSize.Height <= 0) return;

		var innerWidth = stackSize.Width - _stack.Padding * 2;
		foreach (var child in _stack.Submorphs)
		{
			if (child.Size.Width != innerWidth)
				child.Size = new Size(innerWidth, child.Size.Height);
		}

		var targetSize = new Size(stackSize.Width, HeaderHeight + stackSize.Height);
		if (Size != targetSize)
			Size = targetSize;
	}

	#endregion

	#region Public API

	public void AddApp<TMorph>(string displayName)
		where TMorph : Morph
	{
		_hasApps = true;
		var button = new ButtonMorph(Point.Empty, Size.Empty, displayName)
		{
			Command = new ActionCommand(() =>
			{
				var world = GetWorld();
				if (world == null) return;

				var spawnPos = new Point(Position.X + 20, Position.Y + 20);
				var appMorph = _services.GetRequiredService<TMorph>();
				appMorph.Position = spawnPos;
				world.AddMorph(appMorph);

				MarkForDeletion();
			})
		};
		_stack.AddMorph(button);
		InvalidateLayout();
	}

	public void AddAction(string displayName, Action action)
	{
		if (_hasApps && !_hasActions)
		{
			_stack.AddMorph(new DividerMorph());
			_hasActions = true;
		}

		_stack.AddMorph(new ButtonMorph(Point.Empty, Size.Empty, displayName)
		{
			Command = new ActionCommand(action)
		});
		InvalidateLayout();
	}

	#endregion
}
