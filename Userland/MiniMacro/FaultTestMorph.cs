using System.Drawing;
using Userland.Gfx;
using Userland.Morphic;

namespace Userland.MiniMacro;

public sealed class FaultTestMorph : WindowMorph
{
    private int _updateCount;

    public FaultTestMorph()
        : base(Point.Empty, new Size(200, 80), "Fault Test")
    {
        var label = new LabelMorph
        {
            Text = "This morph will fault on every update.",
            IsSelectable = false,
            BackgroundColor = null
        };
        Content.AddMorph(label);
    }

    public override void Update(double deltaMs)
    {
        base.Update(deltaMs);
        _updateCount++;
        throw new InvalidOperationException($"Deliberate test fault #{_updateCount}");
    }
}
