using System.Drawing;
using IronKernel.Common.ValueObjects;
using IronKernel.Userland.Gfx;
using IronKernel.Userland.Morphic.Inspector;

namespace IronKernel.Userland.Morphic;

public sealed class RadialColorValueMorph : Morph, IValueContentMorph
{
	private RadialColor? _color;
	private readonly LabelMorph _label;

	private const int SwatchSize = 6;
	private const int Padding = 1;

	public RadialColorValueMorph()
	{
		IsSelectable = false;

		_label = new LabelMorph
		{
			IsSelectable = false,
			BackgroundColor = null
		};

		AddMorph(_label);
	}

	#region IValueContentMorph

	public void Refresh(object? value)
	{
		_color = value as RadialColor;
		_label.Text = _color?.ToString() ?? "<null>";
		Invalidate();
		InvalidateLayout();
	}

	#endregion

	#region Layout

	protected override void UpdateLayout()
	{
		_label.Position = new Point(SwatchSize + Padding * 2, Padding);

		var height = Math.Max(SwatchSize, _label.Size.Height);
		Size = new Size(
			SwatchSize + _label.Size.Width + Padding * 3,
			height + Padding * 2);
	}

	#endregion

	#region Rendering

	protected override void DrawSelf(IRenderingContext rc)
	{
		if (Style == null)
			return;

		var s = Style.Semantic;

		// Background
		rc.RenderFilledRect(
			new Rectangle(0, 0, Size.Width, Size.Height),
			s.Surface);

		// Swatch border
		rc.RenderFilledRect(
			new Rectangle(Padding, Padding, SwatchSize, SwatchSize),
			s.Border);

		if (_color != null)
		{
			// Swatch fill — draw inner pixels in the RadialColor itself
			for (int x = 1; x < SwatchSize - 1; x++)
				for (int y = 1; y < SwatchSize - 1; y++)
				{
					rc.SetPixel(
						new Point(Padding + x, Padding + y),
						_color);
				}
		}
	}

	#endregion
}