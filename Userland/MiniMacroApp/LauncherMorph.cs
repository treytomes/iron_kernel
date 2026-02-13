using System.Drawing;
using IronKernel.Userland.Morphic;
using IronKernel.Userland.Morphic.Commands;
using IronKernel.Userland.Morphic.Layout;
using Microsoft.Extensions.DependencyInjection;

namespace IronKernel.Userland.MiniMacro;

public sealed class LauncherMorph : WindowMorph
{
	private readonly IServiceProvider _services;
	private readonly VerticalStackMorph _stack;

	public LauncherMorph(IServiceProvider services, Point position)
		: base(position, new Size(216, 128), "Launcher")
	{
		_services = services;
		_stack = new VerticalStackMorph
		{
			ShouldClipToBounds = true
		};

		Content = _stack;
	}

	// public void AddApp<TMorph>(string displayName, Func<TMorph> morphFactory)
	// 	where TMorph : Morph
	// {
	// 	var button = new ButtonMorph(
	// 		Point.Empty,
	// 		new Size(120, 12),
	// 		displayName)
	// 	{
	// 		Command = new ActionCommand(() =>
	// 		{
	// 			var world = GetWorld();
	// 			if (world == null) return;

	// 			var spawnPos = new Point(Position.X + 20, Position.Y + 20);
	// 			var appMorph = morphFactory();
	// 			appMorph.Position = spawnPos;
	// 			world.AddMorph(appMorph);
	// 		})
	// 	};

	// 	_stack.AddMorph(button);
	// }

	public void AddApp<TMorph>(string displayName)
		where TMorph : Morph
	{
		var button = new ButtonMorph(
			Point.Empty,
			new Size(120, 12),
			displayName)
		{
			Command = new ActionCommand(() =>
			{
				var world = GetWorld();
				if (world == null) return;

				var spawnPos = new Point(Position.X + 20, Position.Y + 20);
				var appMorph = _services.GetRequiredService<TMorph>();
				appMorph.Position = spawnPos;
				world.AddMorph(appMorph);
			})
		};

		_stack.AddMorph(button);
	}
}