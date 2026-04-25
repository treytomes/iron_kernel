using System.Drawing;
using Userland.Gfx;
using Userland.Morphic;
using Color = IronKernel.Common.ValueObjects.Color;

namespace Userland.MiniMacro;

public class FireDemoMorph : Morph
{
	#region Constants

	private const int DEFAULT_WIDTH = 320;
	private const int DEFAULT_HEIGHT = 240;

	// Canonical Doom-fire palette size: 36 heat levels (0 = cold, 35 = max).
	private const int HEAT_MAX = 35;
	private const int PALETTE_SIZE = HEAT_MAX + 1;

	#endregion

	#region Fields

	private byte[,] _fire;
	private readonly Color[] _palette = new Color[PALETTE_SIZE];
	private readonly Random _random = new();
	private Color?[] _rowBuffer = [];

	#endregion

	#region Constructors

	public FireDemoMorph()
		: base()
	{
		Size = new Size(DEFAULT_WIDTH, DEFAULT_HEIGHT);
		_fire = new byte[Size.Height, Size.Width];

		BuildPalette();
		SeedBottomRow();
	}

	#endregion

	#region Methods

	protected override void UpdateLayout()
	{
		base.UpdateLayout();
		_fire = new byte[Size.Height, Size.Width];
		SeedBottomRow();
	}

	protected override void DrawSelf(IRenderingContext rc)
	{
		// Propagate fire upward: each pixel cools toward the heat of the pixel below it.
		for (var y = 0; y < Size.Height - 1; y++)
		{
			for (var x = 0; x < Size.Width; x++)
			{
				var below = _fire[y + 1, x];
				var decay = _random.Next(0, 3);
				var heat = Math.Max(0, below - decay);
				// Spread left by 0 or 1 pixels randomly for turbulence.
				var dst = (x - _random.Next(0, 2) + Size.Width) % Size.Width;
				_fire[y, dst] = (byte)heat;
			}
		}

		// Render fire buffer row by row using the palette.
		if (_rowBuffer.Length != Size.Width)
			_rowBuffer = new Color?[Size.Width];

		for (var y = 0; y < Size.Height; y++)
		{
			for (var x = 0; x < Size.Width; x++)
			{
				var heat = _fire[y, x];
				_rowBuffer[x] = heat < PALETTE_SIZE ? _palette[heat] : Color.White;
			}
			rc.RenderSpan(0, y, _rowBuffer);
		}
	}

	// Bottom row stays at maximum heat — it is the perpetual fire source.
	private void SeedBottomRow()
	{
		var lastRow = Size.Height - 1;
		for (var x = 0; x < Size.Width; x++)
			_fire[lastRow, x] = HEAT_MAX;
	}

	// Classic Doom-fire gradient: black → dark red → red → orange → yellow → white.
	private void BuildPalette()
	{
		var stops = new (float t, Color c)[]
		{
			(0f,    Color.Black),
			(0.25f, new Color(0.5f,  0f,    0f)),   // dark red
			(0.45f, new Color(1f,    0f,    0f)),   // red
			(0.65f, new Color(1f,    0.5f,  0f)),   // orange
			(0.85f, new Color(1f,    1f,    0f)),   // yellow
			(1f,    Color.White),
		};

		for (var i = 0; i < PALETTE_SIZE; i++)
		{
			var t = i / (float)(PALETTE_SIZE - 1);
			var s = 0;
			while (s < stops.Length - 2 && t > stops[s + 1].t) s++;
			var lo = stops[s];
			var hi = stops[s + 1];
			var u = (t - lo.t) / (hi.t - lo.t);
			_palette[i] = lo.c.Lerp(hi.c, u);
		}
	}

	#endregion
}
