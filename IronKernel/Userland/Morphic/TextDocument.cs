namespace IronKernel.Userland.Morphic;

/// <summary>
/// Logical multi-line text document.
/// Owns lines, caret position, and cross-line editing semantics.
/// Rendering and projection are handled elsewhere.
/// </summary>
public sealed class TextDocument
{
	private readonly List<TextEditingCore> _lines = new();

	private int _desiredColumn = -1;

	#region Construction

	public TextDocument(string? initialText = null)
	{
		SetText(initialText);
	}

	#endregion

	#region Events

	public event Action? Changed;

	private void OnChanged() => Changed?.Invoke();

	#endregion

	#region Properties

	public int LineCount => _lines.Count;

	public int CaretLine { get; private set; }

	public int CaretColumn
	{
		get => _lines[CaretLine].CursorIndex;
		set
		{
			var line = _lines[CaretLine];
			line.SetCursorIndex(Math.Clamp(value, 0, line.Length));
		}
	}

	public TextEditingCore CurrentLine => _lines[CaretLine];

	public IReadOnlyList<TextEditingCore> Lines => _lines;

	public int TabWidth { get; set; } = 4;

	#endregion

	#region Text Initialization

	public void SetText(string? text = null)
	{
		_lines.Clear();

		if (string.IsNullOrEmpty(text))
		{
			_lines.Add(new TextEditingCore());
			CaretLine = 0;
			CaretColumn = 0;
			OnChanged();
			return;
		}

		var split = text.Split('\n');
		foreach (var line in split)
			_lines.Add(new TextEditingCore(line));

		if (_lines.Count == 0)
			_lines.Add(new TextEditingCore());

		CaretLine = _lines.Count - 1;
		_lines[CaretLine].MoveToEnd();
		OnChanged();
	}

	#endregion

	#region Insertion

	public void InsertChar(char ch)
	{
		if (ch == '\n')
		{
			SplitLine();
			OnChanged();
			return;
		}

		CurrentLine.Insert(ch);
		_desiredColumn = -1;
		OnChanged();
	}

	public void InsertTab()
	{
		int col = CurrentLine.CursorIndex;
		int spaces = TabWidth - (col % TabWidth);
		for (int i = 0; i < spaces; i++)
			CurrentLine.Insert(' ');

		_desiredColumn = -1;
		OnChanged();
	}

	#endregion

	#region Deletion

	public void Backspace()
	{
		var line = CurrentLine;

		if (line.CursorIndex > 0)
		{
			line.Backspace();
			_desiredColumn = -1;
			OnChanged();
			return;
		}

		if (CaretLine == 0)
			return;

		var prev = _lines[CaretLine - 1];
		int prevLen = prev.Length;

		prev.Buffer.Append(line.Buffer);
		_lines.RemoveAt(CaretLine);
		CaretLine--;
		prev.SetCursorIndex(prevLen);

		_desiredColumn = -1;
		OnChanged();
	}

	public void Delete()
	{
		var line = CurrentLine;

		if (line.CursorIndex < line.Length)
		{
			line.Delete();
			_desiredColumn = -1;
			OnChanged();
			return;
		}

		if (CaretLine >= _lines.Count - 1)
			return;

		var next = _lines[CaretLine + 1];
		line.Buffer.Append(next.Buffer);
		_lines.RemoveAt(CaretLine + 1);

		CaretColumn = Math.Min(CaretColumn, line.Length);
		_desiredColumn = -1;
		OnChanged();
	}

	#endregion

	#region Word Deletion

	public void DeleteWordLeft()
	{
		if (CurrentLine.CursorIndex > 0)
		{
			CurrentLine.DeleteWordLeft();
			OnChanged();
			return;
		}

		Backspace();
	}

	public void DeleteWordRight()
	{
		if (CurrentLine.CursorIndex < CurrentLine.Length)
		{
			CurrentLine.DeleteWordRight();
			OnChanged();
			return;
		}

		Delete();
	}

	#endregion

	#region Cursor Movement (Horizontal)

	public void MoveLeft()
	{
		if (CurrentLine.CursorIndex > 0)
		{
			CurrentLine.Move(-1);
			_desiredColumn = -1;
			return;
		}

		if (CaretLine == 0)
			return;

		CaretLine--;
		_lines[CaretLine].MoveToEnd();
		_desiredColumn = -1;
	}

	public void MoveRight()
	{
		if (CurrentLine.CursorIndex < CurrentLine.Length)
		{
			CurrentLine.Move(1);
			_desiredColumn = -1;
			return;
		}

		if (CaretLine >= _lines.Count - 1)
			return;

		CaretLine++;
		_lines[CaretLine].MoveToStart();
		_desiredColumn = -1;
	}

	public void MoveWordLeft()
	{
		if (CurrentLine.CursorIndex > 0)
		{
			CurrentLine.MoveWordLeft();
			_desiredColumn = -1;
			return;
		}

		MoveLeft();
	}

	public void MoveWordRight()
	{
		if (CurrentLine.CursorIndex < CurrentLine.Length)
		{
			CurrentLine.MoveWordRight();
			_desiredColumn = -1;
			return;
		}

		MoveRight();
	}

	public void MoveToLineStart()
	{
		CurrentLine.MoveToStart();
		_desiredColumn = -1;
	}

	public void MoveToLineEnd()
	{
		CurrentLine.MoveToEnd();
		_desiredColumn = -1;
	}

	#endregion

	#region Cursor Movement (Vertical)

	public void SetCaretLine(int line)
	{
		CaretLine = Math.Clamp(line, 0, LineCount - 1);
		CaretColumn = Math.Min(CaretColumn, _lines[CaretLine].Length);
		_desiredColumn = CaretColumn;
	}

	public void MoveUp()
	{
		if (CaretLine == 0)
			return;

		if (_desiredColumn < 0)
			_desiredColumn = CaretColumn;

		CaretLine--;
		CaretColumn = Math.Min(_desiredColumn, _lines[CaretLine].Length);
	}

	public void MoveDown()
	{
		if (CaretLine >= _lines.Count - 1)
			return;

		if (_desiredColumn < 0)
			_desiredColumn = CaretColumn;

		CaretLine++;
		CaretColumn = Math.Min(_desiredColumn, _lines[CaretLine].Length);
	}

	#endregion

	#region Line Operations

	private void SplitLine()
	{
		var line = CurrentLine;
		int index = line.CursorIndex;

		var rightText = line.Buffer.ToString(index, line.Length - index);
		line.Buffer.Remove(index, line.Length - index);

		var newLine = new TextEditingCore(rightText);
		_lines.Insert(CaretLine + 1, newLine);

		CaretLine++;
		newLine.MoveToStart();
		_desiredColumn = -1;
	}

	#endregion

	#region Utilities

	public override string ToString()
	{
		return string.Join(
			"\n",
			_lines.Select(l => l.ToString())
		);
	}

	#endregion
}