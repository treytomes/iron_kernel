using System.Drawing;
using IronKernel.Common.ValueObjects;
using Userland.Gfx;
using Userland.Morphic.Events;
using Userland.Morphic.Inspector;

namespace Userland.Morphic;

public sealed class TextEditMorph : Morph, IValueContentMorph
{
	#region Events

	public event EventHandler? OnCommit = null;
	public event EventHandler? OnCancel = null;

	#endregion

	#region Fields

	private string _text;
	private string _originalText;
	private int _caretIndex;

	private readonly Action<string>? _setter;
	private readonly Func<string, bool>? _validator;

	private readonly LabelMorph _label;

	private double _caretBlinkTime;
	private bool _caretVisible = true;

	#endregion

	#region Constructors

	public TextEditMorph(string initialText, Action<string>? setter)
		: this(Point.Empty, initialText, setter, null)
	{
	}

	public TextEditMorph(
		Point position,
		string initialText,
		Action<string>? setter,
		Func<string, bool>? validator = null)
	{
		Position = position;

		_text = initialText ?? string.Empty;
		_originalText = _text;
		_caretIndex = _text.Length;

		_setter = setter;
		_validator = validator;

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

	#region Properties

	public override bool WantsKeyboardFocus => true;
	public int Padding { get; set; } = 2;
	public double CaretBlinkMs { get; set; } = 500;

	#endregion

	#region Methods

	public override void OnPointerDown(PointerDownEvent e)
	{
		base.OnPointerDown(e);

		if (HasKeyboardFocus())
		{
			_originalText = _text;
			_caretVisible = true;
			_caretBlinkTime = 0;
			Invalidate();
		}

		e.MarkHandled();
	}

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

	public override void OnKey(KeyEvent e)
	{
		if (e.Action != InputAction.Press)
			return;

		switch (e.Key)
		{
			case Key.Enter:
				CommitOrCancel();
				ReleaseFocus();
				OnCommit?.Invoke(this, EventArgs.Empty);
				break;

			case Key.Escape:
				CancelEdit();
				ReleaseFocus();
				OnCancel?.Invoke(this, EventArgs.Empty);
				break;

			case Key.Left:
				if (_caretIndex > 0) _caretIndex--;
				break;

			case Key.Right:
				if (_caretIndex < _text.Length) _caretIndex++;
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
				var ch = e.ToText();
				if (ch.HasValue)
				{
					_text = (_text ?? string.Empty).Insert(_caretIndex, ch.Value.ToString());
					_caretIndex++;
					OnTextChanged();
				}
				break;
		}

		_caretIndex = Math.Clamp(_caretIndex, 0, _text.Length);
		Invalidate();
		e.MarkHandled();
	}

	protected override void UpdateLayout()
	{
		_label.Position = new Point(Padding, Padding);
		Size = new Size(
			Math.Max(40, _label.Size.Width + Padding * 2),
			_label.Size.Height + Padding * 2);

		base.UpdateLayout();
	}

	protected override void DrawSelf(IRenderingContext rc)
	{
		if (Style == null)
			return;

		var s = Style.Semantic;

		rc.RenderFilledRect(
			new Rectangle(Point.Empty, Size),
			s.Surface);

		rc.RenderRect(
			new Rectangle(Point.Empty, Size),
			HasKeyboardFocus() ? s.Primary : s.Border);

		if (HasKeyboardFocus() && _caretVisible)
		{
			var caretX = Padding + MeasureCaretOffset();
			rc.RenderLine(
				new Point(caretX, Padding),
				new Point(caretX, Padding + _label.Size.Height),
				s.Text);
		}
	}

	public void Refresh(object? value)
	{
		var newText = value?.ToString() ?? string.Empty;
		if (_text == newText)
			return;

		_text = newText;
		_originalText = newText;

		_label.Text = _text;
		_caretIndex = _text.Length;

		_caretVisible = true;
		_caretBlinkTime = 0;

		InvalidateLayout();
		Invalidate();
	}

	private void OnTextChanged()
	{
		_label.Text = _text;
		_caretVisible = true;
		_caretBlinkTime = 0;
		InvalidateLayout();
	}

	private void CommitOrCancel()
	{
		if (_validator == null || _validator(_text))
		{
			_setter?.Invoke(_text);
		}
		else
		{
			CancelEdit();
		}
	}

	private void CancelEdit()
	{
		_text = _originalText;
		_label.Text = _text;
		_caretIndex = _text.Length;
		InvalidateLayout();
		Invalidate();
	}

	private int MeasureCaretOffset()
	{
		return _caretIndex * _label.TileSize.Width;
	}

	#endregion
}