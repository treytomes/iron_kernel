using IronKernel.Common.ValueObjects;
using IronKernel.Userland.Morphic.Commands;
using Miniscript;
using System.Drawing;

namespace IronKernel.Userland.Morphic;

public sealed class WorldScriptContext
{
	private readonly WorldMorph _world;

	// id → morph
	private readonly Dictionary<int, MiniScriptMorph> _morphs = new();

	// morph → id (for cleanup)
	private readonly Dictionary<MiniScriptMorph, int> _reverse = new();

	private int _nextId = 1;

	public WorldScriptContext(WorldMorph world)
	{
		_world = world;
	}

	// -------------------------------------------------
	// World access
	// -------------------------------------------------
	public WorldMorph World => _world;
	public WorldCommandManager Commands => _world.Commands;
	public ScriptOutputHub Output => _world.ScriptOutput;

	// -------------------------------------------------
	// Script lifecycle helpers
	// -------------------------------------------------
	public void StopWorldScript() => _world.Interpreter.Stop();
	public void ResetWorldScript() => _world.Interpreter.Reset();
	public bool IsRunning => _world.Interpreter.Running();

	// -------------------------------------------------
	// Handle registry
	// -------------------------------------------------
	public ValMap Register(MiniScriptMorph morph)
	{
		var id = _nextId++;
		_morphs[id] = morph;
		_reverse[morph] = id;

		var handle = new ValMap();
		handle["__kind"] = new ValString("MiniScriptMorph");
		handle["__id"] = new ValNumber(id);

		handle["get"] = Intrinsic.GetByName("slot_get")!.GetFunc().BindAndCopy(handle);
		handle["set"] = Intrinsic.GetByName("slot_set")!.GetFunc().BindAndCopy(handle);
		handle["has"] = Intrinsic.GetByName("slot_has")!.GetFunc().BindAndCopy(handle);
		handle["delete"] = Intrinsic.GetByName("slot_delete")!.GetFunc().BindAndCopy(handle);
		handle["destroy"] = Intrinsic.GetByName("morph_destroy")!.GetFunc().BindAndCopy(handle);
		handle["isAlive"] = Intrinsic.GetByName("morph_isAlive")!.GetFunc().BindAndCopy(handle);

		return handle;
	}

	private static Intrinsic.Result Forward(TAC.Context ctx, string intrinsicName, ValMap self)
	{
		var intrinsic = Intrinsic.GetByName(intrinsicName)!;
		var args = new List<Value> { self };

		foreach (var p in intrinsic.GetFunc().function.parameters.Skip(1))
		{
			args.Add(ctx.GetVar(p.name));
		}

		return intrinsic.code(ctx, Intrinsic.Result.Null);
	}

	public MiniScriptMorph? ResolveAlive(Value handle)
	{
		if (handle is not ValMap map)
			return null;

		if (map.TryGetValue(new ValString("__dead"), out var dead) && dead.BoolValue())
			return null;

		if (!map.TryGetValue(new ValString("__id"), out var idVal))
			return null;

		var id = idVal.IntValue();
		if (!_morphs.TryGetValue(id, out var morph))
			return null;

		if (morph.IsMarkedForDeletion || morph.Owner == null)
		{
			InvalidateHandle(map);
			_morphs.Remove(id);
			_reverse.Remove(morph);
			return null;
		}

		return morph;
	}

	private static void InvalidateHandle(ValMap handle)
	{
		handle["__dead"] = ValNumber.one;
	}

	public void OnMorphDestroyed(MiniScriptMorph morph)
	{
		if (_reverse.TryGetValue(morph, out var id))
		{
			_reverse.Remove(morph);
			_morphs.Remove(id);
		}
	}

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

	// -------------------------------------------------
	// Intrinsics
	// -------------------------------------------------
	static WorldScriptContext()
	{
		CreateMorphIntrinsics();
		CreateRadialColorIntrinsics();
	}

