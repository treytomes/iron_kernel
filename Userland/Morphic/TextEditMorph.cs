using System.Drawing;
using IronKernel.Common.ValueObjects;
using Microsoft.Extensions.Logging.Abstractions;
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

	private readonly TextEditingCore _editor;
	private string _originalText;

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

		_editor = new TextEditingCore(NullLogger.Instance, initialText);
		_originalText = initialText ?? string.Empty;

		_setter = setter;
		_validator = validator;

		IsSelectable = true;

		_label = new LabelMorph(Point.Empty)
		{
			IsSelectable = false,
			Text = _editor.ToString(),
			BackgroundColor = null
		};

		AddMorph(_label);
		InvalidateLayout();
	}

	#endregion

	#region Properties

	public override bool WantsKeyboardFocus => true;
	public int Padding { get; set; } = 2;
	public double CaretBlinkInterval { get; set; } = 0.5;

	#endregion

	#region Input

	public override void OnPointerDown(PointerDownEvent e)
	{
		base.OnPointerDown(e);

		if (HasKeyboardFocus())
		{
			_originalText = _editor.ToString();
			_caretVisible = true;
			_caretBlinkTime = 0;
			Invalidate();
		}

		e.MarkHandled();
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
				_editor.Move(-1);
				break;

			case Key.Right:
				_editor.Move(1);
				break;

			case Key.Home:
				_editor.MoveToStart();
				break;

			case Key.End:
				_editor.MoveToEnd();
				break;

			case Key.Backspace:
				_editor.Backspace();
				OnTextChanged();
				break;

			case Key.Delete:
				_editor.Delete();
				OnTextChanged();
				break;

			default:
				var ch = e.ToText();
				if (ch.HasValue)
				{
					_editor.Insert(ch.Value);
					OnTextChanged();
				}
				break;
		}

		Invalidate();
		e.MarkHandled();
	}

	#endregion

	#region Update

	public override void Update(double deltaTime)
	{
		base.Update(deltaTime);

		if (!HasKeyboardFocus())
		{
			_caretVisible = false;
			_caretBlinkTime = 0;
			return;
		}

		_caretBlinkTime += deltaTime;
		if (_caretBlinkTime >= CaretBlinkInterval)
		{
			_caretBlinkTime = 0;
			_caretVisible = !_caretVisible;
			Invalidate();
		}
	}

	#endregion

	#region Layout and rendering

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
			var caretX = Padding + _editor.CursorIndex * _label.TileSize.Width;
			rc.RenderLine(
				new Point(caretX, Padding),
				new Point(caretX, Padding + _label.Size.Height),
				s.Text);
		}
	}

	#endregion

	#region IValueContentMorph

	public void Refresh(object? value)
	{
		var newText = value?.ToString() ?? string.Empty;
		if (_editor.ToString() == newText)
			return;

		_editor.SetText(newText);
		_originalText = newText;

		_label.Text = newText;
		_caretVisible = true;
		_caretBlinkTime = 0;

		InvalidateLayout();
		Invalidate();
	}

	#endregion

	#region Helpers

	private void OnTextChanged()
	{
		_label.Text = _editor.ToString();
		_caretVisible = true;
		_caretBlinkTime = 0;
		InvalidateLayout();
	}

	private void CommitOrCancel()
	{
		var text = _editor.ToString();
		if (_validator == null || _validator(text))
			_setter?.Invoke(text);
		else
			CancelEdit();
	}

	private void CancelEdit()
	{
		_editor.SetText(_originalText);
		_label.Text = _originalText;
		InvalidateLayout();
		Invalidate();
	}

	#endregion
}
