using Microsoft.Extensions.Logging;

namespace Userland.Morphic;

/// <summary>
/// Logical multi-line text document.
/// Owns lines, caret position, and cross-line editing semantics.
/// Rendering and projection are handled elsewhere.
/// </summary>
public sealed class TextDocument
{
	#region Events
	public event Action? Changed;
	#endregion

	#region Fields
	private readonly ILogger _logger;
	private readonly List<TextEditingCore> _lines = new();
	private int _desiredColumn = -1;
	#endregion

	#region Constructors
	public TextDocument(ILogger logger, string? initialText = null)
	{
		_logger = logger ?? throw new ArgumentNullException(nameof(logger));
		SetText(initialText);
	}
	#endregion

	#region Properties
	public int LineCount => _lines.Count;
	public int CaretLine { get; private set; }

	public int CaretColumn
	{
		get => _lines[CaretLine].CursorIndex;
		private set => _lines[CaretLine].SetCursorIndex(value);
	}

	public TextEditingCore CurrentLine => _lines[CaretLine];
	public IReadOnlyList<TextEditingCore> Lines => _lines;
	public int TabWidth { get; set; } = 4;
	#endregion

	#region Core helpers
	private void OnChanged() => Changed?.Invoke();

	private void ResetDesiredColumn() => _desiredColumn = -1;

	private void EnsureDesiredColumn()
	{
		if (_desiredColumn < 0)
			_desiredColumn = CaretColumn;
	}
	#endregion

	#region Text initialization
	public void SetText(string? text = null)
	{
		_lines.Clear();

		if (string.IsNullOrEmpty(text))
		{
			_lines.Add(new TextEditingCore(_logger));
		}
		else
		{
			foreach (var line in text.Split('\n'))
				_lines.Add(new TextEditingCore(_logger, line));
		}

		CaretLine = _lines.Count - 1;
		CurrentLine.MoveToEnd();
		ResetDesiredColumn();
		OnChanged();
	}
	#endregion

	#region Insertion
	public void InsertChar(char ch)
	{
		if (ch == '\n')
		{
			SplitLine();
			return;
		}

		CurrentLine.Insert(ch);
		ResetDesiredColumn();
		OnChanged();
	}

	public void InsertTab()
	{
		CurrentLine.TabWidth = TabWidth;
		CurrentLine.InsertTab(expandToSpaces: true);

		ResetDesiredColumn();
		OnChanged();
	}
	#endregion

	#region Deletion
	public void Backspace()
	{
		if (CaretColumn > 0)
		{
			CurrentLine.Backspace();
			ResetDesiredColumn();
			OnChanged();
			return;
		}

		if (CaretLine == 0)
			return;

		var current = CurrentLine;
		var prev = _lines[CaretLine - 1];

		int prevLen = prev.Length;
		prev.AppendText(current.ToString());

		_lines.RemoveAt(CaretLine);
		CaretLine--;
		prev.SetCursorIndex(prevLen);

		ResetDesiredColumn();
		OnChanged();
	}

	public void Delete()
	{
		var line = CurrentLine;

		if (CaretColumn < line.Length)
		{
			line.Delete();
			ResetDesiredColumn();
			OnChanged();
			return;
		}

		if (CaretLine >= _lines.Count - 1)
			return;

		var next = _lines[CaretLine + 1];
		line.AppendText(next.ToString());
		_lines.RemoveAt(CaretLine + 1);

		ResetDesiredColumn();
		OnChanged();
	}
	#endregion

	#region Word deletion
	public void DeleteWordLeft()
	{
		if (CaretColumn > 0)
		{
			CurrentLine.DeleteWordLeft();
			OnChanged();
		}
		else
		{
			Backspace();
		}
	}

	public void DeleteWordRight()
	{
		if (CaretColumn < CurrentLine.Length)
		{
			CurrentLine.DeleteWordRight();
			OnChanged();
		}
		else
		{
			Delete();
		}
	}

	public void DeleteRangeAndSetCaret((int line, int column) start, (int line, int column) end)
	{
		DeleteRange(start, end);

		CaretLine = Math.Clamp(start.line, 0, _lines.Count - 1);
		_lines[CaretLine].SetCursorIndex(start.column);
	}

