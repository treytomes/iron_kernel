using System.Collections.Concurrent;

namespace IronKernel.Kernel.Bus;

public sealed class MessageBus : IMessageBus
{
	private readonly ConcurrentDictionary<Type, List<ISubscription>> _handlers = new();

	public void Publish<T>(T message)
		where T : notnull
	{
		if (_handlers.TryGetValue(typeof(T), out var list))
		{
			ISubscription[] snapshot;

			lock (list)
			{
				snapshot = list.ToArray();
			}

			foreach (var sub in snapshot)
			{
				_ = sub.Invoke(message);
			}
		}
	}

	public IDisposable Subscribe<T>(
		Func<T, CancellationToken, Task> handler)
		where T : notnull
	{
		var sub = new Subscription<T>(handler, this);

		var list = _handlers.GetOrAdd(typeof(T), _ => new List<ISubscription>());

		lock (list)
		{
			list.Add(sub);
		}

		return sub;
	}

	private void Unsubscribe(Type type, ISubscription sub)
	{
		if (_handlers.TryGetValue(type, out var list))
		{
			lock (list)
			{
				list.Remove(sub);
			}
		}
	}

	private interface ISubscription
	{
		Task Invoke(object message);
	}

	private sealed class Subscription<T> : ISubscription, IDisposable
		where T : notnull
	{
		private readonly Func<T, CancellationToken, Task> _handler;
		private readonly MessageBus _bus;
		private bool _disposed;

		public Subscription(
			Func<T, CancellationToken, Task> handler,
			MessageBus bus)
		{
			_handler = handler;
			_bus = bus;
		}

		public Task Invoke(object message)
		{
			if (_disposed) return Task.CompletedTask;
			return _handler((T)message, CancellationToken.None);
		}

		public void Dispose()
		{
			if (_disposed) return;
			_disposed = true;
			_bus.Unsubscribe(typeof(T), this);
		}
	}
}
