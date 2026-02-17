using System.Drawing;
using System.Linq.Expressions;
using System.Reflection;

namespace Userland.Morphic.Inspector;

/// <summary>
/// A single row in an Inspector, displaying a property name
/// and a live-updating value.
/// </summary>
public sealed class PropertyRowMorph : Morph
{
	#region Fields

	private readonly LabelMorph _nameLabel;
	private readonly ValueMorph _valueMorph;

	private int _nameColumnWidth = 96;
	private const int Padding = 1;

	#endregion

	#region Constructors

	/// <summary>
	/// Core constructor: explicit name, value provider, optional setter,
	/// and optional declared type.
	/// </summary>
	public PropertyRowMorph(
		IInspectorFactory inspectorFactory,
		string name,
		Func<object?> valueProvider,
		Action<object?>? valueSetter = null,
		Type? declaredType = null
	)
	{
		if (string.IsNullOrWhiteSpace(name))
			throw new ArgumentException("Property name cannot be null or empty.", nameof(name));
		if (valueProvider == null)
			throw new ArgumentNullException(nameof(valueProvider));

		IsSelectable = false;

		_nameLabel = CreateNameLabel(name);

		_valueMorph = new ValueMorph(
			inspectorFactory,
			valueProvider,
			valueSetter,
			declaredType
		);

		AddMorph(_nameLabel);
		AddMorph(_valueMorph);
	}

	/// <summary>
	/// Expression-based constructor.
	/// Read-only unless a setter is supplied elsewhere.
	/// </summary>
	public PropertyRowMorph(IInspectorFactory inspectorFactory, Expression<Func<object?>> expression)
		: this(
			inspectorFactory,
			ExtractName(expression),
			expression.Compile(),
			null,
			ExtractDeclaredType(expression)
		)
	{
	}

	/// <summary>
	/// Reflection-based constructor.
	/// Produces a writable row if the property is writable.
	/// </summary>
	public PropertyRowMorph(IInspectorFactory inspectorFactory, PropertyInfo property, object target)
		: this(
			inspectorFactory,
			property?.Name ?? throw new ArgumentNullException(nameof(property)),
			() => property.GetValue(target),
			property.CanWrite
				? value => property.SetValue(target, value)
				: null,
			property.PropertyType
		)
	{
	}

	#endregion

	#region Properties

	public int NameLabelWidth => _nameLabel.Size.Width;

	/// <summary>
	/// Width of the name column, controlled by the parent list.
	/// </summary>
	public int NameColumnWidth
	{
		get => _nameColumnWidth;
		set
		{
			if (_nameColumnWidth != value)
			{
				_nameColumnWidth = value;
				InvalidateLayout();
			}
		}
	}

	#endregion

	#region Layout

	protected override void UpdateLayout()
	{
		_nameLabel.Position = new Point(Padding, Padding);
		_valueMorph.Position = new Point(_nameColumnWidth + Padding, Padding);

		var height = Math.Max(
			_nameLabel.Size.Height,
			_valueMorph.Size.Height);

		Size = new Size(
			_nameColumnWidth + _valueMorph.Size.Width + Padding * 2,
			height + Padding * 2);

		base.UpdateLayout();
	}

	#endregion

	#region Helpers

	private static LabelMorph CreateNameLabel(string name)
	{
		return new LabelMorph(Point.Empty)
		{
			IsSelectable = false,
			Text = name,
			BackgroundColor = null
		};
	}

	private static string ExtractName(Expression<Func<object?>> expr)
	{
		if (expr == null)
			throw new ArgumentNullException(nameof(expr));

		Expression body = expr.Body;

		// Handle boxing: () => (object)foo.Bar
		if (body is UnaryExpression unary && unary.NodeType == ExpressionType.Convert)
			body = unary.Operand;

		if (body is MemberExpression member)
			return member.Member.Name;

		throw new ArgumentException(
			"Expression must be a simple property or field access.",
			nameof(expr));
	}

	private static Type? ExtractDeclaredType(Expression<Func<object?>> expr)
	{
		Expression body = expr.Body;

		if (body is UnaryExpression unary && unary.NodeType == ExpressionType.Convert)
			body = unary.Operand;

		return body.Type;
	}

	#endregion
}