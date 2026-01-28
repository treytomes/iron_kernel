using IronKernel.Common.ValueObjects;
using IronKernel.Userland.Gfx;
using System.Drawing;

namespace IronKernel.Userland.Morphic;

public sealed class LabelMorph : Morph
{
	#region Fields

	private Font? _font;
	private string _text = string.Empty;

	#endregion

	#region Constructors

	public LabelMorph(Point position, string assetId, Size tileSize)
	{
		Position = position;
		AssetId = assetId;
		IsSelectable = true;
		TileSize = tileSize;
	}

	#endregion

	#region Properties

	public string AssetId { get; }
	public Size TileSize { get; }
	public RadialColor? Foreground { get; set; }
	public RenderImage.RenderFlag Flags { get; set; }
	public RadialColor ForegroundColor { get; set; } = RadialColor.White;
	public RadialColor? BackgroundColor { get; set; } = RadialColor.Black;

	public string Text
	{
		get
		{
			return _text;
		}
		set
		{
			if (_text == value) return;
			_text = value;
			UpdateLayout();
			Invalidate();
		}
	}

	#endregion

	#region Methods

	protected override async void OnLoad(IAssetService assets)
	{
		_font = await assets.LoadFontAsync(AssetId, TileSize);
		UpdateLayout();
	}

	public override void Draw(IRenderingContext rc)
	{
		if (_font == null) return;

		_font.WriteString(rc, _text, Position, ForegroundColor, BackgroundColor);
	}

	private void UpdateLayout()
	{
		if (_font == null) return;
		Size = _font.MeasureString(_text);
	}

	#endregion
}
