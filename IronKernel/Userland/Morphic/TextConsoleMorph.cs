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

	private int _inputStartX;
	private int _inputStartY;

	private bool _isReadingLine;
	private TextEditingCore? _editor;

	private TaskCompletionSource<string>? _pendingReadLine;

	private Font? _font;
	private bool _layoutInitialized;

	private readonly TaskCompletionSource _ready =
		new(TaskCreationOptions.RunContinuationsAsynchronously);

	private readonly List<string> _commandHistory = new();
	private int _historyIndex;

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

		SyncCursorFromEditor();
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
					_editor?.MoveWordLeft();
				else
					_editor?.Move(-1);
				break;

			case Key.Right:
				if (e.Modifiers.HasFlag(KeyModifier.Control))
					_editor?.MoveWordRight();
				else
					_editor?.Move(+1);
				break;

			case Key.Backspace:
				if (e.Modifiers.HasFlag(KeyModifier.Control))
					_editor?.DeleteWordLeft();
				else
					_editor?.Backspace();
				break;

			case Key.Delete:
				if (e.Modifiers.HasFlag(KeyModifier.Control))
					_editor?.DeleteWordRight();
				else
					_editor?.Delete();
				break;

			case Key.Home:
				_editor?.MoveToStart();
				break;

			case Key.End:
				_editor?.MoveToEnd();
				break;

			case Key.Enter:
				CommitLine();
				return;

			default:
				var ch = e.ToText();
				if (ch.HasValue && _isReadingLine)
					_editor?.Insert(ch.Value);
				break;
		}

		if (_isReadingLine && _editor != null)
		{
			SyncCursorFromEditor();
			RedrawInput();
			ClearInputTail();
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
		_editor = new TextEditingCore();

		_inputStartX = _cursorX;
		_inputStartY = _cursorY;

		_pendingReadLine = new TaskCompletionSource<string>(
			TaskCreationOptions.RunContinuationsAsynchronously);

		return _pendingReadLine.Task;
	}

	#endregion

	#region Editing Projection

	private void SyncCursorFromEditor()
	{
		if (_editor == null)
			return;

		var absolute = _inputStartX + _editor.CursorIndex;

		_cursorY = _inputStartY + (absolute / Columns);
		_cursorX = absolute % Columns;

		_cursorY = Math.Min(_cursorY, Rows - 1);
	}

	private void RedrawInput()
	{
		if (_editor == null)
			return;

		var x = _inputStartX;
		var y = _inputStartY;

		foreach (var ch in _editor.Buffer.ToString())
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
		if (_editor == null)
			return;

		var index = _editor.Length;
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

	#endregion

	#region History

	private void HistoryUp()
	{
		if (!_isReadingLine || _commandHistory.Count == 0)
			return;

		if (_historyIndex < _commandHistory.Count)
			_historyIndex++;

		ApplyHistory();
	}

	private void HistoryDown()
	{
		if (!_isReadingLine)
			return;

		if (_historyIndex > 0)
			_historyIndex--;

		ApplyHistory();
	}

	private void ApplyHistory()
	{
		_editor = new TextEditingCore(
			_historyIndex == 0
				? string.Empty
				: _commandHistory[^_historyIndex]
		);

		SyncCursorFromEditor();
		RedrawInput();
		ClearInputTail();
	}

	#endregion

	#region Output Helpers

	private void CommitLine()
	{
		if (!_isReadingLine || _editor == null)
		{
			NewLine();
			return;
		}

		var result = _editor.ToString();

		if (!string.IsNullOrWhiteSpace(result))
			_commandHistory.Add(result);

		_historyIndex = 0;
		_isReadingLine = false;
		_editor = null;

		NewLine();

		_pendingReadLine?.SetResult(result);
		_pendingReadLine = null;
	}

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

		if (_isReadingLine)
			_inputStartY = Math.Max(0, _inputStartY - 1);
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