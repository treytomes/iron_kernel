using System.Drawing;
using IronKernel.Common.ValueObjects;
using Microsoft.Extensions.Logging.Abstractions;
using Userland.Gfx;
using Userland.Morphic.Events;
using Userland.Morphic.Inspector;
using Userland.Services;

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

	private readonly LineEditingBehavior _line;

	private readonly LabelMorph _label;

	private double _caretBlinkTime;
	private bool _caretVisible = true;

	#endregion

	#region Constructors

	public TextEditMorph(string initialText, Action<string>? setter)
		: this(Point.Empty, initialText, setter, null, null)
	{
	}

	public TextEditMorph(
		Point position,
		string initialText,
		Action<string>? setter,
		Func<string, bool>? validator = null,
		IClipboardService? clipboard = null)
	{
		Position = position;

		_editor = new TextEditingCore(NullLogger.Instance, initialText);
		_originalText = initialText ?? string.Empty;

		_setter = setter;
		_validator = validator;
		_line = new LineEditingBehavior(clipboard);

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

		bool shift = e.Modifiers.HasFlag(KeyModifier.Shift);
		bool ctrl  = e.Modifiers.HasFlag(KeyModifier.Control);

		// Ctrl shortcuts
		if (ctrl)
		{
			switch (e.Key)
			{
				case Key.A:
					_line.SelectAll(_editor);
					Invalidate();
					e.MarkHandled();
					return;

				case Key.C:
					_line.CopySelection(_editor);
					e.MarkHandled();
					return;

				case Key.X:
					_line.CutSelection(_editor);
					OnTextChanged();
					e.MarkHandled();
					return;

				case Key.V:
					_line.PasteClipboard(_editor, () => { OnTextChanged(); Invalidate(); });
					e.MarkHandled();
					return;
			}
		}

		// Delete selection on printable input or destructive keys
		if (_line.HasSelection &&
			(e.Key == Key.Backspace || e.Key == Key.Delete || e.ToText().HasValue))
		{
			_line.DeleteSelection(_editor);
			OnTextChanged();
		}

		bool moved = false;

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
				if (shift) _line.BeginIfNeeded(_editor.CursorIndex);
				if (ctrl) _editor.MoveWordLeft();
				else _editor.Move(-1);
				moved = true;
				break;

			case Key.Right:
				if (shift) _line.BeginIfNeeded(_editor.CursorIndex);
				if (ctrl) _editor.MoveWordRight();
				else _editor.Move(1);
				moved = true;
				break;

			case Key.Home:
				if (shift) _line.BeginIfNeeded(_editor.CursorIndex);
				_editor.MoveToStart();
				moved = true;
				break;

			case Key.End:
				if (shift) _line.BeginIfNeeded(_editor.CursorIndex);
				_editor.MoveToEnd();
				moved = true;
				break;

			case Key.Backspace:
				if (ctrl) _editor.DeleteWordLeft();
				else _editor.Backspace();
				OnTextChanged();
				break;

			case Key.Delete:
				if (ctrl) _editor.DeleteWordRight();
				else _editor.Delete();
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

		if (moved)
		{
			if (shift)
				_line.Update(_editor.CursorIndex);
			else
				_line.Clear();
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
		int tileW = _label.TileSize.Width;
		int tileH = _label.TileSize.Height;

		rc.RenderFilledRect(new Rectangle(Point.Empty, Size), s.Surface);
		rc.RenderRect(new Rectangle(Point.Empty, Size), HasKeyboardFocus() ? s.Primary : s.Border);

		// Selection highlight
		if (_line.HasSelection && HasKeyboardFocus())
		{
			var (selStart, selEnd) = _line.GetSelectionRange()!.Value;
			rc.RenderFilledRect(
				new Rectangle(
					Padding + selStart * tileW,
					Padding,
					(selEnd - selStart) * tileW,
					tileH),
				s.Primary);
		}

		// Caret
		if (HasKeyboardFocus() && _caretVisible)
		{
			var caretX = Padding + _editor.CursorIndex * tileW;
			rc.RenderLine(
				new Point(caretX, Padding),
				new Point(caretX, Padding + tileH),
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
		_line.Clear();

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
		_line.Normalize(_editor);
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
		_line.Clear();
		_label.Text = _originalText;
		InvalidateLayout();
		Invalidate();
	}

	#endregion
}
