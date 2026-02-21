using System.Drawing;
using IronKernel.Common.ValueObjects;
using Miniscript;
using Userland.Morphic;

namespace Userland.Scripting;

public static class CanvasIntrinsics
{
	public static void Register()
	{
		CreateCanvasIntrinsics();
		CreateCanvasNamespace();
	}

	#region Canvas namespace

	private static void CreateCanvasNamespace()
	{
		var canvasNs = Intrinsic.Create("Canvas");
		canvasNs.code = (ctx, _) =>
		{
			var map = new ValMap
			{
				["create"] = Intrinsic.GetByName("canvas_create")!.GetFunc()
			};
			return new Intrinsic.Result(map);
		};
	}

	#endregion

	#region Canvas intrinsics

	private static void CreateCanvasIntrinsics()
	{
		// ---------- canvas_create([w,h]) ----------
		var create = Intrinsic.Create("canvas_create");
		create.AddParam("size");

		create.code = (ctx, _) =>
		{
			try
			{
				if (ctx.interpreter.hostData is not WorldScriptContext world)
					return Intrinsic.Result.Null;

				if (!TryReadPair(ctx.GetVar("size"), out var w, out var h))
					return Error(ctx, "Canvas.create expects [width,height]");

				var canvas = new CanvasMorph(new Size(w, h));
				world.World.AddMorph(canvas);

				var handle = world.Handles.Register(canvas);

				// standard lifecycle methods
				handle["destroy"] =
					Intrinsic.GetByName("morph_destroy")!.GetFunc().BindAndCopy(handle);
				handle["isAlive"] =
					Intrinsic.GetByName("morph_isAlive")!.GetFunc().BindAndCopy(handle);

				// expose script-visible properties
				handle["props"] = canvas.ScriptObject;

				// canvas-specific methods
				handle["clear"] =
					Intrinsic.GetByName("canvas_clear")!.GetFunc().BindAndCopy(handle);
				handle["writePixels"] =
					Intrinsic.GetByName("canvas_writePixels")!.GetFunc().BindAndCopy(handle);
				handle["setPixel"] =
					Intrinsic.GetByName("canvas_setPixel")!.GetFunc().BindAndCopy(handle);
				handle["getPixel"] =
					Intrinsic.GetByName("canvas_getPixel")!.GetFunc().BindAndCopy(handle);

				return new Intrinsic.Result(handle);
			}
			catch (Exception ex)
			{
				return Error(ctx, $"Canvas.create error: {ex.Message}");
			}
		};

		// ---------- canvas_clear(color) ----------
		var clear = Intrinsic.Create("canvas_clear");
		clear.AddParam("color");

		clear.code = (ctx, _) =>
		{
			if (ctx.interpreter.hostData is not WorldScriptContext world)
				return Intrinsic.Result.Null;

			if (world.Handles.ResolveAlive(ctx.self) is not CanvasMorph canvas)
				return Intrinsic.Result.Null;

			if (ctx.GetVar("color") is ValMap map && map.IsColor())
			{
				canvas.Clear(map.ToColor());
			}

			return Intrinsic.Result.Null;
		};

		var writePixels = Intrinsic.Create("canvas_writePixels");
		writePixels.AddParam("pixels");
		writePixels.code = (ctx, _) =>
		{
			if (ctx.interpreter.hostData is not WorldScriptContext world)
				return Intrinsic.Result.Null;

			if (world.Handles.ResolveAlive(ctx.self) is not CanvasMorph canvas)
				return Intrinsic.Result.Null;

			if (ctx.GetVar("pixels") is not ValList list)
				return Intrinsic.Result.Null;

			var count = list.values.Count;
			var buffer = new RadialColor?[count];

			for (int i = 0; i < count; i++)
			{
				if (list.values[i] is ValMap map && map.IsColor())
					buffer[i] = map.ToColor();
				else
					buffer[i] = RadialColor.Black;
			}

			canvas.WritePixels(buffer);
			return Intrinsic.Result.Null;
		};

		// ---------- canvas_setPixel(x,y,color) ----------
		var setPixel = Intrinsic.Create("canvas_setPixel");
		setPixel.AddParam("x");
		setPixel.AddParam("y");
		setPixel.AddParam("color");

		setPixel.code = (ctx, _) =>
		{
			if (ctx.interpreter.hostData is not WorldScriptContext world)
				return Intrinsic.Result.Null;

			if (world.Handles.ResolveAlive(ctx.self) is not CanvasMorph canvas)
				return Intrinsic.Result.Null;

			if (ctx.GetVar("color") is not ValMap map || !map.IsColor())
				return Intrinsic.Result.Null;

			canvas.SetPixel(
				ctx.GetVar("x").IntValue(),
				ctx.GetVar("y").IntValue(),
				map.ToColor()
			);

			return Intrinsic.Result.Null;
		};

		// ---------- canvas_getPixel(x,y) ----------
		var getPixel = Intrinsic.Create("canvas_getPixel");
		getPixel.AddParam("x");
		getPixel.AddParam("y");

		getPixel.code = (ctx, _) =>
		{
			if (ctx.interpreter.hostData is not WorldScriptContext world)
				return Intrinsic.Result.Null;

			if (world.Handles.ResolveAlive(ctx.self) is not CanvasMorph canvas)
				return Intrinsic.Result.Null;

			var color = canvas.GetPixel(
				ctx.GetVar("x").IntValue(),
				ctx.GetVar("y").IntValue()
			);

			return new Intrinsic.Result(color.ToMiniScriptValue());
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