using System.Drawing;
using IronKernel.Common.ValueObjects;
using IronKernel.Userland.Gfx;
using IronKernel.Userland.Morphic.Events;

namespace IronKernel.Userland.Morphic;

public sealed class TextConsoleMorph : Morph
{
	#region Fields

	private ConsoleCell[,] _buffer;

	private int _cursorX;
	private int _cursorY;
	private Font? _font;

	#endregion

	#region Constructors

	public TextConsoleMorph()
	{
		CellSize = new Size(1, 1);
		Columns = 1;
		Rows = 1;

		_buffer = new ConsoleCell[Rows, Columns];
		Clear();
	}

	#endregion

	#region Properties

	public int Columns { get; private set; }
	public int Rows { get; private set; }
	public Size CellSize { get; private set; }
	public override bool WantsKeyboardFocus => true;

	#endregion

	#region Methods

	protected override async void OnLoad(IAssetService assets)
	{
		if (Style == null) throw new Exception("Style is null.");

		_font = await assets.LoadFontAsync(
			Style.DefaultFontStyle.AssetId,
			Style.DefaultFontStyle.TileSize,
			Style.DefaultFontStyle.GlyphOffset
		);
		CellSize = _font.TileSize;
		UpdateLayout();
	}

	/* -----------------------------------------------------------------
     * Layout
     * -----------------------------------------------------------------*/

	// protected override void UpdateLayout()
	// {
	// 	if (_font == null)
	// 		return;

	// 	var snappedColumns = Math.Max(1, Size.Width / CellSize.Width);
	// 	var snappedRows = Math.Max(1, Size.Height / CellSize.Height);

	// 	var snappedSize = new Size(
	// 		snappedColumns * CellSize.Width,
	// 		snappedRows * CellSize.Height
	// 	);

	// 	if (snappedSize != Size)
	// 		Size = snappedSize;

	// 	if (snappedColumns != Columns || snappedRows != Rows)
	// 		ResizeGrid(snappedColumns, snappedRows);
	// }

	protected override void UpdateLayout()
	{
		// Font must be loaded
		if (_font == null) return;

		// We must have a parent to size against
		if (Owner == null) return;

		var availableSize = Owner.Size;

		// Snap available size to whole cells
		var snappedColumns = Math.Max(1, availableSize.Width / CellSize.Width);
		var snappedRows = Math.Max(1, availableSize.Height / CellSize.Height);

		var snappedSize = new Size(
			snappedColumns * CellSize.Width,
			snappedRows * CellSize.Height
		);

		// Resize self to fit container (snapped)
		if (Size != snappedSize)
			Size = snappedSize;

		// Resize backing grid if needed
		if (snappedColumns != Columns || snappedRows != Rows)
			ResizeGrid(snappedColumns, snappedRows);
	}

	private void ResizeGrid(int newColumns, int newRows)
	{
		var newBuffer = new ConsoleCell[newRows, newColumns];

		// Initialize entire grid explicitly
		for (var y = 0; y < newRows; y++)
		{
			for (var x = 0; x < newColumns; x++)
			{
				newBuffer[y, x] = ConsoleCell.Empty;
			}
		}

		// Copy old contents into the new grid
		for (var y = 0; y < Math.Min(Rows, newRows); y++)
		{
			for (var x = 0; x < Math.Min(Columns, newColumns); x++)
			{
				newBuffer[y, x] = _buffer[y, x];
			}
		}

		_buffer = newBuffer;
		Columns = newColumns;
		Rows = newRows;

		_cursorX = Math.Min(_cursorX, Columns - 1);
		_cursorY = Math.Min(_cursorY, Rows - 1);
	}

	/* -----------------------------------------------------------------
     * Rendering
     * -----------------------------------------------------------------*/

	protected override void DrawSelf(IRenderingContext rc)
	{
		for (var y = 0; y < Rows; y++)
		{
			for (var x = 0; x < Columns; x++)
			{
				var cell = _buffer[y, x];

				var px = x * CellSize.Width;
				var py = y * CellSize.Height;

				rc.RenderFilledRect(
					new Rectangle(px, py, CellSize.Width, CellSize.Height),
					cell.Background
				);

				_font?.WriteChar(rc, cell.Char, new Point(px, py), cell.Foreground, cell.Background);
			}
		}

		DrawCursor(rc);
	}

	private void DrawCursor(IRenderingContext rc)
	{
		var px = _cursorX * CellSize.Width;
		var py = _cursorY * CellSize.Height;

		rc.RenderFilledRect(
			new Rectangle(px, py + CellSize.Height - 2, CellSize.Width, 2),
			RadialColor.White
		);
	}

	/* -----------------------------------------------------------------
     * Input
     * -----------------------------------------------------------------*/

	public override void OnKey(KeyEvent e)
	{
		if (e.Action != InputAction.Press)
			return;

		switch (e.Key)
		{
			case Key.Backspace:
				Backspace();
				break;

			case Key.Enter:
				NewLine();
				break;

			case Key.Left:
				if (_cursorX > 0) _cursorX--;
				break;

			case Key.Right:
				if (_cursorX < Columns - 1) _cursorX++;
				break;

			default:
				// ✅ character input via extension method
				var ch = e.ToText();
				if (ch.HasValue)
				{
					PutChar(ch.Value);
				}
				break;
		}
	}

	/* -----------------------------------------------------------------
     * Console semantics
     * -----------------------------------------------------------------*/

	public void PutChar(char ch, RadialColor? fg = null, RadialColor? bg = null)
	{
		_buffer[_cursorY, _cursorX] = new ConsoleCell
		{
			Char = ch,
			Foreground = fg ?? RadialColor.White,
			Background = bg ?? RadialColor.Black
		};

		_cursorX++;

		if (_cursorX >= Columns)
			NewLine();
	}

	public void NewLine()
	{
		_cursorX = 0;
		_cursorY = Math.Min(_cursorY + 1, Rows - 1);
		// scrolling intentionally omitted
	}

	public void Backspace()
	{
		if (_cursorX == 0)
			return;

		_cursorX--;
		_buffer[_cursorY, _cursorX] = ConsoleCell.Empty;
	}

	public void Clear()
	{
		for (int y = 0; y < Rows; y++)
			for (int x = 0; x < Columns; x++)
				_buffer[y, x] = ConsoleCell.Empty;

		_cursorX = 0;
		_cursorY = 0;
	}

	#endregion

	private struct ConsoleCell
	{
		public char Char;
		public RadialColor Foreground;
		public RadialColor Background;

		public static ConsoleCell Empty => new()
		{
			Char = ' ',
			Foreground = RadialColor.White,
			Background = RadialColor.Black
		};
	}
}