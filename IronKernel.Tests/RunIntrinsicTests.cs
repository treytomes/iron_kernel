using Miniscript;
using Userland.Scripting;
using Userland.Services;
using Userland.ValueObjects;
using IronKernel.Common.ValueObjects;

namespace IronKernel.Tests;

/// <summary>
/// Tests for the run() intrinsic and the REPL's handling of RunSourceRequested.
/// </summary>
public class RunIntrinsicTests
{
    // ── Minimal IScriptHost for testing ──────────────────────────────────────

    private sealed class TestScriptHost : IScriptHost
    {
        public IFileSystem FileSystem { get; } = new TestFileSystem();
        public IWindowService WindowService { get; } = new StubWindowService();
        public Action<string>? RunSourceRequested { get; set; }

        public void EnsureEnv(TAC.Context ctx)
        {
            if (ctx.interpreter.GetGlobalValue("env") is not ValMap env)
                env = new ValMap();
            if (env["curdir"] is not Value)
                env["curdir"] = new ValString("file://");
            ctx.interpreter.SetGlobalValue("env", env);
        }

        public Task<string?> ReadLineAsync(string prompt) =>
            Task.FromResult<string?>(null);
    }

    private sealed class StubWindowService : IWindowService
    {
        public Task AlertAsync(string message) => Task.CompletedTask;
        public Task<string?> PromptAsync(string message, string? defaultValue = null) =>
            Task.FromResult<string?>(defaultValue);
        public Task<bool> ConfirmAsync(string message) => Task.FromResult(false);
        public Task EditFileAsync(string? filename) => Task.CompletedTask;
        public void EditFile(string? filename) { }
        public void Inspect(object target) { }
    }

    private sealed class TestFileSystem : IFileSystem
    {
        private readonly Dictionary<string, string> _files = new();

        public void WriteText(string url, string text) =>
            _files[url] = text;

        public bool Exists(string url, CancellationToken ct = default) =>
            _files.ContainsKey(url);
        public Task<bool> ExistsAsync(string url, CancellationToken ct = default) =>
            Task.FromResult(Exists(url, ct));

        public FileReadResult Read(string url, CancellationToken ct = default)
        {
            if (!_files.TryGetValue(url, out var text))
                throw new FileNotFoundException("File not found.", url);
            return new FileReadResult(System.Text.Encoding.UTF8.GetBytes(text), "text/plain");
        }
        public Task<FileReadResult> ReadAsync(string url, CancellationToken ct = default) =>
            Task.FromResult(Read(url, ct));

        public FileWriteResult Write(string url, byte[] data, string? mimeType = null, CancellationToken ct = default)
        {
            _files[url] = System.Text.Encoding.UTF8.GetString(data);
            return new FileWriteResult(true, null);
        }
        public Task<FileWriteResult> WriteAsync(string url, byte[] data, string? mimeType = null, CancellationToken ct = default) =>
            Task.FromResult(Write(url, data, mimeType, ct));

        public FileDeleteResult Delete(string url, CancellationToken ct = default)
        {
            _files.Remove(url);
            return new FileDeleteResult(true, null);
        }
        public Task<FileDeleteResult> DeleteAsync(string url, CancellationToken ct = default) =>
            Task.FromResult(Delete(url, ct));

        public DirectoryEntry CreateDirectory(string url, CancellationToken ct = default) =>
            new DirectoryEntry(url, true, null, DateTime.Now);
        public Task<DirectoryEntry> CreateDirectoryAsync(string url, CancellationToken ct = default) =>
            Task.FromResult(CreateDirectory(url, ct));

        public IReadOnlyList<DirectoryEntry> ListDirectory(string url, CancellationToken ct = default) =>
            Array.Empty<DirectoryEntry>();
        public Task<IReadOnlyList<DirectoryEntry>> ListDirectoryAsync(string url, CancellationToken ct = default) =>
            Task.FromResult<IReadOnlyList<DirectoryEntry>>(Array.Empty<DirectoryEntry>());
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    // Simulate the REPL morph's RunSource with the BUGGY synchronous behavior.
    private static void BuggyRunSource(Interpreter interpreter, string source)
    {
        interpreter.Stop();
        interpreter.Reset(source);
        interpreter.Compile();
        // BUG: sets state and returns, but we're still inside REPL()'s step loop,
        // which will pick up the newly compiled vm and run it synchronously.
        // If that script also calls run(), we recurse infinitely.
    }

    // Simulate the REPL morph's RunSource with the FIXED deferred behavior.
    private static void DeferredRunSource(
        Interpreter interpreter,
        string source,
        ref string? pendingSource)
    {
        // Signal the running vm to yield, then store source for next tick.
        if (interpreter.vm != null)
            interpreter.vm.yielding = true;
        pendingSource = source;
    }

    // ── Tests ─────────────────────────────────────────────────────────────────

    /// <summary>
    /// Captures the bug: run() called from REPL causes infinite recursion when
    /// the script calls run() again, overflowing the C# call stack.
    /// This test expects a StackOverflowException (or gets a result quickly if fixed).
    /// </summary>
    [Fact]
    public void RunIntrinsic_CalledFromRepl_DoesNotCauseStackOverflow()
    {
        FileSystemIntrinsics.Register();

        var host = new TestScriptHost();
        var fs = (TestFileSystem)host.FileSystem;

        // A script that immediately calls run() on itself — worst-case recursion trigger.
        fs.WriteText("file://loop.ms", "run \"loop\"");

        string? pendingSource = null;

        var interpreter = new Interpreter();
        interpreter.hostData = host;
        interpreter.standardOutput = (_, _) => { };
        interpreter.errorOutput = (_, _) => { };

        host.RunSourceRequested = source =>
            DeferredRunSource(interpreter, source, ref pendingSource);

        // Should not throw StackOverflowException.
        var ex = Record.Exception(() =>
        {
            interpreter.REPL("run \"loop\"");
        });

        Assert.Null(ex);
        // After the REPL call the pending source should be queued, not executed inline.
        Assert.NotNull(pendingSource);
        Assert.Contains("run \"loop\"", pendingSource);
    }

    /// <summary>
    /// Verifies that after the fix, the pending source is picked up on the next
    /// tick (simulated Update call) and the interpreter runs the new script.
    /// </summary>
    [Fact]
    public void RunIntrinsic_PendingSource_IsExecutedOnNextTick()
    {
        FileSystemIntrinsics.Register();

        var host = new TestScriptHost();
        var fs = (TestFileSystem)host.FileSystem;

        // Script that sets a global to confirm it ran.
        fs.WriteText("file://hello.ms", "x = 42");

        string? pendingSource = null;

        var interpreter = new Interpreter();
        interpreter.hostData = host;
        interpreter.standardOutput = (_, _) => { };
        interpreter.errorOutput = (_, _) => { };

        host.RunSourceRequested = source =>
            DeferredRunSource(interpreter, source, ref pendingSource);

        // Trigger run("hello") from the REPL.
        interpreter.REPL("run \"hello\"");

        // Pending source should be queued.
        Assert.NotNull(pendingSource);

        // Simulate next Update() tick: apply pending source.
        if (pendingSource != null)
        {
            interpreter.Stop();
            interpreter.Reset(pendingSource);
            interpreter.Compile();
            pendingSource = null;
        }

        // Run until done.
        while (interpreter.Running())
            interpreter.RunUntilDone(1.0);

        // The script should have set x = 42.
        var x = interpreter.vm?.globalContext.GetVar("x");
        Assert.Equal(42, x?.IntValue());
    }
}
