using IronKernel.Common.ValueObjects;
using IronKernel.Userland.Morphic.Commands;
using IronKernel.Userland.Services;
using Miniscript;
using System.Drawing;

namespace IronKernel.Userland.Morphic;

public sealed class WorldScriptContext
{
	#region Fields

	private readonly WorldMorph _world;
	private readonly IWindowService _windowService;

	// id → morph
	private readonly Dictionary<int, MiniScriptMorph> _morphs = new();

	// morph → id (for cleanup)
	private readonly Dictionary<MiniScriptMorph, int> _reverse = new();

	private int _nextId = 1;

	#endregion

	#region Constructions

	public WorldScriptContext(WorldMorph world, IServiceProvider services)
	{
		_world = world;

		// TODO: I hate instantiating like this, but there's a circular reference between WorldMorph and WindowService now.
		_windowService = new WindowService(world, services);
	}

	#endregion

	#region World access

	public WorldMorph World => _world;
	public WorldCommandManager Commands => _world.Commands;
	public ScriptOutputHub Output => _world.ScriptOutput;

	public void StopWorldScript() => _world.Interpreter.Stop();
	public void ResetWorldScript() => _world.Interpreter.Reset();
	public bool IsRunning => _world.Interpreter.Running();

	#endregion

	#region Handle registry

	public ValMap Register(MiniScriptMorph morph)
	{
		var id = _nextId++;
		_morphs[id] = morph;
		_reverse[morph] = id;
		return CreateHandleForId(id);
	}

	private ValMap CreateHandleForId(int id)
	{
		var handle = new ValMap
		{
			["__isa"] = new ValString("Morph"),
			["__id"] = new ValNumber(id)
		};

		AttachMorphMethods(handle);
		return handle;
	}

