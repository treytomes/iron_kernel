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
		CreateDirIntrinsic();
		CreateMkdirIntrinsic();
		CreateDelIntrinsic();
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
				return Error(ctx, "import(name): name is required");

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
				return Error(ctx, $"import: failed to read {libName}: {ex.Message}");
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
				return Error(ctx, "run(filename): filename is required");
			filename = $"file://{filename}.ms";

			string source;
			try
			{
				source = world.FileSystem.ReadText(filename);
			}
			catch (Exception ex)
			{
				return Error(ctx, $"run: failed to read file: {ex.Message}");
			}

			// Stop current script and schedule new one
			world.PendingRunSource = source;

			// Force VM to yield / stop execution
			ctx.vm.yielding = true;

			// run never returns a value
			return Intrinsic.Result.Null;
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
				if (string.IsNullOrWhiteSpace(filename))
				{
					return Error(ctx, "edit(filename): filename is required");
				}

				// MiniScript-visible state
				var state = new ValMap
				{
					["done"] = ValNumber.zero
				};

				if (!filename.StartsWith("file://"))
				{
					filename = $"file://{filename}";
				}

				if (!world.FileSystem.Exists(filename))
				{
					filename = $"{filename}.ms";
				}

				// Kick off file open
				try
				{
					world.WindowService.EditFile(filename);
				}
				catch (Exception ex)
				{
					return Error(ctx, $"edit: failed to open file for editing: {ex.Message}");
				}

				// Mark intrinsic as complete
				state["done"] = ValNumber.one;

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

	// ------------------------------------------------------------
	// dir(path)
	// ------------------------------------------------------------
	private static void CreateDirIntrinsic()
	{
		var dir = Intrinsic.Create("dir");
		dir.AddParam("path", ValNull.instance);
		dir.AddParam("prettyPrint", ValNumber.one); // default true

		dir.code = (ctx, _) =>
		{
			try
			{
				if (ctx.interpreter.hostData is not WorldScriptContext world)
					return Intrinsic.Result.Null;

				// Resolve path
				var pathVal = ctx.GetVar("path");
				string path;

				if (pathVal == null || pathVal == ValNull.instance || string.IsNullOrWhiteSpace(pathVal.ToString()))
				{
					// Default to globals.env.curdir
					world.EnsureEnv(ctx);
					if (ctx.interpreter.GetGlobalValue("env") is not ValMap env ||
						env["curdir"] is not Value curdirVal)
					{
						world.EnsureEnv(ctx);
						return Error(ctx, "dir: globals.env.curdir not defined");
					}
					path = curdirVal.ToString();
				}
				else
				{
					path = pathVal.ToString();
				}
				if (!path.StartsWith("file://"))
				{
					path = $"file://{path}";
				}

				bool prettyPrint = ctx.GetVar("prettyPrint")?.BoolValue() ?? true;

				var entries = world.FileSystem.ListDirectory(path);

				// Pretty-print mode
				if (prettyPrint)
				{
					ctx.interpreter.standardOutput?.Invoke($"{path} :", true);
					// Header (optional, but useful)
					const int COLUMN_WIDTH_NAME = -30;
					const int COLUMN_WIDTH_SIZE = 8;
					const int COLUMN_WIDTH_MODIFIED = 19;
					ctx.interpreter.standardOutput?.Invoke(
							$"  {"NAME",COLUMN_WIDTH_NAME}  {"SIZE",COLUMN_WIDTH_SIZE}  {"MODIFIED",COLUMN_WIDTH_MODIFIED}", true);
					foreach (var e in entries)
					{
						var sizeText = e.IsDirectory
							? "DIR"
							: (e.Size?.ToString() ?? "");

						var dateText = e.LastModified.ToString("yyyy-MM-dd HH:mm:ss");

						ctx.interpreter.standardOutput?.Invoke(
							$"  {e.Name,COLUMN_WIDTH_NAME}  {sizeText,COLUMN_WIDTH_SIZE}  {dateText,COLUMN_WIDTH_MODIFIED}", true);
					}

					return Intrinsic.Result.Null;
				}

				// Structured return mode
				var result = new ValList();
				foreach (var e in entries)
				{
					var map = new ValMap
					{
						["name"] = new ValString(e.Name),
						["isDirectory"] = ValNumber.Truth(e.IsDirectory),
						["size"] = e.Size.HasValue
							? new ValNumber(e.Size.Value)
							: ValNull.instance,
						["lastModified"] =
							new ValString(e.LastModified.ToUniversalTime().ToString("o"))
					};
					result.values.Add(map);
				}

				return new Intrinsic.Result(result);
			}
			catch (Exception ex)
			{
				return Error(ctx, $"dir error: {ex.Message}");
			}
		};
	}

	// ------------------------------------------------------------
	// mkdir(path)
	// ------------------------------------------------------------
	private static void CreateMkdirIntrinsic()
	{
		var mkdir = Intrinsic.Create("mkdir");
		mkdir.AddParam("path");

		mkdir.code = (ctx, _) =>
		{
			try
			{
				if (ctx.interpreter.hostData is not WorldScriptContext world)
					return Intrinsic.Result.Null;

				var path = ctx.GetVar("path")?.ToString();
				if (string.IsNullOrWhiteSpace(path))
					return Error(ctx, "mkdir(path): path is required");

				if (!path.StartsWith("file://"))
				{
					path = $"file://{path}";
				}

				var fs = world.FileSystem;

				Console.WriteLine("Does it exist?");
				if (fs.Exists(path))
					return Error(ctx, $"mkdir: path already exists: {path}");
				Console.WriteLine("It must not exist.");

				fs.CreateDirectory(path);

				return Intrinsic.Result.Null;
			}
			catch (Exception ex)
			{
				return Error(ctx, $"mkdir error: {ex.Message}");
			}
		};
	}

	// ------------------------------------------------------------
	// del(path)
	// ------------------------------------------------------------
	private static void CreateDelIntrinsic()
	{
		var del = Intrinsic.Create("del");
		del.AddParam("path");

		del.code = (ctx, _) =>
		{
			try
			{
				if (ctx.interpreter.hostData is not WorldScriptContext world)
					return Intrinsic.Result.Null;

				var path = ctx.GetVar("path")?.ToString();
				if (string.IsNullOrWhiteSpace(path))
					return Error(ctx, "del(path): path is required");

				if (!path.StartsWith("file://"))
				{
					path = $"file://{path}";
				}

				var fs = world.FileSystem;

				if (!fs.Exists(path))
					return Error(ctx, $"del: path does not exist: {path}");

				fs.Delete(path);
				return Intrinsic.Result.Null;
			}
			catch (Exception ex)
			{
				return Error(ctx, $"del error: {ex.Message}");
			}
		};
	}

	// ------------------------------------------------------------
	// Helpers
	// ------------------------------------------------------------
	private static Intrinsic.Result Error(TAC.Context ctx, string message)
	{
		ctx.interpreter.errorOutput?.Invoke(message, true);
		return Intrinsic.Result.Null;
	}
}