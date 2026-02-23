using Miniscript;
using Userland.Morphic;

namespace Userland.Scripting;

public static class FileSystemIntrinsics
{
	public static void Register()
	{
		CreateImportIntrinsic();
		CreateRunIntrinsic();
		CreateEditIntrinsic();
	}

	private static void CreateImportIntrinsic()
	{
		var import = Intrinsic.Create("import");
		import.AddParam("name");

		import.code = (ctx, partialResult) =>
		{
			// SECOND INVOCATION:
			// Import function finished; retrieve result and bind into caller scope.
			if (partialResult != null)
			{
				// Result of import function is stored in temp 0
				if (ctx.GetTemp(0) is not Value imported)
					return Intrinsic.Result.Null;

				// Bind to caller scope under module name
				var caller = ctx.parent;
				caller?.SetVar(partialResult.result.ToString(), imported);

				return Intrinsic.Result.Null;
			}

			// FIRST INVOCATION:
			if (ctx.interpreter.hostData is not WorldScriptContext world)
				return Intrinsic.Result.Null;

			var nameVal = ctx.GetVar("name");
			var libName = nameVal?.ToString();
			if (string.IsNullOrWhiteSpace(libName))
				throw new RuntimeException("import(name): name is required");

			// Resolve file: file://name.ms
			var uri = $"file://{libName}.ms";

			string source;
			try
			{
				// Synchronous read is REQUIRED here.
				// import must be VM-driven, not async-driven.
				source = world.FileSystem.ReadText(uri);
			}
			catch (Exception ex)
			{
				throw new RuntimeException($"import: failed to read {libName}: {ex.Message}");
			}

			// Parse module source
			var parser = new Parser();
			parser.errorContext = $"{libName}.ms";
			parser.Parse(source);

			// Create implicit import function (returns locals unless overridden)
			var fn = parser.CreateImport();

			// Push function call onto VM stack
			// Result goes into temp 0
			ctx.vm.ManuallyPushCall(
				new ValFunction(fn),
				new ValTemp(0)
			);

			// Return partial result containing module name;
			// VM will resume us after function finishes.
			return new Intrinsic.Result(new ValString(libName), done: false);
		};
	}

	private static void CreateRunIntrinsic()
	{
		var run = Intrinsic.Create("run");
		run.AddParam("filename");

		run.code = (ctx, partialResult) =>
		{
			if (ctx.interpreter.hostData is not WorldScriptContext world)
				return Intrinsic.Result.Null;

			var filename = ctx.GetVar("filename")?.ToString();
			if (string.IsNullOrWhiteSpace(filename))
				throw new RuntimeException("run(filename): filename is required");
			filename = $"file://{filename}.ms";

			string source;
			try
			{
				source = world.FileSystem.ReadText(filename);
			}
			catch (Exception ex)
			{
				throw new RuntimeException($"run: failed to read file: {ex.Message}");
			}

			// Stop current script and schedule new one
			world.PendingRunSource = source;

			// Force VM to yield / stop execution
			ctx.vm.yielding = true;

			// run never returns a value
			return Intrinsic.Result.Null;
		};
	}

	// private static void CreateRunIntrinsic()
	// {
	// 	var run = Intrinsic.Create("run");
	// 	run.AddParam("filename");

	// 	run.code = (ctx, partialResult) =>
	// 	{
	// 		try
	// 		{
	// 			if (partialResult == null)
	// 			{
	// 				if (ctx.interpreter.hostData is not WorldScriptContext world)
	// 					return Intrinsic.Result.Null;

	// 				var filename = ctx.GetVar("filename")?.ToString();
	// 				if (string.IsNullOrWhiteSpace(filename))
	// 				{
	// 					ctx.interpreter.errorOutput?.Invoke(
	// 						"run(filename): filename is required", true);
	// 					return Intrinsic.Result.Null;
	// 				}

	// 				var state = new ValMap
	// 				{
	// 					["done"] = ValNumber.zero,
	// 					["error"] = ValNull.instance
	// 				};

	// 				world.FileSystem.ReadTextAsync(filename)
	// 					.ContinueWith(t =>
	// 					{
	// 						if (t.IsFaulted)
	// 						{
	// 							state["error"] = new ValString(
	// 								t.Exception?.GetBaseException().Message
	// 								?? "Failed to read file");
	// 						}
	// 						else
	// 						{
	// 							// Store source OUTSIDE the interpreter
	// 							world.PendingRunSource = t.Result;
	// 						}
	// 						state["done"] = ValNumber.one;
	// 					});

	// 				return new Intrinsic.Result(state, done: false);
	// 			}

	// 			var map = partialResult.result as ValMap;
	// 			if (map == null || !map["done"].BoolValue())
	// 				return partialResult;

	// 			if (map["error"] != ValNull.instance)
	// 			{
	// 				ctx.interpreter.errorOutput?.Invoke(
	// 					map["error"].ToString(), true);
	// 			}

	// 			// Do NOT touch the interpreter here
	// 			return Intrinsic.Result.Null;
	// 		}
	// 		catch (Exception ex)
	// 		{
	// 			ctx.interpreter.errorOutput?.Invoke(ex.Message, true);
	// 			return Intrinsic.Result.Null;
	// 		}
	// 	};
	// }

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