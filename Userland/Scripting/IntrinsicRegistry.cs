using System.Text;
using Miniscript;
using Userland.Morphic;

namespace Userland.Scripting;

public static class IntrinsicRegistry
{
	public static void Register()
	{
		CanvasIntrinsics.Register();
		ColorIntrinsics.Register();
		DialogIntrinsics.Register();
		FileSystemIntrinsics.Register();
		KeyboardIntrinsics.Register();
		MorphIntrinsics.Register();
		SpriteDisplayIntrinsics.Register();
		TileMapIntrinsics.Register();

		CreateHelpIntrinsic();
		CreateDecompileIntrinsic();
	}

	private static void CreateHelpIntrinsic()
	{
		var inspect = Intrinsic.Create("help");
		inspect.AddParam("function");

		inspect.code = (ctx, _) =>
		{
			try
			{
				if (ctx.interpreter.hostData is not WorldScriptContext world)
					return Intrinsic.Result.Null;

				var value = ctx.GetVar("function");

				if (value is not ValFunction fn)
				{
					return Error(ctx, "help: function must be a function reference");
				}

				WriteLine(ctx, fn.ToString());
				var line = fn.function.code.FirstOrDefault();
				if (line is not null)
				{
					var firstValue = line.Evaluate(ctx);
					if (firstValue is ValString helpText)
					{
						WriteLine(ctx, helpText.value);
						return Intrinsic.Result.Null;
					}
				}
				WriteLine(ctx, $"No help available for {fn}.");
				return Intrinsic.Result.Null;
			}
			catch (Exception ex)
			{
				ctx.interpreter.errorOutput?.Invoke(
					$"help error: {ex.Message}",
					true
				);
				return Intrinsic.Result.Null;
			}
		};
	}

	private static void CreateDecompileIntrinsic()
	{
		var inspect = Intrinsic.Create("decompile");
		inspect.AddParam("function");

		inspect.code = (ctx, _) =>
		{
			try
			{
				if (ctx.interpreter.hostData is not WorldScriptContext world)
					return Intrinsic.Result.Null;

				var value = ctx.GetVar("function");

				if (value is not ValFunction fn)
				{
					return Error(ctx, "decompile: function must be a function reference");
				}

				WriteLine(ctx, fn.ToString());

				var list = new ValList();
				foreach (var line in fn.function.code)
				{
					list.values.Add((new
					{
						location = new
						{
							line.location.context,
							line.location.lineNum,
						},
						line.lhs,
						line.op,
						line.rhsA,
						line.rhsB,
					}).ToValue());
				}

				var map = new ValMap();
				map["name"] = new ValString(fn.ToString());
				map["code"] = list;
				return new Intrinsic.Result(map);
			}
			catch (Exception ex)
			{
				return Error(ctx, $"decompile error: {ex.Message}");
			}
		};
	}

	private static void WriteLine(TAC.Context ctx, string message)
	{
		ctx.interpreter.standardOutput?.Invoke(message, true);
	}

	private static Intrinsic.Result Error(TAC.Context ctx, string message)
	{
		ctx.interpreter.errorOutput?.Invoke(message, true);
		return Intrinsic.Result.Null;
	}
}