using Miniscript;
using System.Drawing;
using Userland.Morphic;
using Userland.Roguey;

namespace Userland.Scripting;

public static class SpriteDisplayIntrinsics
{
	public static void Register()
	{
		CreateSpriteDisplayNamespace();
		CreateSpriteDisplayCreateIntrinsic();
		CreateSpriteCreateIntrinsic();
		CreateSpriteClearIntrinsic();
		CreateSpriteDestroyIntrinsic();
	}

	#region Namespace

	private static void CreateSpriteDisplayNamespace()
	{
		var ns = Intrinsic.Create("SpriteDisplay");
		ns.code = (ctx, _) =>
		{
			var map = new ValMap
			{
				["create"] = Intrinsic.GetByName("spritedisplay_create")!.GetFunc()
			};
			return new Intrinsic.Result(map);
		};
	}

	#endregion

	#region SpriteDisplay.create

	private static void CreateSpriteDisplayCreateIntrinsic()
	{
		var create = Intrinsic.Create("spritedisplay_create");
		create.AddParam("viewportSize");
		create.AddParam("url");
		create.AddParam("tileSize");

		create.code = (ctx, _) =>
		{
			try
			{
				if (ctx.interpreter.hostData is not WorldScriptContext world)
					return Intrinsic.Result.Null;

				if (!TryReadPair(ctx.GetVar("viewportSize"), out var vw, out var vh))
					return Error(ctx, "SpriteDisplay.create expects viewportSize [w,h]");

				if (!TryReadPair(ctx.GetVar("tileSize"), out var tw, out var th))
					return Error(ctx, "SpriteDisplay.create expects tileSize [w,h]");

				var url = ctx.GetVar("url").ToString();
				var tileSet = new TileSetInfo(url, new Size(tw, th));

				var morph = new SpriteDisplayMorph(
					new Size(vw, vh),
					tileSet
				);

				world.World.AddMorph(morph);
				var handle = world.Handles.Register(morph);

				// lifecycle
				handle["destroy"] =
					Intrinsic.GetByName("morph_destroy")!.GetFunc().BindAndCopy(handle);
				handle["isAlive"] =
					Intrinsic.GetByName("morph_isAlive")!.GetFunc().BindAndCopy(handle);

				// properties
				handle["props"] = morph.ScriptObject;

				// SpriteDisplay-specific API
				handle["createSprite"] =
					Intrinsic.GetByName("spritedisplay_createSprite")!.GetFunc().BindAndCopy(handle);
				handle["clear"] =
					Intrinsic.GetByName("spritedisplay_clear")!.GetFunc().BindAndCopy(handle);
				handle["destroySprite"] =
					Intrinsic.GetByName("spritedisplay_destroySprite")!.GetFunc().BindAndCopy(handle);

				return new Intrinsic.Result(handle);
			}
			catch (Exception ex)
			{
				return Error(ctx, $"SpriteDisplay.create error: {ex.Message}");
			}
		};
	}

	#endregion

	#region Sprite creation

	private static void CreateSpriteCreateIntrinsic()
	{
		var create = Intrinsic.Create("spritedisplay_createSprite");
		create.code = (ctx, _) =>
		{
			try
			{
				if (ctx.interpreter.hostData is not WorldScriptContext world)
					return Intrinsic.Result.Null;

				if (world.Handles.ResolveAlive(ctx.self) is not SpriteDisplayMorph display)
					return Intrinsic.Result.Null;

				var sprite = new SpriteInfo();
				display.Sprites.Add(sprite);

				return new Intrinsic.Result(sprite.ScriptObject);
			}
			catch (Exception ex)
			{
				return Error(ctx, $"SpriteDisplay.createSprite error: {ex.Message}");
			}
		};
	}

	#endregion

	private static void CreateSpriteClearIntrinsic()
	{
		var clear = Intrinsic.Create("spritedisplay_clear");
		clear.code = (ctx, _) =>
		{
			try
			{
				if (ctx.interpreter.hostData is not WorldScriptContext world)
					return Intrinsic.Result.Null;

				if (world.Handles.ResolveAlive(ctx.self) is not SpriteDisplayMorph display)
					return Intrinsic.Result.Null;

				display.Sprites.Clear();
				display.Invalidate();

				return Intrinsic.Result.Null;
			}
			catch (Exception ex)
			{
				return Error(ctx, $"SpriteDisplay.clear error: {ex.Message}");
			}
		};
	}

	private static void CreateSpriteDestroyIntrinsic()
	{
		var destroy = Intrinsic.Create("spritedisplay_destroySprite");
		destroy.AddParam("sprite");

		destroy.code = (ctx, _) =>
		{
			try
			{
				if (ctx.interpreter.hostData is not WorldScriptContext world)
					return Intrinsic.Result.Null;

				if (world.Handles.ResolveAlive(ctx.self) is not SpriteDisplayMorph display)
					return Intrinsic.Result.Null;

				if (ctx.GetVar("sprite") is not ValMap spriteMap)
					return Intrinsic.Result.Null;

				// Find matching SpriteInfo by script object identity
				var sprites = display.Sprites;
				for (int i = 0; i < sprites.Count; i++)
				{
					if (ReferenceEquals(sprites[i].ScriptObject, spriteMap))
					{
						sprites.RemoveAt(i);
						display.Invalidate();
						break;
					}
				}

				return Intrinsic.Result.Null;
			}
			catch (Exception ex)
			{
				return Error(ctx, $"SpriteDisplay.destroySprite error: {ex.Message}");
			}
		};
	}

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