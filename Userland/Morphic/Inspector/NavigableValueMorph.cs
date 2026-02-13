using System.Drawing;
using Userland.Morphic.Commands;

namespace Userland.Morphic.Inspector;

public sealed class NavigableValueMorph : Morph
{
	private readonly Func<object?> _valueProvider;
	private readonly Action<object> _navigate;
	private readonly ButtonMorph _button;

	public NavigableValueMorph(
		Func<object?> valueProvider,
		Action<object> navigate)
	{
		_valueProvider = valueProvider;
		_navigate = navigate;

		IsSelectable = false;

		_button = new ButtonMorph(Point.Empty, Size.Empty, string.Empty)
		{
			Command = new ActionCommand(() =>
			{
				var value = _valueProvider();
				if (value != null)
				{
					_navigate(value);
				}
			})
		};

		AddMorph(_button);
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
		_button.Text = value != null
			? value.GetType().Name
			: "<null>";

		InvalidateLayout();
	}

	protected override void UpdateLayout()
	{
		_button.Position = Point.Empty;
		Size = _button.Size;
		base.UpdateLayout();
	}
}