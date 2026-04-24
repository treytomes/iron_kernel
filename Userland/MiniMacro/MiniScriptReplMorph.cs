using System.Drawing;
using Miniscript;
using Userland.Morphic;
using IronKernel.Common.ValueObjects;
using Userland.Services;
using Microsoft.Extensions.Logging;
using Color = IronKernel.Common.ValueObjects.Color;

namespace Userland.MiniMacro;

public sealed class MiniScriptReplMorph : WindowMorph
{
	private enum ReplState
	{
		Initializing,
		Reading,
		Running,
	}

	#region Constants

	private const string PROMPT_PRIMARY = "> ";
	private const string PROMPT_CONTINUATION = "| ";

	#endregion

	#region Fields

	private readonly TextConsoleMorph _console;
	private readonly Interpreter _interpreter;
	private readonly ILogger<MiniScriptReplMorph> _logger;
	private ReplState _state = ReplState.Initializing;
	private Task<string>? _readTask = null;
	private string? _pendingRunSource = null;

	#endregion

	#region Constructors

	public MiniScriptReplMorph(ILogger<MiniScriptReplMorph> logger, IClipboardService clipboard)
		: base(Point.Empty, new Size(920, 440), "MiniScript REPL")
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
					_console.CurrentForegroundColor = Color.Yellow;
					if (newline)
						_console.WriteLine(text);
					else
						_console.Write(text);
					_console.CurrentForegroundColor = Color.Orange;
				},

			errorOutput = (text, newline) =>
				{
					if (string.IsNullOrEmpty(text)) return;
					_console.CurrentForegroundColor = Color.Red;
					if (newline)
						_console.WriteLine(text);
					else
						_console.Write(text);
					_console.CurrentForegroundColor = Color.Orange;
				}
		};
	}

	#endregion

	#region Properties

	/// <summary>
	/// Prompt depends on parser state.
	/// </summary>
	private string Prompt => _interpreter.NeedMoreInput()
		? PROMPT_CONTINUATION
		: PROMPT_PRIMARY;

	#endregion

	#region Methods

	protected override void OnLoad(IAssetService assets)
	{
		var world = GetWorld();
		if (world == null) _logger.LogWarning("World is undefined.");

		if (world?.ScriptContext is { } ctx)
		{
			ctx.ReadLineOverride = async prompt =>
			{
				_console.Write(prompt);
				return await _console.ReadLineAsync();
			};
			ctx.RunSourceRequested = RunSource;
			ctx.ClearOutputRequested = _console.Clear;
			_interpreter.hostData = ctx;
		}

		_console.CaptureKeyboard();
		Position = new Point(20, Position.Y);
	}

	protected override void OnUnload()
	{
	}

	public override void Update(double deltaTime)
	{
		base.Update(deltaTime);

		if (_pendingRunSource != null)
		{
			var source = _pendingRunSource;
			_pendingRunSource = null;
			ApplyRunSource(source);
			return;
		}

		if (_state == ReplState.Initializing)
		{
			_console.WriteLine("MiniScript REPL");
			_console.WriteLine("Type MiniScript expressions or statements.");
			_console.WriteLine();
			SwitchState(ReplState.Reading);
		}
		else if (_state == ReplState.Reading)
		{
			if (_readTask == null) return;
			if (!_readTask.IsCompleted) return;
			var line = _readTask.GetAwaiter().GetResult();

			// Feed line into MiniScript REPL.
			_interpreter.REPL(line);
			if (_interpreter.NeedMoreInput())
			{
				_console.Write(Prompt);
				_readTask = _console.ReadLineAsync();
			}
			else
			{
				SwitchState(ReplState.Running);
			}
		}
		else if (_state == ReplState.Running)
		{
			if (_interpreter.Running())
			{
				_interpreter.RunUntilDone(0.03f);

				var world = GetWorld();
				world?.ApplyScriptEdits();
			}
			else
			{
				SwitchState(ReplState.Reading);
			}
		}
	}

	private void ApplyRunSource(string source)
	{
		_interpreter.Stop();
		_interpreter.Reset(source);
		_interpreter.Compile();
		if (_interpreter.NeedMoreInput())
		{
			_interpreter.errorOutput?.Invoke("Script error.", true);
			_interpreter.Stop();
		}
		_state = ReplState.Running;
	}

	private void SwitchState(ReplState newState)
	{
		if (_state == newState) return;
		_state = newState;

		if (_state == ReplState.Reading)
		{
			_console.Write(Prompt);
			_readTask = _console.ReadLineAsync();
		}
	}

	private void RunSource(string source)
	{
		// Defer execution: the run() intrinsic may fire from inside the interpreter's
		// own step loop. Resetting the interpreter synchronously there causes the REPL
		// while-loop to pick up the new VM and run it inline, which recurses infinitely
		// if the new script also calls run(). Signal yielding so the loop exits, then
		// apply the source on the next Update() tick.
		if (_interpreter.vm != null)
			_interpreter.vm.yielding = true;
		_pendingRunSource = source;
	}

	#endregion
}