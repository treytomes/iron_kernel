using System.Drawing;
using Userland.Gfx;
using Userland.Morphic.Events;

namespace Userland.Morphic;

public enum ToastSeverity { Info, Warning, Error }

public sealed class ToastMorph : Morph
{
    private const int Padding = 6;

    private readonly LabelMorph _label;
    private double _elapsed;

    public ToastMorph(string message, ToastSeverity severity, double lifetimeSeconds = 4.0)
    {
        IsSelectable = true;
        ShouldClipToBounds = true;

        Severity = severity;
        LifetimeSeconds = lifetimeSeconds;

        _label = new LabelMorph
        {
            Text = message,
            IsSelectable = false,
            BackgroundColor = null
        };

        AddMorph(_label);
    }

    public ToastSeverity Severity { get; }
    public double LifetimeSeconds { get; }

    public override void Update(double deltaTime)
    {
        base.Update(deltaTime);

        _elapsed += deltaTime;
        if (_elapsed >= LifetimeSeconds)
            MarkForDeletion();
    }

    public override void OnPointerDown(PointerDownEvent e)
    {
        MarkForDeletion();
        e.MarkHandled();
    }

    protected override void UpdateLayout()
    {
        _label.Position = new Point(Padding, Padding);
        Size = new Size(
            _label.Size.Width + Padding * 2,
            _label.Size.Height + Padding * 2);
        base.UpdateLayout();
    }

    protected override void DrawSelf(IRenderingContext rc)
    {
        if (Style == null) return;

        var s = Style.Semantic;

        var bg = Severity switch
        {
            ToastSeverity.Error   => s.Danger,
            ToastSeverity.Warning => s.Warning,
            _                     => s.Info
        };

        var fg = Severity switch
        {
            ToastSeverity.Error   => s.Text,
            ToastSeverity.Warning => s.Text,
            _                     => s.Text
        };

        rc.RenderFilledRect(new Rectangle(Point.Empty, Size), bg);
        rc.RenderRect(new Rectangle(Point.Empty, Size), bg.Lerp(IronKernel.Common.ValueObjects.RadialColor.Black, 0.4f));
        _label.ForegroundColor = fg;
    }
}
