namespace IronKernel.Userland.Morphic;

/// <summary>
/// Logical multi-line text document.
/// Owns lines, caret position, and cross-line editing semantics.
/// Rendering and projection are handled elsewhere.
/// </summary>
public sealed class TextDocument
{
	private readonly List<TextEditingCore> _lines = new();

	#region Construction

	public TextDocument(string? initialText = null)
	{
		SetText(initialText);
	}

	#endregion

	#region Properties

	public int LineCount => _lines.Count;

	public int CaretLine { get; private set; }

	public int CaretColumn
	{
		get => _lines[CaretLine].CursorIndex;
		set => _lines[CaretLine].SetCursorIndex(Math.Clamp(value, 0, _lines[CaretLine].Length));
	}

	public TextEditingCore CurrentLine => _lines[CaretLine];

	public IReadOnlyList<TextEditingCore> Lines => _lines;

	#endregion

	#region Insertion

	public void SetText(string? text = null)
	{
		if (string.IsNullOrEmpty(text))
		{
			_lines.Add(new TextEditingCore());
			CaretLine = 0;
			return;
		}

		var split = text.Split('\n');
		foreach (var line in split)
			_lines.Add(new TextEditingCore(line));

		if (_lines.Count == 0)
			_lines.Add(new TextEditingCore());

		CaretLine = _lines.Count - 1;
		_lines[CaretLine].MoveToEnd();
	}

	public void InsertChar(char ch)
	{
		if (ch == '\n')
		{
			SplitLine();
			return;
		}

		CurrentLine.Insert(ch);
	}

	#endregion

	#region Deletion

	public void Backspace()
	{
		var line = CurrentLine;

		if (line.CursorIndex > 0)
		{
			line.Backspace();
			return;
		}

		// At start of line: merge with previous
		if (CaretLine == 0)
			return;

		var prev = _lines[CaretLine - 1];
		int prevLen = prev.Length;

		prev.Buffer.Append(line.Buffer);
		_lines.RemoveAt(CaretLine);

		CaretLine--;
		prev.SetCursorIndex(prevLen);
	}

	public void Delete()
	{
		var line = CurrentLine;

		if (line.CursorIndex < line.Length)
		{
			line.Delete();
			return;
		}

		// At end of line: merge with next
		if (CaretLine >= _lines.Count - 1)
			return;

		var next = _lines[CaretLine + 1];
		line.Buffer.Append(next.Buffer);
		_lines.RemoveAt(CaretLine + 1);
	}

	#endregion

	#region Word Deletion

	public void DeleteWordLeft()
	{
		if (CurrentLine.CursorIndex > 0)
		{
			CurrentLine.DeleteWordLeft();
			return;
		}

		Backspace();
	}

	public void DeleteWordRight()
	{
		if (CurrentLine.CursorIndex < CurrentLine.Length)
		{
			CurrentLine.DeleteWordRight();
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
			return;
		}

		if (CaretLine == 0)
			return;

		CaretLine--;
		_lines[CaretLine].MoveToEnd();
	}

	public void MoveRight()
	{
		if (CurrentLine.CursorIndex < CurrentLine.Length)
		{
			CurrentLine.Move(1);
			return;
		}

		if (CaretLine >= _lines.Count - 1)
			return;

		CaretLine++;
		_lines[CaretLine].MoveToStart();
	}

	public void MoveWordLeft()
	{
		if (CurrentLine.CursorIndex > 0)
		{
			CurrentLine.MoveWordLeft();
			return;
		}

		MoveLeft();
	}

	public void MoveWordRight()
	{
		if (CurrentLine.CursorIndex < CurrentLine.Length)
		{
			CurrentLine.MoveWordRight();
			return;
		}

		MoveRight();
	}

	public void MoveToLineStart()
	{
		CurrentLine.MoveToStart();
	}

	public void MoveToLineEnd()
	{
		CurrentLine.MoveToEnd();
	}

	#endregion

	#region Cursor Movement (Vertical)

	public void SetCaretLine(int line)
	{
		CaretLine = Math.Clamp(line, 0, LineCount - 1);
		CaretColumn = Math.Min(CaretColumn, _lines[CaretLine].Length);
	}

	public void MoveUp()
	{
		if (CaretLine == 0)
			return;

		int desiredColumn = CaretColumn;
		CaretLine--;

		CaretColumn = Math.Min(
			desiredColumn,
			_lines[CaretLine].Length
		);
	}

	public void MoveDown()
	{
		if (CaretLine >= _lines.Count - 1)
			return;

		int desiredColumn = CaretColumn;
		CaretLine++;

		CaretColumn = Math.Min(
			desiredColumn,
			_lines[CaretLine].Length
		);
	}

	#endregion

	#region Line Operations

	private void SplitLine()
	{
		var line = CurrentLine;
		int index = line.CursorIndex;

		var rightText = line.Buffer
			.ToString(index, line.Length - index);

		line.Buffer.Remove(index, line.Length - index);

		var newLine = new TextEditingCore(rightText);

		_lines.Insert(CaretLine + 1, newLine);
		CaretLine++;
		newLine.MoveToStart();
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