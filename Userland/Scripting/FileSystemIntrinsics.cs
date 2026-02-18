using Miniscript;
using Userland.Morphic;

namespace Userland.Scripting;

public static class FileSystemIntrinsics
{
	public static void Register()
	{
		CreateRunIntrinsic();
		CreateEditIntrinsic();
	}

	private static void CreateRunIntrinsic()
	{
		var run = Intrinsic.Create("run");
		run.AddParam("filename");

		run.code = (ctx, partialResult) =>
		{
			try
			{
				if (partialResult == null)
				{
					if (ctx.interpreter.hostData is not WorldScriptContext world)
						return Intrinsic.Result.Null;

					var filename = ctx.GetVar("filename")?.ToString();
					if (string.IsNullOrWhiteSpace(filename))
					{
						ctx.interpreter.errorOutput?.Invoke(
							"run(filename): filename is required", true);
						return Intrinsic.Result.Null;
					}

					var state = new ValMap
					{
						["done"] = ValNumber.zero,
						["error"] = ValNull.instance
					};

					world.FileSystem.ReadTextAsync(filename)
						.ContinueWith(t =>
						{
							if (t.IsFaulted)
							{
								state["error"] = new ValString(
									t.Exception?.GetBaseException().Message
									?? "Failed to read file");
							}
							else
							{
								// Store source OUTSIDE the interpreter
								world.PendingRunSource = t.Result;
							}
							state["done"] = ValNumber.one;
						});

					return new Intrinsic.Result(state, done: false);
				}

				var map = partialResult.result as ValMap;
				if (map == null || !map["done"].BoolValue())
					return partialResult;

				if (map["error"] != ValNull.instance)
				{
					ctx.interpreter.errorOutput?.Invoke(
						map["error"].ToString(), true);
				}

				// Do NOT touch the interpreter here
				return Intrinsic.Result.Null;
			}
			catch (Exception ex)
			{
				ctx.interpreter.errorOutput?.Invoke(ex.Message, true);
				return Intrinsic.Result.Null;
			}
		};
	}

	private static void CreateEditIntrinsic()
	{
		var edit = Intrinsic.Create("edit");
		edit.AddParam("filename", ValNull.instance);

		edit.code = (ctx, partialResult) =>
		{
			// First invocation
			if (partialResult == null)
			{
				if (ctx.interpreter.hostData is not WorldScriptContext world)
					return Intrinsic.Result.Null;

				var filenameVal = ctx.GetVar("filename");
				string? filename =
					filenameVal == ValNull.instance ? null : filenameVal.ToString();

				// MiniScript-visible state
				var state = new ValMap
				{
					["done"] = ValNumber.zero
				};

				// Kick off async file open
				world.WindowService.EditFileAsync(filename).ContinueWith(_ =>
				{
					// Mark intrinsic as complete
					state["done"] = ValNumber.one;
				});

				return new Intrinsic.Result(state, done: false);
			}

			// Subsequent invocations
			var map = partialResult.result as ValMap;
			if (map == null)
				return Intrinsic.Result.Null;

			if (!map["done"].BoolValue())
				return partialResult; // still waiting

			// Finished
			return Intrinsic.Result.Null;
		};
	}
}