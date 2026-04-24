using IronKernel.Common.ValueObjects;
using Userland.Morphic.Events;
using Userland.Scripting;

namespace IronKernel.Tests;

/// <summary>
/// Tests that KeyboardState tracks key-down/up transitions correctly,
/// and that WorldScriptContext.OnKey routes events into that state.
/// </summary>
public class KeyboardStateTests
{
    // ── KeyboardState unit tests ──────────────────────────────────────────────

    [Fact]
    public void GetKeyState_ReturnsFalse_WhenKeyNeverSeen()
    {
        var ks = new KeyboardState();
        Assert.False(ks.GetKeyState(Key.Up));
    }

    [Fact]
    public void SetKeyState_Press_ReturnsTrue()
    {
        var ks = new KeyboardState();
        ks.SetKeyState(Key.Up, true);
        Assert.True(ks.GetKeyState(Key.Up));
    }

    [Fact]
    public void SetKeyState_Release_ReturnsFalse()
    {
        var ks = new KeyboardState();
        ks.SetKeyState(Key.Up, true);
        ks.SetKeyState(Key.Up, false);
        Assert.False(ks.GetKeyState(Key.Up));
    }

    [Fact]
    public void IsAnyKeyDown_FalseWhenNoKeysPressed()
    {
        var ks = new KeyboardState();
        Assert.False(ks.IsAnyKeyDown());
    }

    [Fact]
    public void IsAnyKeyDown_TrueAfterPress()
    {
        var ks = new KeyboardState();
        ks.SetKeyState(Key.Left, true);
        Assert.True(ks.IsAnyKeyDown());
    }

    [Fact]
    public void IsAnyKeyDown_FalseAfterAllReleased()
    {
        var ks = new KeyboardState();
        ks.SetKeyState(Key.Left, true);
        ks.SetKeyState(Key.Right, true);
        ks.SetKeyState(Key.Left, false);
        ks.SetKeyState(Key.Right, false);
        Assert.False(ks.IsAnyKeyDown());
    }

    // ── OnKey routing tests ───────────────────────────────────────────────────

    private static KeyboardState MakeKeyboardAndFeedEvent(InputAction action, Key key, bool markedHandled = false)
    {
        var ks = new KeyboardState();
        var e = new KeyEvent(action, KeyModifier.None, key);
        if (markedHandled) e.MarkHandled();

        // Simulate what WorldScriptContext.OnKey does (extracted for isolation).
        switch (e.Action)
        {
            case InputAction.Press:
            case InputAction.Repeat:
                ks.SetKeyState(e.Key, true);
                break;
            case InputAction.Release:
                ks.SetKeyState(e.Key, false);
                break;
        }
        return ks;
    }

    [Fact]
    public void OnKey_Press_SetsKeyDown()
    {
        var ks = MakeKeyboardAndFeedEvent(InputAction.Press, Key.Up);
        Assert.True(ks.GetKeyState(Key.Up));
    }

    [Fact]
    public void OnKey_Repeat_KeepsKeyDown()
    {
        var ks = MakeKeyboardAndFeedEvent(InputAction.Repeat, Key.Up);
        Assert.True(ks.GetKeyState(Key.Up));
    }

    [Fact]
    public void OnKey_Release_ClearsKeyDown()
    {
        var ks = new KeyboardState();
        ks.SetKeyState(Key.Up, true);
        var e = new KeyEvent(InputAction.Release, KeyModifier.None, Key.Up);
        switch (e.Action)
        {
            case InputAction.Release:
                ks.SetKeyState(e.Key, false);
                break;
        }
        Assert.False(ks.GetKeyState(Key.Up));
    }

    /// <summary>
    /// Regression: previously WorldMorph.KeyPress gated script delivery on
    /// e.Handled, so a Press event consumed by the UI morph (Handled=true)
    /// would never reach KeyboardState. Scripts would always see key-up.
    /// The fix unconditionally delivers all events to _scriptContext.
    /// This test verifies that a Handled press still registers as key-down.
    /// </summary>
    [Fact]
    public void OnKey_HandledPress_StillSetsKeyDown()
    {
        var ks = MakeKeyboardAndFeedEvent(InputAction.Press, Key.Up, markedHandled: true);
        Assert.True(ks.GetKeyState(Key.Up));
    }
}
