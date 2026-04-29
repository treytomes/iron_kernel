using System.Drawing;
using Miniscript;
using Userland.Morphic;

namespace Userland.Scripting;

public static class MorphIntrinsics
{
	public static void Register()
	{
		CreateMorphIntrinsics();
		CreateLabelIntrinsics();
		CreateLabelNamespace();
		CreateWindowIntrinsics();
		CreateWindowNamespace();
	}

	#region Morph intrinsics

	private static void CreateMorphIntrinsics()
	{
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

	#region Window intrinsics

	private static void CreateWindowNamespace()
	{
		var ns = Intrinsic.Create("Window");
		ns.code = (ctx, _) =>
		{
			var map = new ValMap
			{
				["create"] = Intrinsic.GetByName("window_create")!.GetFunc()
			};
			return new Intrinsic.Result(map);
		};
	}

	private static void CreateWindowIntrinsics()
	{
		var contentOrigin = Intrinsic.Create("window_contentOrigin");
		contentOrigin.code = (ctx, _) =>
		{
			if (ctx.interpreter.hostData is not WorldScriptContext world)
				return Intrinsic.Result.Null;
			if (world.Handles.ResolveAlive(ctx.self) is not WindowMorph win)
				return Intrinsic.Result.Null;

			// Walk the owner chain from Content to the world, accumulating positions.
			var x = 0;
			var y = 0;
			for (Morph? m = win.Content; m != null && m is not WorldMorph; m = m.Owner)
			{
				x += m.Position.X;
				y += m.Position.Y;
			}
			var result = new ValList();
			result.values.Add(new ValNumber(x));
			result.values.Add(new ValNumber(y));
			return new Intrinsic.Result(result);
		};

		var addMorph = Intrinsic.Create("window_addMorph");
		addMorph.AddParam("child");
		addMorph.code = (ctx, _) =>
		{
			if (ctx.interpreter.hostData is not WorldScriptContext world)
				return Intrinsic.Result.Null;
			if (world.Handles.ResolveAlive(ctx.self) is not WindowMorph win)
				return Intrinsic.Result.Null;
			var childHandle = ctx.GetVar("child") as ValMap;
			if (childHandle == null)
				return Intrinsic.Result.Null;
			if (world.Handles.ResolveAlive(childHandle) is not Morph childMorph)
				return Intrinsic.Result.Null;
			win.Content.AddMorph(childMorph);
			return Intrinsic.Result.Null;
		};

		var create = Intrinsic.Create("window_create");
		create.AddParam("pos", ValNull.instance);
		create.AddParam("size", ValNull.instance);
		create.AddParam("title", new ValString(""));

		create.code = (ctx, _) =>
		{
			try
			{
				if (ctx.interpreter.hostData is not WorldScriptContext world)
					return Intrinsic.Result.Null;

				if (!TryReadPair(ctx.GetVar("pos"), out var wx, out var wy))
					return Error(ctx, "Window.create expects pos [x,y]");

				if (!TryReadPair(ctx.GetVar("size"), out var ww, out var wh))
					return Error(ctx, "Window.create expects size [w,h]");

				var title = ctx.GetVar("title")?.ToString() ?? "";

				var window = new WindowMorph(new Point(wx, wy), new Size(ww, wh), title);
				world.World.AddMorph(window);

				var handle = world.Handles.Register(window);
				handle["destroy"] = Intrinsic.GetByName("morph_destroy")!.GetFunc().BindAndCopy(handle);
				handle["isAlive"] = Intrinsic.GetByName("morph_isAlive")!.GetFunc().BindAndCopy(handle);
				handle["props"] = window.ScriptObject;
				handle["addMorph"] = Intrinsic.GetByName("window_addMorph")!.GetFunc().BindAndCopy(handle);
				handle["headerHeight"] = new ValNumber(window.HeaderHeight);
				handle["contentOrigin"] = Intrinsic.GetByName("window_contentOrigin")!.GetFunc().BindAndCopy(handle);

				return new Intrinsic.Result(handle);
			}
			catch (Exception ex)
			{
				return Error(ctx, $"Window.create error: {ex.Message}");
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