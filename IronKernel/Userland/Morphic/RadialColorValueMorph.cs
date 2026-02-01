using System.Drawing;
using IronKernel.Common.ValueObjects;
using IronKernel.Userland.Gfx;
using IronKernel.Userland.Morphic.Inspector;

namespace IronKernel.Userland.Morphic;

public sealed class RadialColorValueMorph : Morph, IValueContentMorph
{
	#region Constants

	private const int SwatchSize = 6;
	private const int Padding = 1;

	#endregion

	#region Fields

	private RadialColor? _color;
	private readonly Action<RadialColor?>? _setter;

	private ChannelStepperMorph _r;
	private ChannelStepperMorph _g;
	private ChannelStepperMorph _b;

	#endregion

	#region Constructors

	public RadialColorValueMorph(Action<RadialColor?>? setter)
	{
		_setter = setter;
		IsSelectable = false;

		_r = new ChannelStepperMorph("R", 0, OnRChanged);
		_g = new ChannelStepperMorph("G", 0, OnGChanged);
		_b = new ChannelStepperMorph("B", 0, OnBChanged);

		AddMorph(_r);
		AddMorph(_g);
		AddMorph(_b);
	}

	#endregion

	#region Methods

	#region IValueContentMorph

	public void Refresh(object? value)
	{
		if (value is not RadialColor c)
			return;

		_color = c;

		_r.Value = c.R;
		_g.Value = c.G;
		_b.Value = c.B;

		Invalidate();
		InvalidateLayout();
	}

	#endregion

	#region Layout

	protected override void UpdateLayout()
	{
		// Reserve space for the color swatch
		var x = SwatchSize + Padding * 2;

		foreach (var m in Submorphs)
		{
			m.Position = new Point(x, Padding);
			x += m.Size.Width + Padding;
		}

		var height = Math.Max(
			SwatchSize,
			Submorphs.Max(m => m.Size.Height)
		);

		Size = new Size(
			x + Padding,
			height + Padding * 2
		);
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
			IsEffectivelyHovered ? s.PrimaryHover : s.Border);

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

	private void OnRChanged(byte r)
	{
		if (_color == null) _color = RadialColor.Black;
		UpdateColor(_color.WithR(r));
	}

	private void OnGChanged(byte g)
	{
		if (_color == null) _color = RadialColor.Black;
		UpdateColor(_color.WithG(g));
	}

	private void OnBChanged(byte b)
	{
		if (_color == null) _color = RadialColor.Black;
		UpdateColor(_color.WithB(b));
	}

	private void UpdateColor(RadialColor newColor)
	{
		if (newColor.Equals(_color))
			return;

		_color = newColor;
		_setter?.Invoke(newColor);
		Invalidate();
	}

	#endregion

	#endregion
}