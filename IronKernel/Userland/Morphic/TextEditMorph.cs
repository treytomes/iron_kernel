using System.Drawing;
using IronKernel.Common.ValueObjects;
using IronKernel.Userland.Gfx;
using IronKernel.Userland.Morphic.Events;
using IronKernel.Userland.Morphic.Inspector;

namespace IronKernel.Userland.Morphic;

public sealed class TextEditMorph : Morph, IValueContentMorph
{
	#region Constants

	private const int Padding = 2;
	private const double CaretBlinkMs = 500;

	#endregion

	#region Fields

	private string _text;
	private int _caretIndex;
	private readonly Action<string>? _setter;
	private readonly LabelMorph _label;

	private double _caretBlinkTime;
	private bool _caretVisible = true;

	#endregion

	#region Constructor

	public TextEditMorph(string initialText, Action<string>? setter)
		: this(Point.Empty, initialText, setter)
	{
	}

	public TextEditMorph(Point position, string initialText, Action<string>? setter)
	{
		Position = position;
		_text = initialText ?? string.Empty;
		_caretIndex = _text.Length; // caret starts at end
		_setter = setter;

		IsSelectable = true;

		_label = new LabelMorph(Point.Empty)
		{
			IsSelectable = false,
			Text = _text,
			BackgroundColor = null
		};

		AddMorph(_label);
		InvalidateLayout();
	}

	#endregion

	#region Focus

	public override bool WantsKeyboardFocus => true;

	#endregion

	#region Methods

	public override void OnPointerDown(PointerDownEvent e)
	{
		base.OnPointerDown(e);

		// If we just gained focus, make caret immediately visible
		if (HasKeyboardFocus())
		{
			_caretVisible = true;
			_caretBlinkTime = 0;
			Invalidate();
		}
	}

	#region Update (caret blink)

	public override void Update(double deltaMs)
	{
		base.Update(deltaMs);

		if (!HasKeyboardFocus())
		{
			_caretVisible = false;
			_caretBlinkTime = 0;
			return;
		}

		_caretBlinkTime += deltaMs;
		if (_caretBlinkTime >= CaretBlinkMs)
		{
			_caretBlinkTime = 0;
			_caretVisible = !_caretVisible;
			Invalidate();
		}
	}

	#endregion

	#region Keyboard input

	public override void OnKey(KeyEvent e)
	{
		if (e.Action != InputAction.Press)
			return;

		switch (e.Key)
		{
			case Key.Enter:
			case Key.Escape:
				if (TryGetWorld(out var world)) world.ReleaseKeyboard(this);
				break;

			case Key.Left:
				if (_caretIndex > 0)
					_caretIndex--;
				break;

			case Key.Right:
				if (_caretIndex < _text.Length)
					_caretIndex++;
				break;

			case Key.Home:
				_caretIndex = 0;
				break;

			case Key.End:
				_caretIndex = _text.Length;
				break;

			case Key.Backspace:
				if (_caretIndex > 0)
				{
					_text = _text.Remove(_caretIndex - 1, 1);
					_caretIndex--;
					OnTextChanged();
				}
				break;

			case Key.Delete:
				if (_caretIndex < _text.Length)
				{
					_text = _text.Remove(_caretIndex, 1);
					OnTextChanged();
				}
				break;

			default:
				{
					// ✅ character input via extension method
					var ch = e.ToText();
					if (ch != null)
					{
						_text = _text.Insert(_caretIndex, ch.Value.ToString());
						_caretIndex++;
						OnTextChanged();
					}
					break;
				}
		}

		_caretIndex = Math.Clamp(_caretIndex, 0, _text.Length);
		Invalidate();
		e.MarkHandled();
	}

	#endregion

	#region Layout

	protected override void UpdateLayout()
	{
		_label.Position = new Point(Padding, Padding);
		Size = new Size(
			Math.Max(40, _label.Size.Width + Padding * 2),
			_label.Size.Height + Padding * 2);
	}

	#endregion

	#region Rendering

	protected override void DrawSelf(IRenderingContext rc)
	{
		if (Style == null)
			return;

		var s = Style.Semantic;

		// Background
		rc.RenderFilledRect(
			new Rectangle(Point.Empty, Size),
			s.Surface);

		// Border
		rc.RenderRect(
			new Rectangle(Point.Empty, Size),
			HasKeyboardFocus() ? s.Primary : s.Border);

		// Caret
		if (HasKeyboardFocus() && _caretVisible)
		{
			var caretX = Padding + MeasureCaretOffset();
			rc.RenderLine(
				new Point(caretX, Padding),
				new Point(caretX, Padding + _label.Size.Height),
				s.Text);
		}
	}

	#endregion

	#region Helpers

	private void OnTextChanged()
	{
		_label.Text = _text;
		_setter?.Invoke(_text);

		// reset caret blink on edit
		_caretVisible = true;
		_caretBlinkTime = 0;

		InvalidateLayout();
	}

	private bool HasKeyboardFocus()
	{
		return TryGetWorld(out var world) && world.KeyboardFocus == this;
	}

	private int MeasureCaretOffset()
	{
		return _caretIndex * _label.TileSize.Width;
	}

	public void Refresh(object? value)
	{
		var newText = value?.ToString() ?? string.Empty;

		if (_text == newText)
			return;

		_text = newText;
		_label.Text = _text;

		// Caret goes to end on external updates.
		_caretIndex = _text.Length;

		// Reset caret blink so it’s visible.
		_caretVisible = true;
		_caretBlinkTime = 0;

		InvalidateLayout();
		Invalidate();
	}

	#endregion

	#endregion
}