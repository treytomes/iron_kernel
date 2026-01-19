namespace IronKernel.Common;

public interface IApplicationBus
{
	/// <summary>
	/// Publish a message to the application.
	/// Fire-and-forget; delivery is supervised by the runtime.
	/// </summary>
	void Publish<T>(T message)
		where T : notnull;

	/// <summary>
	/// Subscribe to a message type within this application.
	/// </summary>
	IDisposable Subscribe<T>(
		string handlerName,
		Func<T, CancellationToken, Task> handler)
		where T : notnull;
}
