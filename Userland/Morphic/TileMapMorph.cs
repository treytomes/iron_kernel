using System.Drawing;
using IronKernel.Common.ValueObjects;
using Miniscript;
using Userland.Gfx;
using Userland.Services;

namespace Userland.Morphic;

public sealed class TileMapMorph : Morph
{
	#region Fields

	private readonly TileInfo[,] _tiles;
	private GlyphSet<Bitmap>? _glyphs;

	private Point _scrollOffset;
	private TileSetInfo _tileSetInfo;

	#endregion

	#region Constructors

	public TileMapMorph(Size viewportSize, Size mapSize, TileSetInfo tileSetInfo)
	{
		MapSize = mapSize;

		_tiles = new TileInfo[MapSize.Width, MapSize.Height];
		_tileSetInfo = tileSetInfo;

		ShouldClipToBounds = true;
		IsSelectable = true;

		// Initialize tiles with defaults
		for (var y = 0; y < MapSize.Height; y++)
			for (var x = 0; x < MapSize.Width; x++)
			{
				_tiles[x, y] = new()
				{
					TileIndex = (int)'.',
					ForegroundColor = RadialColor.White,
					BackgroundColor = RadialColor.Black,
					BlocksMovement = false,
					BlocksVision = false,
					Tag = "floor"
				};
			}

		Size = viewportSize;
	}

	#endregion

	#region Properties

	public Size MapSize { get; }

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

	public bool InBounds(int x, int y)
		=> x >= 0 && y >= 0 && x < MapSize.Width && y < MapSize.Height;

	public TileInfo? GetTile(int x, int y)
		=> InBounds(x, y) ? _tiles[x, y] : null;

	protected override void DrawSelf(IRenderingContext rc)
	{
		if (_glyphs == null)
			return;

		// Apply camera offset
		rc.PushOffset(new Point(-_scrollOffset.X, -_scrollOffset.Y));

		// Compute visible tile bounds
		int minTileX = Math.Max(0, _scrollOffset.X / _glyphs.TileWidth);
		int minTileY = Math.Max(0, _scrollOffset.Y / _glyphs.TileHeight);

		int maxTileX = Math.Min(
			MapSize.Width - 1,
			(_scrollOffset.X + Size.Width) / _glyphs.TileWidth
		);
		int maxTileY = Math.Min(
			MapSize.Height - 1,
			(_scrollOffset.Y + Size.Height) / _glyphs.TileHeight
		);

		for (var y = minTileY; y <= maxTileY; y++)
		{
			for (var x = minTileX; x <= maxTileX; x++)
			{
				var tile = _tiles[x, y];

				if (tile.TileIndex < 0 || tile.TileIndex >= _glyphs.Count)
					continue;

				var glyph = _glyphs[tile.TileIndex];

				var px = x * _glyphs.TileWidth;
				var py = y * _glyphs.TileHeight;

				glyph.Render(
					rc,
					new Point(px, py),
					tile.ForegroundColor,
					tile.BackgroundColor
				);
			}
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

		for (int y = 0; y < MapSize.Height; y++)
			for (int x = 0; x < MapSize.Width; x++)
				GetTile(x, y)?.ApplyScriptState();
	}

	#endregion
}