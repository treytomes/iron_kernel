using System.Drawing;
using Miniscript;
using Userland.Morphic;

namespace Userland.Scripting;

public static class BoxIntrinsics
{
	public static void Register()
	{
		CreateSetColorIntrinsic();
		CreateSetBorderColorIntrinsic();
		CreateConsumeClickIntrinsic();
		CreateNamespace();
		CreateCreateIntrinsic();
	}

	private static void CreateNamespace()
	{
		var ns = Intrinsic.Create("Box");
		ns.code = (ctx, _) =>
		{
			var map = new ValMap
			{
				["create"] = Intrinsic.GetByName("box_create")!.GetFunc()
			};
			return new Intrinsic.Result(map);
		};
	}

	private static void CreateCreateIntrinsic()
	{
		var create = Intrinsic.Create("box_create");
		create.AddParam("pos");
		create.AddParam("size");

		create.code = (ctx, _) =>
		{
			try
			{
				if (ctx.interpreter.hostData is not WorldScriptContext world)
					return Intrinsic.Result.Null;

				var posVal = ctx.GetVar("pos");
				if (!posVal.IsPoint())
					return Error(ctx, "Box.create expects pos [x,y]");

				var sizeVal = ctx.GetVar("size");
				if (!sizeVal.IsSize())
					return Error(ctx, "Box.create expects size [w,h]");

				var box = new BoxMorph(posVal.ToPoint(), sizeVal.ToSize());
				world.World.AddMorph(box);

				var handle = world.Handles.Register(box);
				handle["destroy"]      = Intrinsic.GetByName("morph_destroy")!.GetFunc().BindAndCopy(handle);
				handle["isAlive"]      = Intrinsic.GetByName("morph_isAlive")!.GetFunc().BindAndCopy(handle);
				handle["props"]        = box.ScriptObject;
				handle["setColor"]     = Intrinsic.GetByName("box_setColor")!.GetFunc().BindAndCopy(handle);
				handle["setBorderColor"] = Intrinsic.GetByName("box_setBorderColor")!.GetFunc().BindAndCopy(handle);
				handle["consumeClick"] = Intrinsic.GetByName("box_consumeClick")!.GetFunc().BindAndCopy(handle);

				return new Intrinsic.Result(handle);
			}
			catch (Exception ex)
			{
				return Error(ctx, $"Box.create error: {ex.Message}");
			}
		};
	}

	private static void CreateSetColorIntrinsic()
	{
		var fn = Intrinsic.Create("box_setColor");
		fn.AddParam("color");
		fn.code = (ctx, _) =>
		{
			if (ctx.interpreter.hostData is not WorldScriptContext world)
				return Intrinsic.Result.Null;
			if (world.Handles.ResolveAlive(ctx.self) is not BoxMorph box)
				return Intrinsic.Result.Null;

			var c = ctx.GetVar("color");
			if (c is ValMap cm && cm.IsColor())
			{
				box.FillColor = cm.ToFloatColor();
				box.Invalidate();
			}
			else if (c is ValList cl && cl.values.Count == 3)
			{
				box.FillColor = new IronKernel.Common.ValueObjects.Color(
					(float)cl.values[0].FloatValue(),
					(float)cl.values[1].FloatValue(),
					(float)cl.values[2].FloatValue());
				box.Invalidate();
			}
			return Intrinsic.Result.Null;
		};
	}

	private static void CreateSetBorderColorIntrinsic()
	{
		var fn = Intrinsic.Create("box_setBorderColor");
		fn.AddParam("color");
		fn.code = (ctx, _) =>
		{
			if (ctx.interpreter.hostData is not WorldScriptContext world)
				return Intrinsic.Result.Null;
			if (world.Handles.ResolveAlive(ctx.self) is not BoxMorph box)
				return Intrinsic.Result.Null;

			var c = ctx.GetVar("color");
			if (c is ValMap cm && cm.IsColor())
			{
				box.BorderColor = cm.ToFloatColor();
				box.Invalidate();
			}
			else if (c is ValList cl && cl.values.Count == 3)
			{
				box.BorderColor = new IronKernel.Common.ValueObjects.Color(
					(float)cl.values[0].FloatValue(),
					(float)cl.values[1].FloatValue(),
					(float)cl.values[2].FloatValue());
				box.Invalidate();
			}
			return Intrinsic.Result.Null;
		};
	}

	private static void CreateConsumeClickIntrinsic()
	{
		var fn = Intrinsic.Create("box_consumeClick");
		fn.code = (ctx, _) =>
		{
			if (ctx.interpreter.hostData is not WorldScriptContext world)
				return Intrinsic.Result.False;
			if (world.Handles.ResolveAlive(ctx.self) is not BoxMorph box)
				return Intrinsic.Result.False;
			return box.ConsumeClick() ? Intrinsic.Result.True : Intrinsic.Result.False;
		};
	}

	private static Intrinsic.Result Error(TAC.Context ctx, string message)
	{
		ctx.interpreter.errorOutput?.Invoke(message, true);
		return Intrinsic.Result.Null;
	}
}
