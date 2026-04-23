using IronKernel.Common.ValueObjects;

namespace Userland.Morphic;

public sealed class MiniScriptHighlighter : ISyntaxHighlighter
{
	private static readonly HashSet<string> Keywords = new()
	{
		"if", "else", "then", "for", "while",
		"function", "return", "end"
	};

	public RadialColor? GetForeground(
		TextDocument document,
		int line,
		int column)
	{
		string text = document.Lines[line].ToString();
		if (column < 0 || column >= text.Length)
			return null;

		GetLineState(text, column, out bool inString, out bool inComment);

		// ----- Line comment
		if (inComment)
			return RadialColor.Green;

		// ----- String literal
		if (inString)
			return RadialColor.Orange;

		// ----- Keywords
		if (char.IsLetter(text[column]))
		{
			int start = column;
			while (start > 0 && char.IsLetter(text[start - 1]))
				start--;

			int end = column;
			while (end < text.Length && char.IsLetter(text[end]))
				end++;

			string word = text.Substring(start, end - start);
			if (Keywords.Contains(word))
				return RadialColor.Yellow;
		}

		// ----- Numbers
		if (IsNumberAt(text, column))
			return RadialColor.Cyan;

		return null;
	}

	private static bool IsNumberAt(string text, int column)
	{
		char ch = text[column];
		if (!char.IsDigit(ch))
			return false;

		// Must not be part of an identifier
		if (column > 0 && char.IsLetter(text[column - 1]))
			return false;
		if (column + 1 < text.Length && char.IsLetter(text[column + 1]))
			return false;

		// Walk left
		int i = column;
		while (i > 0 && char.IsDigit(text[i - 1]))
			i--;

		// Optional single decimal point
		if (i > 0 && text[i - 1] == '.')
		{
			int dot = i - 1;
			if (dot > 0 && char.IsDigit(text[dot - 1]))
				i = dot - 1;
		}

		// Walk right
		i = column;
		while (i + 1 < text.Length && char.IsDigit(text[i + 1]))
			i++;

		if (i + 1 < text.Length && text[i + 1] == '.')
		{
			int dot = i + 1;
			if (dot + 1 < text.Length && char.IsDigit(text[dot + 1]))
			{
				i = dot + 1;
				while (i + 1 < text.Length && char.IsDigit(text[i + 1]))
					i++;
			}
		}

		return true;
	}

	private static void GetLineState(
		string text,
		int targetColumn,
		out bool inString,
		out bool inComment)
	{
		inString = false;
		inComment = false;

		for (int i = 0; i <= targetColumn && i < text.Length; i++)
		{
			if (inComment)
				return;

			if (!inString &&
				i + 1 < text.Length &&
				text[i] == '/' &&
				text[i + 1] == '/')
			{
				inComment = true;
				return;
			}

			if (i < targetColumn &&
				text[i] == '"' &&
				(i == 0 || text[i - 1] != '\\'))
			{
				inString = !inString;
			}
		}
	}
}