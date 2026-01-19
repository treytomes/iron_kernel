using IronKernel.Common;
using IronKernel.Common.ValueObjects;
using IronKernel.Modules.ApplicationHost;
using Microsoft.Extensions.Logging;

namespace IronKernel.Userland.DemoApp;

/// <summary>
/// Simple demo user application.
/// </summary>
public sealed class SimpleUserApplication : IUserApplication
{
	private readonly ILogger<SimpleUserApplication> _logger;

	public SimpleUserApplication(ILogger<SimpleUserApplication> logger)
	{
		_logger = logger;
	}

	public async Task RunAsync(
		IApplicationContext context,
		CancellationToken stoppingToken)
	{
		_logger.LogInformation("SimpleUserApplication starting");

		// Initialize application state
		context.State.Set("tickCount", 0);

		// Subscribe to a message
		context.Bus.Subscribe<PingMessage>(
			"PingHandler",
			async (msg, ct) =>
			{
				_logger.LogInformation(
					"Received Ping {Value}",
					msg.Value);

				context.Bus.Publish(
					new PongMessage(msg.Value + 1));

				await Task.CompletedTask;
			});

		context.Bus.Subscribe<AppKeyboardEvent>(
			"KeyboardHandler",
			async (msg, ct) =>
			{
				_logger.LogInformation(
					"Received keyboard event: {Key}, pressed: {}",
					msg.Key, msg.Action == InputAction.Press);
				await Task.CompletedTask;
			});

		// Start a periodic task
		await context.Scheduler.RunAsync(
			"Ticker",
			ApplicationTaskKind.LongRunning,
			async ct =>
			{
				while (!ct.IsCancellationRequested)
				{
					await Task.Delay(1000, ct);

					context.State.TryGet("tickCount", out int ticks);
					ticks++;

					context.State.Set("tickCount", ticks);

					_logger.LogInformation(
						"Tick {TickCount}",
						ticks);

					context.Bus.Publish(new TickMessage(ticks));

					if (ticks % 5 == 0)
					{
						context.Bus.Publish(new PingMessage(ticks));
					}
				}
			},
			stoppingToken);

		// Keep main alive until shutdown
		await Task.Delay(Timeout.Infinite, stoppingToken);
	}
}