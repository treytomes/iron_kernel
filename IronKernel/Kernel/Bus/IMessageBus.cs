namespace IronKernel.Kernel.Bus;

public interface IMessageBus
{
	/// <summary>
	/// Publish a message to all subscribers.
	/// </summary>
	void Publish<T>(T message)
		where T : notnull;

	/// <summary>
	/// Subscribe to messages of type T.
	/// Returns an IDisposable that unsubscribes.
	/// </summary>
	IDisposable Subscribe<T>(
		Func<T, CancellationToken, Task> handler)
		where T : notnull;
}
