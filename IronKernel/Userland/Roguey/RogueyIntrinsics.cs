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

		CreateTileMapIntrinsics();
		CreateTileMapNamespace();

		CreateTileMapGetTileIntrinsic();
		CreateTileGetIntrinsic();
		CreateTileSetIntrinsic();
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

				var tile = new TileMorph
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

				if (world.ResolveAlive(ctx.self) is not TileMorph tile)
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

	private static void CreateTileMapIntrinsics()
	{
		var create = Intrinsic.Create("tilemap_create");
		create.AddParam("viewportSize");
		create.AddParam("mapSize");
		create.AddParam("assetId");
		create.AddParam("tileSize");

		create.code = (ctx, _) =>
		{
			try
			{
				if (ctx.interpreter.hostData is not WorldScriptContext world)
					return Intrinsic.Result.Null;

				if (!TryReadPair(ctx.GetVar("viewportSize"), out var vw, out var vh))
					return Error(ctx, "TileMap.create expects viewportSize [w,h]");

				if (!TryReadPair(ctx.GetVar("mapSize"), out var mw, out var mh))
					return Error(ctx, "TileMap.create expects mapSize [w,h]");

				if (!TryReadPair(ctx.GetVar("tileSize"), out var tw, out var th))
					return Error(ctx, "TileMap.create expects tileSize [w,h]");

				var assetId = ctx.GetVar("assetId").ToString();

				var tileSet = new TileSetInfo(assetId, new Size(tw, th));
				var map = new TileMapMorph(
					new Size(vw, vh),
					new Size(mw, mh),
					tileSet
				);

				world.World.AddMorph(map);

				// ✅ Register like any other Morph
				var handle = world.Register(map);

				// ✅ Attach TileMap-specific instance methods
				AttachTileMapMethods(handle);

				return new Intrinsic.Result(handle);
			}
			catch (Exception ex)
			{
				return Error(ctx, $"TileMap.create error: {ex.Message}");
			}
		};
	}

	private static void AttachTileMapMethods(ValMap handle)
	{
		handle["getTile"] =
			Intrinsic.GetByName("tilemap_getTile")!
					 .GetFunc()
					 .BindAndCopy(handle);
	}

	private static void CreateTileMapNamespace()
	{
		var tileMapNamespace = Intrinsic.Create("TileMap");
		tileMapNamespace.code = (ctx, _) =>
		{
			var map = new ValMap
			{
				["create"] = Intrinsic.GetByName("tilemap_create")!.GetFunc(),
				// ["getTile"] = Intrinsic.GetByName("tilemap_getTile")!.GetFunc(),
			};

			return new Intrinsic.Result(map);
		};
	}

	private static void CreateTileMapGetTileIntrinsic()
	{
		var getTile = Intrinsic.Create("tilemap_getTile");
		getTile.AddParam("x");
		getTile.AddParam("y");

		getTile.code = (ctx, _) =>
		{
			try
			{
				if (ctx.interpreter.hostData is not WorldScriptContext world)
					return Intrinsic.Result.Null;

				if (world.ResolveAlive(ctx.self) is not TileMapMorph map)
					return Intrinsic.Result.Null;

				var x = ctx.GetVar("x").IntValue();
				var y = ctx.GetVar("y").IntValue();

				if (!map.InBounds(x, y))
					return Intrinsic.Result.Null;

				// Create Tile handle (data-backed)
				var tile = new ValMap
				{
					["__isa"] = new ValString("Tile"),
					["_map"] = ctx.self,
					["_x"] = new ValNumber(x),
					["_y"] = new ValNumber(y)
				};

				AttachTileMethods(tile);

				return new Intrinsic.Result(tile);
			}
			catch (Exception ex)
			{
				ctx.interpreter.errorOutput?.Invoke(
					$"TileMap.getTile error: {ex.Message}",
					true
				);
				return Intrinsic.Result.Null;
			}
		};
	}

	private static void AttachTileMethods(ValMap tile)
	{
		tile["get"] = Intrinsic.GetByName("tile_get")!.GetFunc().BindAndCopy(tile);
		tile["set"] = Intrinsic.GetByName("tile_set")!.GetFunc().BindAndCopy(tile);
	}

	private static void CreateTileGetIntrinsic()
	{
		var get = Intrinsic.Create("tile_get");
		get.AddParam("key");

		get.code = (ctx, _) =>
		{
			try
			{
				var tile = ctx.self as ValMap;
				if (tile == null)
					return Intrinsic.Result.Null;

				var mapHandle = tile["_map"] as ValMap;
				if (mapHandle == null) throw new NullReferenceException("_map");
				var x = tile["_x"].IntValue();
				var y = tile["_y"].IntValue();

				if (ctx.interpreter.hostData is not WorldScriptContext world)
					return Intrinsic.Result.Null;

				if (world.ResolveAlive(mapHandle) is not TileMapMorph map)
					return Intrinsic.Result.Null;

				var info = map.GetTile(x, y);
				if (info == null)
					return Intrinsic.Result.Null;

				var key = ctx.GetVar("key").ToString();

				return key switch
				{
					"TileIndex" => new Intrinsic.Result(info.TileIndex),
					"BlocksMovement" => new Intrinsic.Result(info.BlocksMovement ? 1 : 0),
					"BlocksVision" => new Intrinsic.Result(info.BlocksVision ? 1 : 0),
					"Tag" => new Intrinsic.Result(info.Tag),
					_ => Intrinsic.Result.Null
				};
			}
			catch (Exception ex)
			{
				ctx.interpreter.errorOutput?.Invoke(
					$"Tile.get error: {ex.Message}",
					true
				);
				return Intrinsic.Result.Null;
			}
		};
	}

	private static void CreateTileSetIntrinsic()
	{
		var set = Intrinsic.Create("tile_set");
		set.AddParam("key");
		set.AddParam("value");

		set.code = (ctx, _) =>
		{
			try
			{
				var tile = ctx.self as ValMap;
				if (tile == null)
					return Intrinsic.Result.Null;

				var mapHandle = tile["_map"] as ValMap;
				if (mapHandle == null) throw new NullReferenceException("_map");
				var x = tile["_x"].IntValue();
				var y = tile["_y"].IntValue();

				if (ctx.interpreter.hostData is not WorldScriptContext world)
					return Intrinsic.Result.Null;

				if (world.ResolveAlive(mapHandle) is not TileMapMorph map)
					return Intrinsic.Result.Null;

				var info = map.GetTile(x, y);
				if (info == null)
					return Intrinsic.Result.Null;

				var key = ctx.GetVar("key").ToString();
				var value = ctx.GetVar("value");

				switch (key)
				{
					case "TileIndex":
						info.TileIndex = value.IntValue();
						break;
					case "BlocksMovement":
						info.BlocksMovement = value.BoolValue();
						break;
					case "BlocksVision":
						info.BlocksVision = value.BoolValue();
						break;
					case "Tag":
						info.Tag = value.ToString();
						break;
				}

				return Intrinsic.Result.Null;
			}
			catch (Exception ex)
			{
				ctx.interpreter.errorOutput?.Invoke(
					$"Tile.set error: {ex.Message}",
					true
				);
				return Intrinsic.Result.Null;
			}
		};
	}
}