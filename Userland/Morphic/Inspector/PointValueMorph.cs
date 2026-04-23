using System.Drawing;
using Userland.Morphic.Layout;

namespace Userland.Morphic.Inspector;

public sealed class PointValueMorph : Morph, IValueContentMorph
{
    private readonly Action<object?>? _setter;
    private Point _value;

    private readonly HorizontalStackMorph _root;
    private readonly TextEditMorph _xField;
    private readonly TextEditMorph _yField;

    public PointValueMorph(Func<object?> valueProvider, Action<object?>? setter)
    {
        _setter = setter;
        _value = valueProvider() is Point p ? p : Point.Empty;

        IsSelectable = false;

        _xField = new TextEditMorph(_value.X.ToString(), s =>
        {
            if (int.TryParse(s, out var x))
            {
                _value = new Point(x, _value.Y);
                _setter?.Invoke(_value);
            }
        });

        _yField = new TextEditMorph(_value.Y.ToString(), s =>
        {
            if (int.TryParse(s, out var y))
            {
                _value = new Point(_value.X, y);
                _setter?.Invoke(_value);
            }
        });

        _root = new HorizontalStackMorph { Padding = 0, Spacing = 4 };
        _root.AddMorph(new LabelMorph(Point.Empty) { Text = "X:", IsSelectable = false, BackgroundColor = null });
        _root.AddMorph(_xField);
        _root.AddMorph(new LabelMorph(Point.Empty) { Text = "Y:", IsSelectable = false, BackgroundColor = null });
        _root.AddMorph(_yField);

        AddMorph(_root);
    }

    protected override void UpdateLayout()
    {
        Size = _root.Size;
        base.UpdateLayout();
    }

    public void Refresh(object? value)
    {
        if (value is not Point p || p == _value)
            return;

        _value = p;
        _xField.Refresh(_value.X);
        _yField.Refresh(_value.Y);
    }
}
