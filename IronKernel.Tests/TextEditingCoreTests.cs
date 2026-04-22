using Microsoft.Extensions.Logging.Abstractions;
using Userland.Morphic;

namespace IronKernel.Tests;

public class TextEditingCoreTests
{
    private static TextEditingCore Make(string text = "") =>
        new(NullLogger.Instance, text);

    // ── Construction ──────────────────────────────────────────────────────────

    [Fact]
    public void Constructor_EmptyText_CursorAtZero()
    {
        var core = Make();
        Assert.Equal(0, core.CursorIndex);
        Assert.Equal(0, core.Length);
        Assert.True(core.IsEmpty);
    }

    [Fact]
    public void Constructor_WithText_CursorAtEnd()
    {
        var core = Make("hello");
        Assert.Equal(5, core.CursorIndex);
        Assert.Equal(5, core.Length);
    }

    // ── Cursor movement ───────────────────────────────────────────────────────

    [Fact]
    public void MoveToStart_SetsCursorToZero()
    {
        var core = Make("hello");
        core.MoveToStart();
        Assert.Equal(0, core.CursorIndex);
    }

    [Fact]
    public void MoveToEnd_SetsCursorToLength()
    {
        var core = Make("hello");
        core.MoveToStart();
        core.MoveToEnd();
        Assert.Equal(5, core.CursorIndex);
    }

    [Fact]
    public void Move_ClampedToBufferBounds()
    {
        var core = Make("abc");
        core.MoveToStart();
        core.Move(-10);
        Assert.Equal(0, core.CursorIndex);

        core.MoveToEnd();
        core.Move(10);
        Assert.Equal(3, core.CursorIndex);
    }

    [Fact]
    public void MoveWordRight_SkipsToStartOfNextWord()
    {
        var core = Make("foo bar");
        core.MoveToStart();
        core.MoveWordRight();
        Assert.Equal(3, core.CursorIndex); // after "foo"
        core.MoveWordRight();
        Assert.Equal(7, core.CursorIndex); // after "bar"
    }

    [Fact]
    public void MoveWordLeft_SkipsToPreviousWordStart()
    {
        var core = Make("foo bar");
        core.MoveWordLeft(); // from end: back to start of "bar"
        Assert.Equal(4, core.CursorIndex);
        core.MoveWordLeft(); // back to start of "foo"
        Assert.Equal(0, core.CursorIndex);
    }

    [Fact]
    public void MoveWordRight_AtEnd_DoesNotMove()
    {
        var core = Make("hi");
        core.MoveWordRight();
        Assert.Equal(2, core.CursorIndex);
    }

    [Fact]
    public void MoveWordLeft_AtStart_DoesNotMove()
    {
        var core = Make("hi");
        core.MoveToStart();
        core.MoveWordLeft();
        Assert.Equal(0, core.CursorIndex);
    }

    // ── Insert ────────────────────────────────────────────────────────────────

    [Fact]
    public void Insert_AppendsCharAtCursor()
    {
        var core = Make("ab");
        core.MoveToStart();
        core.Move(1); // after 'a'
        core.Insert('X');
        Assert.Equal("aXb", core.ToString());
        Assert.Equal(2, core.CursorIndex);
    }

    [Fact]
    public void InsertText_InsertsStringAtCursor()
    {
        var core = Make("ac");
        core.MoveToStart();
        core.Move(1);
        core.InsertText("b");
        Assert.Equal("abc", core.ToString());
    }

    [Fact]
    public void Insert_FiresChangedEvent()
    {
        var core = Make();
        bool fired = false;
        core.Changed += () => fired = true;
        core.Insert('x');
        Assert.True(fired);
    }

    // ── Backspace / Delete ────────────────────────────────────────────────────

    [Fact]
    public void Backspace_RemovesCharBeforeCursor()
    {
        var core = Make("abc");
        core.Backspace();
        Assert.Equal("ab", core.ToString());
        Assert.Equal(2, core.CursorIndex);
    }

