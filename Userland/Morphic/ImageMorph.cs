using System.Drawing;
using IronKernel.Common.ValueObjects;
using Userland.Gfx;
using Userland.Services;

namespace Userland.Morphic;

public sealed class ImageMorph : Morph
{
	#region Fields

	private RenderImage? _image;

	#endregion

	#region Constructors

	public ImageMorph(Point position, string url)
	{
		Position = position;
		Url = url;
		IsSelectable = true;
	}

	#endregion

	#region Properties

	public string Url { get; }
	public RadialColor? Foreground { get; set; }
	public RenderImage.RenderFlag Flags { get; set; }

	#endregion

	#region Methods

	protected override async void OnLoad(IAssetService assets)
	{
		_image = await assets.LoadImageAsync(Url);
		_image.Recolor(RadialColor.Black, null);
		Size = _image.Size;
	}

	protected override void DrawSelf(IRenderingContext rc)
	{
		if (_image == null) return;
		var img = new RenderImage(_image);
		if (Foreground != null) img.Recolor(RadialColor.White, Foreground);
		img.Render(rc, new Point(0, 0), Flags);
	}

	#endregion
}
