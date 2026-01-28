using IronKernel.Common.ValueObjects;

namespace IronKernel.Userland.Morphic;

public sealed class ImageMorph : Morph
{
	private RenderImage? _image;

	public ImageMorph(string assetId)
	{
		AssetId = assetId;
	}

	public string AssetId { get; }
	public RadialColor? Foreground { get; set; }
	public RenderImage.RenderFlag Flags { get; set; }
	public override bool IsSelectable => false;

	protected override async void OnLoad(IAssetService assets)
	{
		_image = await assets.LoadImageAsync(AssetId);
		_image.Recolor(RadialColor.Black, null);
		Size = _image.Size;
	}

	public override void Draw(IRenderingContext rc)
	{
		if (_image == null) return;
		var img = new RenderImage(_image);
		if (Foreground != null) img.Recolor(RadialColor.White, Foreground);
		img.Render(rc, Position, Flags);
	}
}
