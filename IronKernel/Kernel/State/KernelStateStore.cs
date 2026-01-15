using System.Collections.Concurrent;
using IronKernel.Kernel.State;

namespace IronKernel.State;

public sealed class KernelStateStore : IKernelState
{
	private readonly ConcurrentDictionary<string, object?> _state =
		new(StringComparer.Ordinal);

	public bool Contains(string key)
	{
		if (key is null)
			throw new ArgumentNullException(nameof(key));

		return _state.ContainsKey(key);
	}

	public T Get<T>(string key, T defaultValue = default!)
	{
		if (key is null)
			throw new ArgumentNullException(nameof(key));

		if (_state.TryGetValue(key, out var value))
		{
			if (value is T typed)
				return typed;

			throw new InvalidCastException(
				$"State value '{key}' is of type {value?.GetType().Name}, not {typeof(T).Name}");
		}

		return defaultValue;
	}

	public void Set<T>(string key, T value)
		where T : notnull
	{
		if (key is null)
			throw new ArgumentNullException(nameof(key));

		_state[key] = value;
	}

	public T? Update<T>(string key, Func<T, T> updater, T defaultValue = default!)
		where T : notnull
	{
		if (key is null)
			throw new ArgumentNullException(nameof(key));
		if (updater is null)
			throw new ArgumentNullException(nameof(updater));

		var result = _state.AddOrUpdate(
			key,
			_ => updater(defaultValue),
			(_, existing) =>
			{
				if (existing is not T typed)
					throw new InvalidCastException(
						$"State value '{key}' is of type {existing?.GetType().Name}, not {typeof(T).Name}");

				return updater(typed);
			});

		// Result is object, but guaranteed by construction.
		return (T?)result;
	}
}
