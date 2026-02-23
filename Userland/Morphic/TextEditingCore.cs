using System.Text;
using Microsoft.Extensions.Logging;

namespace Userland.Morphic;

public sealed class TextEditingCore
{
	#region Fields

	private readonly ILogger _logger;
	private readonly StringBuilder _buffer = new();

	#endregion

	#region Constructors

	public TextEditingCore(ILogger logger, string? initialText = null)
	{
		_logger = logger;
		if (!string.IsNullOrEmpty(initialText))
		{
			_buffer.Append(initialText);
			CursorIndex = _buffer.Length;
		}
	}

	#endregion

	#region Properties

	public StringBuilder Buffer => _buffer;
	public int CursorIndex { get; private set; }
	public int Length => _buffer.Length;
	public bool IsEmpty => _buffer.Length == 0;
	public int TabWidth { get; set; } = 4;

	#endregion

	#region Methods

	/* ---------------- Cursor movement ---------------- */

	public void Move(int delta)
	{
		CursorIndex = Math.Clamp(CursorIndex + delta, 0, _buffer.Length);
	}

	public void MoveToStart()
	{
		CursorIndex = 0;
	}

	public void MoveToEnd()
	{
		CursorIndex = _buffer.Length;
	}

	public void MoveWordLeft()
	{
		if (CursorIndex == 0)
			return;

		int i = CursorIndex;

		// Skip separators
		while (i > 0 && !IsWordChar(_buffer[i - 1]))
			i--;

		// Skip word chars
		while (i > 0 && IsWordChar(_buffer[i - 1]))
			i--;

		CursorIndex = i;
	}

	public void MoveWordRight()
	{
		int len = _buffer.Length;
		int i = CursorIndex;

		if (i >= len)
			return;

		// Skip separators
		while (i < len && !IsWordChar(_buffer[i]))
			i++;

		// Skip word chars
		while (i < len && IsWordChar(_buffer[i]))
			i++;

		CursorIndex = i;
	}

	/* ---------------- Editing ---------------- */

	public void Insert(char ch)
	{
		if (CursorIndex < 0) CursorIndex = 0;
		if (CursorIndex < _buffer.Length)
			_buffer.Insert(CursorIndex, ch);
		else
			_buffer.Append(ch);
		CursorIndex++;
	}

	public void InsertTab()
	{
		Insert('\t');
	}

	public void Backspace()
	{
		if (CursorIndex == 0)
			return;

		try
		{
			_buffer.Remove(CursorIndex - 1, 1);
			CursorIndex--;
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Failed to backspace from {CursorIndex} with length {Length}.", CursorIndex, _buffer.Length);
		}
	}

	public void Delete()
	{
		if (CursorIndex >= _buffer.Length)
			return;

		_buffer.Remove(CursorIndex, 1);
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
	}

	public void SetCursorIndex(int index)
	{
		CursorIndex = Math.Clamp(index, 0, _buffer.Length);
	}

	/* ---------------- Utilities ---------------- */

	public override string ToString()
	{
		return _buffer.ToString();
	}

	private static bool IsWordChar(char ch)
	{
		return char.IsLetterOrDigit(ch) || ch == '_';
	}

	#endregion
}