    [Fact]
    public void Backspace_AtStart_DoesNothing()
    {
        var core = Make("abc");
        core.MoveToStart();
        core.Backspace();
        Assert.Equal("abc", core.ToString());
        Assert.Equal(0, core.CursorIndex);
    }

    [Fact]
    public void Delete_RemovesCharAtCursor()
    {
        var core = Make("abc");
        core.MoveToStart();
        core.Delete();
        Assert.Equal("bc", core.ToString());
        Assert.Equal(0, core.CursorIndex);
    }

    [Fact]
    public void Delete_AtEnd_DoesNothing()
    {
        var core = Make("abc");
        core.Delete();
        Assert.Equal("abc", core.ToString());
    }

    // ── Word deletion ─────────────────────────────────────────────────────────

    [Fact]
    public void DeleteWordLeft_DeletesPreviousWord()
    {
        var core = Make("foo bar");
        core.DeleteWordLeft();
        Assert.Equal("foo ", core.ToString());
        Assert.Equal(4, core.CursorIndex);
    }

    [Fact]
    public void DeleteWordRight_DeletesNextWord()
    {
        var core = Make("foo bar");
        core.MoveToStart();
        core.DeleteWordRight();
        Assert.Equal(" bar", core.ToString());
        Assert.Equal(0, core.CursorIndex);
    }

    [Fact]
    public void DeleteWordLeft_AtStart_DoesNothing()
    {
        var core = Make("abc");
        core.MoveToStart();
        core.DeleteWordLeft();
        Assert.Equal("abc", core.ToString());
    }

    [Fact]
    public void DeleteWordRight_AtEnd_DoesNothing()
    {
        var core = Make("abc");
        core.DeleteWordRight();
        Assert.Equal("abc", core.ToString());
    }

    // ── DeleteRange / GetSubstring ────────────────────────────────────────────

    [Fact]
    public void DeleteRange_RemovesCorrectSegment()
    {
        var core = Make("abcdef");
        core.DeleteRange(2, 3); // remove "cde"
        Assert.Equal("abf", core.ToString());
    }

    [Fact]
    public void DeleteRange_AdjustsCursorWhenInsideDeletedRegion()
    {
        var core = Make("abcdef");
        core.MoveToStart();
        core.Move(4); // cursor at index 4 ('e')
        core.DeleteRange(2, 3); // delete "cde"
        Assert.Equal(2, core.CursorIndex);
    }

    [Fact]
    public void GetSubstring_ReturnsCorrectText()
    {
        var core = Make("hello world");
        Assert.Equal("world", core.GetSubstring(6, 5));
    }

    [Fact]
    public void GetSubstring_OutOfBounds_ReturnsEmpty()
    {
        var core = Make("hi");
        Assert.Equal(string.Empty, core.GetSubstring(10, 5));
    }

    // ── SplitAt ───────────────────────────────────────────────────────────────

    [Fact]
    public void SplitAt_ReturnsRightHalfAndTruncatesLeft()
    {
        var core = Make("hello world");
        string right = core.SplitAt(5);
        Assert.Equal(" world", right);
        Assert.Equal("hello", core.ToString());
    }

    [Fact]
    public void SplitAt_ClampsCursorToNewLength()
    {
        var core = Make("abcde");
        core.MoveToEnd(); // cursor = 5
        core.SplitAt(2);  // left = "ab", cursor should clamp to 2
        Assert.Equal(2, core.CursorIndex);
    }

    // ── AppendText ────────────────────────────────────────────────────────────

    [Fact]
    public void AppendText_AddsToEndAndMovesCursor()
    {
        var core = Make("foo");
        core.MoveToStart();
        core.AppendText("bar");
        Assert.Equal("foobar", core.ToString());
        Assert.Equal(6, core.CursorIndex);
    }
}
