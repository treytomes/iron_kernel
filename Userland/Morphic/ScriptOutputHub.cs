using System.Text;
using Miniscript;

namespace IronKernel.Userland.Morphic;

public sealed class ScriptOutputHub
{
	public readonly StringBuilder StdOut = new();
	public readonly StringBuilder Implicit = new();
	public readonly StringBuilder Errors = new();

	public void Clear()
	{
		StdOut.Clear();
		Implicit.Clear();
		Errors.Clear();
	}

	public void Attach(Interpreter interpreter)
	{
		interpreter.standardOutput = (text, newline) =>
		{
			if (newline)
			{
				StdOut.AppendLine(text);
			}
			else
			{
				StdOut.Append(text);
			}
		};

		interpreter.implicitOutput = (text, newline) =>
		{
			if (newline)
			{
				Implicit.AppendLine(text);
			}
			else
			{
				Implicit.Append(text);
			}
		};

		interpreter.errorOutput = (text, newline) =>
		{
			if (newline)
			{
				Errors.AppendLine(text);
			}
			else
			{
				Errors.Append(text);
			}
		};

	}
}