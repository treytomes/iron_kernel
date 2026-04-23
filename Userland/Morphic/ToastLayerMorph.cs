using System.Drawing;

namespace Userland.Morphic;

public sealed class ToastLayerMorph : Morph
{
    private const int Margin = 8;
    private const int Spacing = 4;

    public ToastLayerMorph()
    {
        IsSelectable = false;
    }

    public void Show(string message, ToastSeverity severity = ToastSeverity.Info)
    {
        AddMorph(new ToastMorph(message, severity));
        InvalidateLayout();
    }

    protected override void UpdateLayout()
    {
        base.UpdateLayout();

        // Stack toasts bottom-up in the bottom-right corner
        int y = Size.Height - Margin;

        foreach (var child in Submorphs.ToArray())
        {
            if (child is not ToastMorph toast) continue;

            y -= toast.Size.Height;
            toast.Position = new Point(Size.Width - toast.Size.Width - Margin, y);
            y -= Spacing;
        }
    }
}
