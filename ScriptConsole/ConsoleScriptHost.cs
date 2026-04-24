using Miniscript;
using Userland.Scripting;
using Userland.Services;

namespace ScriptConsole;

/// <summary>
/// Console implementation of IScriptHost. Provides a real (or in-memory)
/// file system, stub window service, and env/curdir tracking.
/// </summary>
public sealed class ConsoleScriptHost : IScriptHost
{
    public IFileSystem FileSystem { get; }
    public IWindowService WindowService { get; } = new StubWindowService();
    public Action<string>? RunSourceRequested { get; set; }
    public Action? ClearOutputRequested { get; set; }

    public ConsoleScriptHost(IFileSystem fileSystem)
    {
        FileSystem = fileSystem;
    }

    public Task<string?> ReadLineAsync(string prompt)
    {
        Console.Write(prompt);
        return Task.FromResult<string?>(Console.ReadLine());
    }

    public void EnsureEnv(TAC.Context ctx)
    {
        if (ctx.interpreter.GetGlobalValue("env") is not ValMap env)
            env = new ValMap();

        if (env["curdir"] is not Value)
            env["curdir"] = new ValString("file://");

        ctx.interpreter.SetGlobalValue("env", env);
    }

    // ── Stub window service ───────────────────────────────────────────────────

    private sealed class StubWindowService : IWindowService
    {
        public Task AlertAsync(string message)
        {
            Console.WriteLine($"[alert] {message}");
            return Task.CompletedTask;
        }

        public Task<string?> PromptAsync(string message, string? defaultValue = null)
        {
            Console.Write($"[prompt] {message} [{defaultValue}]: ");
            var line = Console.ReadLine();
            return Task.FromResult(string.IsNullOrEmpty(line) ? defaultValue : line);
        }

        public Task<bool> ConfirmAsync(string message)
        {
            Console.Write($"[confirm] {message} (y/n): ");
            var line = Console.ReadLine();
            return Task.FromResult(line?.Trim().ToLower() == "y");
        }

        public Task EditFileAsync(string? filename)
        {
            Console.WriteLine($"[edit] {filename} (not supported in console)");
            return Task.CompletedTask;
        }

        public void EditFile(string? filename) =>
            Console.WriteLine($"[edit] {filename} (not supported in console)");

        public void Inspect(object target) =>
            Console.WriteLine($"[inspect] {target}");
    }
}
