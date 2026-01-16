using System.Collections.Concurrent;

namespace IronKernel.Modules.ApplicationHost;

internal sealed class ApplicationState : IApplicationState
{
	private readonly ConcurrentDictionary<string, object> _state = new();

	public bool TryGet<T>(string key, out T value)
	{
		if (_state.TryGetValue(key, out var obj) && obj is T t)
		{
			value = t;
			return true;
		}

		value = default!;
		return false;
	}

	public void Set<T>(string key, T value)
	{
		_state[key] = value!;
	}

	public bool Remove(string key)
	{
		return _state.TryRemove(key, out _);
	}
}
