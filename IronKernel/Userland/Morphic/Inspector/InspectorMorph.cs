using System;
using System.Drawing;
using System.Linq;
using System.Reflection;

namespace IronKernel.Userland.Morphic.Inspector;

/// <summary>
/// A live inspector for an arbitrary object.
/// Displays public instance properties in a PropertyListMorph.
/// </summary>
public sealed class InspectorMorph : WindowMorph
{
	#region Fields

	private readonly object _target;
	private readonly PropertyListMorph _propertyList;

	#endregion

	#region Constructors

	public InspectorMorph(object target)
		: this(
			target,
			new Point(32, 32),
			new Size(240, 180))
	{
	}

	public InspectorMorph(object target, Point position, Size size)
		: base(position, size, GetTitle(target))
	{
		_target = target ?? throw new ArgumentNullException(nameof(target));

		IsSelectable = true;

		_propertyList = BuildPropertyList(target);

		Content.AddMorph(_propertyList);
	}

	#endregion

	#region Helpers

	private static string GetTitle(object target)
	{
		return target.GetType().Name;
	}

	private static PropertyListMorph BuildPropertyList(object target)
	{
		var properties = target
			.GetType()
			.GetProperties(BindingFlags.Instance | BindingFlags.Public)
			.Where(p => p.CanRead && p.GetIndexParameters().Length == 0);

		// This assumes you added FromProperties(IEnumerable<PropertyInfo>, object)
		return PropertyListMorph.FromProperties(properties, target);
	}

	#endregion
}