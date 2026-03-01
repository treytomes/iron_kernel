using System.Text;
using Microsoft.Extensions.Logging;

namespace Userland.Morphic;

public sealed class TextEditingCore
{
	#region Fields
	private readonly ILogger _logger;
	private readonly StringBuilder _buffer;
	#endregion

	#region Events
	/// <summary>
	/// Fired after any mutation to the buffer or cursor.
	/// </summary>
	public event Action? Changed;
	#endregion

	#region Constructors
	public TextEditingCore(ILogger logger, string? initialText = null)
	{
		_logger = logger ?? throw new ArgumentNullException(nameof(logger));
		_buffer = new StringBuilder(initialText ?? string.Empty);
		CursorIndex = _buffer.Length;
	}
	#endregion

	#region Properties
	public int CursorIndex { get; private set; }
	public int Length => _buffer.Length;
	public bool IsEmpty => _buffer.Length == 0;

	/// <summary>
	/// Defines how many spaces a tab represents if expanded.
	/// This class inserts '\t' by default; expansion is optional.
	/// </summary>
	public int TabWidth { get; set; } = 4;
	#endregion

	#region Cursor movement
	public void Move(int delta)
	{
		SetCursorIndex(CursorIndex + delta);
	}

	public void MoveToStart()
	{
		SetCursorIndex(0);
	}

	public void MoveToEnd()
	{
		SetCursorIndex(_buffer.Length);
	}

	public void MoveWordLeft()
	{
		if (CursorIndex == 0)
			return;

		int i = CursorIndex;
		while (i > 0 && !IsWordChar(_buffer[i - 1]))
			i--;
		while (i > 0 && IsWordChar(_buffer[i - 1]))
			i--;

		SetCursorIndex(i);
	}

	public void MoveWordRight()
	{
		int len = _buffer.Length;
		int i = CursorIndex;

		if (i >= len)
			return;

		while (i < len && !IsWordChar(_buffer[i]))
			i++;
		while (i < len && IsWordChar(_buffer[i]))
			i++;

		SetCursorIndex(i);
	}
	#endregion

	#region Editing
	public void Insert(char ch)
	{
		NormalizeCursor();

		_buffer.Insert(CursorIndex, ch);
		CursorIndex++;

		OnChanged();
	}

	// TODO: This method is suspiciously unused.
	public void InsertTab(bool expandToSpaces = false)
	{
		if (!expandToSpaces)
		{
			Insert('\t');
			return;
		}

		int spaces = Math.Max(1, TabWidth);
		_buffer.Insert(CursorIndex, new string(' ', spaces));
		CursorIndex += spaces;

		OnChanged();
	}

	public void Backspace()
	{
		if (CursorIndex == 0)
			return;

		NormalizeCursor();

		_buffer.Remove(CursorIndex - 1, 1);
		CursorIndex--;

		OnChanged();
	}

	public void Delete()
	{
		if (CursorIndex >= _buffer.Length)
			return;

		NormalizeCursor();

		_buffer.Remove(CursorIndex, 1);

		OnChanged();
	}

	public void DeleteWordLeft()
	{
		if (CursorIndex == 0)
			return;

		int start = CursorIndex;
		while (start > 0 && !IsWordChar(_buffer[start - 1]))
			start--;
		while (start > 0 && IsWordChar(_buffer[start - 1]))
			start--;

		int length = CursorIndex - start;
		if (length <= 0)
			return;

		_buffer.Remove(start, length);
		CursorIndex = start;

		OnChanged();
	}

	public void DeleteWordRight()
	{
		int len = _buffer.Length;
		int end = CursorIndex;

		if (end >= len)
			return;

		while (end < len && !IsWordChar(_buffer[end]))
			end++;
		while (end < len && IsWordChar(_buffer[end]))
			end++;

		int length = end - CursorIndex;
		if (length <= 0)
			return;

		_buffer.Remove(CursorIndex, length);

		OnChanged();
	}
	#endregion

	#region Utilities

	public void SetCursorIndex(int index)
	{
		int clamped = Math.Clamp(index, 0, _buffer.Length);
		if (clamped == CursorIndex)
			return;

		CursorIndex = clamped;
		OnChanged();
	}

	public override string ToString()
	{
		return _buffer.ToString();
	}

	public char this[int index] => _buffer[index];

	private void NormalizeCursor()
	{
		if (CursorIndex < 0 || CursorIndex > _buffer.Length)
		{
			// _logger.LogWarning(
			// 	"CursorIndex {CursorIndex} out of range for buffer length {Length}. Normalizing.",
			// 	CursorIndex,
			// 	_buffer.Length);

			CursorIndex = Math.Clamp(CursorIndex, 0, _buffer.Length);
		}
	}

	private void OnChanged()
	{
		NormalizeCursor();
		Changed?.Invoke();
	}

	private static bool IsWordChar(char ch)
	{
		// Identifier-style word definition by design
		return char.IsLetterOrDigit(ch) || ch == '_';
	}

	public void InsertText(string text)
	{
		if (string.IsNullOrEmpty(text))
			return;

		NormalizeCursor();

		_buffer.Insert(CursorIndex, text);
		CursorIndex += text.Length;

		OnChanged();
	}

	/// <summary>
	/// Appends text to the end of the buffer and moves the cursor to the end.
	/// Intended for line-merge operations.
	/// </summary>
	public void AppendText(string text)
	{
		if (string.IsNullOrEmpty(text))
			return;

		_buffer.Append(text);
		CursorIndex = _buffer.Length;

		OnChanged();
	}

	public string GetSubstring(int start, int length)
	{
		if (length <= 0 || start >= _buffer.Length)
			return string.Empty;

		start = Math.Clamp(start, 0, _buffer.Length);
		length = Math.Clamp(length, 0, _buffer.Length - start);

		return _buffer.ToString(start, length);
	}

	public void DeleteRange(int start, int length)
	{
		if (length <= 0)
			return;

		start = Math.Clamp(start, 0, _buffer.Length);
		length = Math.Clamp(length, 0, _buffer.Length - start);

		_buffer.Remove(start, length);

		if (CursorIndex > start)
		{
			CursorIndex = Math.Max(start, CursorIndex - length);
		}

		OnChanged();
	}

	public string SplitAt(int index)
	{
		index = Math.Clamp(index, 0, _buffer.Length);

		string right = _buffer.ToString(index, _buffer.Length - index);
		_buffer.Remove(index, _buffer.Length - index);

		CursorIndex = Math.Min(CursorIndex, _buffer.Length);

		OnChanged();
		return right;
	}

	#endregion
}