namespace IronKernel.Kernel.Bus;

public interface IMessageBus
{
	/// <summary>
	/// Publish a message to all subscribers.
	/// </summary>
	void Publish<T>(T message)
		where T : notnull;

	// Module subscription (supervised)
	IDisposable Subscribe<T>(
		IModuleRuntime runtime,
		string handlerName,
		Func<T, CancellationToken, Task> handler)
		where T : notnull;

	IDisposable SubscribeApplication<T>(
		string handlerName,
		Func<T, CancellationToken, Task> handler)
		where T : notnull;

	/// <summary>
	/// Subscribe with synchronous inline dispatch — no Task.Run, no supervision.
	/// Use only for fast, non-blocking handlers on the hot path.
	/// </summary>
	IDisposable SubscribeInline<T>(Action<T> handler)
		where T : notnull;
}

public interface IKernelMessageBus : IMessageBus
{
	// Kernel-only subscription
	IDisposable SubscribeKernel<T>(
		Func<T, CancellationToken, Task> handler)
		where T : notnull;
}