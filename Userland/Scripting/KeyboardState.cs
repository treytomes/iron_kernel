using System.Collections.Concurrent;
using IronKernel.Common.ValueObjects;

namespace Userland.Scripting;

public sealed class KeyboardState
{
	private readonly ConcurrentDictionary<Key, bool> _keysDown = new();

	public void SetKeyState(Key key, bool isPressed)
	{
		// _keysDown[key] = isPressed;
		_keysDown.AddOrUpdate(key, isPressed, (k, v) => isPressed);
		// Console.WriteLine($"SetKeyState({key}, {isPressed})");
	}

	// private bool _oldValue = false;
	public bool GetKeyState(Key key)
	{
		// if (!_keysDown.ContainsKey(key)) return false;
		// return _keysDown[key];
		var value = _keysDown.GetOrAdd(key, false);
		// if (key == Key.Down && value != _oldValue)
		// {
		// 	Console.WriteLine($"GetKeyState({key}, {value})");
		// 	_oldValue = value;
		// }
		return value;
	}

	public bool IsAnyKeyDown()
	{
		return _keysDown.Any(x => x.Value);
	}
}