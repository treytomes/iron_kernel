using System.Drawing;
using IronKernel.Common.ValueObjects;

namespace Userland.Morphic.Inspector;

public class InspectorFactory : IInspectorFactory
{
	private readonly Action<object>? _navigate;

	public InspectorFactory(Action<object>? navigate = null)
	{
		_navigate = navigate;
	}

	public Morph GetInspectorFor(
		Type? declaredType,
		Func<object?> valueProvider,
		Action<object?>? setter = null)
	{
		// Console.WriteLine($"Inspecting type: {declaredType}");

		// --- MiniScript scalar values ---
		if (declaredType == typeof(Miniscript.ValNumber))
		{
			return new TextEditMorph(
				Point.Empty,
				(valueProvider() as Miniscript.ValNumber)?.ToString() ?? "0",
				s =>
				{
					if (double.TryParse(s, out var d))
						setter?.Invoke(new Miniscript.ValNumber(d));
				},
				s => double.TryParse(s, out _)
			);
		}

		if (declaredType == typeof(Miniscript.ValString))
		{
			return new TextEditMorph(
				Point.Empty,
				(valueProvider() as Miniscript.ValString)?.ToString() ?? string.Empty,
				s => setter?.Invoke(new Miniscript.ValString(s))
			);
		}

		if (declaredType == typeof(Miniscript.ValNull))
		{
			return new LabelMorph
			{
				Text = "null",
				IsSelectable = false,
				BackgroundColor = null
			};
		}

		// --- Boolean ---
		if (declaredType == typeof(bool) || declaredType == typeof(bool?))
		{
			return new CheckBoxMorph(b => setter?.Invoke(b));
		}

		// --- Color ---
		if (declaredType == typeof(RadialColor))
		{
			return new RadialColorSliderValueMorph(c => setter?.Invoke(c));
			// return new RadialColorValueMorph(c => setter?.Invoke(c));
		}

		// --- String ---
		if (declaredType == typeof(string))
		{
			return new TextEditMorph(
				Point.Empty,
				valueProvider()?.ToString() ?? string.Empty,
				s => setter?.Invoke(s)
			);
		}

		// --- Numeric primitives (first slice) ---
		if (IsNumericType(declaredType))
		{
			return new TextEditMorph(
				Point.Empty,
				valueProvider()?.ToString() ?? string.Empty,
				s =>
				{
					if (TryParseNumber(declaredType!, s, out var value))
					{
						setter?.Invoke(value);
					}
				},
				s => TryParseNumber(declaredType!, s, out _)
			);
		}

		// --- Drill-down ---
		if (_navigate != null && IsNavigableType(declaredType))
		{
			return new NavigableValueMorph(valueProvider, _navigate);
		}

		// --- Fallback ---
		return new LabelMorph
		{
			IsSelectable = false,
			BackgroundColor = null
		};
	}

	#region Helpers

	private static bool IsNumericType(Type? type)
	{
		return type == typeof(byte)
			|| type == typeof(sbyte)
			|| type == typeof(short)
			|| type == typeof(ushort)
			|| type == typeof(int)
			|| type == typeof(uint)
			|| type == typeof(long)
			|| type == typeof(ulong)
			|| type == typeof(float)
			|| type == typeof(double)
			|| type == typeof(decimal);
	}

	private static bool TryParseNumber(Type type, string text, out object? value)
	{
		value = null;

		try
		{
			if (type == typeof(byte) && byte.TryParse(text, out var b)) value = b;
			else if (type == typeof(sbyte) && sbyte.TryParse(text, out var sb)) value = sb;
			else if (type == typeof(short) && short.TryParse(text, out var s)) value = s;
			else if (type == typeof(ushort) && ushort.TryParse(text, out var us)) value = us;
			else if (type == typeof(int) && int.TryParse(text, out var i)) value = i;
			else if (type == typeof(uint) && uint.TryParse(text, out var ui)) value = ui;
			else if (type == typeof(long) && long.TryParse(text, out var l)) value = l;
			else if (type == typeof(ulong) && ulong.TryParse(text, out var ul)) value = ul;
			else if (type == typeof(float) && float.TryParse(text, out var f)) value = f;
			else if (type == typeof(double) && double.TryParse(text, out var d)) value = d;
			else if (type == typeof(decimal) && decimal.TryParse(text, out var m)) value = m;
			else return false;

			return true;
		}
		catch
		{
			return false;
		}
	}

	private static bool IsNavigableType(Type? type)
	{
		if (type == null)
			return false;
		if (type.IsPrimitive)
			return false;
		if (type == typeof(string))
			return false;
		return !type.IsEnum;
	}

	#endregion
}