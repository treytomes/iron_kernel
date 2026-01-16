using System.Collections.Concurrent;
using IronKernel.Kernel.Messages;

namespace IronKernel.Kernel.Bus;

public sealed class MessageBus : IKernelMessageBus
{
	private const int FloodThreshold = 10_000;
	private static readonly TimeSpan FloodWindow = TimeSpan.FromSeconds(1);

	private readonly ConcurrentDictionary<Type, List<ISubscription>> _handlers = new();
	private readonly ConcurrentDictionary<(Type module, Type message), FloodCounter> _floodCounters = new();

	public void Publish<T>(T message)
		where T : notnull
	{
		var messageType = typeof(T);

		// Identify publisher (module if inside a module runtime)
		var moduleType = ModuleContext.CurrentModule.Value;

		// Flood detection applies only to kernel-managed modules
		if (moduleType != null)
		{
			if (IsFlooding(moduleType, messageType))
			{
				// Flood already reported to kernel; drop message
				return;
			}
		}

		// Fast exit if no subscribers
		if (!_handlers.TryGetValue(messageType, out var list))
			return;

		ISubscription[] snapshot;
		lock (list)
		{
			// Snapshot to avoid reentrancy + mutation issues
			snapshot = list.ToArray();
		}

		// Dispatch without awaiting — handlers decide their own execution model
		foreach (var sub in snapshot)
		{
			try
			{
				sub.Dispatch(message);
			}
			catch (Exception ex)
			{
				// Dispatch must never crash the kernel
				Console.Error.WriteLine(
					$"MessageBus dispatch failure for {messageType.Name}: {ex}");
			}
		}
	}

	private bool IsFlooding(Type moduleType, Type messageType)
	{
		var now = DateTime.UtcNow;

		var counter = _floodCounters.GetOrAdd(
			(moduleType, messageType),
			_ => new FloodCounter
			{
				WindowStart = now,
				Count = 0
			});

		lock (counter)
		{
			if (now - counter.WindowStart > FloodWindow)
			{
				counter.WindowStart = now;
				counter.Count = 0;
			}

			counter.Count++;

			if (counter.Count > FloodThreshold)
			{
				PublishKernel(new ModuleMessageFlooded(
					moduleType,
					messageType,
					counter.Count,
					now - counter.WindowStart));

				return true;
			}
		}

		return false;
	}

	private void PublishKernel<T>(T message)
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
				sub.Dispatch(message);
			}
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

	public IDisposable SubscribeApplication<T>(
	string handlerName,
	Func<T, CancellationToken, Task> handler)
	where T : notnull
	{
		var sub = new ApplicationSubscription<T>(
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
				ModuleTaskKind.Finite,
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

	private sealed class ApplicationSubscription<T> : ISubscription, IDisposable
		where T : notnull
	{
		private readonly string _handlerName;
		private readonly Func<T, CancellationToken, Task> _handler;
		private readonly MessageBus _bus;
		private bool _disposed;

		public ApplicationSubscription(
			string handlerName,
			Func<T, CancellationToken, Task> handler,
			MessageBus bus)
		{
			_handlerName = handlerName;
			_handler = handler;
			_bus = bus;
		}

		public void Dispatch(object message)
		{
			if (_disposed) return;

			// Application code is NOT kernel supervised
			_ = InvokeSafely((T)message);
		}

		private async Task InvokeSafely(T message)
		{
			try
			{
				await _handler(message, CancellationToken.None);
			}
			catch (Exception ex)
			{
				Console.Error.WriteLine(
					$"Application handler '{_handlerName}' faulted: {ex}");
			}
		}

		public void Dispose()
		{
			if (_disposed) return;
			_disposed = true;
			_bus.Unsubscribe(typeof(T), this);
		}
	}

	private sealed class FloodCounter
	{
		public int Count;
		public DateTime WindowStart;
	}
}
