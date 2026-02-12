using IronKernel.Kernel;
using IronKernel.Kernel.Bus;
using IronKernel.Kernel.State;
using IronKernel.Modules.Clipboard.ValueObjects;
using Microsoft.Extensions.Logging;

namespace IronKernel.Modules.Clipboard;

internal sealed class ClipboardModule(
	AppSettings settings,
	IMessageBus bus,
	ILogger<ClipboardModule> logger
) : IKernelModule
{
	#region Fields

	private readonly AppSettings _settings = settings;
	private readonly IMessageBus _bus = bus;
	private readonly ILogger<ClipboardModule> _logger = logger;
	private bool _hostClipboardEnabled = true;
	private string? _localText;

	#endregion

	#region Methods

	public Task StartAsync(IKernelState state, IModuleRuntime runtime, CancellationToken stoppingToken)
	{
		_bus.Subscribe<ClipboardGetQuery>(
			runtime,
			"ClipboardGetHandler",
			HandleClipboardGetAsync);

		_bus.Subscribe<ClipboardSetCommand>(
			runtime,
			"ClipboardSetHandler",
			HandleClipboardSetAsync);

		return Task.CompletedTask;
	}

	public ValueTask DisposeAsync()
	{
		_logger.LogInformation("FileSystem disposed.");
		return ValueTask.CompletedTask;
	}

	private Task HandleClipboardGetAsync(ClipboardGetQuery msg, CancellationToken ct)
	{
		if (_hostClipboardEnabled)
		{
			try
			{
				var text = TextCopy.ClipboardService.GetText();
				_bus.Publish(new ClipboardGetResponse(msg.CorrelationID, text ?? string.Empty));
			}
			catch (Exception ex)
			{
				_logger.LogWarning(ex, ex.Message);
				_hostClipboardEnabled = false;
				// Fall through to using local clipboard text.
			}
		}

		_bus.Publish(new ClipboardGetResponse(msg.CorrelationID, _localText ?? string.Empty));
		return Task.CompletedTask;
	}

	private Task HandleClipboardSetAsync(ClipboardSetCommand msg, CancellationToken ct)
	{
		if (_hostClipboardEnabled)
		{
			try
			{
				// Platform-specific clipboard write.
				TextCopy.ClipboardService.SetText(msg.Text);
			}
			catch (Exception ex)
			{
				_logger.LogWarning(ex, ex.Message);
				_hostClipboardEnabled = false;
				// Fall through to using local clipboard text.
			}
		}
		_localText = msg.Text;
		return Task.CompletedTask;
	}

	#endregion
}
