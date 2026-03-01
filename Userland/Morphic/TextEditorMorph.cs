using System.Drawing;
using System.Text;
using IronKernel.Common.ValueObjects;
using Userland.Gfx;
using Userland.Morphic.Events;
using Userland.Services;

namespace Userland.Morphic;

public sealed class TextEditorMorph : Morph
{
	#region Fields

	private readonly TextDocument _document;
	private readonly IClipboardService _clipboard;

	private Font? _font;
	private Size _cellSize;

	private int _firstVisibleLine;
	private int _visibleLineCount;
	private bool _layoutInitialized;

	private readonly SelectionController<(int line, int column)> _selection =
		new((a, b) =>
		{
			if (a.line != b.line)
				return a.line.CompareTo(b.line);
			return a.column.CompareTo(b.column);
		});

	private bool _mouseSelecting;

	#endregion

	#region Constructor

	public TextEditorMorph(TextDocument document, IClipboardService clipboard)
	{
		_document = document;
		_clipboard = clipboard;

		IsSelectable = true;

		_document.Changed += OnDocumentChanged;
	}

	#endregion

	#region Properties

	public bool ShowLineNumbers { get; set; } = true;
	public int TabWidth { get; set; } = 4;
	public override bool WantsKeyboardFocus => true;

	#endregion

	#region Loading / Layout

	protected override async void OnLoad(IAssetService assets)
	{
		if (Style == null)
			throw new InvalidOperationException("Style is null.");

		_font = await assets.LoadFontAsync(
			Style.DefaultFontStyle.Url,
			Style.DefaultFontStyle.TileSize,
			Style.DefaultFontStyle.GlyphOffset
		);

		_cellSize = _font.TileSize;
		InvalidateLayout();
	}

	protected override void UpdateLayout()
	{
		if (_font == null || Owner == null)
			return;

		Size = Owner.Size;
		_visibleLineCount = Math.Max(
			1,
			Size.Height / _cellSize.Height
		);

		_layoutInitialized = true;
	}

	#endregion

	#region Document integration

	private void OnDocumentChanged()
	{
		_selection.Normalize(pos =>
			pos.line >= 0 &&
			pos.line < _document.LineCount &&
			pos.column >= 0 &&
			pos.column <= _document.Lines[pos.line].Length);

		EnsureCaretVisible();
		Invalidate();
	}

	#endregion

	#region Keyboard Input

	public override void OnKey(KeyEvent e)
	{
		if (e.Action != InputAction.Press)
			return;

		bool shift = e.Modifiers.HasFlag(KeyModifier.Shift);
		bool ctrl = e.Modifiers.HasFlag(KeyModifier.Control);

		switch (e.Key)
		{
			case Key.A when ctrl:
				_selection.SelectAll(_document);
				e.MarkHandled();
				return;

			case Key.C when ctrl:
				CopySelection();
				e.MarkHandled();
				return;

			case Key.X when ctrl:
				CutSelection();
				e.MarkHandled();
				return;

			case Key.V when ctrl:
				PasteClipboard();
				e.MarkHandled();
				return;
		}

		if (_selection.HasSelection &&
			(e.Key == Key.Backspace ||
			 e.Key == Key.Delete ||
			 e.ToText().HasValue))
		{
			DeleteSelection();
		}

		_selection.BeginIfShift(
			shift,
			_document.CaretLine,
			_document.CaretColumn
		);

		bool moved = false;

		switch (e.Key)
		{
			case Key.Left:
				if (ctrl)
					_document.MoveWordLeft();
				else
					_document.MoveLeft();
				moved = true;
				break;

			case Key.Right:
				if (ctrl)
					_document.MoveWordRight();
				else
					_document.MoveRight();
				moved = true;
				break;

			case Key.Up:
				_document.MoveUp();
				moved = true;
				break;

			case Key.Down:
				_document.MoveDown();
				moved = true;
				break;

			case Key.Home:
				_document.MoveToLineStart();
				moved = true;
				break;

			case Key.End:
				_document.MoveToLineEnd();
				moved = true;
				break;

			case Key.Backspace:
				if (ctrl)
					_document.DeleteWordLeft();
				else
					_document.Backspace();
				break;

			case Key.Delete:
				if (ctrl)
					_document.DeleteWordRight();
				else
					_document.Delete();
				break;

			case Key.Enter:
				_document.InsertChar('\n');
				break;

			case Key.Tab:
				_document.InsertTab();
				break;

			default:
				var ch = e.ToText();
				if (ch.HasValue)
					_document.InsertChar(ch.Value);
				break;
		}

		if (shift && moved)
			_selection.Update((_document.CaretLine, _document.CaretColumn));
		else if (!shift)
			_selection.Clear();

		e.MarkHandled();
	}

