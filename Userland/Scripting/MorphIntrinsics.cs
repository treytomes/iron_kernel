using System.Drawing;
using Miniscript;
using Userland.Morphic;

namespace Userland.Scripting;

public static class MorphIntrinsics
{
	public static void Register()
	{
		CreateMorphIntrinsics();
		CreateMorphNamespace();
	}

	private static void CreateMorphNamespace()
	{
		var morphNamespace = Intrinsic.Create("Morph");
		morphNamespace.code = (ctx, _) =>
		{
			var map = new ValMap
			{
				["create"] = Intrinsic.GetByName("morph_create")!.GetFunc(),
				["findBySlot"] = Intrinsic.GetByName("world_findMorphsBySlot")!.GetFunc()
			};
			return new Intrinsic.Result(map);
		};
	}

	private static void CreateMorphIntrinsics()
	{
		// ---------- morph_create ----------
		var create = Intrinsic.Create("morph_create");
		create.AddParam("arg1", ValNull.instance);
		create.AddParam("arg2", ValNull.instance);

		create.code = (ctx, _) =>
		{
			try
			{
				if (ctx.interpreter.hostData is not WorldScriptContext world)
					return Intrinsic.Result.Null;

				var arg1 = ctx.GetVar("arg1");
				var arg2 = ctx.GetVar("arg2");

				Point position = Point.Empty;
				Size size = new Size(16, 16);

				if (arg2 == ValNull.instance)
				{
					if (!TryReadPair(arg1, out var w, out var h))
						return Error(ctx, "Morph.create expects [w,h] or [x,y],[w,h]");
					size = new Size(w, h);
				}
				else
				{
					if (!TryReadPair(arg1, out var x, out var y) ||
						!TryReadPair(arg2, out var w, out var h))
						return Error(ctx, "Morph.create expects [x,y],[w,h]");
					position = new Point(x, y);
					size = new Size(w, h);
				}

				var morph = new MiniScriptMorph
				{
					Position = position,
					Size = size
				};

				world.World.AddMorph(morph);
				return new Intrinsic.Result(world.Handles.Register(morph));
			}
			catch (Exception ex)
			{
				return Error(ctx, $"Morph.create error: {ex.Message}");
			}
		};

		// ---------- morph_destroy ----------
		var destroy = Intrinsic.Create("morph_destroy");
		destroy.code = (ctx, _) =>
		{
			try
			{
				if (ctx.interpreter.hostData is not WorldScriptContext world)
					return Intrinsic.Result.Null;

				world.Handles.Destroy(ctx.self);
				return Intrinsic.Result.Null;
			}
			catch (Exception ex)
			{
				return Error(ctx, $"Morph.destroy error: {ex.Message}");
			}
		};

		// ---------- morph_isAlive ----------
		var isAlive = Intrinsic.Create("morph_isAlive");
		isAlive.code = (ctx, _) =>
		{
			if (ctx.interpreter.hostData is not WorldScriptContext world)
				return Intrinsic.Result.False;

			return world.Handles.ResolveAlive(ctx.self) != null
				? Intrinsic.Result.True
				: Intrinsic.Result.False;
		};

		// ---------- slot_* ----------
		CreateSlotIntrinsic("slot_get", (ctx, world) =>
		{
			var morph = world.Handles.ResolveAlive(ctx.self);
			if (morph == null)
				return Error(ctx, "slot_get: invalid or dead morph handle");

			return new Intrinsic.Result(
				morph.GetSlot<Value>(ctx.GetVar("key").ToString())
			);
		}, "key");

		CreateSlotIntrinsic("slot_set", (ctx, world) =>
		{
			var morph = world.Handles.ResolveAlive(ctx.self);
			if (morph == null)
				return Error(ctx, "slot_set: invalid or dead morph handle");

			morph.SetSlot(
				ctx.GetVar("key").ToString(),
				ctx.GetVar("value")
			);
			return Intrinsic.Result.Null;
		}, "key", "value");

		CreateSlotIntrinsic("slot_has", (ctx, world) =>
		{
			var morph = world.Handles.ResolveAlive(ctx.self);
			return morph != null && morph.HasSlot(ctx.GetVar("key").ToString())
				? Intrinsic.Result.True
				: Intrinsic.Result.False;
		}, "key");

		CreateSlotIntrinsic("slot_delete", (ctx, world) =>
		{
			var morph = world.Handles.ResolveAlive(ctx.self);
			morph?.DeleteSlot(ctx.GetVar("key").ToString());
			return Intrinsic.Result.Null;
		}, "key");

		// ---------- world_findMorphsBySlot ----------
		var find = Intrinsic.Create("world_findMorphsBySlot");
		find.AddParam("key");
		find.AddParam("value", ValNull.instance);

		find.code = (ctx, _) =>
		{
			try
			{
				if (ctx.interpreter.hostData is not WorldScriptContext world)
					return Intrinsic.Result.Null;

				var key = ctx.GetVar("key").ToString();
				var value = ctx.GetVar("value");
				var list = new ValList();

				foreach (var handle in world.Handles.EnumerateAliveHandles())
				{
					var morph = world.Handles.ResolveAlive(handle)!;

					if (!morph.HasSlot(key))
						continue;

					if (value != ValNull.instance)
					{
						var slotVal = morph.GetSlot<Value>(key);
						if (slotVal == null || slotVal.Equality(value) == 0)
							continue;
					}

					list.values.Add(handle);
				}

				return new Intrinsic.Result(list);
			}
			catch (Exception ex)
			{
				return Error(ctx, $"world_findMorphsBySlot error: {ex.Message}");
			}
		};
	}

	private static void CreateSlotIntrinsic(string name, Func<TAC.Context, WorldScriptContext, Intrinsic.Result> body, params string[] parameters)
	{
		var i = Intrinsic.Create(name);
		foreach (var p in parameters)
			i.AddParam(p);

		i.code = (ctx, _) =>
		{
			try
			{
				if (ctx.interpreter.hostData is not WorldScriptContext world)
					return Intrinsic.Result.Null;

				return body(ctx, world);
			}
			catch (Exception ex)
			{
				return Error(ctx, $"{name} error: {ex.Message}");
			}
		};
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

	private static Intrinsic.Result Error(TAC.Context ctx, string message)
	{
		ctx.interpreter.errorOutput?.Invoke(message, true);
		return Intrinsic.Result.Null;
	}
}