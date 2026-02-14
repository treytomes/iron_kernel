using System.Drawing;
using IronKernel.Common.ValueObjects;
using Userland.Gfx;
using Userland.Morphic.Events;
using Userland.Morphic.Inspector;
using Userland.Services;

namespace Userland.Morphic;

public sealed class CheckBoxMorph : Morph, IValueContentMorph
{
	#region Fields
	private bool? _checked;
	private readonly Action<bool?>? _setter;

	private Font? _font;

	private readonly StateLerp _hover = new();
	private readonly StateLerp _press = new();
	#endregion

	#region Configuration
	public bool AllowIndeterminate { get; set; } = false;

	public int EmptyCheckBoxTile { get; set; } = 132;
	public int CheckedCheckBoxTile { get; set; } = 133;
	public int IndeterminateCheckBoxTile { get; set; } = 134;
	#endregion

	#region Construction
	public CheckBoxMorph(Action<bool?>? setter = null)
	{
		_setter = setter;
		IsSelectable = true;
	}
	#endregion

	#region Loading
	protected override async void OnLoad(IAssetService assets)
	{
		if (Style == null)
			throw new InvalidOperationException("Style is null.");

		var fs = Style.DefaultFontStyle;

		_font = await assets.LoadFontAsync(
			fs.Url,
			fs.TileSize,
			fs.GlyphOffset
		);

		// Entire control derives from font metrics
		Size = _font.TileSize;
		InvalidateLayout();
	}
	#endregion

	#region Value binding
	public void Refresh(object? value)
	{
		_checked = value as bool?;
		Invalidate();
	}
	#endregion

	#region Input
	public override void OnPointerDown(PointerDownEvent e)
	{
		if (!IsEnabled || e.Button != MouseButton.Left)
			return;

		_checked = NextState(_checked);
		_setter?.Invoke(_checked);
		Invalidate();

		base.OnPointerDown(e);
	}

	private bool? NextState(bool? current)
	{
		if (!AllowIndeterminate)
			return current != true;

		return current switch
		{
			false => true,
			true => null,
			null => false
		};
	}
	#endregion

	#region Update
	public override void Update(double deltaMs)
	{
		_hover.Update(IsEffectivelyHovered && IsEnabled, deltaMs, 0.015f);
		_press.Update(IsPressed && IsEnabled, deltaMs, 0.02f);

		if (_hover.Value > 0f || _press.Value > 0f)
			Invalidate();
	}
	#endregion

	#region Rendering
	protected override void DrawSelf(IRenderingContext rc)
	{
		if (_font == null || Style == null)
			return;

		var s = Style.Semantic;

		int tile =
			_checked == true
				? CheckedCheckBoxTile
				: _checked == null
					? IndeterminateCheckBoxTile
					: EmptyCheckBoxTile;

		// Base state resolution (shared pattern) [1]
		RadialColor fg =
			!IsEnabled ? s.SecondaryText : s.Text;

		RadialColor bg = s.Surface;

		// Animated overlays
		if (_hover.Value > 0f)
			fg = fg.Lerp(s.PrimaryHover, _hover.Value);

		if (_press.Value > 0f)
			fg = fg.Lerp(s.PrimaryActive, _press.Value);

		_font.WriteChar(
			rc,
			(char)tile,
			Point.Empty,
			fg,
			bg
		);
	}
	#endregion
}