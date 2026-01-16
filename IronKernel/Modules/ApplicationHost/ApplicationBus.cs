using IronKernel.Kernel;
using IronKernel.Kernel.Bus;

namespace IronKernel.Modules.ApplicationHost;

internal sealed class ApplicationBus : IApplicationBus, IDisposable
{
	private readonly IModuleRuntime _runtime;
	private readonly IMessageBus _kernelBus;
	private readonly List<IDisposable> _subscriptions = new();

	public ApplicationBus(
		IModuleRuntime runtime,
		IMessageBus kernelBus)
	{
		_runtime = runtime ?? throw new ArgumentNullException(nameof(runtime));
		_kernelBus = kernelBus ?? throw new ArgumentNullException(nameof(kernelBus));
	}

	public void Publish<T>(T message)
		where T : notnull
	{
		// Application publishes into kernel bus
		_kernelBus.Publish(message);
	}

	public IDisposable Subscribe<T>(
		string handlerName,
		Func<T, CancellationToken, Task> handler)
		where T : notnull
	{
		if (handler is null)
			throw new ArgumentNullException(nameof(handler));

		var sub = _kernelBus.Subscribe(
			runtime: _runtime,
			handlerName: handlerName,
			handler: handler);

		_subscriptions.Add(sub);
		return sub;
	}

	public void Dispose()
	{
		foreach (var sub in _subscriptions)
			sub.Dispose();

		_subscriptions.Clear();
	}
}
