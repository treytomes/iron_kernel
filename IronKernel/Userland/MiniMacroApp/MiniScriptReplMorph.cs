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

	public MiniScriptReplMorph()
		: base(Point.Empty, new Size(640, 400), "MiniScript REPL")
	{
		_console = new TextConsoleMorph();
		Content.AddMorph(_console);

		_interpreter = new Interpreter
		{
			// Route MiniScript output into the console.
			standardOutput = (text, newline) =>
				{
					if (newline)
						_console.WriteLine(text);
					else
						_console.Write(text);
				},

			implicitOutput = (text, newline) =>
				{
					_console.CurrentForegroundColor = RadialColor.Yellow;
					if (newline)
						_console.WriteLine(text);
					else
						_console.Write(text);
					_console.CurrentForegroundColor = RadialColor.Orange;
				},

			errorOutput = (text, newline) =>
				{
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

			// Pump async intrinsics
			while (_interpreter.Running())
			{
				_interpreter.RunUntilDone(returnEarly: true);
				await Task.Yield();
			}

			// After execution, ensure we're on a fresh line
			if (!_interpreter.NeedMoreInput())
				_console.WriteLine();
		}
	}
}