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

	#region Construction

	public TextEditorMorph(TextDocument document)
	{
		_document = document;
		IsSelectable = true;
	}

	#endregion

	#region Configuration

	public bool ShowLineNumbers { get; set; } = true;

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

	#region Input

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

	public override void OnPointerDown(PointerDownEvent e)
	{
		base.OnPointerDown(e);

		if (!ShowLineNumbers)
			return;

		var localPosition = WorldToLocal(e.Position);
		if (localPosition.Y < 0)
			return;

		// ----- Row -----
		int row = localPosition.Y / _cellSize.Height;
		int lineIndex = _firstVisibleLine + row;

		lineIndex = Math.Clamp(
			lineIndex,
			0,
			_document.LineCount - 1
		);

		// ----- Column -----
		int localX = localPosition.X - (ShowLineNumbers ? LineNumberGutterWidth : 0);
		if (localX < 0)
			localX = 0;

		int column = localX / _cellSize.Width;

		int lineLength = _document.Lines[lineIndex].Length;
		column = Math.Clamp(column, 0, lineLength);

		// ----- Apply -----
		_document.SetCaretLine(lineIndex);
		_document.CaretColumn = column;

		EnsureCaretVisible();
		Invalidate();
		e.MarkHandled();
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
				int lnX =
					LineNumberGutterWidth -
					ln.Length * _cellSize.Width;

				if (IsActiveLine(line))
				{
					rc.RenderFilledRect(
						new Rectangle(
							0,
							y,
							LineNumberGutterWidth,
							_cellSize.Height),
						Style!.Semantic.Primary.Lerp(Style!.Semantic.Background, 0.15f)
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

			var text = _document.Lines[line].ToString();
			_font!.WriteString(
				rc,
				text,
				new Point(LineNumberGutterWidth, y),
				Style!.Semantic.Text,
				Style.Semantic.Background
			);
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

		int column = _document.CaretColumn;

		int x =
			LineNumberGutterWidth +
			column * _cellSize.Width;

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