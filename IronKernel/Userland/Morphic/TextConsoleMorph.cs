using System.Drawing;
using System.Text;
using IronKernel.Common.ValueObjects;
using IronKernel.Userland.Gfx;
using IronKernel.Userland.Morphic.Events;

namespace IronKernel.Userland.Morphic;

// TODO: Backspacing from end of string isn't erasing the cell at the cursor like it should.
public sealed class TextConsoleMorph : Morph
{
	#region Fields

	private ConsoleCell[,] _buffer;

	private int _cursorX;
	private int _cursorY;

	private int _inputStartX;
	private int _inputStartY;

	private bool _isReadingLine;
	private readonly StringBuilder _inputBuffer = new();
	private TaskCompletionSource<string>? _pendingReadLine;

	private Font? _font;
	private bool _layoutInitialized = false;
	private readonly TaskCompletionSource _ready = new(TaskCreationOptions.RunContinuationsAsynchronously);

	#endregion

	#region Constructor

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

	public Task Ready => _ready.Task;
	public int Columns { get; private set; }
	public int Rows { get; private set; }
	public Size CellSize { get; private set; }

	public RadialColor CurrentForegroundColor { get; set; } = RadialColor.Orange;
	public RadialColor CurrentBackgroundColor { get; set; } = RadialColor.Black;

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

