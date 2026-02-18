using Miniscript;
using Userland.Morphic;

namespace Userland.Scripting;

public static class DialogIntrinsics
{
	public static void Register()
	{
		CreateAlertIntrinsic();
		CreatePromptIntrinsic();
		CreateConfirmIntrinsic();
	}

	// TODO: We could stuff these calls into a namespace, but I'm not sure that I want to.
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

				world.WindowService.AlertAsync(message)
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

				world.WindowService.PromptAsync(message, defText)
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

				world.WindowService.ConfirmAsync(message)
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
}