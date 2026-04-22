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
	string? PendingRunSource { get; set; }
	void EnsureEnv(TAC.Context ctx);
	Task<string?> ReadLineAsync(string prompt);
}
