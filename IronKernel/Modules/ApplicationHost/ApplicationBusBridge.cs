using IronKernel.Kernel;
using IronKernel.Kernel.Bus;
using IronKernel.Modules.ApplicationHost.ValueObjects;
using IronKernel.Modules.OpenTKHost.ValueObjects;

namespace IronKernel.Modules.ApplicationHost;

internal sealed class ApplicationBusBridge(
	IMessageBus kernelBus,
	ApplicationBus appBus,
	IModuleRuntime runtime
) : IDisposable
{
	private readonly IMessageBus _kernelBus = kernelBus;
	private readonly ApplicationBus _appBus = appBus;
	private readonly IModuleRuntime _runtime = runtime;
	private readonly List<IDisposable> _subs = new();

	public void ForwardClocked<TKernel, TApp>(
		string name,
		TimeSpan interval,
		Func<ClockState, TKernel, TApp> map)
		where TKernel : notnull
		where TApp : notnull
	{
		double accumulator = 0;
		double total = 0;
		double step = interval.TotalSeconds;

		var sub = _kernelBus.Subscribe<TKernel>(
			_runtime,
			name,
			(msg, ct) =>
			{
				if (msg is not HostUpdateTick tick)
					return Task.CompletedTask;

				accumulator += tick.ElapsedTime;

				while (accumulator >= step)
				{
					total += step;
					accumulator -= step;

					var clock = new ClockState(total, step);
					_appBus.Publish(map(clock, msg));
				}

				return Task.CompletedTask;
			});

		_subs.Add(sub);
	}

	public void Forward<TKernel, TApp>(
		string name,
		Func<TKernel, CancellationToken, TApp> map)
		where TKernel : notnull
		where TApp : notnull
	{
		var sub = _kernelBus.Subscribe<TKernel>(
			_runtime,
			name,
			(msg, ct) =>
			{
				_appBus.Publish(map(msg, ct));
				return Task.CompletedTask;
			});

		_subs.Add(sub);
	}

	public void Dispose()
	{
		foreach (var s in _subs)
			s.Dispose();
		_subs.Clear();
	}
}
