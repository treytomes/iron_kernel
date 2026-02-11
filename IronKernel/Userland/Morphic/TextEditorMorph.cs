using System.Drawing;
using IronKernel.Common.ValueObjects;
using IronKernel.Userland.Gfx;
using IronKernel.Userland.Morphic.Events;
using IronKernel.Userland.Services;

namespace IronKernel.Userland.Morphic;

public sealed class TextEditorMorph : Morph
{
	#region Fields

	private readonly TextDocument _document;

	private Font? _font;
	private Size _cellSize;

	private int _firstVisibleLine;
	private int _visibleLineCount;

	private bool _layoutInitialized;

	#endregion

	#region Configuration

	public bool ShowLineNumbers { get; set; } = true;

	public int TabWidth { get; set; } = 4;

	#endregion

	#region Construction

	public TextEditorMorph(TextDocument document)
	{
		_document = document;
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

	#region Input (Keyboard)

	public override void OnKey(KeyEvent e)
	{
		if (e.Action != InputAction.Press)
			return;

		switch (e.Key)
		{
			case Key.Tab:
				_document.InsertTab();
				break;

			case Key.Left:
				if (e.Modifiers.HasFlag(KeyModifier.Control))
					_document.MoveWordLeft();
				else
					_document.MoveLeft();
				break;

			case Key.Right:
				if (e.Modifiers.HasFlag(KeyModifier.Control))
					_document.MoveWordRight();
				else
					_document.MoveRight();
				break;

			case Key.Up:
				_document.MoveUp();
				break;

			case Key.Down:
				_document.MoveDown();
				break;

			case Key.Home:
				_document.MoveToLineStart();
				break;

			case Key.End:
				_document.MoveToLineEnd();
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

		EnsureCaretVisible();
		Invalidate();
		e.MarkHandled();
	}

	#endregion

	#region Input (Mouse)

	public override void OnPointerDown(PointerDownEvent e)
	{
		base.OnPointerDown(e);

		var local = WorldToLocal(e.Position);
		Console.WriteLine("local: " + local);
		if (local.Y < 0)
			return;

		// ----- Row -----
		int row = local.Y / _cellSize.Height;
		int lineIndex = _firstVisibleLine + row;

		lineIndex = Math.Clamp(
			lineIndex,
			0,
			_document.LineCount - 1
		);

		// ----- Column (visual → logical) -----
		int localX = local.X - TextOriginX;
		if (localX < 0)
			localX = 0;

		int targetVisualCol = localX / _cellSize.Width;

		var lineText = _document.Lines[lineIndex].ToString();
		int caretIndex = VisualColumnToCaretIndex(lineText, targetVisualCol);

		_document.SetCaretLine(lineIndex);
		_document.CaretColumn = caretIndex;

		EnsureCaretVisible();
		Invalidate();
		e.MarkHandled();
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

		var s = Style.Semantic;

		rc.RenderFilledRect(
			new Rectangle(Point.Empty, Size),
			s.Background
		);

		DrawTextAndLineNumbers(rc);
		DrawCaret(rc);
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