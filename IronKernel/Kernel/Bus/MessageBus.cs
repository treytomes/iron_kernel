using System.Collections.Concurrent;

namespace IronKernel.Kernel.Bus;

public sealed class MessageBus : IKernelMessageBus
{
	private readonly ConcurrentDictionary<Type, List<ISubscription>> _handlers = new();

	public void Publish<T>(T message)
		where T : notnull
	{
		if (!_handlers.TryGetValue(typeof(T), out var list))
			return;

		ISubscription[] snapshot;
		lock (list)
		{
			snapshot = list.ToArray();
		}

		foreach (var sub in snapshot)
		{
			sub.Dispatch(message);
		}
	}

	public IDisposable Subscribe<T>(
		IModuleRuntime runtime,
		string handlerName,
		Func<T, CancellationToken, Task> handler
	) where T : notnull
	{
		if (runtime is null)
			throw new ArgumentNullException(nameof(runtime));

		var sub = new Subscription<T>(
			runtime,
			handlerName,
			handler,
			this);

		var list = _handlers.GetOrAdd(
			typeof(T),
			_ => new List<ISubscription>());

		lock (list)
		{
			list.Add(sub);
		}

		return sub;
	}

	public IDisposable SubscribeKernel<T>(
		Func<T, CancellationToken, Task> handler)
		where T : notnull
	{
		var sub = new KernelSubscription<T>(handler, this);

		var list = _handlers.GetOrAdd(
			typeof(T),
			_ => new List<ISubscription>());

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
		void Dispatch(object message);
	}

	private sealed class Subscription<T> : ISubscription, IDisposable
		where T : notnull
	{
		private readonly IModuleRuntime _runtime;
		private readonly string _handlerName;
		private readonly Func<T, CancellationToken, Task> _handler;
		private readonly MessageBus _bus;
		private bool _disposed;

		public Subscription(
			IModuleRuntime runtime,
			string handlerName,
			Func<T, CancellationToken, Task> handler,
			MessageBus bus)
		{
			_runtime = runtime;
			_handlerName = handlerName;
			_handler = handler;
			_bus = bus;
		}

		public void Dispatch(object message)
		{
			if (_disposed) return;

			_runtime.RunAsync(
				_handlerName,
				ct => _handler((T)message, ct),
				CancellationToken.None);
		}

		public void Dispose()
		{
			if (_disposed) return;
			_disposed = true;
			_bus.Unsubscribe(typeof(T), this);
		}
	}

	private sealed class KernelSubscription<T> : ISubscription, IDisposable
		where T : notnull
	{
		private readonly Func<T, CancellationToken, Task> _handler;
		private readonly MessageBus _bus;
		private bool _disposed;

		public KernelSubscription(
			Func<T, CancellationToken, Task> handler,
			MessageBus bus)
		{
			_handler = handler;
			_bus = bus;
		}

		public void Dispatch(object message)
		{
			if (_disposed) return;

			// Fire safely — kernel must never crash on bus delivery
			_ = SafeInvoke((T)message);
		}

		private async Task SafeInvoke(T message)
		{
			try
			{
				await _handler(message, CancellationToken.None);
			}
			catch (Exception ex)
			{
				Console.Error.WriteLine(
					$"Kernel message handler faulted: {ex}");
			}
		}

		public void Dispose()
		{
			if (_disposed) return;
			_disposed = true;
			_bus.Unsubscribe(typeof(T), this);
		}
	}
}
