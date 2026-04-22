using Userland.Morphic.Commands;

namespace IronKernel.Tests;

public class CommandHistoryTests
{
    private static FakeCommand MakeCommand(List<string> log, string name) =>
        new(log, name);

    // ── Initial state ─────────────────────────────────────────────────────────

    [Fact]
    public void Initial_CannotUndoOrRedo()
    {
        var history = new CommandHistory();
        Assert.False(history.CanUndo);
        Assert.False(history.CanRedo);
    }

    // ── Record / Undo / Redo ──────────────────────────────────────────────────

    [Fact]
    public void Record_EnablesUndo()
    {
        var history = new CommandHistory();
        history.Record(MakeCommand([], "a"));
        Assert.True(history.CanUndo);
    }

    [Fact]
    public void Undo_CallsUndoOnCommand()
    {
        var log = new List<string>();
        var history = new CommandHistory();
        history.Record(MakeCommand(log, "a"));
        history.Undo();
        Assert.Equal(["undo:a"], log);
    }

    [Fact]
    public void Undo_MovesCommandToRedoStack()
    {
        var history = new CommandHistory();
        history.Record(MakeCommand([], "a"));
        history.Undo();
        Assert.False(history.CanUndo);
        Assert.True(history.CanRedo);
    }

    [Fact]
    public void Redo_CallsExecuteOnCommand()
    {
        var log = new List<string>();
        var history = new CommandHistory();
        history.Record(MakeCommand(log, "a"));
        history.Undo();
        log.Clear();
        history.Redo();
        Assert.Equal(["execute:a"], log);
    }

    [Fact]
    public void Redo_MovesCommandBackToUndoStack()
    {
        var history = new CommandHistory();
        history.Record(MakeCommand([], "a"));
        history.Undo();
        history.Redo();
        Assert.True(history.CanUndo);
        Assert.False(history.CanRedo);
    }

    [Fact]
    public void Undo_WhenEmpty_DoesNotThrow()
    {
        var history = new CommandHistory();
        history.Undo(); // no-op
    }

    [Fact]
    public void Redo_WhenEmpty_DoesNotThrow()
    {
        var history = new CommandHistory();
        history.Redo(); // no-op
    }

    // ── Redo stack cleared on new Record ─────────────────────────────────────

    [Fact]
    public void Record_AfterUndo_ClearsRedoStack()
    {
        var history = new CommandHistory();
        history.Record(MakeCommand([], "a"));
        history.Undo();
        Assert.True(history.CanRedo);

        history.Record(MakeCommand([], "b"));
        Assert.False(history.CanRedo);
    }

    // ── Batch Record ─────────────────────────────────────────────────────────

    [Fact]
    public void RecordBatch_AllCommandsUndoneInOrder()
    {
        var log = new List<string>();
        var history = new CommandHistory();
        history.Record([MakeCommand(log, "a"), MakeCommand(log, "b"), MakeCommand(log, "c")]);

        history.Undo();
        history.Undo();
        history.Undo();

        // Pushed in order a,b,c so stack is [a,b,c] (c on top); undo order: c,b,a
        Assert.Equal(["undo:c", "undo:b", "undo:a"], log);
    }

    // ── LIFO order ────────────────────────────────────────────────────────────

    [Fact]
    public void Undo_IsLIFO()
    {
        var log = new List<string>();
        var history = new CommandHistory();
        history.Record(MakeCommand(log, "first"));
        history.Record(MakeCommand(log, "second"));
        history.Undo();
        Assert.Equal(["undo:second"], log);
    }

    // ── Helper ────────────────────────────────────────────────────────────────

    private sealed class FakeCommand(List<string> log, string name) : ICommand
    {
        public bool CanExecute() => true;
        public void Execute() => log.Add($"execute:{name}");
        public bool CanUndo() => true;
        public void Undo() => log.Add($"undo:{name}");
    }
}
