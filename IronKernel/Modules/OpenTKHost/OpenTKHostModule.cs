using IronKernel.Kernel;
using IronKernel.Kernel.Bus;
using IronKernel.Kernel.Messages;
using IronKernel.Kernel.State;
using Microsoft.Extensions.Logging;
using OpenTK.Windowing.Desktop;

namespace IronKernel.Modules.OpenTKHost;

public sealed class OpenTKHostModule :
	IKernelModule,
	IPrimaryKernelModule,
	IAsyncDisposable
{
	private readonly IMessageBus _bus;
	private readonly ILogger<OpenTKHostModule> _logger;

	private GameWindow? _window;
	private volatile bool _shutdownRequested;

	public OpenTKHostModule(
		IMessageBus bus,
		ILogger<OpenTKHostModule> logger)
	{
		_bus = bus;
		_logger = logger;
	}

	public Task StartAsync(
		IKernelState state,
		IModuleRuntime runtime,
		CancellationToken stoppingToken)
	{
		var settings = GameWindowSettings.Default;
		var native = NativeWindowSettings.Default;

		_window = new GameWindow(settings, native);

		_bus.Subscribe<KernelShutdownRequested>(runtime, "KernelShutdownRequestHandler", OnShutdownRequested);

		HookEvents();

		return Task.CompletedTask;
	}

	public void Run()
	{
		_logger.LogInformation("OpenTK host running");

		_window!.Run();

		_logger.LogInformation("OpenTK host exited");
	}

	private Task OnShutdownRequested(
		KernelShutdownRequested msg,
		CancellationToken _)
	{
		_logger.LogInformation(
			"OpenTK received shutdown request: {Reason}",
			msg.Reason);

		_shutdownRequested = true;
		return Task.CompletedTask;
	}

	private void HookEvents()
	{
		_window!.UpdateFrame += e =>
		{
			if (_shutdownRequested)
				_window.Close();
		};

		_window.RenderFrame += e =>
		{
			_bus.Publish(new HostRenderTick(e.Time, e.Time));
			_window.SwapBuffers();
		};

		_window.Resize += e =>
			_bus.Publish(new HostResizeEvent(e.Width, e.Height));

		_window.Closing += _ =>
			_bus.Publish(new HostShutdownEvent());
	}

	public ValueTask DisposeAsync()
	{
		_window?.Dispose();
		return ValueTask.CompletedTask;
	}
}
