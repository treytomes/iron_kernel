using IronKernel.Userland.Morphic;
using Miniscript;
using System.Drawing;

namespace IronKernel.Userland.Roguey;

/// <summary>
/// Registers MiniScript intrinsics specific to the Roguey game.
/// Follows the same patterns as WorldScriptContext.
/// </summary>
public static class RogueyIntrinsics
{
	public static void Create()
	{
		CreateTileIntrinsics();
		CreateTileNamespace();
	}

	#region Helpers

	private static Intrinsic.Result Error(TAC.Context ctx, string message)
	{
		ctx.interpreter.errorOutput?.Invoke(message, true);
		return Intrinsic.Result.Null;
	}

	private static bool TryReadPair(Value v, out int a, out int b)
	{
		a = b = 0;
		if (v is not ValList list || list.values.Count != 2)
			return false;

		a = list.values[0].IntValue();
		b = list.values[1].IntValue();
		return true;
	}

	#endregion

	#region Tile Intrinsics

	private static void CreateTileIntrinsics()
	{
		// ---------- tile_create([x,y]) ----------
		var create = Intrinsic.Create("tile_create");
		create.AddParam("pos");

		create.code = (ctx, _) =>
		{
			try
			{
				if (ctx.interpreter.hostData is not WorldScriptContext world)
					return Intrinsic.Result.Null;

				if (!TryReadPair(ctx.GetVar("pos"), out var x, out var y))
					return Error(ctx, "Tile.create expects [x,y]");

				var tile = new MapTileMorph
				{
					Position = new Point(x, y)
				};

				world.World.AddMorph(tile);

				// IMPORTANT: use the same registration path as Morphs
				return new Intrinsic.Result(world.Register(tile));
			}
			catch (Exception ex)
			{
				return Error(ctx, $"Tile.create error: {ex.Message}");
			}
		};

		// ---------- tile_isBlocked ----------
		var isBlocked = Intrinsic.Create("tile_isBlocked");
		isBlocked.code = (ctx, _) =>
		{
			try
			{
				if (ctx.interpreter.hostData is not WorldScriptContext world)
					return Intrinsic.Result.False;

				if (world.ResolveAlive(ctx.self) is not MapTileMorph tile)
					return Intrinsic.Result.False;

				return tile.BlocksMovement
					? Intrinsic.Result.True
					: Intrinsic.Result.False;
			}
			catch
			{
				return Intrinsic.Result.False;
			}
		};
	}

	#endregion

	#region Tile Namespace

	private static void CreateTileNamespace()
	{
		var tileNamespace = Intrinsic.Create("Tile");
		tileNamespace.code = (ctx, _) =>
		{
			var map = new ValMap
			{
				["create"] = Intrinsic.GetByName("tile_create")!.GetFunc(),
				["isBlocked"] = Intrinsic.GetByName("tile_isBlocked")!.GetFunc()
			};

			return new Intrinsic.Result(map);
		};
	}

	#endregion
}