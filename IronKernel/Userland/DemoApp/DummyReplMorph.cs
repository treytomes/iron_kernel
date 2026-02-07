using System.Drawing;
using IronKernel.Userland.Morphic;

namespace IronKernel.Userland.DemoApp;

public sealed class DummyReplMorph : WindowMorph
{
	private readonly TextConsoleMorph _console;
	private CancellationTokenSource? _cts;

	public DummyReplMorph()
		: base(Point.Empty, new Size(256, 192), "Dummy REPL")
	{
		_console = new TextConsoleMorph();
		Content.AddMorph(_console);
	}

	protected override void OnLoad(IAssetService assetService)
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
		await _console.Ready;
		_console.WriteLine("Dummy REPL");
		_console.WriteLine();

		while (!ct.IsCancellationRequested)
		{
			_console.Write("> ");
			var line = await _console.ReadLineAsync();
			_console.WriteLine($"echo: {line}");
		}
	}
}