	public void DeleteRange((int line, int column) start, (int line, int column) end)
	{
		// Normalize order
		if (start.line > end.line ||
			(start.line == end.line && start.column > end.column))
		{
			(start, end) = (end, start);
		}

		start.line = Math.Clamp(start.line, 0, _lines.Count - 1);
		end.line = Math.Clamp(end.line, 0, _lines.Count - 1);

		var startLine = _lines[start.line];
		var endLine = _lines[end.line];

		start.column = Math.Clamp(start.column, 0, startLine.Length);
		end.column = Math.Clamp(end.column, 0, endLine.Length);

		// ----- Single-line delete -----
		if (start.line == end.line)
		{
			startLine.DeleteRange(
				start.column,
				end.column - start.column);

			OnChanged();
			return;
		}

		// ----- Multi-line delete -----

		// Capture tail of end line
		string endTail = endLine.GetSubstring(
			end.column,
			endLine.Length - end.column);

		// Truncate start line
		startLine.DeleteRange(
			start.column,
			startLine.Length - start.column);

		// Remove intermediate lines (including end line)
		for (int i = end.line; i > start.line; i--)
			_lines.RemoveAt(i);

		// Append tail
		startLine.AppendText(endTail);

		OnChanged();
	}

	public void SetCaretLine(int line)
	{
		CaretLine = Math.Clamp(line, 0, _lines.Count - 1);

		var currentLine = _lines[CaretLine];

		// Clamp existing column to new line length
		currentLine.SetCursorIndex(
			Math.Min(
				currentLine.CursorIndex,
				currentLine.Length));
	}

	#endregion

	#region Cursor movement (horizontal)
	public void MoveLeft()
	{
		if (CaretColumn > 0)
		{
			CurrentLine.Move(-1);
		}
		else if (CaretLine > 0)
		{
			CaretLine--;
			_lines[CaretLine].MoveToEnd();
		}

		ResetDesiredColumn();
		OnChanged();
	}

	public void MoveRight()
	{
		if (CaretColumn < CurrentLine.Length)
		{
			CurrentLine.Move(1);
		}
		else if (CaretLine < _lines.Count - 1)
		{
			CaretLine++;
			_lines[CaretLine].MoveToStart();
		}

		ResetDesiredColumn();
		OnChanged();
	}

	public void MoveToLineStart()
	{
		CurrentLine.MoveToStart();
		ResetDesiredColumn();
		OnChanged();
	}

	public void MoveToLineEnd()
	{
		CurrentLine.MoveToEnd();
		ResetDesiredColumn();
		OnChanged();
	}
	#endregion

	#region Cursor movement (vertical)
	public void MoveUp()
	{
		if (CaretLine == 0)
			return;

		EnsureDesiredColumn();
		CaretLine--;
		CaretColumn = Math.Min(_desiredColumn, CurrentLine.Length);
		OnChanged();
	}

	public void MoveDown()
	{
		if (CaretLine >= _lines.Count - 1)
			return;

		EnsureDesiredColumn();
		CaretLine++;
		CaretColumn = Math.Min(_desiredColumn, CurrentLine.Length);
		OnChanged();
	}
	#endregion

	#region Line operations
	private void SplitLine()
	{
		var line = CurrentLine;
		int index = line.CursorIndex;

		string right = line.SplitAt(index);
		var newLine = new TextEditingCore(_logger, right);

		_lines.Insert(CaretLine + 1, newLine);
		CaretLine++;
		newLine.MoveToStart();

		ResetDesiredColumn();
		OnChanged();
	}
	#endregion

	#region Utilities
	public override string ToString()
	{
		return string.Join("\n", _lines.Select(l => l.ToString()));
	}
	#endregion

	public void MoveWordLeft()
	{
		var line = CurrentLine;

		if (line.CursorIndex > 0)
		{
			line.MoveWordLeft();
			ResetDesiredColumn();
			OnChanged();
			return;
		}

		// At start of line: move to previous line
		if (CaretLine == 0)
			return;

		CaretLine--;
		_lines[CaretLine].MoveToEnd();

		ResetDesiredColumn();
		OnChanged();
	}

	public void MoveWordRight()
	{
		var line = CurrentLine;

		if (line.CursorIndex < line.Length)
		{
			line.MoveWordRight();
			ResetDesiredColumn();
			OnChanged();
			return;
		}

		// At end of line: move to next line
		if (CaretLine >= _lines.Count - 1)
			return;

		CaretLine++;
		_lines[CaretLine].MoveToStart();

		ResetDesiredColumn();
		OnChanged();
	}
}