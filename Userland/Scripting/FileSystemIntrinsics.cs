using Miniscript;

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
		CreateCopyIntrinsic();
		CreateMoveIntrinsic();
		CreatePwdIntrinsic();
		CreateCdIntrinsic();
	}

	// ============================================================
	// Path resolution helpers
	// ============================================================

	private static string ResolvePath(
		TAC.Context ctx,
		WorldScriptContext world,
		string path)
	{
		world.EnsureEnv(ctx);

		// Root shortcut
		if (path == "/" || path == "file://")
			return "file://";

		// Strip scheme if present
		if (path.StartsWith("file://"))
			path = path.Substring("file://".Length);

		// Normalize separators
		path = path.Replace('\\', '/');

		// Get current directory (without scheme)
		var env = ctx.interpreter.GetGlobalValue("env") as ValMap;
		var curdir = env!["curdir"].ToString();
		if (curdir.StartsWith("file://"))
			curdir = curdir.Substring("file://".Length);

		// Build combined path
		string combined;
		if (path.StartsWith("/"))
		{
			// Absolute path
			combined = path;
		}
		else
		{
			// Relative path
			combined = string.IsNullOrEmpty(curdir)
				? path
				: $"{curdir}/{path}";
		}

		// Normalize path segments (handle . and ..)
		var parts = new Stack<string>();
		foreach (var part in combined.Split('/', StringSplitOptions.RemoveEmptyEntries))
		{
			if (part == ".")
				continue;
			if (part == "..")
			{
				if (parts.Count > 0)
					parts.Pop();
				continue;
			}
			parts.Push(part);
		}

		var normalized = string.Join('/', parts.Reverse());
		return string.IsNullOrEmpty(normalized)
			? "file://"
			: $"file://{normalized}";
	}

	private static string ResolveScriptPath(
		TAC.Context ctx,
		WorldScriptContext world,
		string name)
	{
		var path = ResolvePath(ctx, world, name);
		if (!Path.HasExtension(path))
			path += ".ms";
		return path;
	}

	// ============================================================
	// import(name)
	// ============================================================

	private static void CreateImportIntrinsic()
	{
		var import = Intrinsic.Create("import");
		import.AddParam("name");

		import.code = (ctx, partial) =>
		{
			if (partial != null)
			{
				if (ctx.GetTemp(0) is Value imported)
					ctx.parent?.SetVar(partial.result.ToString(), imported);
				return Intrinsic.Result.Null;
			}

			if (ctx.interpreter.hostData is not WorldScriptContext world)
				return Intrinsic.Result.Null;

			var name = ctx.GetVar("name")?.ToString();
			if (string.IsNullOrWhiteSpace(name))
				return Error(ctx, "import(name): name is required");

			string source;
			try
			{
				source = world.FileSystem.ReadText(
					ResolveScriptPath(ctx, world, name));
			}
			catch (Exception ex)
			{
				return Error(ctx, $"import: {ex.Message}");
			}

			var parser = new Parser { errorContext = name };
			parser.Parse(source);
			var fn = parser.CreateImport();

			ctx.vm.ManuallyPushCall(
				new ValFunction(fn),
				new ValTemp(0));

			return new Intrinsic.Result(new ValString(name), done: false);
		};
	}

	// ============================================================
	// run(filename)
	// ============================================================

	private static void CreateRunIntrinsic()
	{
		var run = Intrinsic.Create("run");
		run.AddParam("filename");

		run.code = (ctx, _) =>
		{
			if (ctx.interpreter.hostData is not WorldScriptContext world)
				return Intrinsic.Result.Null;

			var name = ctx.GetVar("filename")?.ToString();
			if (string.IsNullOrWhiteSpace(name))
				return Error(ctx, "run(filename): filename is required");

			string source;
			try
			{
				source = world.FileSystem.ReadText(
					ResolveScriptPath(ctx, world, name));
			}
			catch (Exception ex)
			{
				return Error(ctx, $"run: {ex.Message}");
			}

			world.PendingRunSource = source;
			ctx.vm.yielding = true;
			return Intrinsic.Result.Null;
		};
	}

	// ============================================================
	// edit(filename)
	// ============================================================

	private static void CreateEditIntrinsic()
	{
		var edit = Intrinsic.Create("edit");
		edit.AddParam("filename");

		edit.code = (ctx, _) =>
		{
			if (ctx.interpreter.hostData is not WorldScriptContext world)
				return Intrinsic.Result.Null;

			var name = ctx.GetVar("filename")?.ToString();
			if (string.IsNullOrWhiteSpace(name))
				return Error(ctx, "edit(filename): filename is required");

			var path = ResolveScriptPath(ctx, world, name);
			try
			{
				world.WindowService.EditFile(path);
			}
			catch (Exception ex)
			{
				return Error(ctx, $"edit: {ex.Message}");
			}

			return Intrinsic.Result.Null;
		};
	}

	// ============================================================
	// dir(path?, prettyPrint?)
	// ============================================================

	private static void CreateDirIntrinsic()
	{
		var dir = Intrinsic.Create("dir");
		dir.AddParam("path", ValNull.instance);
		dir.AddParam("prettyPrint", ValNumber.one);

		dir.code = (ctx, _) =>
		{
			if (ctx.interpreter.hostData is not WorldScriptContext world)
				return Intrinsic.Result.Null;

			string path;
			var p = ctx.GetVar("path");
			world.EnsureEnv(ctx);
			if (p == null || p == ValNull.instance || string.IsNullOrWhiteSpace(p.ToString()))
			{
				// path = ResolvePath(ctx, world, (ctx.interpreter.GetGlobalValue("env") as ValMap)!["curdir"].ToString());
				path = (ctx.interpreter.GetGlobalValue("env") as ValMap)!["curdir"].ToString();
				Console.WriteLine("path0=" + path);
			}
			else
			{
				path = ResolvePath(ctx, world, p.ToString());
				Console.WriteLine("path1=" + path);
			}

			var entries = world.FileSystem.ListDirectory(path);
			bool pretty = ctx.GetVar("prettyPrint")?.BoolValue() ?? true;

			if (pretty)
			{
				ctx.interpreter.standardOutput?.Invoke($"{path} :", true);
				ctx.interpreter.standardOutput?.Invoke(
					$"  {"NAME",-30}  {"SIZE",8}  {"MODIFIED",19}", true);

				foreach (var e in entries)
				{
					var size = e.IsDirectory ? "DIR" : (e.Size?.ToString() ?? "");
					var date = e.LastModified.ToString("yyyy-MM-dd HH:mm:ss");
					ctx.interpreter.standardOutput?.Invoke(
						$"  {e.Name,-30}  {size,8}  {date,19}", true);
				}
				return Intrinsic.Result.Null;
			}

			var list = new ValList();
			foreach (var e in entries)
			{
				list.values.Add(new ValMap
				{
					["name"] = new ValString(e.Name),
					["isDirectory"] = ValNumber.Truth(e.IsDirectory),
					["size"] = e.Size.HasValue ? new ValNumber(e.Size.Value) : ValNull.instance,
					["lastModified"] = new ValString(e.LastModified.ToUniversalTime().ToString("o"))
				});
			}
			return new Intrinsic.Result(list);
		};
	}

	// ============================================================
	// mkdir(path)
	// ============================================================

	private static void CreateMkdirIntrinsic()
	{
		var mkdir = Intrinsic.Create("mkdir");
		mkdir.AddParam("path");

		mkdir.code = (ctx, _) =>
		{
			if (ctx.interpreter.hostData is not WorldScriptContext world)
				return Intrinsic.Result.Null;

			var path = ResolvePath(ctx, world, ctx.GetVar("path")?.ToString() ?? "");
			if (world.FileSystem.Exists(path))
				return Error(ctx, $"mkdir: path already exists: {path}");

			world.FileSystem.CreateDirectory(path);
			return Intrinsic.Result.Null;
		};
	}

	// ============================================================
	// del(path)
	// ============================================================

	private static void CreateDelIntrinsic()
	{
		var del = Intrinsic.Create("del");
		del.AddParam("path");

		del.code = (ctx, _) =>
		{
			if (ctx.interpreter.hostData is not WorldScriptContext world)
				return Intrinsic.Result.Null;

			var path = ResolvePath(ctx, world, ctx.GetVar("path")?.ToString() ?? "");
			if (!world.FileSystem.Exists(path))
				return Error(ctx, $"del: path does not exist: {path}");

			world.FileSystem.Delete(path);
			return Intrinsic.Result.Null;
		};
	}

	// ============================================================
	// copy(from, to)
	// ============================================================

	private static void CreateCopyIntrinsic()
	{
		var copy = Intrinsic.Create("copy");
		copy.AddParam("from");
		copy.AddParam("to");

		copy.code = (ctx, _) =>
		{
			if (ctx.interpreter.hostData is not WorldScriptContext world)
				return Intrinsic.Result.Null;

			var from = ResolvePath(ctx, world, ctx.GetVar("from")?.ToString() ?? "");
			var to = ResolvePath(ctx, world, ctx.GetVar("to")?.ToString() ?? "");

			if (!world.FileSystem.Exists(from))
				return Error(ctx, $"copy: source does not exist: {from}");

			var data = world.FileSystem.Read(from);
			var write = world.FileSystem.Write(to, data.Data, data.MimeType);
			if (!write.Success)
				return Error(ctx, $"copy: failed to write: {to}");

			return Intrinsic.Result.Null;
		};
	}

	// ============================================================
	// move(from, to)
	// ============================================================

	private static void CreateMoveIntrinsic()
	{
		var move = Intrinsic.Create("move");
		move.AddParam("from");
		move.AddParam("to");

		move.code = (ctx, _) =>
		{
			if (ctx.interpreter.hostData is not WorldScriptContext world)
				return Intrinsic.Result.Null;

			var from = ResolvePath(ctx, world, ctx.GetVar("from")?.ToString() ?? "");
			var to = ResolvePath(ctx, world, ctx.GetVar("to")?.ToString() ?? "");

			if (!world.FileSystem.Exists(from))
				return Error(ctx, $"move: source does not exist: {from}");

			var data = world.FileSystem.Read(from);
			var write = world.FileSystem.Write(to, data.Data, data.MimeType);
			if (!write.Success)
				return Error(ctx, $"move: failed to write: {to}");

			world.FileSystem.Delete(from);
			return Intrinsic.Result.Null;
		};
	}

	private static void CreatePwdIntrinsic()
	{
		var pwd = Intrinsic.Create("pwd");
		pwd.code = (ctx, _) =>
		{
			if (ctx.interpreter.hostData is not WorldScriptContext world)
				return Intrinsic.Result.Null;

			world.EnsureEnv(ctx);

			if (ctx.interpreter.GetGlobalValue("env") is not ValMap env ||
				env["curdir"] is not Value curdir)
			{
				return Error(ctx, "pwd: env.curdir not defined");
			}

			return new Intrinsic.Result(curdir);
		};
	}

	private static void CreateCdIntrinsic()
	{
		var cd = Intrinsic.Create("cd");
		cd.AddParam("path", ValNull.instance);

		cd.code = (ctx, _) =>
		{
			if (ctx.interpreter.hostData is not WorldScriptContext world)
				return Intrinsic.Result.Null;

			world.EnsureEnv(ctx);

			var env = ctx.interpreter.GetGlobalValue("env") as ValMap;
			if (env == null)
				return Error(ctx, "cd: env not initialized");

			string path;

			var pathVal = ctx.GetVar("path");
			if (pathVal == null || pathVal == ValNull.instance || string.IsNullOrWhiteSpace(pathVal.ToString()))
			{
				// Default to filesystem root
				path = "file://";
			}
			else
			{
				path = ResolvePath(ctx, world, pathVal.ToString());
			}

			// Validate target exists and is a directory
			if (!world.FileSystem.Exists(path))
				return Error(ctx, $"cd: path does not exist: {path}");

			try
			{
				// Directory must be listable
				world.FileSystem.ListDirectory(path);
			}
			catch
			{
				return Error(ctx, $"cd: not a directory: {path}");
			}

			env["curdir"] = new ValString(path);
			return Intrinsic.Result.Null;
		};
	}

	// ============================================================
	// Error helper
	// ============================================================

	private static Intrinsic.Result Error(TAC.Context ctx, string message)
	{
		ctx.interpreter.errorOutput?.Invoke(message, true);
		return Intrinsic.Result.Null;
	}
}