	private static void CreateMorphIntrinsics()
	{       // ---------- morph_create ----------
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
				return new Intrinsic.Result(world.Register(morph));
			}
			catch (Exception ex)
			{
				return Error(ctx, $"Morph.create error: {ex.Message}");
			}
		};

		// ---------- morph_destroy ----------
		var destroy = Intrinsic.Create("morph_destroy");
		// destroy.AddParam("handle");

		destroy.code = (ctx, _) =>
		{
			try
			{
				if (ctx.interpreter.hostData is not WorldScriptContext world)
					return Intrinsic.Result.Null;

				var handle = ctx.self; // ctx.GetVar("handle");
				var morph = world.ResolveAlive(handle);
				if (morph == null)
					return Intrinsic.Result.Null;

				morph.MarkForDeletion();
				if (handle is ValMap map)
					InvalidateHandle(map);

				return Intrinsic.Result.Null;
			}
			catch (Exception ex)
			{
				return Error(ctx, $"Morph.destroy error: {ex.Message}");
			}
		};

		// ---------- morph_isAlive ----------
		var isAlive = Intrinsic.Create("morph_isAlive");
		// isAlive.AddParam("handle");

		isAlive.code = (ctx, _) =>
		{
			try
			{
				if (ctx.interpreter.hostData is not WorldScriptContext world)
					return Intrinsic.Result.False;

				// return world.ResolveAlive(ctx.GetVar("handle")) != null
				return world.ResolveAlive(ctx.self) != null
					? Intrinsic.Result.True
					: Intrinsic.Result.False;
			}
			catch
			{
				return Intrinsic.Result.False;
			}
		};

		// ---------- slot_get ----------
		var slotGet = Intrinsic.Create("slot_get");
		// slotGet.AddParam("handle");
		slotGet.AddParam("key");

		slotGet.code = (ctx, _) =>
		{
			try
			{
				if (ctx.interpreter.hostData is not WorldScriptContext world)
					return Intrinsic.Result.Null;

				// var morph = world.ResolveAlive(ctx.GetVar("handle"));
				var morph = world.ResolveAlive(ctx.self);
				if (morph == null)
					return Error(ctx, "slot_get: invalid or dead morph handle");

				return new Intrinsic.Result(
					morph.GetSlot<Value>(ctx.GetVar("key").ToString())
				);
			}
			catch (Exception ex)
			{
				return Error(ctx, $"slot_get error: {ex.Message}");
			}
		};

		// ---------- slot_set ----------
		var slotSet = Intrinsic.Create("slot_set");
		// slotSet.AddParam("handle");
		slotSet.AddParam("key");
		slotSet.AddParam("value");

		slotSet.code = (ctx, _) =>
		{
			try
			{
				if (ctx.interpreter.hostData is not WorldScriptContext world)
					return Intrinsic.Result.Null;

				// var morph = world.ResolveAlive(ctx.GetVar("handle"));
				var morph = world.ResolveAlive(ctx.self);
				if (morph == null)
					return Error(ctx, "slot_set: invalid or dead morph handle");

				morph.SetSlot(
					ctx.GetVar("key").ToString(),
					ctx.GetVar("value")
				);

				return Intrinsic.Result.Null;
			}
			catch (Exception ex)
			{
				return Error(ctx, $"slot_set error: {ex.Message}");
			}
		};

		// ---------- slot_has ----------
		var slotHas = Intrinsic.Create("slot_has");
		// slotHas.AddParam("handle");
		slotHas.AddParam("key");

		slotHas.code = (ctx, _) =>
		{
			try
			{
				if (ctx.interpreter.hostData is not WorldScriptContext world)
					return Intrinsic.Result.False;

				// var morph = world.ResolveAlive(ctx.GetVar("handle"));
				var morph = world.ResolveAlive(ctx.self);
				return morph != null && morph.HasSlot(ctx.GetVar("key").ToString())
					? Intrinsic.Result.True
					: Intrinsic.Result.False;
			}
			catch
			{
				return Intrinsic.Result.False;
			}
		};

		// ---------- slot_delete ----------
		var slotDelete = Intrinsic.Create("slot_delete");
		// slotDelete.AddParam("handle");
		slotDelete.AddParam("key");

		slotDelete.code = (ctx, _) =>
		{
			try
			{
				if (ctx.interpreter.hostData is not WorldScriptContext world)
					return Intrinsic.Result.Null;

				// var morph = world.ResolveAlive(ctx.GetVar("handle"));
				var morph = world.ResolveAlive(ctx.self);
				morph?.DeleteSlot(ctx.GetVar("key").ToString());

				return Intrinsic.Result.Null;
			}
			catch (Exception ex)
			{
				return Error(ctx, $"slot_delete error: {ex.Message}");
			}
		};

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

				foreach (var (id, morph) in world._morphs)
				{
					if (morph.IsMarkedForDeletion || morph.Owner == null)
						continue;

					if (!morph.HasSlot(key))
						continue;

					if (value != ValNull.instance)
					{
						var slotVal = morph.GetSlot<Value>(key);
						if (slotVal == null || slotVal.Equality(value) == 0)
							continue;
					}

					var handle = new ValMap();
					handle["__kind"] = new ValString("MiniScriptMorph");
					handle["__id"] = new ValNumber(id);
					list.values.Add(handle);
				}

				return new Intrinsic.Result(list);
			}
			catch (Exception ex)
			{
				return Error(ctx, $"world_findMorphsBySlot error: {ex.Message}");
			}
		};

		// ---------- Morph namespace ----------
		var morphNamespace = Intrinsic.Create("Morph");
		morphNamespace.code = (ctx, _) =>
		{
			var map = new ValMap();

			// --- construction ---
			map["create"] = Intrinsic.GetByName("morph_create")!.GetFunc();
			// map["destroy"] = Intrinsic.GetByName("morph_destroy")!.GetFunc();
			// map["isAlive"] = Intrinsic.GetByName("morph_isAlive")!.GetFunc();

			// --- slot access ---
			// map["get"] = Intrinsic.GetByName("slot_get")!.GetFunc();
			// map["set"] = Intrinsic.GetByName("slot_set")!.GetFunc();
			// map["has"] = Intrinsic.GetByName("slot_has")!.GetFunc();
			// map["delete"] = Intrinsic.GetByName("slot_delete")!.GetFunc();

			// --- queries ---
			map["findBySlot"] = Intrinsic.GetByName("world_findMorphsBySlot")!.GetFunc();

			return new Intrinsic.Result(map);
		};
	}

	private static void CreateRadialColorIntrinsics()
	{
		// ---------- RadialColor.create ----------
		var radialColorCreate = Intrinsic.Create("RadialColor_create");
		radialColorCreate.AddParam("r");
		radialColorCreate.AddParam("g");
		radialColorCreate.AddParam("b");

		radialColorCreate.code = (ctx, _) =>
		{
			try
			{
				var r = (byte)ctx.GetVar("r").IntValue();
				var g = (byte)ctx.GetVar("g").IntValue();
				var b = (byte)ctx.GetVar("b").IntValue();

				// Let RadialColor enforce its own constraints (0..5)
				var color = new RadialColor(r, g, b);

				return new Intrinsic.Result(color.ToMiniScriptValue());
			}
			catch (Exception ex)
			{
				ctx.interpreter.errorOutput?.Invoke(
					$"RadialColor.create error: {ex.Message}",
					true
				);
				return Intrinsic.Result.Null;
			}
		};

		// ---------- RadialColor namespace ----------
		var radialColorNamespace = Intrinsic.Create("RadialColor");
		radialColorNamespace.code = (ctx, _) =>
		{
			var map = new ValMap();
			map["create"] = radialColorCreate.GetFunc();
			return new Intrinsic.Result(map);
		};
	}
}