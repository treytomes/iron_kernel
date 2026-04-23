using System.Drawing;
using Userland.Morphic.Commands;
using Userland.Morphic.Events;
using Userland.Morphic.Layout;

namespace Userland.Morphic;

internal sealed class WindowTitleBarMorph : DockPanelMorph
{
    private readonly Morph _window;
    private Point _startMouse;
    private bool _dragging;

    public WindowTitleBarMorph(Morph window)
    {
        _window = window;
        IsSelectable = true;
        ShouldClipToBounds = true;
    }

    public override void OnPointerDown(PointerDownEvent e)
    {
        base.OnPointerDown(e);

        if (!TryGetWorld(out var world)) return;

        _startMouse = e.Position;
        _dragging = true;
        world.CapturePointer(this);
        e.MarkHandled();
    }

    public override void OnPointerMove(PointerMoveEvent e)
    {
        if (!_dragging) return;
        if (!TryGetWorld(out var world)) return;

        var dx = e.Position.X - _startMouse.X;
        var dy = e.Position.Y - _startMouse.Y;

        if (dx != 0 || dy != 0)
        {
            world.Commands.Submit(new MoveCommand(_window, dx, dy));
            _startMouse = e.Position;
        }

        e.MarkHandled();
    }

    public override void OnPointerUp(PointerUpEvent e)
    {
        _dragging = false;
        if (TryGetWorld(out var world)) world.ReleasePointer(this);
        base.OnPointerUp(e);
        e.MarkHandled();
    }
}
