using Miniscript;
using System.Drawing;
using Userland.Morphic;

namespace Userland.Scripting;

public static class TileMapIntrinsics
{
	public static void Register()
	{
		CreateTileMapIntrinsics();
		CreateTileMapNamespace();
		CreateTileMapGetTileIntrinsic();
		CreateTileGetIntrinsic();
		CreateTileSetIntrinsic();
	}

	#region TileMap create / namespace

	private static void CreateTileMapNamespace()
	{
		var ns = Intrinsic.Create("TileMap");
		ns.code = (ctx, _) =>
		{
			var map = new ValMap
			{
				["create"] = Intrinsic.GetByName("tilemap_create")!.GetFunc()
			};
			return new Intrinsic.Result(map);
		};
	}

	private static void CreateTileMapIntrinsics()
	{
		var create = Intrinsic.Create("tilemap_create");
		create.AddParam("viewportSize");
		create.AddParam("mapSize");
		create.AddParam("url");
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

				var url = ctx.GetVar("url").ToString();

				var tileSet = new TileSetInfo(url, new Size(tw, th));
				var mapMorph = new TileMapMorph(
					new Size(vw, vh),
					new Size(mw, mh),
					tileSet
				);

				world.World.AddMorph(mapMorph);

				var handle = world.Handles.Register(mapMorph);

				// lifecycle
				handle["destroy"] =
					Intrinsic.GetByName("morph_destroy")!.GetFunc().BindAndCopy(handle);
				handle["isAlive"] =
					Intrinsic.GetByName("morph_isAlive")!.GetFunc().BindAndCopy(handle);

				// properties
				handle["props"] = mapMorph.ScriptObject;

				// TileMap-specific method
				handle["getTile"] =
					Intrinsic.GetByName("tilemap_getTile")!.GetFunc().BindAndCopy(handle);

				return new Intrinsic.Result(handle);
			}
			catch (Exception ex)
			{
				return Error(ctx, $"TileMap.create error: {ex.Message}");
			}
		};
	}

	#endregion

	#region Tile access

	private static void CreateTileMapGetTileIntrinsic()
	{
		var getTile = Intrinsic.Create("tilemap_getTile");
		getTile.AddParam("x");
		getTile.AddParam("y");

		getTile.code = (ctx, _) =>
		{
			if (ctx.interpreter.hostData is not WorldScriptContext world)
				return Intrinsic.Result.Null;

			if (world.Handles.ResolveAlive(ctx.self) is not TileMapMorph map)
				return Intrinsic.Result.Null;

			var x = ctx.GetVar("x").IntValue();
			var y = ctx.GetVar("y").IntValue();

			var info = map.GetTile(x, y);
			if (info == null)
				return Intrinsic.Result.Null;

			return new Intrinsic.Result(info.ScriptObject);
		};
	}

	#endregion

	#region Tile get/set

	private static void CreateTileGetIntrinsic()
	{
		var get = Intrinsic.Create("tile_get");
		get.AddParam("key");

		get.code = (ctx, _) =>
		{
			try
			{
				if (ctx.self is not ValMap tile)
					return Intrinsic.Result.Null;

				if (ctx.interpreter.hostData is not WorldScriptContext world)
					return Intrinsic.Result.Null;

				var mapHandle = tile["_map"] as ValMap;
				if (mapHandle == null || world.Handles.ResolveAlive(mapHandle) is not TileMapMorph map)
					return Intrinsic.Result.Null;

				var x = tile["_x"].IntValue();
				var y = tile["_y"].IntValue();
				var info = map.GetTile(x, y);
				if (info == null)
					return Intrinsic.Result.Null;

				var key = ctx.GetVar("key").ToString();
				return key switch
				{
					"TileIndex" => new Intrinsic.Result(info.TileIndex),
					"BlocksMovement" => info.BlocksMovement
						? Intrinsic.Result.True
						: Intrinsic.Result.False,
					"BlocksVision" => info.BlocksVision
						? Intrinsic.Result.True
						: Intrinsic.Result.False,
					"Tag" => new Intrinsic.Result(info.Tag),
					_ => Intrinsic.Result.Null
				};
			}
			catch (Exception ex)
			{
				return Error(ctx, $"Tile.get error: {ex.Message}");
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
				if (ctx.self is not ValMap tile)
					return Intrinsic.Result.Null;

				if (ctx.interpreter.hostData is not WorldScriptContext world)
					return Intrinsic.Result.Null;

				var mapHandle = tile["_map"] as ValMap;
				if (mapHandle == null || world.Handles.ResolveAlive(mapHandle) is not TileMapMorph map)
					return Intrinsic.Result.Null;

				var x = tile["_x"].IntValue();
				var y = tile["_y"].IntValue();
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
				return Error(ctx, $"Tile.set error: {ex.Message}");
			}
		};
	}

	#endregion

	#region Helpers

	private static bool TryReadPair(Value v, out int a, out int b)
	{
		a = b = 0;
		if (v is not ValList list || list.values.Count != 2)
			return false;

		a = list.values[0].IntValue();
		b = list.values[1].IntValue();
		return true;
	}

	private static Intrinsic.Result Error(TAC.Context ctx, string message)
	{
		ctx.interpreter.errorOutput?.Invoke(message, true);
		return Intrinsic.Result.Null;
	}

	#endregion
}