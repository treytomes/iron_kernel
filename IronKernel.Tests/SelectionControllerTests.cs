using Microsoft.Extensions.Logging.Abstractions;
using Userland.Morphic;

namespace IronKernel.Tests;

public class SelectionControllerTests
{
    private static Comparison<int> IntCompare => (a, b) => a.CompareTo(b);
    private static Comparison<(int, int)> TupleCompare => ((int, int) a, (int, int) b) =>
    {
        int lineCmp = a.Item1.CompareTo(b.Item1);
        return lineCmp != 0 ? lineCmp : a.Item2.CompareTo(b.Item2);
    };

    private static SelectionController<int> MakeInt() =>
        new(IntCompare);

    private static SelectionController<(int, int)> MakeTuple() =>
        new(TupleCompare);

    // ── Initial state ─────────────────────────────────────────────────────────

    [Fact]
    public void Initial_HasNoSelection()
    {
        Assert.False(MakeInt().HasSelection);
    }

    // ── Begin / Update ────────────────────────────────────────────────────────

    [Fact]
    public void Begin_AtSinglePoint_NoSelection()
    {
        var sel = MakeInt();
        sel.Begin(5);
        Assert.False(sel.HasSelection); // anchor == caret
    }

    [Fact]
    public void Update_AfterBegin_CreatesSelection()
    {
        var sel = MakeInt();
        sel.Begin(3);
        sel.Update(7);
        Assert.True(sel.HasSelection);
    }

    [Fact]
    public void GetRange_ReturnsSortedStartEnd()
    {
        var sel = MakeInt();
        sel.Begin(7);
        sel.Update(3); // reversed: anchor > caret
        var (start, end) = sel.GetRange();
        Assert.Equal(3, start);
        Assert.Equal(7, end);
    }

    [Fact]
    public void GetRange_ForwardSelection_PreservesOrder()
    {
        var sel = MakeInt();
        sel.Begin(2);
        sel.Update(9);
        var (start, end) = sel.GetRange();
        Assert.Equal(2, start);
        Assert.Equal(9, end);
    }

    [Fact]
    public void GetRange_WhenNoSelection_Throws()
    {
        var sel = MakeInt();
        Assert.Throws<InvalidOperationException>(() => sel.GetRange());
    }

    // ── Clear ─────────────────────────────────────────────────────────────────

    [Fact]
    public void Clear_RemovesSelection()
    {
        var sel = MakeInt();
        sel.Begin(1);
        sel.Update(5);
        sel.Clear();
        Assert.False(sel.HasSelection);
    }

    [Fact]
    public void Clear_ThenGetRange_Throws()
    {
        var sel = MakeInt();
        sel.Begin(1);
        sel.Update(5);
        sel.Clear();
        Assert.Throws<InvalidOperationException>(() => sel.GetRange());
    }

    // ── BeginIfNeeded ─────────────────────────────────────────────────────────

    [Fact]
    public void BeginIfNeeded_WhenNoAnchor_SetsAnchor()
    {
        var sel = MakeInt();
        sel.BeginIfNeeded(3);
        sel.Update(7);
        Assert.True(sel.HasSelection);
        Assert.Equal(3, sel.GetRange().start);
    }

    [Fact]
    public void BeginIfNeeded_WhenAnchorExists_DoesNotOverwrite()
    {
        var sel = MakeInt();
        sel.Begin(2);
        sel.BeginIfNeeded(10); // should not move anchor
        sel.Update(8);
        Assert.Equal(2, sel.GetRange().start);
    }

    // ── Normalize ─────────────────────────────────────────────────────────────

    [Fact]
    public void Normalize_ValidPositions_RetainsSelection()
    {
        var sel = MakeInt();
        sel.Begin(1);
        sel.Update(5);
        sel.Normalize(pos => pos >= 0 && pos <= 10);
        Assert.True(sel.HasSelection);
    }

    [Fact]
    public void Normalize_InvalidAnchor_ClearsSelection()
    {
        var sel = MakeInt();
        sel.Begin(1);
        sel.Update(5);
        sel.Normalize(pos => pos != 1); // anchor (1) is invalid
        Assert.False(sel.HasSelection);
    }

    [Fact]
    public void Normalize_InvalidCaret_ClearsSelection()
    {
        var sel = MakeInt();
        sel.Begin(1);
        sel.Update(5);
        sel.Normalize(pos => pos != 5); // caret (5) is invalid
        Assert.False(sel.HasSelection);
    }

    // ── SelectAll extension ───────────────────────────────────────────────────

    [Fact]
    public void SelectAll_SingleLine_SelectsEntireLine()
    {
        var doc = new TextDocument(NullLogger.Instance, "hello");
        var sel = MakeTuple();
        sel.SelectAll(doc);
        Assert.True(sel.HasSelection);
        var (start, end) = sel.GetRange();
        Assert.Equal((0, 0), start);
        Assert.Equal((0, 5), end);
    }

    [Fact]
    public void SelectAll_MultiLine_SelectsFromFirstToLast()
    {
        var doc = new TextDocument(NullLogger.Instance, "foo\nbar\nbaz");
        var sel = MakeTuple();
        sel.SelectAll(doc);
        var (start, end) = sel.GetRange();
        Assert.Equal((0, 0), start);
        Assert.Equal((2, 3), end);
    }

    [Fact]
    public void SelectAll_EmptyDocument_ClearsSelection()
    {
        var doc = new TextDocument(NullLogger.Instance, null);
        var sel = MakeTuple();
        sel.Begin((0, 0));
        sel.Update((0, 5));
        // With a fresh empty doc (1 line, 0 chars), SelectAll sets
        // anchor=(0,0) and caret=(0,0), which means no selection.
        sel.SelectAll(doc);
        Assert.False(sel.HasSelection);
    }

    // ── BeginIfShift extension ────────────────────────────────────────────────

    [Fact]
    public void BeginIfShift_WhenShiftTrue_SetsAnchorIfNeeded()
    {
        var sel = MakeTuple();
        sel.BeginIfShift(true, 1, 3);
        sel.Update((1, 7));
        Assert.True(sel.HasSelection);
        Assert.Equal((1, 3), sel.GetRange().start);
    }

    [Fact]
    public void BeginIfShift_WhenShiftFalse_DoesNothing()
    {
        var sel = MakeTuple();
        sel.BeginIfShift(false, 1, 3);
        Assert.False(sel.HasSelection);
    }
}
