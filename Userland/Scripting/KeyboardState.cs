using System.Collections.Concurrent;
using IronKernel.Common.ValueObjects;

namespace Userland.Scripting;

public sealed class KeyboardState
{
	private readonly ConcurrentDictionary<Key, bool> _keysDown = new();

	public void SetKeyState(Key key, bool isPressed)
	{
		_keysDown.AddOrUpdate(key, isPressed, (k, v) => isPressed);
	}

	public bool GetKeyState(Key key)
	{
		return _keysDown.GetOrAdd(key, false);
	}

	public bool IsAnyKeyDown()
	{
		return _keysDown.Any(x => x.Value);
	}
}