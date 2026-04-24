using System.Drawing;
using Miniscript;
using Userland.Morphic;

namespace IronKernel.Tests;

/// <summary>
/// Tests for Morph.ApplyScriptEdits() traversal depth.
/// </summary>
public class ApplyScriptEditsTests
{
    private static ValMap MakePosition(int x, int y)
    {
        var m = new ValMap();
        m["x"] = new ValNumber(x);
        m["y"] = new ValNumber(y);
        return m;
    }

    private static ValMap MakeSize(int w, int h)
    {
        var m = new ValMap();
        m["w"] = new ValNumber(w);
        m["h"] = new ValNumber(h);
        return m;
    }

    // ── Direct child ─────────────────────────────────────────────────────────

    [Fact]
    public void ApplyScriptEdits_UpdatesDirectChild()
    {
        var parent = new CanvasMorph();
        var child = new CanvasMorph();
        parent.AddMorph(child);

        child.ScriptObject["position"] = MakePosition(10, 20);

        parent.ApplyScriptEdits();

        Assert.Equal(new Point(10, 20), child.Position);
    }

    // ── Grandchild — captures the bug ────────────────────────────────────────

    [Fact]
    public void ApplyScriptEdits_UpdatesGrandchild()
    {
        var root = new CanvasMorph();
        var child = new CanvasMorph();
        var grandchild = new CanvasMorph();
        root.AddMorph(child);
        child.AddMorph(grandchild);

        grandchild.ScriptObject["position"] = MakePosition(50, 60);

        root.ApplyScriptEdits();

        Assert.Equal(new Point(50, 60), grandchild.Position);
    }

    // ── Arbitrary depth ──────────────────────────────────────────────────────

    [Fact]
    public void ApplyScriptEdits_UpdatesDeepDescendant()
    {
        var root = new CanvasMorph();
        var current = root;
        for (int i = 0; i < 5; i++)
        {
            var next = new CanvasMorph();
            current.AddMorph(next);
            current = next;
        }
        // current is now 5 levels deep
        current.ScriptObject["position"] = MakePosition(99, 88);

        root.ApplyScriptEdits();

        Assert.Equal(new Point(99, 88), current.Position);
    }

    // ── Root itself still updated ─────────────────────────────────────────────

    [Fact]
    public void ApplyScriptEdits_UpdatesRootMorph()
    {
        var root = new CanvasMorph();
        root.ScriptObject["position"] = MakePosition(7, 8);

        root.ApplyScriptEdits();

        Assert.Equal(new Point(7, 8), root.Position);
    }

    // ── Size round-trip ───────────────────────────────────────────────────────

    [Fact]
    public void ApplyScriptEdits_UpdatesSizeOnGrandchild()
    {
        var root = new CanvasMorph();
        var child = new CanvasMorph();
        var grandchild = new CanvasMorph();
        root.AddMorph(child);
        child.AddMorph(grandchild);

        grandchild.ScriptObject["size"] = MakeSize(100, 200);

        root.ApplyScriptEdits();

        Assert.Equal(new Size(100, 200), grandchild.Size);
    }
}
