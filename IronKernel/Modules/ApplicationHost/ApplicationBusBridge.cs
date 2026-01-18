using IronKernel.Kernel;
using IronKernel.Kernel.Bus;

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
