using System.Drawing;
using System.Text;
using IronKernel.Common.ValueObjects;
using Microsoft.Extensions.Logging;
using Userland.Gfx;
using Userland.Morphic.Events;
using Userland.Services;

namespace Userland.Morphic;

public sealed class TextConsoleMorph : Morph
{
	#region Fields

	private readonly ILogger _logger;
	private readonly IClipboardService _clipboard;

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

	private readonly SelectionController<int> _selection = new((a, b) => a.CompareTo(b));
	private bool _mouseSelecting;

	#endregion

	#region Constructor

	public TextConsoleMorph(ILogger logger, IClipboardService clipboard)
	{
		_logger = logger;
		_clipboard = clipboard;

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

		CellSize = _font.TileSize;
		InvalidateLayout();
	}

	protected override void UpdateLayout()
	{
		if (_font == null || Owner == null)
			return;

		var available = Owner.Size;
		int cols = Math.Max(1, available.Width / CellSize.Width);
		int rows = Math.Max(1, available.Height / CellSize.Height);

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
				bool selected = IsCellSelected(x, y);

				var fg = selected ? cell.Background : cell.Foreground;
				var bg = selected ? cell.Foreground : cell.Background;

				var px = x * CellSize.Width;
				var py = y * CellSize.Height;

				rc.RenderFilledRect(
					new Rectangle(px, py, CellSize.Width, CellSize.Height),
					bg
				);

				_font?.WriteChar(rc, cell.Char, new Point(px, py), fg, bg);
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

	private bool IsCellSelected(int x, int y)
	{
		if (!_isReadingLine || _editor == null || !_selection.HasSelection)
			return false;

		// Reject cells before the input start
		if (y < _inputStartY)
			return false;

		if (y == _inputStartY && x < _inputStartX)
			return false;

		int index = CellToEditorIndex(x, y);
		if (index < 0 || index > _editor.Length)
			return false;

		var (start, end) = _selection.GetRange();
		return index >= start && index < end;
	}

	#endregion

	#region Pointer Input (Unified Selection)

	public override void OnPointerDown(PointerDownEvent e)
	{
		base.OnPointerDown(e);
		if (e.Button != MouseButton.Left || !_isReadingLine || _editor == null)
			return;

		if (TryGetWorld(out var world))
			world.CapturePointer(this);

		ClearSelection();

		var cell = PixelToCell(WorldToLocal(e.Position));
		int index = CellToEditorIndex(cell.X, cell.Y);

		_selection.Begin(index);
		_mouseSelecting = true;

		e.MarkHandled();
		Invalidate();
	}

	public override void OnPointerMove(PointerMoveEvent e)
	{
		base.OnPointerMove(e);
		if (!_mouseSelecting || _editor == null)
			return;

		var cell = PixelToCell(WorldToLocal(e.Position));
		int index = CellToEditorIndex(cell.X, cell.Y);

		_selection.Update(index);
		Invalidate();
	}

	public override void OnPointerUp(PointerUpEvent e)
	{
		base.OnPointerUp(e);
		if (e.Button != MouseButton.Left)
			return;

		if (TryGetWorld(out var world))
			world.ReleasePointer(this);

		_mouseSelecting = false;
		e.MarkHandled();
	}

	#endregion

	#region Keyboard Input

	public override void OnKey(KeyEvent e)
	{
		if (e.Action != InputAction.Press)
			return;

		switch (e.Key)
		{
			case Key.A when e.Modifiers.HasFlag(KeyModifier.Control):
				SelectAll();
				break;

			case Key.C when e.Modifiers.HasFlag(KeyModifier.Control):
				CopySelection();
				break;

			case Key.X when e.Modifiers.HasFlag(KeyModifier.Control):
				CutSelection();
				break;

			case Key.V when e.Modifiers.HasFlag(KeyModifier.Control):
				PasteClipboard();
				break;

			case Key.Up:
				HistoryUp();
				e.MarkHandled();
				break;

			case Key.Down:
				HistoryDown();
				e.MarkHandled();
				break;

			case Key.Left:
				HandleHorizontalMove(-1, e);
				break;

			case Key.Right:
				HandleHorizontalMove(+1, e);
				break;

			case Key.Home:
				HandleHomeEnd(start: true, e);
				break;

			case Key.End:
				HandleHomeEnd(start: false, e);
				break;

			case Key.Backspace:
				if (!DeleteSelectionIfAny())
					_editor?.Backspace();
				break;

			case Key.Delete:
				if (!DeleteSelectionIfAny())
					_editor?.Delete();
				break;

			case Key.Enter:
				CommitLine();
				return;

			default:
				var ch = e.ToText();
				if (ch.HasValue && _isReadingLine)
				{
					DeleteSelectionIfAny();
					_editor?.Insert(ch.Value);
				}
				break;
		}
	}

	#endregion

	#region Editor Integration

	private void AttachEditor(TextEditingCore editor)
	{
		DetachEditor();
		_editor = editor;
		_editor.Changed += OnEditorChanged;
	}

	private void DetachEditor()
	{
		if (_editor != null)
			_editor.Changed -= OnEditorChanged;
	}

	private void OnEditorChanged()
	{
		SyncCursorFromEditor();
		RedrawInput();
		ClearInputTail();
		Invalidate();
	}

	private void SyncCursorFromEditor()
	{
		if (_editor == null)
			return;

		int absolute = _inputStartX + _editor.CursorIndex;

		int newY = _inputStartY + (absolute / Columns);
		int newX = absolute % Columns;

		// Scroll if wrapped input exceeds bottom row
		while (newY >= Rows)
		{
			ScrollUp();
			newY--;
			_inputStartY = Math.Max(0, _inputStartY - 1);
		}

		_cursorY = newY;
		_cursorX = Math.Clamp(newX, 0, Columns - 1);

		_selection.Normalize(i => i >= 0 && i <= _editor.Length);
	}

	#endregion

	#region Console API

	public Task<string> ReadLineAsync()
	{
		if (_isReadingLine)
			throw new InvalidOperationException("ReadLine already in progress.");

		CaptureKeyboard();

		_isReadingLine = true;
		AttachEditor(new TextEditingCore(_logger));

		_inputStartX = _cursorX;
		_inputStartY = _cursorY;

		_pendingReadLine = new TaskCompletionSource<string>(
			TaskCreationOptions.RunContinuationsAsynchronously);

		return _pendingReadLine.Task;
	}

	#endregion

	#region Utilities / Projection

	private int CellToEditorIndex(int x, int y)
	{
		if (y < _inputStartY)
			return -1;

		if (y == _inputStartY && x < _inputStartX)
			return -1;

		if (_editor == null)
			return -1;

		int absolute =
			(y - _inputStartY) * Columns +
			(x - _inputStartX);

		return Math.Clamp(absolute, 0, _editor.Length);
	}

	private Point PixelToCell(Point p)
	{
		int x = Math.Clamp(p.X / CellSize.Width, 0, Columns - 1);
		int y = Math.Clamp(p.Y / CellSize.Height, 0, Rows - 1);
		return new Point(x, y);
	}

	#endregion

	public void Clear()
	{
		for (int y = 0; y < Rows; y++)
			for (int x = 0; x < Columns; x++)
				_buffer[y, x] = ConsoleCell.Empty;

		_cursorX = 0;
		_cursorY = 0;
	}

	private void ClearSelection()
	{
		_selection.Clear();
		_mouseSelecting = false;
	}

	private void SelectAll()
	{
		if (!_isReadingLine || _editor == null)
			return;

		_selection.Begin(0);
		_selection.Update(_editor.Length);
	}

	private void CopySelection()
	{
		if (_editor == null || !_selection.HasSelection)
			return;

		var (start, end) = _selection.GetRange();
		string text = _editor.GetSubstring(start, end - start);
		_clipboard.SetText(text);
	}

	private void CutSelection()
	{
		if (_editor == null || !_selection.HasSelection)
			return;

		CopySelection();
		DeleteSelectionIfAny();
	}

	private async void PasteClipboard()
	{
		if (!_isReadingLine || _editor == null)
			return;

		ClearSelection();

		var text = await _clipboard.GetTextAsync();
		if (string.IsNullOrEmpty(text))
			return;

		foreach (char ch in text)
		{
			if (ch == '\n' || ch == '\r')
				continue; // console input is single-line
			_editor.Insert(ch);
		}
	}

	private void HandleHorizontalMove(int delta, KeyEvent e)
	{
		if (_editor == null)
			return;

		if (e.Modifiers.HasFlag(KeyModifier.Shift))
		{
			_selection.Update(_editor.CursorIndex);
			_editor.Move(delta);
			_selection.Update(_editor.CursorIndex);
		}
		else
		{
			ClearSelection();
			_editor.Move(delta);
		}
	}

	private void HandleHomeEnd(bool start, KeyEvent e)
	{
		if (_editor == null)
			return;

		if (e.Modifiers.HasFlag(KeyModifier.Shift))
		{
			_selection.Update(_editor.CursorIndex);
			if (start)
				_editor.MoveToStart();
			else
				_editor.MoveToEnd();
			_selection.Update(_editor.CursorIndex);
		}
		else
		{
			ClearSelection();
			if (start)
				_editor.MoveToStart();
			else
				_editor.MoveToEnd();
		}
	}

	private bool DeleteSelectionIfAny()
	{
		if (_editor == null || !_selection.HasSelection)
			return false;

		var (start, end) = _selection.GetRange();
		_editor.DeleteRange(start, end - start);
		_editor.SetCursorIndex(start);
		_selection.Clear();
		return true;
	}

	private void CommitLine()
	{
		if (!_isReadingLine || _editor == null)
		{
			NewLine();
			return;
		}

		ClearSelection();

		string result = _editor.ToString();
		if (!string.IsNullOrWhiteSpace(result))
			_commandHistory.Add(result);

		_historyIndex = 0;
		_isReadingLine = false;

		_editor = null;
		NewLine();

		_pendingReadLine?.SetResult(result);
		_pendingReadLine = null;
	}

	private void RedrawInput()
	{
		if (_editor == null)
			return;

		int x = _inputStartX;
		int y = _inputStartY;

		string text = _editor.ToString();

		for (int i = 0; i < text.Length && y < Rows; i++)
		{
			_buffer[y, x] = new ConsoleCell
			{
				Char = text[i],
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

		int absolute = _inputStartX + _editor.Length;
		int y = _inputStartY + (absolute / Columns);
		int x = absolute % Columns;

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

	private void NewLine()
	{
		_cursorX = 0;
		if (_cursorY == Rows - 1)
			ScrollUp();
		else
			_cursorY++;
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

	private void ApplyHistory()
	{
		ClearSelection();

		if (_editor != null)
			_editor.Changed -= OnEditorChanged;

		_editor = new TextEditingCore(
			_logger,
			_historyIndex == 0
				? string.Empty
				: _commandHistory[^_historyIndex]
		);

		_editor.Changed += OnEditorChanged;

		SyncCursorFromEditor();
		RedrawInput();
		ClearInputTail();
	}

	private void HistoryUp()
	{
		if (!_isReadingLine || _commandHistory.Count == 0)
			return;

		// Clamp to oldest entry
		if (_historyIndex < _commandHistory.Count)
			_historyIndex++;

		ApplyHistory();
	}

	private void HistoryDown()
	{
		if (!_isReadingLine || _commandHistory.Count == 0)
			return;

		if (_historyIndex > 0)
			_historyIndex--;

		ApplyHistory();
	}

	public void Write(string text)
	{
		if (string.IsNullOrEmpty(text))
			return;

		foreach (char ch in text)
			PutChar(ch);
	}

	public void WriteLine(string text = "")
	{
		if (!string.IsNullOrEmpty(text))
			Write(text);

		NewLine();
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