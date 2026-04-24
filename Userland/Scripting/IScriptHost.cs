using Miniscript;
using Userland.Services;

namespace Userland.Scripting;

/// <summary>
/// The minimal surface the file system intrinsics need from their host environment.
/// Implemented by WorldScriptContext in the full application and by the console
/// harness for testing.
/// </summary>
public interface IScriptHost
{
	IFileSystem FileSystem { get; }
	IWindowService WindowService { get; }
	/// <summary>
	/// Called by the <c>run()</c> intrinsic to hand off a loaded script source
	/// to whoever owns the interpreter (typically the REPL morph).
	/// </summary>
	Action<string>? RunSourceRequested { get; set; }

	/// <summary>
	/// Called by the <c>cls</c> intrinsic to clear the active console output.
	/// </summary>
	Action? ClearOutputRequested { get; set; }
	void EnsureEnv(TAC.Context ctx);
	Task<string?> ReadLineAsync(string prompt);
}
