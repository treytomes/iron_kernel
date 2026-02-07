using System.Drawing;
using IronKernel.Common.ValueObjects;
using IronKernel.Userland.Gfx;

namespace IronKernel.Userland.Morphic;

public sealed class ImageMorph : Morph
{
	#region Fields

	private RenderImage? _image;

	#endregion

	#region Constructors

	public ImageMorph(Point position, string assetId)
	{
		Position = position;
		AssetId = assetId;
		IsSelectable = true;
	}

	#endregion

	#region Properties

	public string AssetId { get; }
	public RadialColor? Foreground { get; set; }
	public RenderImage.RenderFlag Flags { get; set; }

	#endregion

	#region Methods

	protected override async void OnLoad(IAssetService assets)
	{
		_image = await assets.LoadImageAsync(AssetId);
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
