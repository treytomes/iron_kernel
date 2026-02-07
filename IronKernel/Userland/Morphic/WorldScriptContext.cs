using System.Drawing;
using IronKernel.Userland.Morphic;
using IronKernel.Userland.Morphic.Commands;
using Miniscript;

namespace IronKernel.Userland.Morphic;

public sealed class WorldScriptContext
{
	private readonly WorldMorph _world;
	private readonly Dictionary<int, MiniScriptMorph> _morphs = new();
	private int _nextId = 1;

	public WorldScriptContext(WorldMorph world)
	{
		_world = world;
	}

	// --- Core world access ---

	public WorldMorph World => _world;

	public WorldCommandManager Commands => _world.Commands;

	public ScriptOutputHub Output => _world.ScriptOutput;

	// --- Script lifecycle helpers ---

	public void StopWorldScript()
	{
		_world.Interpreter.Stop();
	}

	public void ResetWorldScript()
	{
		_world.Interpreter.Reset();
	}

	// --- Convenience / safety wrappers (grow over time) ---

	public bool IsRunning => _world.Interpreter.Running();

	public ValMap Register(MiniScriptMorph morph)
	{
		var id = _nextId++;
		_morphs[id] = morph;

		var handle = new ValMap();
		handle["__kind"] = new ValString("MiniScriptMorph");
		handle["__id"] = new ValNumber(id);

		return handle;
	}

	public MiniScriptMorph? Resolve(Value handle)
	{
		if (handle is not ValMap map)
			return null;

		if (!map.TryGetValue(new ValString("__id"), out var idVal))
			return null;

		var id = idVal.IntValue();
		return _morphs.TryGetValue(id, out var morph) ? morph : null;
	}

	static WorldScriptContext()
	{
		// --- morph_create(arg1, arg2) ---
		var create = Intrinsic.Create("morph_create");

		// Accept up to 2 parameters
		create.AddParam("arg1", ValNull.instance);
		create.AddParam("arg2", ValNull.instance);

		create.code = (ctx, _) =>
		{
			if (ctx.interpreter.hostData is not WorldScriptContext world)
				return Intrinsic.Result.Null;

			// Helper to read a 2-number list as a Point or Size
			static bool TryReadPair(Value v, out int a, out int b)
			{
				a = b = 0;
				if (v is not ValList list || list.values.Count != 2)
					return false;

				a = list.values[0].IntValue();
				b = list.values[1].IntValue();
				return true;
			}

			var arg1 = ctx.GetVar("arg1");
			var arg2 = ctx.GetVar("arg2");

			var position = Point.Empty;
			var size = new Size(16, 16); // sensible default

			if (arg2 == ValNull.instance)
			{
				// One parameter: size only
				if (!TryReadPair(arg1, out var w, out var h))
					return Intrinsic.Result.Null;

				size = new Size(w, h);
			}
			else
			{
				// Two parameters: position, size
				if (!TryReadPair(arg1, out var x, out var y))
					return Intrinsic.Result.Null;
				if (!TryReadPair(arg2, out var w, out var h))
					return Intrinsic.Result.Null;

				position = new Point(x, y);
				size = new Size(w, h);
			}

			var morph = new MiniScriptMorph
			{
				Position = position,
				Size = size
			};

			world.World.AddMorph(morph);

			var handle = world.Register(morph);
			return new Intrinsic.Result(handle);
		};

		// --- slot_get(handle, key) ---
		var slotGet = Intrinsic.Create("slot_get");
		slotGet.AddParam("handle");
		slotGet.AddParam("key");
		slotGet.code = (ctx, _) =>
		{
			if (ctx.interpreter.hostData is not WorldScriptContext world)
				return Intrinsic.Result.Null;

			var morph = world.Resolve(ctx.GetVar("handle"));
			if (morph == null)
				return Intrinsic.Result.Null;

			return new Intrinsic.Result(
				morph.GetSlot<Value>(ctx.GetVar("key").ToString())
			);
		};

		// --- slot_set(handle, key, value) ---
		var slotSet = Intrinsic.Create("slot_set");
		slotSet.AddParam("handle");
		slotSet.AddParam("key");
		slotSet.AddParam("value");
		slotSet.code = (ctx, _) =>
		{
			if (ctx.interpreter.hostData is not WorldScriptContext world)
				return Intrinsic.Result.Null;

			var morph = world.Resolve(ctx.GetVar("handle"));
			if (morph != null)
			{
				morph.SetSlot(
					ctx.GetVar("key").ToString(),
					ctx.GetVar("value")
				);
			}
			return Intrinsic.Result.Null;
		};

		// --- slot_has(handle, key) ---
		var slotHas = Intrinsic.Create("slot_has");
		slotHas.AddParam("handle");
		slotHas.AddParam("key");
		slotHas.code = (ctx, _) =>
		{
			if (ctx.interpreter.hostData is not WorldScriptContext world)
				return Intrinsic.Result.False;

			var morph = world.Resolve(ctx.GetVar("handle"));
			return morph != null && morph.HasSlot(ctx.GetVar("key").ToString())
				? Intrinsic.Result.True
				: Intrinsic.Result.False;
		};

		// --- slot_delete(handle, key) ---
		var slotDelete = Intrinsic.Create("slot_delete");
		slotDelete.AddParam("handle");
		slotDelete.AddParam("key");
		slotDelete.code = (ctx, _) =>
		{
			if (ctx.interpreter.hostData is not WorldScriptContext world)
				return Intrinsic.Result.Null;

			var morph = world.Resolve(ctx.GetVar("handle"));
			morph?.DeleteSlot(ctx.GetVar("key").ToString());
			return Intrinsic.Result.Null;
		};
	}
}