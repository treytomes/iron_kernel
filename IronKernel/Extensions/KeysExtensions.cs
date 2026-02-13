using IronKernel.Common.ValueObjects;

namespace IronKernel;

internal static class KeysExtensions
{
	public static Key ToHost(this OpenTK.Windowing.GraphicsLibraryFramework.Keys @this)
	{
		// There are a lot of keys.  A direct exhaustive mapping just feels like too much,
		// so we're cheating just a bit here.

		// A direct cast might cause a mismatch if the number mapping ever changes.

		var name = Enum.GetName(@this.GetType(), @this);
		if (string.IsNullOrWhiteSpace(name))
		{
			throw new NotImplementedException($"Unknown key: {@this}");
		}
		return Enum.Parse<Key>(name);
	}
}