using System.Drawing;
using System.Text;
using IronKernel.Common.ValueObjects;
using IronKernel.Userland.Gfx;
using IronKernel.Userland.Morphic.Events;
using IronKernel.Userland.Services;

namespace IronKernel.Userland.Morphic;

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

	// Selection (document-relative)
	private (int line, int column)? _selectionAnchor;
	private (int line, int column)? _selectionCaret;

	private bool _mouseSelecting;
	#endregion

	#region Configuration

	public bool ShowLineNumbers { get; set; } = true;

	public int TabWidth { get; set; } = 4;

	#endregion

	#region Construction

	public TextEditorMorph(TextDocument document, IClipboardService clipboard)
	{
		_document = document;
		_clipboard = clipboard;
		IsSelectable = true;
	}

	#endregion

	#region Focus

	public override bool WantsKeyboardFocus => true;

	#endregion

	#region Loading

	protected override async void OnLoad(IAssetService assets)
	{
		if (Style == null)
			throw new Exception("Style is null.");

		_font = await assets.LoadFontAsync(
			Style.DefaultFontStyle.Url,
			Style.DefaultFontStyle.TileSize,
			Style.DefaultFontStyle.GlyphOffset
		);

		_cellSize = _font.TileSize;
		InvalidateLayout();
	}

	#endregion

	#region Layout

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

	#region Selection helpers
	private bool HasSelection =>
		_selectionAnchor.HasValue &&
		_selectionCaret.HasValue &&
		_selectionAnchor.Value != _selectionCaret.Value;

	private static int ComparePos(
		(int line, int col) a,
		(int line, int col) b)
	{
		if (a.line != b.line)
			return a.line.CompareTo(b.line);
		return a.col.CompareTo(b.col);
	}

	private ((int l, int c) start, (int l, int c) end)
		GetSelectionRange()
	{
		var a = _selectionAnchor!.Value;
		var b = _selectionCaret!.Value;
		return ComparePos(a, b) <= 0 ? (a, b) : (b, a);
	}

	private void ClearSelection()
	{
		_selectionAnchor = null;
		_selectionCaret = null;
	}
	#endregion

	#region Input (Keyboard)

	public override void OnKey(KeyEvent e)
	{
		if (e.Action != InputAction.Press)
			return;

		if (e.Key is
			Key.LeftControl or Key.RightControl or
			Key.LeftShift or Key.RightShift or
			Key.LeftAlt or Key.RightAlt)
			return;

		bool shift = e.Modifiers.HasFlag(KeyModifier.Shift);
		bool ctrl = e.Modifiers.HasFlag(KeyModifier.Control);

		bool moved = false;

		void BeginSelection()
		{
			if (!shift)
				ClearSelection();
			else if (!_selectionAnchor.HasValue)
				_selectionAnchor = (_document.CaretLine, _document.CaretColumn);
		}

		switch (e.Key)
		{
			case Key.A when ctrl:
				_selectionAnchor = (0, 0);
				_selectionCaret = (
					_document.LineCount - 1,
					_document.Lines[^1].Length);
				Invalidate();
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

		if (HasSelection &&
			(e.Key == Key.Backspace ||
			 e.Key == Key.Delete ||
			 e.ToText().HasValue))
		{
			DeleteSelection();
		}

		BeginSelection();

		switch (e.Key)
		{
			case Key.Tab:
				_document.InsertTab();
				break;

			case Key.Left:
				if (ctrl && shift)
				{
					if (!_selectionAnchor.HasValue)
						_selectionAnchor = (_document.CaretLine, _document.CaretColumn);
					_document.MoveWordLeft();
					moved = true;
				}
				else if (ctrl)
				{
					ClearSelection();
					_document.MoveWordLeft();
					moved = true;
				}
				else
				{
					if (!shift) ClearSelection();
					_document.MoveLeft();
					moved = true;
				}
				break;

			case Key.Right:
				if (ctrl) _document.MoveWordRight();
				else _document.MoveRight();
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
				if (e.Modifiers.HasFlag(KeyModifier.Control))
					_document.DeleteWordLeft();
				else
					_document.Backspace();
				break;

			case Key.Delete:
				if (e.Modifiers.HasFlag(KeyModifier.Control))
					_document.DeleteWordRight();
				else
					_document.Delete();
				break;

			case Key.Enter:
				_document.InsertChar('\n');
				break;

			default:
				var ch = e.ToText();
				if (ch.HasValue)
					_document.InsertChar(ch.Value);
				break;
		}

		if (shift && moved)
		{
			_selectionCaret = (_document.CaretLine, _document.CaretColumn);
		}
		else if (moved && !shift)
		{
			ClearSelection();
		}

		EnsureCaretVisible();
		Invalidate();
		e.MarkHandled();
	}

	#endregion

	#region Input (Mouse)

	public override void OnPointerDown(PointerDownEvent e)
	{
		base.OnPointerDown(e);
		if (e.Button != MouseButton.Left)
			return;

		if (TryGetWorld(out var world))
			world.CapturePointer(this);

		_mouseSelecting = true;
		SetCaretFromPointer(e.Position);
		_selectionAnchor = (_document.CaretLine, _document.CaretColumn);
		_selectionCaret = _selectionAnchor;

		e.MarkHandled();
	}

	public override void OnPointerMove(PointerMoveEvent e)
	{
		if (!_mouseSelecting)
			return;

		SetCaretFromPointer(e.Position);
		_selectionCaret = (_document.CaretLine, _document.CaretColumn);
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

	#region Clipboard
	private void CopySelection()
	{
		if (!HasSelection)
			return;

		var (start, end) = GetSelectionRange();
		var sb = new StringBuilder();

		for (int l = start.l; l <= end.l; l++)
		{
			var line = _document.Lines[l].ToString();
			int s = (l == start.l) ? start.c : 0;
			int e = (l == end.l) ? end.c : line.Length;
			sb.Append(line.Substring(s, e - s));
			if (l < end.l)
				sb.Append('\n');
		}

		_clipboard.SetText(sb.ToString());
	}

	private void CutSelection()
	{
		if (!HasSelection)
			return;

		CopySelection();
		DeleteSelection();
	}

	private void PasteClipboard()
	{
		_clipboard.GetTextAsync().ContinueWith(t =>
		{
			var text = t.Result;
			if (string.IsNullOrEmpty(text))
				return;

			if (HasSelection)
				DeleteSelection();

			foreach (var ch in text)
				_document.InsertChar(ch);

			ClearSelection();
			EnsureCaretVisible();
			Invalidate();
		});
	}
	#endregion

	#region Selection deletion
	private void DeleteSelection()
	{
		if (!HasSelection)
			return;

		var (start, end) = GetSelectionRange();
		_document.DeleteRange(start, end);
		_document.SetCaretLine(start.l);
		_document.CaretColumn = start.c;
		ClearSelection();
	}
	#endregion

	#region Scrolling
	private void EnsureCaretVisible()
	{
		if (_document.CaretLine < _firstVisibleLine)
			_firstVisibleLine = _document.CaretLine;
		else if (_document.CaretLine >= _firstVisibleLine + _visibleLineCount)
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

	private void SetCaretFromPointer(Point worldPosition)
	{
		var local = WorldToLocal(worldPosition);

		if (local.Y < 0)
			return;

		int visualRow = local.Y / _cellSize.Height;
		int lineIndex = _firstVisibleLine + visualRow;

		lineIndex = Math.Clamp(
			lineIndex,
			0,
			_document.LineCount - 1);

		int localX = local.X - TextOriginX;
		if (localX < 0)
			localX = 0;

		int targetVisualCol = localX / _cellSize.Width;

		var lineText = _document.Lines[lineIndex].ToString();

		// Account for wrapped rows
		int visualColsPerRow = Math.Max(
			1,
			(Size.Width - TextOriginX) / _cellSize.Width);

		int wrappedRow = visualRow -
			((lineIndex - _firstVisibleLine) * 1);

		int absoluteVisualCol =
			wrappedRow * visualColsPerRow + targetVisualCol;

		int caretColumn =
			VisualColumnToCaretIndex(lineText, absoluteVisualCol);

		_document.SetCaretLine(lineIndex);
		_document.CaretColumn = caretColumn;

		EnsureCaretVisible();
		Invalidate();
	}

	private void DrawTextAndSelection(IRenderingContext rc)
	{
		int yIndex = 0;

		bool hasSelection = HasSelection;
		((int l, int c) selStart, (int l, int c) selEnd) = ((0, 0), (0, 0));
		if (hasSelection)
			(selStart, selEnd) = GetSelectionRange();

		int visualColsPerRow = Math.Max(
			1,
			(Size.Width - TextOriginX) / _cellSize.Width);

		for (int line = _firstVisibleLine;
			 line < _document.LineCount &&
			 yIndex < _visibleLineCount;
			 line++, yIndex++)
		{
			int baseY = yIndex * _cellSize.Height;

			// ----- Line numbers -----
			if (ShowLineNumbers)
			{
				var ln = (line + 1).ToString();
				int lnX = LineNumberGutterWidth - ln.Length * _cellSize.Width;

				if (IsActiveLine(line))
				{
					rc.RenderFilledRect(
						new Rectangle(
							0,
							baseY,
							LineNumberGutterWidth,
							_cellSize.Height),
						Style!.Semantic.Primary.Lerp(
							Style!.Semantic.Background,
							0.15f)
					);
				}

				_font!.WriteString(
					rc,
					ln,
					new Point(lnX, baseY),
					IsActiveLine(line)
						? Style!.Semantic.Primary
						: Style!.Semantic.SecondaryText,
					Style.Semantic.Background
				);
			}

			string text = _document.Lines[line].ToString();

			// ----- Selection background (per wrapped row) -----
			if (hasSelection &&
				line >= selStart.l &&
				line <= selEnd.l)
			{
				int logicalStart =
					line == selStart.l ? selStart.c : 0;
				int logicalEnd =
					line == selEnd.l ? selEnd.c : text.Length;

				int visualStart =
					ComputeVisualColumn(text, logicalStart);
				int visualEnd =
					ComputeVisualColumn(text, logicalEnd);

				if (visualEnd > visualStart)
				{
					int firstRow = visualStart / visualColsPerRow;
					int lastRow = (visualEnd - 1) / visualColsPerRow;

					for (int vr = firstRow; vr <= lastRow; vr++)
					{
						int rowY =
							baseY + vr * _cellSize.Height;

						int rowStartCol =
							vr == firstRow
								? visualStart % visualColsPerRow
								: 0;

						int rowEndCol =
							vr == lastRow
								? (visualEnd % visualColsPerRow)
								: visualColsPerRow;

						if (rowEndCol == 0)
							rowEndCol = visualColsPerRow;

						rc.RenderFilledRect(
							new Rectangle(
								TextOriginX +
								rowStartCol * _cellSize.Width,
								rowY,
								(rowEndCol - rowStartCol) *
								_cellSize.Width,
								_cellSize.Height),
							Style!.Semantic.Text
						);
					}
				}
			}

			// ----- Text drawing -----
			int visualCol = 0;

			for (int i = 0; i < text.Length; i++)
			{
				char ch = text[i];

				int cellWidth;
				if (ch == '\t')
					cellWidth = TabWidth - (visualCol % TabWidth);
				else
					cellWidth = 1;

				bool selected = false;
				if (hasSelection)
				{
					var pos = (line, i);
					selected =
						ComparePos(pos, selStart) >= 0 &&
						ComparePos(pos, selEnd) < 0;
				}

				var fg = selected
					? Style!.Semantic.Background
					: Style!.Semantic.Text;

				var bg = selected
					? Style!.Semantic.Text
					: Style!.Semantic.Background;

				int drawX =
					TextOriginX +
					(visualCol % visualColsPerRow) *
					_cellSize.Width;

				int drawY =
					baseY +
					(visualCol / visualColsPerRow) *
					_cellSize.Height;

				if (ch != '\t')
				{
					_font!.WriteChar(
						rc,
						ch,
						new Point(drawX, drawY),
						fg,
						bg
					);
				}

				visualCol += cellWidth;
			}
		}
	}

	private int LineNumberDigits =>
		Math.Max(1, _document.LineCount.ToString().Length);

	private int LineNumberGutterWidth =>
		ShowLineNumbers
			? (LineNumberDigits + 1) * _cellSize.Width
			: 0;

	private int TextOriginX =>
		LineNumberGutterWidth;

	private void DrawTextAndLineNumbers(IRenderingContext rc)
	{
		int yIndex = 0;

		for (int line = _firstVisibleLine;
			 line < _document.LineCount &&
			 yIndex < _visibleLineCount;
			 line++, yIndex++)
		{
			int y = yIndex * _cellSize.Height;

			if (ShowLineNumbers)
			{
				var ln = (line + 1).ToString();
				int lnX = LineNumberGutterWidth - ln.Length * _cellSize.Width;

				if (IsActiveLine(line))
				{
					rc.RenderFilledRect(
						new Rectangle(
							0,
							y,
							LineNumberGutterWidth,
							_cellSize.Height),
						Style!.Semantic.Primary.Lerp(
							Style!.Semantic.Background,
							0.15f)
					);
				}

				_font!.WriteString(
					rc,
					ln,
					new Point(lnX, y),
					IsActiveLine(line)
						? Style!.Semantic.Primary
						: Style!.Semantic.SecondaryText,
					Style.Semantic.Background
				);
			}

			DrawLineWithTabs(
				rc,
				_document.Lines[line].ToString(),
				new Point(TextOriginX, y)
			);
		}
	}

	private void DrawLineWithTabs(
		IRenderingContext rc,
		string text,
		Point origin)
	{
		int visualCol = 0;

		foreach (char ch in text)
		{
			if (ch == '\t')
			{
				int spaces = TabWidth - (visualCol % TabWidth);
				visualCol += spaces;
				continue;
			}

			_font!.WriteChar(
				rc,
				ch,
				new Point(
					origin.X + visualCol * _cellSize.Width,
					origin.Y),
				Style!.Semantic.Text,
				Style.Semantic.Background
			);

			visualCol++;
		}
	}

	private int VisualColumnsPerRow =>
		Math.Max(1, (Size.Width - TextOriginX) / _cellSize.Width);

	private void DrawCaret(IRenderingContext rc)
	{
		if (!HasKeyboardFocus())
			return;

		int line = _document.CaretLine;
		if (line < _firstVisibleLine ||
			line >= _firstVisibleLine + _visibleLineCount)
			return;

		var lineText = _document.Lines[line].ToString();
		int visualCol = ComputeVisualColumn(
			lineText,
			_document.CaretColumn);

		int x =
			TextOriginX +
			visualCol * _cellSize.Width;

		int y =
			(line - _firstVisibleLine) * _cellSize.Height;

		rc.RenderFilledRect(
			new Rectangle(
				x,
				y + _cellSize.Height - 2,
				_cellSize.Width,
				2),
			Style!.Semantic.Text
		);
	}

	#endregion

	#region Tab helpers

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

	#endregion

	#region Helpers

	private bool HasKeyboardFocus()
	{
		return TryGetWorld(out var world) &&
			   world.KeyboardFocus == this;
	}

	private bool IsActiveLine(int lineIndex)
	{
		return lineIndex == _document.CaretLine;
	}

	#endregion
}