	private static void AttachMorphMethods(ValMap handle)
	{
		handle["get"] = Intrinsic.GetByName("slot_get")!.GetFunc().BindAndCopy(handle);
		handle["set"] = Intrinsic.GetByName("slot_set")!.GetFunc().BindAndCopy(handle);
		handle["has"] = Intrinsic.GetByName("slot_has")!.GetFunc().BindAndCopy(handle);
		handle["delete"] = Intrinsic.GetByName("slot_delete")!.GetFunc().BindAndCopy(handle);
		handle["destroy"] = Intrinsic.GetByName("morph_destroy")!.GetFunc().BindAndCopy(handle);
		handle["isAlive"] = Intrinsic.GetByName("morph_isAlive")!.GetFunc().BindAndCopy(handle);
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
	#endregion

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

	#region Intrinsics

	static WorldScriptContext()
	{
		CreateDialogIntrinsics();
		CreateMorphIntrinsics();
		CreateMorphNamespace();
		CreateRadialColorIntrinsics();
	}

	private static void CreateDialogIntrinsics()
	{
		CreateAlertIntrinsic();
		CreatePromptIntrinsic();
		CreateConfirmIntrinsic();
	}

	private static void CreateAlertIntrinsic()
	{
		var alert = Intrinsic.Create("alert");
		alert.AddParam("message");

		alert.code = (ctx, partialResult) =>
		{
			// First call
			if (partialResult == null)
			{
				if (ctx.interpreter.hostData is not WorldScriptContext world)
					return Intrinsic.Result.Null;

				var message = ctx.GetVar("message").ToString();

				// MiniScript-visible state
				var state = new ValMap();
				state["done"] = ValNumber.zero;

				world._windowService.AlertAsync(message)
					.ContinueWith(_ =>
					{
						// SAFE: mutating ValMap only
						state["done"] = ValNumber.one;
					});

				return new Intrinsic.Result(state, done: false);
			}

			// Subsequent calls
			var map = partialResult.result as ValMap;
			if (map == null)
				return Intrinsic.Result.Null;

			if (map["done"].BoolValue())
				return Intrinsic.Result.Null;

			return partialResult; // still waiting
		};
	}

	private static void CreatePromptIntrinsic()
	{
		var prompt = Intrinsic.Create("prompt");
		prompt.AddParam("message");
		prompt.AddParam("default", ValNull.instance);

		prompt.code = (ctx, partialResult) =>
		{
			if (partialResult == null)
			{
				if (ctx.interpreter.hostData is not WorldScriptContext world)
					return Intrinsic.Result.Null;

				var message = ctx.GetVar("message").ToString();
				var def = ctx.GetVar("default");
				string? defText = def == ValNull.instance ? null : def.ToString();

				var state = new ValMap
				{
					["done"] = ValNumber.zero,
					["value"] = ValNull.instance
				};

				world._windowService.PromptAsync(message, defText)
					.ContinueWith(t =>
					{
						state["value"] = t.Result == null
							? ValNull.instance
							: new ValString(t.Result);
						state["done"] = ValNumber.one;
					});

				return new Intrinsic.Result(state, done: false);
			}

			var map = (ValMap)partialResult.result;
			if (!map["done"].BoolValue())
				return partialResult;

			return new Intrinsic.Result(map["value"]);
		};
	}

	private static void CreateConfirmIntrinsic()
	{
		var confirm = Intrinsic.Create("confirm");
		confirm.AddParam("message");

		confirm.code = (ctx, partialResult) =>
		{
			if (partialResult == null)
			{
				if (ctx.interpreter.hostData is not WorldScriptContext world)
					return Intrinsic.Result.False;

				var message = ctx.GetVar("message").ToString();

				var state = new ValMap
				{
					["done"] = ValNumber.zero,
					["result"] = ValNumber.zero
				};

				world._windowService.ConfirmAsync(message)
					.ContinueWith(t =>
					{
						state["result"] = t.Result ? ValNumber.one : ValNumber.zero;
						state["done"] = ValNumber.one;
					});

				return new Intrinsic.Result(state, done: false);
			}

			var map = (ValMap)partialResult.result;
			if (!map["done"].BoolValue())
				return partialResult;

			return map["result"].BoolValue()
				? Intrinsic.Result.True
				: Intrinsic.Result.False;
		};
	}

	// private static void CreateDialogNamespace()
	// {
	// 	var ns = Intrinsic.Create("Dialog");
	// 	ns.code = (ctx, _) =>
	// 	{
	// 		var map = new ValMap
	// 		{
	// 			["alert"] = Intrinsic.GetByName("alert")!.GetFunc(),
	// 			["prompt"] = Intrinsic.GetByName("prompt")!.GetFunc(),
	// 			["confirm"] = Intrinsic.GetByName("confirm")!.GetFunc()
	// 		};
	// 		return new Intrinsic.Result(map);
	// 	};
	// }

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
				return new Intrinsic.Result(world.Register(morph));
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

				var morph = world.ResolveAlive(ctx.self);
				if (morph == null)
					return Intrinsic.Result.Null;

				morph.MarkForDeletion();
				if (ctx.self is ValMap map)
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
		isAlive.code = (ctx, _) =>
		{
			if (ctx.interpreter.hostData is not WorldScriptContext world)
				return Intrinsic.Result.False;

			return world.ResolveAlive(ctx.self) != null
				? Intrinsic.Result.True
				: Intrinsic.Result.False;
		};

		// ---------- slot_* ----------
		CreateSlotIntrinsic("slot_get", (ctx, world) =>
		{
			var morph = world.ResolveAlive(ctx.self);
			if (morph == null)
				return Error(ctx, "slot_get: invalid or dead morph handle");

			return new Intrinsic.Result(
				morph.GetSlot<Value>(ctx.GetVar("key").ToString())
			);
		}, "key");

		CreateSlotIntrinsic("slot_set", (ctx, world) =>
		{
			var morph = world.ResolveAlive(ctx.self);
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
			var morph = world.ResolveAlive(ctx.self);
			return morph != null && morph.HasSlot(ctx.GetVar("key").ToString())
				? Intrinsic.Result.True
				: Intrinsic.Result.False;
		}, "key");

		CreateSlotIntrinsic("slot_delete", (ctx, world) =>
		{
			var morph = world.ResolveAlive(ctx.self);
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

					list.values.Add(world.CreateHandleForId(id));
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

	private static void CreateRadialColorIntrinsics()
	{
		var create = Intrinsic.Create("RadialColor_create");
		create.AddParam("r");
		create.AddParam("g");
		create.AddParam("b");

		create.code = (ctx, _) =>
		{
			try
			{
				var color = new RadialColor(
					(byte)ctx.GetVar("r").IntValue(),
					(byte)ctx.GetVar("g").IntValue(),
					(byte)ctx.GetVar("b").IntValue()
				);
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

		var ns = Intrinsic.Create("RadialColor");
		ns.code = (ctx, _) =>
		{
			var map = new ValMap { ["create"] = create.GetFunc() };
			return new Intrinsic.Result(map);
		};
	}

	#endregion
}