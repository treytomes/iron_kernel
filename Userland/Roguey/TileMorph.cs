using System.Drawing;
using IronKernel.Common.ValueObjects;
using IronKernel.Userland.Gfx;
using IronKernel.Userland.Morphic;
using IronKernel.Userland.Services;
using Miniscript;

namespace IronKernel.Userland.Roguey;

/// <summary>
/// A single map tile backed entirely by MiniScript slots.
/// Rendering and collision read from slot-backed properties.
/// </summary>
public sealed class TileMorph : MiniScriptMorph
{
	#region Constants

	private static readonly Size TILE_SIZE = new Size(16, 24);

	#endregion

	#region Fields

	private GlyphSet<Bitmap>? _glyphs;

	#endregion

	#region Constructors

	public TileMorph()
	{
		IsSelectable = true;
		Size = TILE_SIZE;

		// Sensible defaults (written once, script may override)
		if (!HasSlot(nameof(TileIndex)))
			SetSlot(nameof(TileIndex), new ValNumber((int)'.'));

		if (!HasSlot(nameof(ForegroundColor)))
			SetSlot(nameof(ForegroundColor), RadialColor.White.ToMiniScriptValue());

		// BackgroundColor is optional (null means transparent)

		if (!HasSlot(nameof(BlocksMovement)))
			SetSlot(nameof(BlocksMovement), ValNumber.zero);

		if (!HasSlot(nameof(BlocksVision)))
			SetSlot(nameof(BlocksVision), ValNumber.zero);

		if (!HasSlot(nameof(TileTag)))
			SetSlot(nameof(TileTag), new ValString("floor"));
	}

	#endregion

	#region Slot-backed Properties

	public int TileIndex
	{
		get =>
			HasSlot(nameof(TileIndex))
				? GetSlot<ValNumber>(nameof(TileIndex))!.IntValue()
				: 0;
		set =>
			SetSlot(nameof(TileIndex), new ValNumber(value));
	}

	public RadialColor ForegroundColor
	{
		get =>
			HasSlot(nameof(ForegroundColor))
				? GetSlot<ValMap>(nameof(ForegroundColor))!.ToRadialColor()
				: RadialColor.White;
		set =>
			SetSlot(nameof(ForegroundColor), value.ToMiniScriptValue());
	}

	public RadialColor? BackgroundColor
	{
		get
		{
			if (!HasSlot(nameof(BackgroundColor)))
				return null;

			var map = GetSlot<ValMap>(nameof(BackgroundColor));
			return map != null ? map.ToRadialColor() : null;
		}
		set
		{
			if (value == null)
				DeleteSlot(nameof(BackgroundColor));
			else
				SetSlot(nameof(BackgroundColor), value.ToMiniScriptValue());
		}
	}

	public bool BlocksMovement
	{
		get =>
			HasSlot(nameof(BlocksMovement))
				&& GetSlot<ValNumber>(nameof(BlocksMovement))!.BoolValue();
		set =>
			SetSlot(nameof(BlocksMovement), value ? ValNumber.one : ValNumber.zero);
	}

	public bool BlocksVision
	{
		get =>
			HasSlot(nameof(BlocksVision))
				&& GetSlot<ValNumber>(nameof(BlocksVision))!.BoolValue();
		set =>
			SetSlot(nameof(BlocksVision), value ? ValNumber.one : ValNumber.zero);
	}

	public string TileTag
	{
		get =>
			HasSlot(nameof(TileTag))
				? GetSlot<ValString>(nameof(TileTag))!.ToString()
				: "floor";
		set =>
			SetSlot(nameof(TileTag), new ValString(value ?? string.Empty));
	}

	#endregion

	#region Asset Loading

	protected override async void OnLoad(IAssetService assets)
	{
		_glyphs = await assets.LoadGlyphSetAsync(
			"asset://image.screen_font",
			TILE_SIZE
		);

		UpdateLayout();
	}

	#endregion

	#region Rendering

	protected override void DrawSelf(IRenderingContext rc)
	{
		if (_glyphs == null)
			return;

		var index = TileIndex;
		if (index < 0 || index >= _glyphs.Count)
			return;

		var glyph = _glyphs[index];

		glyph.Render(
			rc,
			Point.Empty,
			ForegroundColor,
			BackgroundColor
		);
	}

	protected override void UpdateLayout()
	{
		Size = TILE_SIZE;
		base.UpdateLayout();
	}

	#endregion
}