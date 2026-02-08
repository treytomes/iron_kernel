using System.Drawing;
using System.Text;
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

	private int _inputStartX;
	private int _inputStartY;

	private bool _isReadingLine;
	private readonly StringBuilder _inputBuffer = new();

	private TaskCompletionSource<string>? _pendingReadLine;

	private Font? _font;
	private bool _layoutInitialized;
	private readonly TaskCompletionSource _ready = new(TaskCreationOptions.RunContinuationsAsynchronously);

	private readonly List<string> _commandHistory = new();
	private int _historyIndex = 0;
	// 0 = current (empty / live input)
	// 1 = last command
	// 2 = second-to-last, etc.

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

		SetCursorFromInputIndex(GetInputIndex());
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
			DrawCursor(rc);
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
			case Key.Up:
				HistoryUp();
				break;

			case Key.Down:
				HistoryDown();
				break;

			case Key.Left:
				if (e.Modifiers.HasFlag(KeyModifier.Control))
					MoveCursorWordLeft();
				else
					MoveCursor(-1);
				break;

			case Key.Right:
				if (e.Modifiers.HasFlag(KeyModifier.Control))
					MoveCursorWordRight();
				else
					MoveCursor(+1);
				break;

			case Key.Backspace:
				if (e.Modifiers.HasFlag(KeyModifier.Control))
					DeleteWordLeft();
				else
					Backspace();
				break;

			case Key.Delete:
				if (e.Modifiers.HasFlag(KeyModifier.Control))
					DeleteWordRight();
				else
					DeleteChar();
				break;

			case Key.Home:
				MoveCursorToStart();
				break;

			case Key.End:
				MoveCursorToEnd();
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
		SetCursorFromInputIndex(index + 1);
		RedrawInput();
		ClearInputTail();
	}

	private void Backspace()
	{
		if (!_isReadingLine)
			return;

		var index = GetInputIndex();
		if (index == 0)
			return;

		_inputBuffer.Remove(index - 1, 1);
		SetCursorFromInputIndex(index - 1);
		RedrawInput();
		ClearInputTail();
	}

	private void DeleteChar()
	{
		if (!_isReadingLine)
			return;

		var index = GetInputIndex();

		// Nothing to delete if cursor is at end
		if (index >= _inputBuffer.Length)
			return;

		// Remove character at cursor
		_inputBuffer.Remove(index, 1);

		// Cursor stays in the same logical position
		SetCursorFromInputIndex(index);

		// Redraw buffer and clear stale cells
		RedrawInput();
		ClearInputTail();
	}

	private void MoveCursorToStart()
	{
		if (!_isReadingLine)
			return;

		SetCursorFromInputIndex(0);
	}

	private void MoveCursorToEnd()
	{
		if (!_isReadingLine)
			return;

		SetCursorFromInputIndex(_inputBuffer.Length);
	}

	private void CommitLine()
	{
		if (!_isReadingLine)
		{
			NewLine();
			return;
		}

		var result = _inputBuffer.ToString();

		// Save to history if non-empty (optional policy)
		if (!string.IsNullOrWhiteSpace(result))
		{
			_commandHistory.Add(result);
		}

		// Reset history navigation
		_historyIndex = 0;

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
			if (y >= Rows)
				break;

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
				y++;
			}
		}
	}

	private void ClearInputTail()
	{
		var index = _inputBuffer.Length;
		var absolute = _inputStartX + index;

		var y = _inputStartY + (absolute / Columns);
		var x = absolute % Columns;

		while (y < Rows)
		{
			while (x < Columns)
			{
				_buffer[y, x] = ConsoleCell.Empty;
				x++;
			}
			x = 0;
			y++;
		}
	}

	private int GetInputIndex()
	{
		return (_cursorY - _inputStartY) * Columns
			 + (_cursorX - _inputStartX);
	}

	private void SetCursorFromInputIndex(int index)
	{
		var absolute = _inputStartX + index;
		_cursorY = _inputStartY + (absolute / Columns);
		_cursorX = absolute % Columns;

		_cursorY = Math.Min(_cursorY, Rows - 1);
	}

	private void MoveCursor(int delta)
	{
		if (!_isReadingLine)
			return;

		var index = GetInputIndex();
		var newIndex = Math.Clamp(index + delta, 0, _inputBuffer.Length);
		SetCursorFromInputIndex(newIndex);
	}

	private void HistoryUp()
	{
		if (!_isReadingLine)
			return;

		if (_commandHistory.Count == 0)
			return;

		// Clamp to max history depth
		if (_historyIndex < _commandHistory.Count)
			_historyIndex++;

		ApplyHistoryEntry();
	}

	private void HistoryDown()
	{
		if (!_isReadingLine)
			return;

		if (_historyIndex > 0)
			_historyIndex--;

		ApplyHistoryEntry();
	}

	private void ApplyHistoryEntry()
	{
		_inputBuffer.Clear();

		if (_historyIndex == 0)
		{
			// Live input (empty line)
		}
		else
		{
			var index = _commandHistory.Count - _historyIndex;
			if (index >= 0 && index < _commandHistory.Count)
			{
				_inputBuffer.Append(_commandHistory[index]);
			}
		}

		SetCursorFromInputIndex(_inputBuffer.Length);
		RedrawInput();
		ClearInputTail();
	}

	private static bool IsWordChar(char ch)
	{
		return char.IsLetterOrDigit(ch) || ch == '_';
	}

	private void DeleteWordLeft()
	{
		if (!_isReadingLine)
			return;

		int index = GetInputIndex();
		if (index == 0)
			return;

		int start = index;

		// Skip separators to the left
		while (start > 0 && !IsWordChar(_inputBuffer[start - 1]))
			start--;

		// Skip word characters to the left
		while (start > 0 && IsWordChar(_inputBuffer[start - 1]))
			start--;

		int length = index - start;
		if (length <= 0)
			return;

		_inputBuffer.Remove(start, length);

		SetCursorFromInputIndex(start);
		RedrawInput();
		ClearInputTail();
	}

	private void DeleteWordRight()
	{
		if (!_isReadingLine)
			return;

		int index = GetInputIndex();
		int len = _inputBuffer.Length;

		if (index >= len)
			return;

		int end = index;

		// Skip separators to the right
		while (end < len && !IsWordChar(_inputBuffer[end]))
			end++;

		// Skip word characters to the right
		while (end < len && IsWordChar(_inputBuffer[end]))
			end++;

		int length = end - index;
		if (length <= 0)
			return;

		_inputBuffer.Remove(index, length);

		SetCursorFromInputIndex(index);
		RedrawInput();
		ClearInputTail();
	}

	private void MoveCursorWordLeft()
	{
		if (!_isReadingLine)
			return;

		int index = GetInputIndex();
		if (index == 0)
			return;

		// Step 1: skip separators to the left
		while (index > 0 && !IsWordChar(_inputBuffer[index - 1]))
			index--;

		// Step 2: skip word characters to the left
		while (index > 0 && IsWordChar(_inputBuffer[index - 1]))
			index--;

		SetCursorFromInputIndex(index);
	}

	private void MoveCursorWordRight()
	{
		if (!_isReadingLine)
			return;

		int index = GetInputIndex();
		int len = _inputBuffer.Length;

		if (index >= len)
			return;

		// Step 1: skip separators to the right
		while (index < len && !IsWordChar(_inputBuffer[index]))
			index++;

		// Step 2: skip word characters to the right
		while (index < len && IsWordChar(_inputBuffer[index]))
			index++;

		SetCursorFromInputIndex(index);
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
			ScrollUp();
		else
			_cursorY++;
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
			for (int x = 0; x < Columns; x++)
				_buffer[y - 1, x] = _buffer[y, x];

		for (int x = 0; x < Columns; x++)
			_buffer[Rows - 1, x] = ConsoleCell.Empty;
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