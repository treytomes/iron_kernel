using System.Drawing;
using IronKernel.Common.ValueObjects;
using IronKernel.Userland.Gfx;

namespace IronKernel.Userland.Morphic;

public sealed class HandMorph : Morph
{
	#region Fields

	private Point _grabOffset;
	private RenderImage? _image;

	#endregion

	#region Properties

	public Morph? GrabbedMorph { get; private set; }

	#endregion

	#region Methods

	protected override void OnLoad(IAssetService assets)
	{
		_ = LoadImageAsync(assets);
	}

	private async Task LoadImageAsync(IAssetService assets)
	{
		_image = await assets.LoadImageAsync("image.mouse_cursor");
		_image.Recolor(RadialColor.Black, null);
	}

	public void MoveTo(Point p)
	{
		Position = p;
	}

	public void Grab(Morph morph, Point at)
	{
		GrabbedMorph = morph;
		_grabOffset = new Point(
			at.X - morph.Position.X,
			at.Y - morph.Position.Y);
	}

	public void Release()
	{
		GrabbedMorph = null;
	}

	public void Update()
	{
		if (GrabbedMorph != null)
		{
			GrabbedMorph.Position = new Point(
				Position.X - _grabOffset.X,
				Position.Y - _grabOffset.Y);
		}
	}

	public override void Draw(IRenderingContext rc)
	{
		_image?.Render(rc, Position);
	}

	#endregion
}
