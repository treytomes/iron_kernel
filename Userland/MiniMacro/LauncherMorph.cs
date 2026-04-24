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

	#region Constants

	private const int BUTTON_WIDTH = 160;
	private const int BUTTON_HEIGHT = 14;

	#endregion

	#region Fields

	private readonly IServiceProvider _services;
	private readonly VerticalStackMorph _stack;
	private bool _hasApps = false;
	private bool _hasActions = false;

	#endregion

	#region Constructor

	public LauncherMorph(IServiceProvider services, Point position)
		: base(position, new Size(BUTTON_WIDTH + 8, 32), "Launcher")
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
		// Stretch all button-like children to fill the stack width.
		foreach (var child in _stack.Submorphs)
		{
			if (child is ButtonMorph)
				child.Size = new Size(BUTTON_WIDTH, child.Size.Height);
		}

		base.UpdateLayout();

		// Fit window height to stack contents.
		var stackHeight = _stack.Size.Height;
		if (stackHeight > 0)
			Size = new Size(Size.Width, HeaderHeight + stackHeight);
	}

	#endregion

	#region Public API

	public void AddApp<TMorph>(string displayName)
		where TMorph : Morph
	{
		_hasApps = true;
		var button = new ButtonMorph(Point.Empty, new Size(BUTTON_WIDTH, BUTTON_HEIGHT), displayName)
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

		_stack.AddMorph(new ButtonMorph(Point.Empty, new Size(BUTTON_WIDTH, BUTTON_HEIGHT), displayName)
		{
			Command = new ActionCommand(action)
		});
		InvalidateLayout();
	}

	#endregion
}
