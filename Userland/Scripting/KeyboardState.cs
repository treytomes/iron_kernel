using IronKernel.Common.ValueObjects;

namespace Userland.Scripting;

public sealed class KeyboardState
{
	public readonly HashSet<Key> KeysDown = new();
}