using System.Drawing;
using IronKernel.Common.ValueObjects;

namespace IronKernel.Userland.Morphic;

public sealed class HandMorph : Morph
{
	#region Fields

	private Point _grabOffset;

	#endregion

	#region Properties

	public RenderImage? Image { get; set; }
	public Morph? GrabbedMorph { get; private set; }

	#endregion

	#region Methods

	protected override void OnLoad(IAssetService assets)
	{
		_ = LoadCursorAsync(assets);
	}

	private async Task LoadCursorAsync(IAssetService assets)
	{
		var image = await assets.LoadImageAsync("image.mouse_cursor");
		image.Recolor(RadialColor.Black, null);
		Image = image;
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
		if (Image == null) return;
		Image.Render(rc, Position);
	}

	#endregion
}
