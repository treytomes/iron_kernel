using Miniscript;
using ScriptConsole;
using Userland.Scripting;

// Set up the host with an in-memory file system
var fs = new InMemoryFileSystem();
var host = new ConsoleScriptHost(fs);

// Register only the file system intrinsics (no graphics/UI)
FileSystemIntrinsics.Register();

// Build interpreter
var interpreter = new Interpreter();
interpreter.hostData = host;
interpreter.standardOutput = (s, _) => Console.WriteLine(s);
interpreter.errorOutput = (s, _) => Console.Error.WriteLine($"[error] {s}");
interpreter.implicitOutput = (s, _) => Console.WriteLine(s);

Console.WriteLine("IronKernel Script Console");
Console.WriteLine("Type MiniScript expressions, or 'exit' to quit.");
Console.WriteLine();

while (true)
{
    Console.Write("> ");
    var line = Console.ReadLine();

    if (line == null || line.Trim() == "exit")
        break;

    if (string.IsNullOrWhiteSpace(line))
        continue;

    // Feed line to interpreter; run until it needs more input or completes
    interpreter.REPL(line);
    while (interpreter.Running())
        interpreter.RunUntilDone(0.1);

    // Handle any pending run request (from the run() intrinsic)
    if (host.PendingRunSource != null)
    {
        var source = host.PendingRunSource;
        host.PendingRunSource = null;
        interpreter.Reset(source);
        interpreter.Compile();
        while (interpreter.Running())
            interpreter.RunUntilDone(0.1);
    }
}

Console.WriteLine("Bye.");
