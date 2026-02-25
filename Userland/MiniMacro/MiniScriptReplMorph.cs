using System.Drawing;
using Miniscript;
using Userland.Morphic;
using IronKernel.Common.ValueObjects;
using Userland.Services;
using Microsoft.Extensions.Logging;

namespace Userland.MiniMacro;

public sealed class MiniScriptReplMorph : WindowMorph
{
	#region Constants

	private const string PROMPT_PRIMARY = "> ";
	private const string PROMPT_CONTINUATION = "| ";

	#endregion

	#region Fields

	private readonly TextConsoleMorph _console;
	private readonly Interpreter _interpreter;
	private CancellationTokenSource? _cts;
	private readonly ILogger<MiniScriptReplMorph> _logger;

	#endregion

	#region Constructors

	public MiniScriptReplMorph(ILogger<MiniScriptReplMorph> logger, IClipboardService clipboard)
		: base(Point.Empty, new Size(640, 400), "MiniScript REPL")
	{
		_logger = logger;
		_console = new TextConsoleMorph(logger, clipboard);
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

	#endregion

	#region Methods

	protected override void OnLoad(IAssetService assets)
	{
		_cts = new CancellationTokenSource();

		var world = GetWorld();
		if (world == null) _logger.LogWarning("World is undefined.");
		_interpreter.hostData = world?.ScriptContext;
		_ = RunAsync(_cts.Token);

		_console.CaptureKeyboard();
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
		if (world == null) _logger.LogWarning("World is undefined.");

		_console.WriteLine("MiniScript REPL");
		_console.WriteLine("Type MiniScript expressions or statements.");
		_console.WriteLine();

		while (!ct.IsCancellationRequested)
		{
			if (world == null) world = GetWorld();

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

			if (world?.ScriptContext.PendingRunSource != null)
			{
				var source = world.ScriptContext.PendingRunSource;
				world.ScriptContext.PendingRunSource = null;

				_interpreter.Stop();        // ensure clean state
				_interpreter.Reset(source);
				_interpreter.RunUntilDone(); // THIS is where compile & runtime errors appear
			}

			// Pump async intrinsics
			while (_interpreter.Running() && !_interpreter.NeedMoreInput())
			{
				// Allow the VM to consume completed async intrinsics
				_interpreter.RunUntilDone(returnEarly: false);
				await Task.Yield();
			}

			world?.ApplyScriptEdits();

			// After execution, ensure we're on a fresh line
			if (!_interpreter.NeedMoreInput())
				_console.WriteLine();
		}
	}

	#endregion
}