using System.Drawing;
using Miniscript;
using IronKernel.Userland.Morphic;
using IronKernel.Common.ValueObjects;
using IronKernel.Userland.Services;

namespace IronKernel.Userland.MiniMacro;

public sealed class MiniScriptReplMorph : WindowMorph
{
	private const string PROMPT_PRIMARY = "> ";
	private const string PROMPT_CONTINUATION = "| ";

	private readonly TextConsoleMorph _console;
	private readonly Interpreter _interpreter;
	private CancellationTokenSource? _cts;

	public MiniScriptReplMorph(IClipboardService clipboard)
		: base(Point.Empty, new Size(640, 400), "MiniScript REPL")
	{
		_console = new TextConsoleMorph(clipboard);
		Content.AddMorph(_console);

		_interpreter = new Interpreter
		{
			// Route MiniScript output into the console.
			standardOutput = (text, newline) =>
				{
					if (string.IsNullOrEmpty(text)) return;
					if (newline)
						_console.WriteLine(text);
					else
						_console.Write(text);
				},

			implicitOutput = (text, newline) =>
				{
					if (string.IsNullOrEmpty(text)) return;
					_console.CurrentForegroundColor = RadialColor.Yellow;
					if (newline)
						_console.WriteLine(text);
					else
						_console.Write(text);
					_console.CurrentForegroundColor = RadialColor.Orange;
				},

			errorOutput = (text, newline) =>
				{
					if (string.IsNullOrEmpty(text)) return;
					_console.CurrentForegroundColor = RadialColor.Red;
					if (newline)
						_console.WriteLine(text);
					else
						_console.Write(text);
					_console.CurrentForegroundColor = RadialColor.Orange;
				}
		};
	}

	protected override void OnLoad(IAssetService assets)
	{
		_cts = new CancellationTokenSource();
		_interpreter.hostData = GetWorld().ScriptContext;
		_ = RunAsync(_cts.Token);
	}

	protected override void OnUnload()
	{
		_cts?.Cancel();
	}

	private async Task RunAsync(CancellationToken ct)
	{
		// Wait until the console is layout-stable
		await _console.Ready;

		var world = GetWorld();

		_console.WriteLine("MiniScript REPL");
		_console.WriteLine("Type MiniScript expressions or statements.");
		_console.WriteLine();

		while (!ct.IsCancellationRequested)
		{
			// Prompt depends on parser state
			var prompt = _interpreter.NeedMoreInput()
				? PROMPT_CONTINUATION
				: PROMPT_PRIMARY;

			_console.Write(prompt);

			string line;
			try
			{
				line = await _console.ReadLineAsync();
			}
			catch
			{
				break;
			}

			// Feed line into MiniScript REPL
			_interpreter.REPL(line);

			if (world.ScriptContext.PendingRunSource != null)
			{
				var source = world.ScriptContext.PendingRunSource;
				world.ScriptContext.PendingRunSource = null;

				_interpreter.Stop();        // ensure clean state
				_interpreter.Reset(source);
				_interpreter.RunUntilDone(); // THIS is where compile & runtime errors appear
			}

			// Pump async intrinsics
			while (_interpreter.Running())
			{
				// Allow the VM to consume completed async intrinsics
				_interpreter.RunUntilDone(returnEarly: false);
				await Task.Yield();
			}

			// After execution, ensure we're on a fresh line
			if (!_interpreter.NeedMoreInput())
				_console.WriteLine();
		}
	}
}