using System.Drawing;
using IronKernel.Common.ValueObjects;
using IronKernel.Userland;
using IronKernel.Userland.Gfx;
using IronKernel.Userland.Morphic;

namespace Game.Morphs;

public sealed class MapTileMorph : Morph
{
	#region Fields

	private int _tileIndex = (int)'.';
	private RadialColor _foreground = RadialColor.White;
	private RadialColor? _background;
	private GlyphSet<Bitmap>? _glyphs;

	#endregion

	#region Properties

	public int TileIndex
	{
		get => _tileIndex;
		set { _tileIndex = value; Invalidate(); }
	}

	public RadialColor ForegroundColor
	{
		get => _foreground;
		set { _foreground = value; Invalidate(); }
	}

	public RadialColor? BackgroundColor
	{
		get => _background;
		set { _background = value; Invalidate(); }
	}

	public bool BlocksMovement { get; set; }
	public bool BlocksVision { get; set; }

	public string TileTag { get; set; } = "floor";

	#endregion

	#region Constructors

	public MapTileMorph()
	{
		IsSelectable = true;
		Size = new Size(8, 8); // matches OEM437 tile size
	}

	#endregion

	#region Methods

	protected override async void OnLoad(IAssetService assets)
	{
		if (Style == null) throw new Exception("Style is null.");

		_glyphs = await assets.LoadGlyphSetAsync("image.oem437_8", new Size(8, 8));
		UpdateLayout();
	}

	protected override void DrawSelf(IRenderingContext rc)
	{
		if (_glyphs == null)
			return;

		if (TileIndex < 0 || TileIndex >= _glyphs.Count)
			return;

		var glyph = _glyphs[TileIndex];

		glyph.Render(
			rc,
			Point.Empty,
			ForegroundColor,
			BackgroundColor
		);
	}

	#endregion
}