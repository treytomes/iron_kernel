using Microsoft.Extensions.Logging.Abstractions;
using Userland.Morphic;

namespace IronKernel.Tests;

/// <summary>
/// Covers TextEditingCore paths not exercised by TextEditingCoreTests:
/// InsertTab (expand-to-spaces), indexer, NormalizeCursor warning path,
/// SetText, DeleteRange (cursor-before-deletion), static tab helpers,
/// DeleteWordLeft/Right length==0 guard, and InsertText/AppendText edge cases.
/// </summary>
public class TextEditingCoreExtendedTests
{
    private static TextEditingCore Make(string text = "") =>
        new(NullLogger.Instance, text);

    // ── InsertTab ─────────────────────────────────────────────────────────────

    [Fact]
    public void InsertTab_Default_InsertsTabChar()
    {
        var core = Make();
        core.InsertTab();
        Assert.Equal("\t", core.ToString());
        Assert.Equal(1, core.CursorIndex);
    }

    [Fact]
    public void InsertTab_ExpandToSpaces_InsertsTabWidthSpaces()
    {
        var core = Make();
        core.TabWidth = 4;
        core.InsertTab(expandToSpaces: true);
        Assert.Equal("    ", core.ToString());
        Assert.Equal(4, core.CursorIndex);
    }

    [Fact]
    public void InsertTab_ExpandToSpaces_TabWidthOne_InsertsOneSpace()
    {
        var core = Make();
        core.TabWidth = 1;
        core.InsertTab(expandToSpaces: true);
        Assert.Equal(" ", core.ToString());
        Assert.Equal(1, core.CursorIndex);
    }

    // ── Indexer ───────────────────────────────────────────────────────────────

    [Fact]
    public void Indexer_ReturnsCorrectChar()
    {
        var core = Make("hello");
        Assert.Equal('h', core[0]);
        Assert.Equal('e', core[1]);
        Assert.Equal('o', core[4]);
    }

    // ── SetText ───────────────────────────────────────────────────────────────

    [Fact]
    public void SetText_ReplacesBuffer()
    {
        var core = Make("old");
        core.SetText("new content");
        Assert.Equal("new content", core.ToString());
        Assert.Equal(11, core.CursorIndex);
    }

    [Fact]
    public void SetText_Null_SetsEmpty()
    {
        var core = Make("something");
        core.SetText(null!);
        Assert.Equal("", core.ToString());
        Assert.Equal(0, core.CursorIndex);
    }

    [Fact]
    public void SetText_FiresChangedEvent()
    {
        var core = Make("old");
        bool fired = false;
        core.Changed += () => fired = true;
        core.SetText("new");
        Assert.True(fired);
    }

    // ── DeleteRange (cursor before deleted region) ────────────────────────────

    [Fact]
    public void DeleteRange_CursorBeforeRange_DoesNotMoveCursor()
    {
        var core = Make("abcdef");
        core.MoveToStart();
        core.Move(1); // cursor at 1, before the deleted region
        core.DeleteRange(3, 2); // delete "de"
        Assert.Equal("abcf", core.ToString());
        Assert.Equal(1, core.CursorIndex); // cursor unchanged
    }

    [Fact]
    public void DeleteRange_ZeroLength_DoesNothing()
    {
        var core = Make("abc");
        core.DeleteRange(1, 0);
        Assert.Equal("abc", core.ToString());
    }

    [Fact]
    public void DeleteRange_NegativeLength_DoesNothing()
    {
        var core = Make("abc");
        core.DeleteRange(1, -5);
        Assert.Equal("abc", core.ToString());
    }

    [Fact]
    public void DeleteRange_OutOfBoundsStart_ClampsAndDeletes()
    {
        var core = Make("abc");
        core.DeleteRange(10, 1); // start clamped to 3 (length), length clamped to 0
        Assert.Equal("abc", core.ToString());
    }

    // ── DeleteWordLeft length==0 guard ────────────────────────────────────────

    [Fact]
    public void DeleteWordLeft_OnlyNonWordCharsBeforeCursor_DeletesNonWordChars()
    {
        // "   abc" with cursor after spaces — non-word chars first, then hit start
        var core = Make("   ");
        core.DeleteWordLeft(); // skips spaces, hits start of buffer, length=3
        Assert.Equal("", core.ToString());
    }

