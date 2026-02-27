using System.Drawing;
using Miniscript;
using Userland.Morphic;
using IronKernel.Common.ValueObjects;
using Userland.Services;
using Microsoft.Extensions.Logging;

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
		_interpreter.hostData = world?.ScriptContext;

		_console.CaptureKeyboard();
		Position = new Point(20, Position.Y);
	}

	protected override void OnUnload()
	{
	}

	public override void Update(double deltaTime)
	{
		base.Update(deltaTime);

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
				// _console.WriteLine();
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

	private void SwitchState(ReplState newState)
	{
		if (_state == newState) return;
		_state = newState;

		switch (_state)
		{
			case ReplState.Reading:
				_console.Write(Prompt);
				_readTask = _console.ReadLineAsync();
				break;

			case ReplState.Running:
				var world = GetWorld();
				if (world == null) return;

				if (world?.ScriptContext.PendingRunSource != null)
				{
					var source = world.ScriptContext.PendingRunSource;
					Console.WriteLine($"Source: {source}");
					world.ScriptContext.PendingRunSource = null;

					_interpreter.Stop();        // ensure clean state
					_interpreter.Reset(source);
					_interpreter.Compile();
					if (_interpreter.NeedMoreInput())
					{
						_interpreter.errorOutput?.Invoke("Script error.", true);
						_interpreter.Stop();
					}
					else
					{
						// _interpreter.REPL("");
					}
					// _interpreter.RunUntilDone(0.03f); // THIS is where compile & runtime errors appear
				}
				break;
		}
	}

	#endregion
}