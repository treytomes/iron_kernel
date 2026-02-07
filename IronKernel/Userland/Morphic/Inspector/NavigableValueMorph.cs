using System.Drawing;
using IronKernel.Userland.Gfx;
using IronKernel.Userland.Morphic.Events;

namespace IronKernel.Userland.Morphic.Inspector;

public sealed class NavigableValueMorph : Morph
{
	private readonly Func<object?> _valueProvider;
	private readonly Action<object> _navigate;
	private readonly LabelMorph _label;

	public NavigableValueMorph(
		Func<object?> valueProvider,
		Action<object> navigate)
	{
		_valueProvider = valueProvider;
		_navigate = navigate;

		IsSelectable = true;

		_label = new LabelMorph(Point.Empty)
		{
			IsSelectable = false,
			BackgroundColor = null
		};

		AddMorph(_label);
		UpdateLabel();
	}

	public override void Update(double deltaTime)
	{
		base.Update(deltaTime);
		UpdateLabel();
	}

	private void UpdateLabel()
	{
		var value = _valueProvider();
		_label.Text = value != null
			? value.GetType().Name
			: "<null>";
	}

	protected override void UpdateLayout()
	{
		_label.Position = Point.Empty;
		Size = _label.Size;
		base.UpdateLayout();
	}

	public override void OnPointerDown(PointerDownEvent e)
	{
		var value = _valueProvider();
		if (value != null)
		{
			_navigate(value);
			e.MarkHandled();
		}
	}
}