	#endregion

	#region Mouse Input

	public override void OnPointerDown(PointerDownEvent e)
	{
		if (e.Button != MouseButton.Left)
			return;

		if (TryGetWorld(out var world))
			world.CapturePointer(this);

		_mouseSelecting = true;
		SetCaretFromPointer(e.Position);

		_selection.Begin((_document.CaretLine, _document.CaretColumn));

		e.MarkHandled();
	}

	public override void OnPointerMove(PointerMoveEvent e)
	{
		if (!_mouseSelecting)
			return;

		SetCaretFromPointer(e.Position);

		_selection.Update((
			_document.CaretLine,
			_document.CaretColumn));

		Invalidate();
	}

	private void SetCaretFromPointer(Point worldPosition)
	{
		if (_font == null)
			return;

		var local = WorldToLocal(worldPosition);

		if (local.Y < 0)
			return;

		// ----- Determine target line -----
		int visualRow = local.Y / _cellSize.Height;
		int lineIndex = _firstVisibleLine + visualRow;

		lineIndex = Math.Clamp(
			lineIndex,
			0,
			_document.LineCount - 1);

		// ----- Determine target visual column -----
		int localX = local.X - TextOriginX;
		if (localX < 0)
			localX = 0;

		int visualCol = localX / _cellSize.Width;

		// ----- Convert visual column to caret column -----
		string lineText = _document.Lines[lineIndex].ToString();
		int caretColumn =
			VisualColumnToCaretIndex(lineText, visualCol);

		// ----- Apply caret -----
		_document.SetCaretLine(lineIndex);
		_document.CurrentLine.SetCursorIndex(caretColumn);

		EnsureCaretVisible();
	}

	private int VisualColumnToCaretIndex(string line, int targetCol)
	{
		int col = 0;

		for (int i = 0; i < line.Length; i++)
		{
			int nextCol;

			if (line[i] == '\t')
				nextCol = col + (TabWidth - (col % TabWidth));
			else
				nextCol = col + 1;

			if (targetCol < nextCol)
				return i;

			col = nextCol;
		}

		return line.Length;
	}

	public override void OnPointerUp(PointerUpEvent e)
	{
		if (e.Button != MouseButton.Left)
			return;

		if (TryGetWorld(out var world))
			world.ReleasePointer(this);

		_mouseSelecting = false;
		e.MarkHandled();
	}

	#endregion

	#region Clipboard

	private void CopySelection()
	{
		if (!_selection.HasSelection)
			return;

		var (start, end) = _selection.GetRange();

		var sb = new StringBuilder();
		for (int l = start.line; l <= end.line; l++)
		{
			string text = _document.Lines[l].ToString();
			int s = (l == start.line) ? start.column : 0;
			int e = (l == end.line) ? end.column : text.Length;

			sb.Append(text.Substring(s, e - s));
			if (l < end.line)
				sb.Append('\n');
		}

		_clipboard.SetText(sb.ToString());
	}

	private void CutSelection()
	{
		CopySelection();
		DeleteSelection();
	}

	private async void PasteClipboard()
	{
		var text = await _clipboard.GetTextAsync();
		if (string.IsNullOrEmpty(text))
			return;

		if (_selection.HasSelection)
			DeleteSelection();

		foreach (char ch in text)
			_document.InsertChar(ch);
	}

	#endregion

	#region Selection deletion

	private void DeleteSelection()
	{
		if (!_selection.HasSelection)
			return;

		var (start, end) = _selection.GetRange();
		_document.DeleteRangeAndSetCaret(start, end);
		_selection.Clear();
	}

	#endregion

	#region Scrolling

	private void EnsureCaretVisible()
	{
		if (_document.CaretLine < _firstVisibleLine)
			_firstVisibleLine = _document.CaretLine;
		else if (_document.CaretLine >=
				 _firstVisibleLine + _visibleLineCount)
			_firstVisibleLine =
				_document.CaretLine - _visibleLineCount + 1;

		_firstVisibleLine = Math.Max(0, _firstVisibleLine);
	}

