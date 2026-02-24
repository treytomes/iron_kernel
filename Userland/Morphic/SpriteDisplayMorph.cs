using System.Drawing;
using Miniscript;
using Userland.Gfx;
using Userland.Morphic;
using Userland.Services;

namespace Userland.Roguey;

public sealed class SpriteDisplayMorph : Morph
{
	#region Fields

	private readonly List<SpriteInfo> _sprites = new();
	private GlyphSet<Bitmap>? _glyphs;
	private Point _scrollOffset;
	private readonly TileSetInfo _tileSetInfo;

	#endregion

	#region Constructors

	public SpriteDisplayMorph(Size viewportSize, TileSetInfo tileSetInfo)
	{
		_tileSetInfo = tileSetInfo;
		Size = viewportSize;
		ShouldClipToBounds = true;
		IsSelectable = true;
	}

	#endregion

	#region Properties

	public IList<SpriteInfo> Sprites => _sprites;

	public Point ScrollOffset
	{
		get => _scrollOffset;
		set
		{
			_scrollOffset = value;
			Invalidate();
		}
	}

	#endregion

	#region Methods

	protected override async void OnLoad(IAssetService assets)
	{
		_glyphs = await assets.LoadGlyphSetAsync(
			_tileSetInfo.Url,
			_tileSetInfo.TileSize
		);
		Invalidate();
	}

	protected override void DrawSelf(IRenderingContext rc)
	{
		if (_glyphs == null)
			return;

		rc.PushOffset(new Point(-_scrollOffset.X, -_scrollOffset.Y));

		foreach (var sprite in _sprites)
		{
			if (sprite.TileIndex < 0 || sprite.TileIndex >= _glyphs.Count)
				continue;

			var glyph = _glyphs[sprite.TileIndex];
			var px = sprite.X * _glyphs.TileWidth;
			var py = sprite.Y * _glyphs.TileHeight;

			glyph.Render(
				rc,
				new Point(px, py),
				sprite.ForegroundColor,
				sprite.BackgroundColor
			);
		}

		rc.PopOffset();
	}

	protected override ValMap CreateScriptObject()
	{
		var map = base.CreateScriptObject();
		map["scrollX"] = new ValNumber(_scrollOffset.X);
		map["scrollY"] = new ValNumber(_scrollOffset.Y);
		return map;
	}

	protected override void ApplyScriptState()
	{
		base.ApplyScriptState();

		if (ScriptObject["scrollX"] is ValNumber sx && ScriptObject["scrollY"] is ValNumber sy)
		{
			_scrollOffset = new Point(sx.IntValue(), sy.IntValue());
			Invalidate();
		}

		foreach (var sprite in _sprites)
			sprite.ApplyScriptState();
	}

	#endregion
}