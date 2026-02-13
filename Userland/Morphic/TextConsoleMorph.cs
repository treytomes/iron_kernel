using System.Drawing;
using System.Text;
using IronKernel.Common.ValueObjects;
using Userland.Gfx;
using Userland.Morphic.Events;
using Userland.Services;

namespace Userland.Morphic;

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

	// Selection is expressed in editor-relative indices
	private int? _selectionAnchor;   // index where selection started
	private int? _selectionCaret;    // current selection end
	private readonly IClipboardService _clipboard;

	private bool _mouseSelecting;
	private bool _hasMouseSelection;
	private Point _mouseSelectStart; // grid coords (x,y)
	private Point _mouseSelectEnd;   // grid coords (x,y)

	#endregion

	#region Constructor

	public TextConsoleMorph(IClipboardService clipboard)
	{
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
	private bool HasSelection => _selectionAnchor.HasValue && _selectionCaret.HasValue && _selectionAnchor.Value != _selectionCaret.Value;

	private bool HasKeyboardSelection =>
		_selectionAnchor.HasValue &&
		_selectionCaret.HasValue &&
		_selectionAnchor.Value != _selectionCaret.Value &&
		_editor != null;

	private bool HasMouseSelection =>
		_hasMouseSelection &&
		_mouseSelectStart != _mouseSelectEnd;

	#endregion

	#region Loading

	private (int start, int end) GetSelectionRange()
	{
		int a = _selectionAnchor!.Value;
		int b = _selectionCaret!.Value;
		return a < b ? (a, b) : (b, a);
	}

	protected override async void OnLoad(IAssetService assets)
	{
		if (Style == null)
			throw new Exception("Style is null.");

		_font = await assets.LoadFontAsync(
			Style.DefaultFontStyle.Url,
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

				bool selected =
					(HasKeyboardSelection && IsEditorIndexSelected(x, y)) ||
					(HasMouseSelection && IsMouseCellSelected(x, y));

				var fg = selected ? cell.Background : cell.Foreground;
				var bg = selected ? cell.Foreground : cell.Background;

				rc.RenderFilledRect(
					new Rectangle(px, py, CellSize.Width, CellSize.Height),
					bg
				);

				_font?.WriteChar(
					rc,
					cell.Char,
					new Point(px, py),
					fg,
					bg
				);
			}
		}

		if (TryGetWorld(out var world) && world.KeyboardFocus == this)
			DrawCursor(rc);
	}

	private bool IsEditorIndexSelected(int x, int y)
	{
		if (!_isReadingLine || _editor == null || !HasKeyboardSelection)
			return false;

		if (y != _inputStartY)
			return false;

		int index = x - _inputStartX;
		if (index < 0 || index >= _editor.Length)
			return false;

		var (start, end) = GetSelectionRange();
		return index >= start && index < end;
	}

	private bool IsMouseCellSelected(int x, int y)
	{
		int minY = Math.Min(_mouseSelectStart.Y, _mouseSelectEnd.Y);
		int maxY = Math.Max(_mouseSelectStart.Y, _mouseSelectEnd.Y);

		if (y < minY || y > maxY)
			return false;

		if (_mouseSelectStart.Y == _mouseSelectEnd.Y)
		{
			int minX = Math.Min(_mouseSelectStart.X, _mouseSelectEnd.X);
			int maxX = Math.Max(_mouseSelectStart.X, _mouseSelectEnd.X);
			return x >= minX && x <= maxX;
		}

		if (y == minY)
			return x >= _mouseSelectStart.X;
		if (y == maxY)
			return x <= _mouseSelectEnd.X;

		return true;
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

	public override void OnPointerDown(PointerDownEvent e)
	{
		base.OnPointerDown(e);

		if (e.Button != MouseButton.Left)
			return;

		if (TryGetWorld(out var world))
			world.CapturePointer(this);

		ClearSelection();
		_hasMouseSelection = true;
		_mouseSelecting = true;

		var local = WorldToLocal(e.Position);
		_mouseSelectStart = PixelToCell(local);
		_mouseSelectEnd = _mouseSelectStart;

		e.MarkHandled();
		Invalidate();
	}

	public override void OnPointerMove(PointerMoveEvent e)
	{
		base.OnPointerMove(e);

		if (!_mouseSelecting)
			return;

		var local = WorldToLocal(e.Position);
		_mouseSelectEnd = PixelToCell(local);

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

	private void RedrawSelectionOverlay()
	{
		// Re-project input (if any) so selection swaps colors correctly
		if (_editor != null)
		{
			RedrawInput();
			ClearInputTail();
		}
	}

	private Point PixelToCell(Point p)
	{
		int x = Math.Clamp(p.X / CellSize.Width, 0, Columns - 1);
		int y = Math.Clamp(p.Y / CellSize.Height, 0, Rows - 1);
		return new Point(x, y);
	}

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
				return;

			case Key.V when e.Modifiers.HasFlag(KeyModifier.Control):
				PasteClipboard();
				return;

			case Key.X when e.Modifiers.HasFlag(KeyModifier.Control):
				CutSelection();
				return;

			case Key.Tab:
				ClearSelection();
				InsertTabSpaces();
				break;

			case Key.Up:
				HistoryUp();
				break;

			case Key.Down:
				HistoryDown();
				break;

			case Key.Left:
				if (e.Modifiers.HasFlag(KeyModifier.Control) &&
					e.Modifiers.HasFlag(KeyModifier.Shift))
				{
					SelectWordLeft();
				}
				else if (e.Modifiers.HasFlag(KeyModifier.Shift))
				{
					StartOrUpdateSelection(-1);
				}
				else if (e.Modifiers.HasFlag(KeyModifier.Control))
				{
					ClearSelection();
					_editor?.MoveWordLeft();
				}
				else
				{
					ClearSelection();
					_editor?.Move(-1);
				}
				break;

			case Key.Right:
				if (e.Modifiers.HasFlag(KeyModifier.Control) &&
					e.Modifiers.HasFlag(KeyModifier.Shift))
				{
					SelectWordRight();
				}
				else if (e.Modifiers.HasFlag(KeyModifier.Shift))
				{
					StartOrUpdateSelection(+1);
				}
				else if (e.Modifiers.HasFlag(KeyModifier.Control))
				{
					ClearSelection();
					_editor?.MoveWordRight();
				}
				else
				{
					ClearSelection();
					_editor?.Move(+1);
				}
				break;

			case Key.Backspace:
				if (HasSelection)
				{
					DeleteSelectionIfAny();
				}
				else if (e.Modifiers.HasFlag(KeyModifier.Control))
				{
					_editor?.DeleteWordLeft();
				}
				else
				{
					_editor?.Backspace();
				}
				break;

			case Key.Delete:
				if (HasSelection)
				{
					DeleteSelectionIfAny();
				}
				else if (e.Modifiers.HasFlag(KeyModifier.Control))
				{
					_editor?.DeleteWordRight();
				}
				else
				{
					_editor?.Delete();
				}
				break;

			case Key.Home:
				if (e.Modifiers.HasFlag(KeyModifier.Shift))
				{
					SelectToLineStart();
				}
				else
				{
					ClearSelection();
					_editor?.MoveToStart();
				}
				break;

			case Key.End:
				if (e.Modifiers.HasFlag(KeyModifier.Shift))
				{
					SelectToLineEnd();
				}
				else
				{
					ClearSelection();
					_editor?.MoveToEnd();
				}
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

		if (_isReadingLine && _editor != null)
		{
			SyncCursorFromEditor();
			RedrawInput();
			ClearInputTail();
		}
	}

	private void SelectAll()
	{
		if (!_isReadingLine || _editor == null)
			return;

		_selectionAnchor = 0;
		_selectionCaret = _editor.Length;
	}

	private void SelectWordLeft()
	{
		if (!_isReadingLine || _editor == null)
			return;

		if (!_selectionAnchor.HasValue)
			_selectionAnchor = _editor.CursorIndex;

		_editor.MoveWordLeft();
		_selectionCaret = _editor.CursorIndex;
	}

	private void SelectWordRight()
	{
		if (!_isReadingLine || _editor == null)
			return;

		if (!_selectionAnchor.HasValue)
			_selectionAnchor = _editor.CursorIndex;

		_editor.MoveWordRight();
		_selectionCaret = _editor.CursorIndex;
	}

	private void SelectToLineStart()
	{
		if (!_isReadingLine || _editor == null)
			return;

		if (!_selectionAnchor.HasValue)
			_selectionAnchor = _editor.CursorIndex;

		_editor.MoveToStart();
		_selectionCaret = _editor.CursorIndex;
	}

	private void SelectToLineEnd()
	{
		if (!_isReadingLine || _editor == null)
			return;

		if (!_selectionAnchor.HasValue)
			_selectionAnchor = _editor.CursorIndex;

		_editor.MoveToEnd();
		_selectionCaret = _editor.CursorIndex;
	}

	private void CopySelection()
	{
		if (HasKeyboardSelection)
		{
			var (start, end) = GetSelectionRange();
			_clipboard.SetText(_editor!.Buffer.ToString(start, end - start));
			return;
		}

		if (HasMouseSelection)
		{
			var sb = new StringBuilder();

			int minY = Math.Min(_mouseSelectStart.Y, _mouseSelectEnd.Y);
			int maxY = Math.Max(_mouseSelectStart.Y, _mouseSelectEnd.Y);

			for (int y = minY; y <= maxY; y++)
			{
				int startX = (y == minY)
					? Math.Min(_mouseSelectStart.X, _mouseSelectEnd.X)
					: 0;

				int endX = (y == maxY)
					? Math.Max(_mouseSelectStart.X, _mouseSelectEnd.X)
					: Columns - 1;

				for (int x = startX; x <= endX; x++)
					sb.Append(_buffer[y, x].Char);

				if (y < maxY)
					sb.Append('\n');
			}

			_clipboard.SetText(sb.ToString().TrimEnd());
		}
	}

	private void ClearMouseSelection()
	{
		_mouseSelecting = false;
	}

	private void PasteClipboard()
	{
		if (!_isReadingLine || _editor == null)
			return;
		ClearSelection();

		_clipboard.GetTextAsync().ContinueWith(response =>
		{
			var text = response.Result;
			if (string.IsNullOrEmpty(text))
				return;

			foreach (char ch in text)
			{
				if (ch == '\n' || ch == '\r')
					continue; // console input stays single-line
				_editor.Insert(ch);
			}

			SyncCursorFromEditor();
			RedrawInput();
			ClearInputTail();
		});
	}

	private void CutSelection()
	{
		if (!HasSelection || _editor == null)
			return;

		CopySelection();

		var (start, end) = GetSelectionRange();
		_editor.Buffer.Remove(start, end - start);
		_editor.SetCursorIndex(start);

		ClearSelection();
		SyncCursorFromEditor();
		RedrawInput();
		ClearInputTail();
	}

	private void StartOrUpdateSelection(int delta)
	{
		if (!_isReadingLine || _editor == null)
			return;

		if (!_selectionAnchor.HasValue)
			_selectionAnchor = _editor.CursorIndex;

		_editor.Move(delta);
		_selectionCaret = _editor.CursorIndex;
	}

	private void ClearSelection()
	{
		_selectionAnchor = null;
		_selectionCaret = null;
		_hasMouseSelection = false;
	}

	private void DeleteSelectionIfAny()
	{
		if (!HasSelection || _editor == null)
			return;

		var (start, end) = GetSelectionRange();
		_editor.Buffer.Remove(start, end - start);
		_editor.SetCursorIndex(start);
		ClearSelection();
	}

	private void InsertTabSpaces()
	{
		if (!_isReadingLine || _editor == null)
			return;

		int tabWidth = _editor.TabWidth;
		int column = (_inputStartX + _editor.CursorIndex) % tabWidth;
		int spaces = tabWidth - column;

		for (int i = 0; i < spaces; i++)
			_editor.Insert(' ');
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

		if (_selectionAnchor.HasValue &&
			_selectionAnchor.Value > _editor.Length)
		{
			ClearSelection();
		}
	}

	private void RedrawInput()
	{
		if (_editor == null)
			return;

		int x = _inputStartX;
		int y = _inputStartY;

		int index = 0;
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

			index++;

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
		ClearSelection();

		if (_historyIndex < _commandHistory.Count)
			_historyIndex++;

		ApplyHistory();
	}

	private void HistoryDown()
	{
		if (!_isReadingLine)
			return;
		ClearSelection();

		if (_historyIndex > 0)
			_historyIndex--;

		ApplyHistory();
	}

	private void ApplyHistory()
	{
		ClearSelection();
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

		ClearSelection();

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