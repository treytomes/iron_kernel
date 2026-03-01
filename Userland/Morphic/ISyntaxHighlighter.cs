using IronKernel.Common.ValueObjects;

namespace Userland.Morphic;

public interface ISyntaxHighlighter
{
	/// <summary>
	/// Returns a color for the character at (line, column),
	/// or null to use the default text color.
	/// </summary>
	RadialColor? GetForeground(
		TextDocument document,
		int line,
		int column);
}
