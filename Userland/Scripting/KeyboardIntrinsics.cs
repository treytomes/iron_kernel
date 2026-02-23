using IronKernel.Common.ValueObjects;
using Miniscript;
using Userland.Morphic;

namespace Userland.Scripting;

public static class KeyboardIntrinsics
{
	public static void Register()
	{
		CreateIsDown();
		CreateIsUp();
		CreateAnyDown();
		CreateKeyboardNamespace();
	}

	private static void CreateKeyboardNamespace()
	{
		var keyboardNs = Intrinsic.Create("Keyboard");
		keyboardNs.code = (ctx, _) =>
		{
			var map = new ValMap
			{
				["isDown"] = Intrinsic.GetByName("keyboard_isDown")!.GetFunc(),
				["isUp"] = Intrinsic.GetByName("keyboard_isUp")!.GetFunc(),
				["anyDown"] = Intrinsic.GetByName("keyboard_anyDown")!.GetFunc()
			};
			return new Intrinsic.Result(map);
		};
	}

	// ------------------------------------------------------------
	// keyboard_isDown(key)
	// ------------------------------------------------------------
	private static Intrinsic CreateIsDown()
	{
		var fn = Intrinsic.Create("keyboard_isDown");
		fn.AddParam("key");
		fn.code = (ctx, _) =>
		{
			if (ctx.interpreter.hostData is not WorldScriptContext world)
				return Intrinsic.Result.False;

			var keyVal = ctx.GetVar("key");
			if (!TryParseKey(keyVal, out var key))
				return Intrinsic.Result.False;

			return world.Keyboard.KeysDown.Contains(key)
				? Intrinsic.Result.True
				: Intrinsic.Result.False;
		};
		return fn;
	}

	// ------------------------------------------------------------
	// keyboard_isUp(key)
	// ------------------------------------------------------------
	private static Intrinsic CreateIsUp()
	{
		var fn = Intrinsic.Create("keyboard_isUp");
		fn.AddParam("key");
		fn.code = (ctx, _) =>
		{
			if (ctx.interpreter.hostData is not WorldScriptContext world)
				return Intrinsic.Result.False;

			var keyVal = ctx.GetVar("key");
			if (!TryParseKey(keyVal, out var key))
				return Intrinsic.Result.False;

			return world.Keyboard.KeysDown.Contains(key)
				? Intrinsic.Result.False
				: Intrinsic.Result.True;
		};
		return fn;
	}

	// ------------------------------------------------------------
	// keyboard_anyDown()
	// ------------------------------------------------------------
	private static Intrinsic CreateAnyDown()
	{
		var fn = Intrinsic.Create("keyboard_anyDown");
		fn.code = (ctx, _) =>
		{
			if (ctx.interpreter.hostData is not WorldScriptContext world)
				return Intrinsic.Result.False;

			return world.Keyboard.KeysDown.Count > 0
				? Intrinsic.Result.True
				: Intrinsic.Result.False;
		};
		return fn;
	}

	// ------------------------------------------------------------
	// Helpers
	// ------------------------------------------------------------
	private static bool TryParseKey(Value v, out Key key)
	{
		key = default;

		if (v is ValString s &&
			Enum.TryParse<Key>(s.value, ignoreCase: true, out var parsed))
		{
			key = parsed;
			return true;
		}

		return false;
	}
}