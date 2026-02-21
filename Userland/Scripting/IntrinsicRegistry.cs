using Miniscript;
using Userland.Morphic;

namespace Userland.Scripting;

public static class IntrinsicRegistry
{
	public static void Register()
	{
		CanvasIntrinsics.Register();
		ColorIntrinsics.Register();
		DialogIntrinsics.Register();
		FileSystemIntrinsics.Register();
		MorphIntrinsics.Register();
		TileMapIntrinsics.Register();

		CreateInspectIntrinsic();
	}

	private static void CreateInspectIntrinsic()
	{
		var inspect = Intrinsic.Create("inspect");
		inspect.AddParam("value");

		inspect.code = (ctx, _) =>
		{
			try
			{
				if (ctx.interpreter.hostData is not WorldScriptContext world)
					return Intrinsic.Result.Null;

				var value = ctx.GetVar("value");

				// 1. Try to resolve as a live Morph handle
				var morph = world.Handles.ResolveAlive(value);
				if (morph != null)
				{
					// Engine-level morph inspection
					world.World.OpenInspector(morph);
					return Intrinsic.Result.Null;
				}

				// 2. Fallback: inspect MiniScript value
				world.World.OpenInspector(value);
				return Intrinsic.Result.Null;
			}
			catch (Exception ex)
			{
				ctx.interpreter.errorOutput?.Invoke(
					$"inspect error: {ex.Message}",
					true
				);
				return Intrinsic.Result.Null;
			}
		};
	}
}