    [Fact]
    public void DeleteWordLeft_EmptyBuffer_DoesNothing()
    {
        var core = Make();
        core.DeleteWordLeft(); // CursorIndex==0, should return immediately
        Assert.Equal("", core.ToString());
    }

    // ── DeleteWordRight length==0 guard ──────────────────────────────────────

    [Fact]
    public void DeleteWordRight_OnlyNonWordCharsAfterCursor_DeletesNonWordChars()
    {
        var core = Make("   ");
        core.MoveToStart();
        core.DeleteWordRight(); // skips spaces to end, length=3
        Assert.Equal("", core.ToString());
    }

    [Fact]
    public void DeleteWordRight_EmptyBuffer_DoesNothing()
    {
        var core = Make();
        core.DeleteWordRight();
        Assert.Equal("", core.ToString());
    }

    // ── InsertText edge cases ─────────────────────────────────────────────────

    [Fact]
    public void InsertText_EmptyString_DoesNothing()
    {
        var core = Make("abc");
        int before = core.CursorIndex;
        core.InsertText("");
        Assert.Equal("abc", core.ToString());
        Assert.Equal(before, core.CursorIndex);
    }

    [Fact]
    public void InsertText_Null_DoesNothing()
    {
        var core = Make("abc");
        core.InsertText(null!);
        Assert.Equal("abc", core.ToString());
    }

    // ── AppendText edge cases ─────────────────────────────────────────────────

    [Fact]
    public void AppendText_EmptyString_DoesNothing()
    {
        var core = Make("abc");
        core.MoveToStart();
        core.AppendText("");
        Assert.Equal("abc", core.ToString());
        Assert.Equal(0, core.CursorIndex); // cursor unmoved
    }

    // ── Static tab helpers ────────────────────────────────────────────────────

    [Theory]
    [InlineData("abc",    0, 4, 0)]  // before any char
    [InlineData("abc",    1, 4, 1)]  // after 'a'
    [InlineData("abc",    3, 4, 3)]  // at end
    [InlineData("\tabc",  0, 4, 0)]  // before tab
    [InlineData("\tabc",  1, 4, 4)]  // after tab → visual col 4
    [InlineData("\tabc",  2, 4, 5)]  // after tab+'a'
    [InlineData("a\tb",   2, 4, 4)]  // 'a'(1) + tab fills to col 4; charIndex=2 is at 'b' which starts at col 4
    public void ComputeVisualColumn_CorrectResult(string line, int charIndex, int tabWidth, int expected)
    {
        Assert.Equal(expected, TextEditingCore.ComputeVisualColumn(line, charIndex, tabWidth));
    }

    [Theory]
    [InlineData("abc",   0, 4, 0)]   // visual col 0 → char index 0
    [InlineData("abc",   1, 4, 1)]   // visual col 1 → char index 1
    [InlineData("abc",   3, 4, 3)]   // visual col 3 → char index 3
    [InlineData("abc",  99, 4, 3)]   // past end → length
    [InlineData("\tabc", 0, 4, 0)]   // visual col 0 → char 0
    [InlineData("\tabc", 2, 4, 0)]   // visual col 2 is inside the tab → char 0
    [InlineData("\tabc", 4, 4, 1)]   // visual col 4 → char 1 (after tab)
    [InlineData("\tabc", 5, 4, 2)]   // visual col 5 → char 2
    public void VisualColumnToCharIndex_CorrectResult(string line, int targetCol, int tabWidth, int expected)
    {
        Assert.Equal(expected, TextEditingCore.VisualColumnToCharIndex(line, targetCol, tabWidth));
    }

    [Theory]
    [InlineData("",      80, 4, 1)]  // empty line → 1 visual row
    [InlineData("abc",   80, 4, 1)]  // short line fits in one row
    [InlineData("abcde", 3,  4, 2)]  // 5 chars, viewport 3 → 2 rows
    [InlineData("\t",    4,  4, 1)]  // tab expands to 4, fits in width-4 viewport
    [InlineData("\t\t",  4,  4, 2)]  // two tabs = 8 visual cols, viewport 4 → 2 rows
    public void GetVisualRowCount_CorrectResult(string text, int visualCols, int tabWidth, int expected)
    {
        Assert.Equal(expected, TextEditingCore.GetVisualRowCount(text, visualCols, tabWidth));
    }
}
