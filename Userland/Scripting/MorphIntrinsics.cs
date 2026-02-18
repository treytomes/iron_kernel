using System.Drawing;
using Miniscript;
using Userland.Morphic;

namespace Userland.Scripting;

public static class MorphIntrinsics
{
	public static void Register()
	{
		CreateMorphIntrinsics();
		// CreateMorphNamespace();

		CreateLabelIntrinsics();
		CreateLabelNamespace();
	}

	#region Morph intrinsics

	// private static void CreateMorphNamespace()
	// {
	// 	var morphNs = Intrinsic.Create("Morph");
	// 	morphNs.code = (ctx, _) =>
	// 	{
	// 		var map = new ValMap
	// 		{
	// 			["create"] = Intrinsic.GetByName("morph_create")!.GetFunc()
	// 		};
	// 		return new Intrinsic.Result(map);
	// 	};
	// }

	private static void CreateMorphIntrinsics()
	{
		// // morph_create([x,y], [w,h]) OR morph_create([w,h])
		// var create = Intrinsic.Create("morph_create");
		// create.AddParam("arg1", ValNull.instance);
		// create.AddParam("arg2", ValNull.instance);
		// create.code = (ctx, _) =>
		// {
		// 	try
		// 	{
		// 		if (ctx.interpreter.hostData is not WorldScriptContext world)
		// 			return Intrinsic.Result.Null;

		// 		Point position = Point.Empty;
		// 		Size size = new Size(16, 16);

		// 		var a1 = ctx.GetVar("arg1");
		// 		var a2 = ctx.GetVar("arg2");

		// 		if (a2 == ValNull.instance)
		// 		{
		// 			if (!TryReadPair(a1, out var w, out var h))
		// 				return Error(ctx, "Morph.create expects [w,h] or [x,y],[w,h]");
		// 			size = new Size(w, h);
		// 		}
		// 		else
		// 		{
		// 			if (!TryReadPair(a1, out var x, out var y) ||
		// 				!TryReadPair(a2, out var w, out var h))
		// 				return Error(ctx, "Morph.create expects [x,y],[w,h]");
		// 			position = new Point(x, y);
		// 			size = new Size(w, h);
		// 		}

		// 		var morph = new Morph
		// 		{
		// 			Position = position,
		// 			Size = size
		// 		};

		// 		world.World.AddMorph(morph);

		// 		var handle = world.Handles.Register(morph);
		// 		handle["props"] = morph.ScriptObject;

		// 		return new Intrinsic.Result(handle);
		// 	}
		// 	catch (Exception ex)
		// 	{
		// 		return Error(ctx, $"Morph.create error: {ex.Message}");
		// 	}
		// };

		// morph_destroy()
		var destroy = Intrinsic.Create("morph_destroy");
		destroy.code = (ctx, _) =>
		{
			if (ctx.interpreter.hostData is not WorldScriptContext world)
				return Intrinsic.Result.Null;

			world.Handles.Destroy(ctx.self);
			return Intrinsic.Result.Null;
		};

		// morph_isAlive()
		var isAlive = Intrinsic.Create("morph_isAlive");
		isAlive.code = (ctx, _) =>
		{
			if (ctx.interpreter.hostData is not WorldScriptContext world)
				return Intrinsic.Result.False;

			return world.Handles.ResolveAlive(ctx.self) != null
				? Intrinsic.Result.True
				: Intrinsic.Result.False;
		};
	}

	#endregion

	#region Label intrinsics

	private static void CreateLabelNamespace()
	{
		var labelNs = Intrinsic.Create("Label");
		labelNs.code = (ctx, _) =>
		{
			var map = new ValMap
			{
				["create"] = Intrinsic.GetByName("label_create")!.GetFunc()
			};
			return new Intrinsic.Result(map);
		};
	}

	private static void CreateLabelIntrinsics()
	{
		// label_create([x,y], text)
		var create = Intrinsic.Create("label_create");
		create.AddParam("pos", ValNull.instance);
		create.AddParam("text", ValNull.instance);

		create.code = (ctx, _) =>
		{
			try
			{
				if (ctx.interpreter.hostData is not WorldScriptContext world)
					return Intrinsic.Result.Null;

				Point position = Point.Empty;
				if (ctx.GetVar("pos") != ValNull.instance)
				{
					if (!TryReadPair(ctx.GetVar("pos"), out var x, out var y))
						return Error(ctx, "Label.create expects pos [x,y]");
					position = new Point(x, y);
				}

				var label = new LabelMorph(position);

				if (ctx.GetVar("text") is ValString s)
					label.Text = s.value;

				world.World.AddMorph(label);

				var handle = world.Handles.Register(label);
				handle["destroy"] = Intrinsic.GetByName("morph_destroy")!.GetFunc().BindAndCopy(handle);
				handle["isAlive"] = Intrinsic.GetByName("morph_isAlive")!.GetFunc().BindAndCopy(handle);
				handle["props"] = label.ScriptObject;

				return new Intrinsic.Result(handle);
			}
			catch (Exception ex)
			{
				return Error(ctx, $"Label.create error: {ex.Message}");
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