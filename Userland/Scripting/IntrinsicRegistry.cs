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
		SoundIntrinsics.Register();
		SpriteDisplayIntrinsics.Register();
		TileMapIntrinsics.Register();

		CreateHelpIntrinsic();
		CreateDecompileIntrinsic();
		CreateClsIntrinsic();
	}

	private static readonly string GeneralHelp = """
		IronKernel MiniScript environment

		FILE PROTOCOLS
		  file://path       User storage (read/write).  e.g. file://notes.txt
		  sys://path        System assets (read-only).  e.g. sys://sounds/blipA4.wav
		  asset://kind.key  Named asset.               e.g. asset://sound.blipa4

		FILESYSTEM
		  dir [path]        List directory
		  cd [path]         Change directory
		  pwd               Print working directory
		  mkdir path        Create directory
		  del path          Delete file or directory
		  copy from, to     Copy file
		  move from, to     Move/rename file
		  run filename      Run a .ms script
		  import name       Import a module

		SOUND
		  Sound.playAsset path   Play a WAV (sys://, file://, or bare key)
		  s = new Sound          Create a synthesizer voice
		  s.init wf, freq, dur   Configure waveform/frequency/duration
		  s.play                 Generate and play
		  noteFreq n             MIDI note → Hz  (A4 = noteFreq(69) = 440)
		  Sound.Sine / .Triangle / .Sawtooth / .Square / .Noise

		DISPLAY
		  Canvas — pixel drawing   TileMap — tile grid   Sprite — sprites

		OTHER
		  help [fn]          Show this help, or docstring for a function
		  decompile fn       Dump bytecode for a function
		  cls                Clear the terminal
		""";

	private static void CreateHelpIntrinsic()
	{
		var inspect = Intrinsic.Create("help");
		inspect.AddParam("function", ValNull.instance);

		inspect.code = (ctx, _) =>
		{
			try
			{
				var value = ctx.GetVar("function");

				if (value == null || value == ValNull.instance)
				{
					foreach (var line in GeneralHelp.Split('\n'))
						WriteLine(ctx, line.TrimEnd());
					return Intrinsic.Result.Null;
				}

				if (value is not ValFunction fn)
				{
					foreach (var line in GeneralHelp.Split('\n'))
						WriteLine(ctx, line.TrimEnd());
					return Intrinsic.Result.Null;
				}

				WriteLine(ctx, fn.ToString());
				var code = fn.function.code.FirstOrDefault();
				if (code is not null)
				{
					var firstValue = code.Evaluate(ctx);
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
				ctx.interpreter.errorOutput?.Invoke($"help error: {ex.Message}", true);
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

	private static void CreateClsIntrinsic()
	{
		var cls = Intrinsic.Create("cls");
		cls.code = (ctx, _) =>
		{
			if (ctx.interpreter.hostData is IScriptHost host)
				host.ClearOutputRequested?.Invoke();
			return Intrinsic.Result.Null;
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