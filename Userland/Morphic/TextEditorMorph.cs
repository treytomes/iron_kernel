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
	private int _visibleRowCount;
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

	#region Events

	public event Action? SaveRequested;

	#endregion

	#region Constructors

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
	public ISyntaxHighlighter? SyntaxHighlighter { get; set; }
	public override bool WantsKeyboardFocus => true;

	#endregion

	#region Methods

	protected override async void OnLoad(IAssetService assets)
	{
		if (Style == null)
			throw new InvalidOperationException("Style is null.");

		_font = await assets.LoadFontAsync(
			Style.DefaultFontStyle.Url,
			Style.DefaultFontStyle.TileSize,
			Style.DefaultFontStyle.GlyphOffset);

		_cellSize = _font.TileSize;
		InvalidateLayout();
	}

	protected override void UpdateLayout()
	{
		if (_font == null || Owner == null)
			return;

		_visibleRowCount = Math.Max(1, Size.Height / _cellSize.Height);
		_layoutInitialized = true;
	}

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

	public override void OnKey(KeyEvent e)
	{
		if (e.Action != InputAction.Press)
			return;

		bool shift = e.Modifiers.HasFlag(KeyModifier.Shift);
		bool ctrl = e.Modifiers.HasFlag(KeyModifier.Control);

		switch (e.Key)
		{
			case Key.A when ctrl:
				SelectAll();
				e.MarkHandled();
				return;

			case Key.C when ctrl:
				CopySelection();
				e.MarkHandled();
				return;

			case Key.X when ctrl:
				if (_selection.HasSelection)
					CutSelection();
				else
					CutCurrentLine();
				e.MarkHandled();
				return;

			case Key.V when ctrl:
				PasteClipboard();
				e.MarkHandled();
				return;

			case Key.S when ctrl:
				SaveRequested?.Invoke();
				e.MarkHandled();
				return;
		}

		if (_selection.HasSelection &&
			(e.Key == Key.Backspace || e.Key == Key.Delete || e.ToText().HasValue))
		{
			DeleteSelection();
		}

		if (shift)
			_selection.BeginIfNeeded((_document.CaretLine, _document.CaretColumn));

		bool moved = false;

		switch (e.Key)
		{
			case Key.Left:
				if (ctrl)
				{
					_document.MoveWordLeft();
				}
				else
				{
					_document.MoveLeft();
				}
				moved = true;
				break;

			case Key.Right:
				if (ctrl)
				{
					_document.MoveWordRight();
				}
				else
				{
					_document.MoveRight();
				}
				moved = true;
				break;

			case Key.Up:
				if (!shift && _selection.HasSelection)
				{
					var (start, _) = _selection.GetRange();
					_document.SetCaretLine(start.line);
					_document.Lines[start.line].SetCursorIndex(start.column);
					_selection.Clear();
					EnsureCaretVisible();
					Invalidate();
					e.MarkHandled();
					return;
				}
				_document.MoveUp();
				moved = true;
				break;

			case Key.Down:
				if (!shift && _selection.HasSelection)
				{
					var (_, end) = _selection.GetRange();
					_document.SetCaretLine(end.line);
					_document.Lines[end.line].SetCursorIndex(end.column);
					_selection.Clear();
					EnsureCaretVisible();
					Invalidate();
					e.MarkHandled();
					return;
				}
				_document.MoveDown();
				moved = true;
				break;

			case Key.PageUp:
				_document.SetCaretLine(Math.Max(0, _document.CaretLine - _visibleRowCount));
				_firstVisibleLine = Math.Max(0, _firstVisibleLine - _visibleRowCount);
				moved = true;
				break;

			case Key.PageDown:
				_document.SetCaretLine(Math.Min(_document.LineCount - 1, _document.CaretLine + _visibleRowCount));
				_firstVisibleLine = Math.Min(_document.LineCount - 1, _firstVisibleLine + _visibleRowCount);
				moved = true;
				break;

			case Key.Tab when shift:
				UnindentCurrentLine();
				e.MarkHandled();
				return;

			case Key.Home:
				if (ctrl)
					_document.MoveToStart();
				else
					_document.MoveToLineStart();
				moved = true;
				break;

			case Key.End:
				if (ctrl)
					_document.MoveToEnd();
				else
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

	public override void OnPointerWheel(PointerWheelEvent e)
	{
		if (e.Delta.Y == 0)
			return;

		const int ScrollStep = 3;
		_firstVisibleLine = Math.Clamp(
			_firstVisibleLine - e.Delta.Y * ScrollStep,
			0,
			Math.Max(0, _document.LineCount - 1));

		Invalidate();
		e.MarkHandled();
	}

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
		_selection.Update((_document.CaretLine, _document.CaretColumn));
		Invalidate();
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
		int visualCols = Math.Max(
			1,
			(Size.Width - TextOriginX) / _cellSize.Width);

		bool hasSelection = _selection.HasSelection;
		(int line, int column) selStart = default;
		(int line, int column) selEnd = default;

		if (hasSelection)
		{
			var range = _selection.GetRange();
			selStart = range.start;
			selEnd = range.end;
		}

		int row = 0;

		for (int line = _firstVisibleLine;
			 line < _document.LineCount && row < _visibleRowCount;
			 line++)
		{
			string text = _document.Lines[line].ToString();
			int baseY = row * _cellSize.Height;

			DrawLineNumber(rc, line, baseY);

			int visualCol = 0;

			for (int i = 0; i < text.Length; i++)
			{
				char ch = text[i];

				int width;
				if (ch == '\t')
					width = TabWidth - (visualCol % TabWidth);
				else
					width = 1;

				int drawX =
					TextOriginX +
					(visualCol % visualCols) * _cellSize.Width;

				int drawY =
					baseY +
					(visualCol / visualCols) * _cellSize.Height;

				bool selected = false;
				if (hasSelection)
				{
					if (Compare((line, i), selStart) >= 0 &&
						Compare((line, i), selEnd) < 0)
					{
						selected = true;
					}
				}

				RadialColor fg;
				if (selected)
					fg = Style!.Semantic.Background;
				else if (SyntaxHighlighter != null)
					fg = SyntaxHighlighter.GetForeground(_document, line, i)
						?? Style!.Semantic.Text;
				else
					fg = Style!.Semantic.Text;

				RadialColor bg;
				if (selected)
					bg = Style!.Semantic.Text;
				else
					bg = Style!.Semantic.Background;

				if (ch != '\t')
				{
					_font!.WriteChar(
						rc,
						ch,
						new Point(drawX, drawY),
						fg,
						bg);
				}

				visualCol += width;
			}

			row += GetVisualRowCount(text, visualCols);
		}
	}

	private void DrawLineNumber(IRenderingContext rc, int line, int baseY)
	{
		if (!ShowLineNumbers || _font == null || Style == null)
			return;

		string ln = (line + 1).ToString();
		int x = LineNumberGutterWidth - ln.Length * _cellSize.Width;

		if (line == _document.CaretLine)
		{
			rc.RenderFilledRect(
				new Rectangle(
					0,
					baseY,
					LineNumberGutterWidth,
					_cellSize.Height),
				Style.Semantic.Primary.Lerp(
					Style.Semantic.Background,
					0.15f));
		}

		_font.WriteString(
			rc,
			ln,
			new Point(x, baseY),
			line == _document.CaretLine
				? Style.Semantic.Primary
				: Style.Semantic.SecondaryText,
			Style.Semantic.Background);
	}

	private void DrawCaret(IRenderingContext rc)
	{
		if (!HasKeyboardFocus())
			return;

		int visualCols = Math.Max(
			1,
			(Size.Width - TextOriginX) / _cellSize.Width);

		int caretLine = _document.CaretLine;
		if (caretLine < _firstVisibleLine)
			return;

		int rowOffset = 0;
		for (int l = _firstVisibleLine; l < caretLine; l++)
		{
			rowOffset += GetVisualRowCount(
				_document.Lines[l].ToString(),
				visualCols);
		}

		string text = _document.Lines[caretLine].ToString();
		int visualCol = ComputeVisualColumn(text, _document.CaretColumn);

		int caretRow = rowOffset + (visualCol / visualCols);
		if (caretRow >= _visibleRowCount)
			return;

		int x =
			TextOriginX +
			(visualCol % visualCols) * _cellSize.Width;

		int y = caretRow * _cellSize.Height;

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

	private void SelectAll()
	{
		if (_document.LineCount == 0)
			return;

		int lastLine = _document.LineCount - 1;
		int lastCol = _document.Lines[lastLine].Length;

		_selection.Begin((0, 0));
		_selection.Update((lastLine, lastCol));
	}

	private void DeleteSelection()
	{
		if (!_selection.HasSelection)
			return;

		var (start, end) = _selection.GetRange();
		_document.DeleteRangeAndSetCaret(start, end);
		_selection.Clear();
	}

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

	private void CutCurrentLine()
	{
		int line = _document.CaretLine;
		string text = _document.Lines[line].ToString();
		_clipboard.SetText(text + "\n");

		if (_document.LineCount == 1)
		{
			_document.DeleteRangeAndSetCaret((0, 0), (0, text.Length));
		}
		else if (line < _document.LineCount - 1)
		{
			_document.DeleteRangeAndSetCaret((line, 0), (line + 1, 0));
		}
		else
		{
			// Last line — remove newline before it and the line itself
			int prevLen = _document.Lines[line - 1].Length;
			_document.DeleteRangeAndSetCaret((line - 1, prevLen), (line, text.Length));
		}
	}

	private void UnindentCurrentLine()
	{
		int line = _document.CaretLine;
		string text = _document.Lines[line].ToString();

		if (text.Length == 0)
			return;

		int removed = 0;
		if (text[0] == '\t')
		{
			removed = 1;
		}
		else
		{
			int spaces = Math.Min(_document.TabWidth, text.Length);
			for (int i = 0; i < spaces; i++)
			{
				if (text[i] != ' ')
					break;
				removed++;
			}
		}

		if (removed == 0)
			return;

		int col = _document.CaretColumn;
		_document.DeleteRangeAndSetCaret((line, 0), (line, removed));
		// Restore caret column past the removed indent, clamped to line length
		_document.Lines[line].SetCursorIndex(Math.Max(0, col - removed));
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

	private void EnsureCaretVisible()
	{
		if (!_layoutInitialized)
			return;

		int caretLine = _document.CaretLine;

		// Scroll up if caret is above the visible window
		if (caretLine < _firstVisibleLine)
		{
			_firstVisibleLine = caretLine;
			return;
		}

		// Scroll down if caret's visual row is below the visible window
		int visualCols = Math.Max(1, (Size.Width - TextOriginX) / _cellSize.Width);

		// Advance _firstVisibleLine until the caret row fits
		while (_firstVisibleLine < caretLine)
		{
			int rowOffset = 0;
			for (int l = _firstVisibleLine; l < caretLine; l++)
				rowOffset += GetVisualRowCount(_document.Lines[l].ToString(), visualCols);

			string caretText = _document.Lines[caretLine].ToString();
			int visualCol = ComputeVisualColumn(caretText, _document.CaretColumn);
			int caretRow = rowOffset + (visualCol / visualCols);

			if (caretRow < _visibleRowCount)
				break;

			_firstVisibleLine++;
		}
	}

	private static int Compare(
		(int line, int col) a,
		(int line, int col) b)
	{
		if (a.line != b.line)
			return a.line.CompareTo(b.line);
		return a.col.CompareTo(b.col);
	}

	private int ComputeVisualColumn(string line, int caretIndex)
	{
		int col = 0;
		for (int i = 0; i < caretIndex && i < line.Length; i++)
		{
			if (line[i] == '\t')
				col += TabWidth - (col % TabWidth);
			else
				col++;
		}
		return col;
	}

	private int GetVisualRowCount(string text, int visualCols)
	{
		int visualCol = 0;
		foreach (char ch in text)
		{
			if (ch == '\t')
				visualCol += TabWidth - (visualCol % TabWidth);
			else
				visualCol++;
		}
		return Math.Max(1, (visualCol + visualCols - 1) / visualCols);
	}

	private int LineNumberDigits =>
		Math.Max(1, _document.LineCount.ToString().Length);

	private int LineNumberGutterWidth =>
		ShowLineNumbers
			? (LineNumberDigits + 1) * _cellSize.Width
			: 0;

	private int TextOriginX => LineNumberGutterWidth;

	private void SetCaretFromPointer(Point worldPosition)
	{
		if (_font == null)
			return;

		var local = WorldToLocal(worldPosition);
		int visualRow = local.Y / _cellSize.Height;

		int row = 0;
		for (int line = _firstVisibleLine; line < _document.LineCount; line++)
		{
			int rows = GetVisualRowCount(
				_document.Lines[line].ToString(),
				Math.Max(1, (Size.Width - TextOriginX) / _cellSize.Width));

			if (row + rows > visualRow)
			{
				int localRow = visualRow - row;
				int visualCol = Math.Max(0, (local.X - TextOriginX) / _cellSize.Width);
				int absoluteCol = localRow *
					Math.Max(1, (Size.Width - TextOriginX) / _cellSize.Width)
					+ visualCol;

				string text = _document.Lines[line].ToString();
				int caretCol = VisualColumnToCaretIndex(text, absoluteCol);

				_document.SetCaretLine(line);
				_document.CurrentLine.SetCursorIndex(caretCol);
				return;
			}

			row += rows;
		}
	}

	private int VisualColumnToCaretIndex(string line, int targetCol)
	{
		int col = 0;
		for (int i = 0; i < line.Length; i++)
		{
			int nextCol = line[i] == '\t'
				? col + (TabWidth - (col % TabWidth))
				: col + 1;

			if (targetCol < nextCol)
				return i;

			col = nextCol;
		}
		return line.Length;
	}

	#endregion
}