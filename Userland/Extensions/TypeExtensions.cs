namespace Userland;

public static class TypeExtensions
{
	public static bool IsNumeric(this Type? @this)
	{
		if (@this == null) return false;
		return @this == typeof(byte)
			|| @this == typeof(sbyte)
			|| @this == typeof(short)
			|| @this == typeof(ushort)
			|| @this == typeof(int)
			|| @this == typeof(uint)
			|| @this == typeof(long)
			|| @this == typeof(ulong)
			|| @this == typeof(float)
			|| @this == typeof(double)
			|| @this == typeof(decimal);
	}
}