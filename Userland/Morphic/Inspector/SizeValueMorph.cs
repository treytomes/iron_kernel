using System.Drawing;
using Userland.Morphic.Layout;

namespace Userland.Morphic.Inspector;

public sealed class SizeValueMorph : Morph, IValueContentMorph
{
    private readonly Action<object?>? _setter;
    private Size _value;

    private readonly HorizontalStackMorph _root;
    private readonly TextEditMorph _wField;
    private readonly TextEditMorph _hField;

    public SizeValueMorph(Func<object?> valueProvider, Action<object?>? setter)
    {
        _setter = setter;
        _value = valueProvider() is Size s ? s : Size.Empty;

        IsSelectable = false;

        _wField = new TextEditMorph(_value.Width.ToString(), s =>
        {
            if (int.TryParse(s, out var w))
            {
                _value = new Size(w, _value.Height);
                _setter?.Invoke(_value);
            }
        });

        _hField = new TextEditMorph(_value.Height.ToString(), s =>
        {
            if (int.TryParse(s, out var h))
            {
                _value = new Size(_value.Width, h);
                _setter?.Invoke(_value);
            }
        });

        _root = new HorizontalStackMorph { Padding = 0, Spacing = 4 };
        _root.AddMorph(new LabelMorph(Point.Empty) { Text = "W:", IsSelectable = false, BackgroundColor = null });
        _root.AddMorph(_wField);
        _root.AddMorph(new LabelMorph(Point.Empty) { Text = "H:", IsSelectable = false, BackgroundColor = null });
        _root.AddMorph(_hField);

        AddMorph(_root);
    }

    protected override void UpdateLayout()
    {
        Size = _root.Size;
        base.UpdateLayout();
    }

    public void Refresh(object? value)
    {
        if (value is not Size s || s == _value)
            return;

        _value = s;
        _wField.Refresh(_value.Width);
        _hField.Refresh(_value.Height);
    }
}
