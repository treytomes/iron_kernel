using System.Drawing;
using IronKernel.Common.ValueObjects;
using IronKernel.Userland.Gfx;
using IronKernel.Userland.Morphic.Events;

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

	#region Focus

	public override bool WantsKeyboardFocus => true;

	#endregion

	#region Loading

	protected override async void OnLoad(IAssetService assets)
	{
		if (Style == null)
			throw new Exception("Style is null.");

		_font = await assets.LoadFontAsync(
			Style.DefaultFontStyle.AssetId,
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

	#endregion

	#region Rendering

	protected override void DrawSelf(IRenderingContext rc)
	{
		if (!_layoutInitialized || _font == null || Style == null)
			return;

		rc.RenderFilledRect(
			new Rectangle(Point.Empty, Size),
			RadialColor.Black
		);

		DrawText(rc);
		DrawCaret(rc);
	}

	private void DrawText(IRenderingContext rc)
	{
		int y = 0;

		for (int line = _firstVisibleLine;
			 line < _document.LineCount &&
			 y < _visibleLineCount;
			 line++, y++)
		{
			var text = _document.Lines[line].ToString();
			var pos = new Point(0, y * _cellSize.Height);

			_font!.WriteString(
				rc,
				text,
				pos,
				Style!.Semantic.Text,
				Style.Semantic.Surface
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

		int x = column * _cellSize.Width;
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

	#endregion
}