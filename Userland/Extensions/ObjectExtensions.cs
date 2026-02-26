using System.Collections;
using Miniscript;

namespace Userland;

public static class ObjectExtensions
{
	public static Value ToValue(this object? @this)
	{
		if (@this == null)
		{
			return ValNull.instance;
		}
		if (@this is Value)
		{
			return (@this as Value)!;
		}
		if (@this is bool)
		{
			return ValNumber.Truth(Convert.ToBoolean(@this));
		}
		if (@this.GetType().IsNumeric())
		{
			return new ValNumber(Convert.ToDouble(@this));
		}
		if (@this is string)
		{
			return new ValString(Convert.ToString(@this));
		}
		if (@this is IEnumerable)
		{
			var list = new ValList();
			foreach (var item in (@this as IEnumerable)!)
			{
				list.values.Add(item.ToValue());
			}
		}
		if (@this is Enum)
		{
			return new ValString(@this.ToString());
		}

		// Otherwise, convert to a map.

		var props = @this.GetType().GetProperties();
		var map = new ValMap();
		foreach (var prop in props)
		{
			var value = prop.GetValue(@this).ToValue();
			map[prop.Name] = value;
		}
		return map;
	}
}