		CellSize = _font.TileSize;
		InvalidateLayout();
	}

	#endregion

	#region Layout

	protected override void UpdateLayout()
	{
		if (_font == null || Owner == null)
			return;

		var available = Owner.Size;
		var cols = Math.Max(1, available.Width / CellSize.Width);
		var rows = Math.Max(1, available.Height / CellSize.Height);

		if (cols != Columns || rows != Rows)
			ResizeGrid(cols, rows);

		Size = new Size(
			Columns * CellSize.Width,
			Rows * CellSize.Height
		);

		if (!_layoutInitialized)
		{
			_layoutInitialized = true;
			_ready.TrySetResult();
		}
	}

	private void ResizeGrid(int newColumns, int newRows)
	{
		var newBuffer = new ConsoleCell[newRows, newColumns];

		for (int y = 0; y < newRows; y++)
			for (int x = 0; x < newColumns; x++)
				newBuffer[y, x] = ConsoleCell.Empty;

		for (int y = 0; y < Math.Min(Rows, newRows); y++)
			for (int x = 0; x < Math.Min(Columns, newColumns); x++)
				newBuffer[y, x] = _buffer[y, x];

		_buffer = newBuffer;
		Columns = newColumns;
		Rows = newRows;

		_cursorX = Math.Min(_cursorX, Columns - 1);
		_cursorY = Math.Min(_cursorY, Rows - 1);
	}

	#endregion

	#region Rendering

	protected override void DrawSelf(IRenderingContext rc)
	{
		for (int y = 0; y < Rows; y++)
		{
			for (int x = 0; x < Columns; x++)
			{
				var cell = _buffer[y, x];
				var px = x * CellSize.Width;
				var py = y * CellSize.Height;

				rc.RenderFilledRect(
					new Rectangle(px, py, CellSize.Width, CellSize.Height),
					cell.Background
				);

				_font?.WriteChar(
					rc,
					cell.Char,
					new Point(px, py),
					cell.Foreground,
					cell.Background
				);
			}
		}

		if (TryGetWorld(out var world) && world.KeyboardFocus == this)
		{
			DrawCursor(rc);
		}
	}

	private void DrawCursor(IRenderingContext rc)
	{
		var px = _cursorX * CellSize.Width;
		var py = _cursorY * CellSize.Height;

		rc.RenderFilledRect(
			new Rectangle(px, py + CellSize.Height - 2, CellSize.Width, 2),
			CurrentForegroundColor
		);
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
				MoveCursorLeft();
				break;

			case Key.Right:
				MoveCursorRight();
				break;

			case Key.Backspace:
				Backspace();
				break;

			case Key.Enter:
				CommitLine();
				break;

			default:
				var ch = e.ToText();
				if (ch.HasValue && _isReadingLine)
					InsertChar(ch.Value);
				break;
		}
	}

	#endregion

	#region Console API

	public void Write(string text)
	{
		foreach (var ch in text)
			PutChar(ch);
	}

	public void WriteLine(string text = "")
	{
		Write(text);
		NewLine();
	}

	public Task<string> ReadLineAsync()
	{
		if (_isReadingLine)
			throw new InvalidOperationException("ReadLine already in progress.");

		_isReadingLine = true;
		_inputBuffer.Clear();

		_inputStartX = _cursorX;
		_inputStartY = _cursorY;

		_pendingReadLine = new TaskCompletionSource<string>(
			TaskCreationOptions.RunContinuationsAsynchronously);

		return _pendingReadLine.Task;
	}

	#endregion

	#region Editing Logic

	private void InsertChar(char ch)
	{
		var index = GetInputIndex();
		_inputBuffer.Insert(index, ch);
		RedrawInput();
		MoveCursorRight();
	}

	private void Backspace()
	{
		var index = GetInputIndex();
		if (!_isReadingLine || index == 0)
			return;

		_inputBuffer.Remove(index - 1, 1);
		MoveCursorLeft();
		RedrawInput();
	}

	private void CommitLine()
	{
		if (!_isReadingLine)
		{
			NewLine();
			return;
		}

		var result = _inputBuffer.ToString();
		_inputBuffer.Clear();
		_isReadingLine = false;

		NewLine();

		_pendingReadLine?.SetResult(result);
		_pendingReadLine = null;
	}

	private void RedrawInput()
	{
		var x = _inputStartX;
		var y = _inputStartY;

		foreach (var ch in _inputBuffer.ToString())
		{
			_buffer[y, x] = new ConsoleCell
			{
				Char = ch,
				Foreground = CurrentForegroundColor,
				Background = CurrentBackgroundColor
			};

			x++;
			if (x >= Columns)
			{
				x = 0;
				y = Math.Min(y + 1, Rows - 1);
			}
		}
	}

	private int GetInputIndex()
	{
		return (_cursorY - _inputStartY) * Columns
			 + (_cursorX - _inputStartX);
	}

	private void MoveCursorLeft()
	{
		if (!_isReadingLine)
			return;

		if (_cursorY > _inputStartY ||
		   (_cursorY == _inputStartY && _cursorX > _inputStartX))
			_cursorX--;
	}

	private void MoveCursorRight()
	{
		if (!_isReadingLine)
			return;

		if (GetInputIndex() < _inputBuffer.Length)
			_cursorX++;
	}

	#endregion

	#region Output Helpers

	private void PutChar(char ch)
	{
		if (ch == '\n')
		{
			NewLine();
			return;
		}

		_buffer[_cursorY, _cursorX] = new ConsoleCell
		{
			Char = ch,
			Foreground = CurrentForegroundColor,
			Background = CurrentBackgroundColor
		};

		_cursorX++;
		if (_cursorX >= Columns)
			NewLine();
	}

	private void NewLine()
	{
		_cursorX = 0;

		if (_cursorY == Rows - 1)
		{
			ScrollUp();
		}
		else
		{
			_cursorY++;
		}
	}

	public void Clear()
	{
		for (int y = 0; y < Rows; y++)
			for (int x = 0; x < Columns; x++)
				_buffer[y, x] = ConsoleCell.Empty;

		_cursorX = 0;
		_cursorY = 0;
	}

	private void ScrollUp()
	{
		for (int y = 1; y < Rows; y++)
		{
			for (int x = 0; x < Columns; x++)
			{
				_buffer[y - 1, x] = _buffer[y, x];
			}
		}

		// Clear last row
		for (int x = 0; x < Columns; x++)
		{
			_buffer[Rows - 1, x] = ConsoleCell.Empty;
		}
	}

	#endregion

	#region ConsoleCell

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

	#endregion
}