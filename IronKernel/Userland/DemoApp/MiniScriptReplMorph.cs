using System.Drawing;
using Miniscript;
using IronKernel.Userland.Morphic;
using IronKernel.Common.ValueObjects;

namespace IronKernel.Userland.DemoApp;

public sealed class MiniScriptReplMorph : WindowMorph
{
	private readonly TextConsoleMorph _console;
	private readonly Interpreter _interpreter;
	private CancellationTokenSource? _cts;

	private const string PrimaryPrompt = "> ";
	private const string ContinuationPrompt = "| ";

	public MiniScriptReplMorph(Point position)
		: base(position, new Size(320, 200), "MiniScript REPL")
	{
		_console = new TextConsoleMorph();
		Content.AddMorph(_console);

		_interpreter = new Interpreter();

		// Route MiniScript output into the console
		_interpreter.standardOutput = (text, newline) =>
		{
			if (newline)
				_console.WriteLine(text);
			else
				_console.Write(text);
		};

		_interpreter.implicitOutput = (text, newline) =>
		{
			if (newline)
				_console.WriteLine(text);
			else
				_console.Write(text);
		};

		_interpreter.errorOutput = (text, newline) =>
		{
			_console.CurrentForegroundColor = RadialColor.Red;
			if (newline)
				_console.WriteLine(text);
			else
				_console.Write(text);
			_console.CurrentForegroundColor = RadialColor.Orange;
		};
	}

	protected override void OnLoad(IAssetService assets)
	{
		_cts = new CancellationTokenSource();
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
				? ContinuationPrompt
				: PrimaryPrompt;

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

			// After execution, ensure we're on a fresh line
			if (!_interpreter.NeedMoreInput())
				_console.WriteLine();
		}
	}
}