	#endregion

	#region Rendering

	protected override void DrawSelf(IRenderingContext rc)
	{
		if (!_layoutInitialized || _font == null || Style == null)
			return;

		rc.RenderFilledRect(
			new Rectangle(Point.Empty, Size),
			Style.Semantic.Background);

		DrawTextAndSelection(rc);
		DrawCaret(rc);
	}

	private void DrawTextAndSelection(IRenderingContext rc)
	{
		int visualCols =
			Math.Max(1,
				(Size.Width - TextOriginX) /
				_cellSize.Width);

		bool hasSelection = _selection.HasSelection;
		var (selStart, selEnd) = hasSelection
			? _selection.GetRange()
			: default;

		int yIndex = 0;

		for (int line = _firstVisibleLine;
			 line < _document.LineCount &&
			 yIndex < _visibleLineCount;
			 line++, yIndex++)
		{
			int baseY = yIndex * _cellSize.Height;
			string text = _document.Lines[line].ToString();

			DrawLineNumber(rc, line, baseY);

			int visualCol = 0;

			for (int i = 0; i < text.Length; i++)
			{
				char ch = text[i];
				int width = ch == '\t'
					? TabWidth - (visualCol % TabWidth)
					: 1;

				bool selected = hasSelection &&
					Compare(
						(line, i),
						selStart) >= 0 &&
					Compare(
						(line, i),
						selEnd) < 0;

				var fg = selected
					? Style!.Semantic.Background
					: Style!.Semantic.Text;
				var bg = selected
					? Style!.Semantic.Text
					: Style!.Semantic.Background;

				if (ch != '\t')
				{
					int x = TextOriginX +
							(visualCol % visualCols) *
							_cellSize.Width;
					int y = baseY +
							(visualCol / visualCols) *
							_cellSize.Height;

					_font!.WriteChar(
						rc,
						ch,
						new Point(x, y),
						fg,
						bg);
				}

				visualCol += width;
			}
		}
	}

	private void DrawLineNumber(
		IRenderingContext rc,
		int line,
		int y)
	{
		if (!ShowLineNumbers)
			return;

		string ln = (line + 1).ToString();
		int x = LineNumberGutterWidth -
				ln.Length * _cellSize.Width;

		if (line == _document.CaretLine)
		{
			rc.RenderFilledRect(
				new Rectangle(
					0,
					y,
					LineNumberGutterWidth,
					_cellSize.Height),
				Style!.Semantic.Primary.Lerp(
					Style.Semantic.Background,
					0.15f));
		}

		_font!.WriteString(
			rc,
			ln,
			new Point(x, y),
			line == _document.CaretLine
				? Style!.Semantic.Primary
				: Style!.Semantic.SecondaryText,
			Style.Semantic.Background);
	}

	private void DrawCaret(IRenderingContext rc)
	{
		if (!HasKeyboardFocus())
			return;

		int line = _document.CaretLine;
		if (line < _firstVisibleLine ||
			line >= _firstVisibleLine + _visibleLineCount)
			return;

		string text = _document.Lines[line].ToString();
		int visualCol =
			ComputeVisualColumn(
				text,
				_document.CaretColumn);

		int x = TextOriginX +
				visualCol * _cellSize.Width;
		int y =
			(line - _firstVisibleLine) *
			_cellSize.Height;

		rc.RenderFilledRect(
			new Rectangle(
				x,
				y + _cellSize.Height - 2,
				_cellSize.Width,
				2),
			Style!.Semantic.Text);
	}

	#endregion

	#region Helpers

	private static int Compare(
		(int line, int col) a,
		(int line, int col) b)
	{
		if (a.line != b.line)
			return a.line.CompareTo(b.line);
		return a.col.CompareTo(b.col);
	}

	private int ComputeVisualColumn(
		string line,
		int caretIndex)
	{
		int col = 0;
		for (int i = 0;
			 i < caretIndex && i < line.Length;
			 i++)
		{
			col += line[i] == '\t'
				? TabWidth - (col % TabWidth)
				: 1;
		}
		return col;
	}

	private int LineNumberDigits =>
		Math.Max(
			1,
			_document.LineCount.ToString().Length);

	private int LineNumberGutterWidth =>
		ShowLineNumbers
			? (LineNumberDigits + 1) *
			  _cellSize.Width
			: 0;

	private int TextOriginX =>
		LineNumberGutterWidth;

	#endregion
}