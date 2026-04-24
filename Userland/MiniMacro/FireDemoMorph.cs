using IronKernel.Common.ValueObjects;
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
	private const int PALETTE_SIZE = 256;

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
		// ShouldClipToBounds = true;

		Size = new Size(DEFAULT_WIDTH, DEFAULT_HEIGHT);
		_fire = new byte[Size.Height, Size.Width];
		Array.Clear(_fire);

		// Fire gradient: black → red → orange → yellow → white.
		// Stops are biased so the typical simulation output range (intensity
		// 80–140, t≈0.31–0.55) maps to vivid orange rather than dark red.
		var stops = new (float t, Color c)[]
		{
			(0f,    Color.Black),
			(0.1f,  new Color(1f, 0f,   0f)),   // red
			(0.4f,  new Color(1f, 0.6f, 0f)),   // orange
			(0.65f, new Color(1f, 1f,   0f)),   // yellow
			(1f,    Color.White),
		};
		for (var x = 0; x < PALETTE_SIZE; x++)
		{
			var t = x / (PALETTE_SIZE - 1f);
			// Find the two stops that bracket t.
			int i = 0;
			while (i < stops.Length - 2 && t > stops[i + 1].t) i++;
			var lo = stops[i];
			var hi = stops[i + 1];
			var u = (t - lo.t) / (hi.t - lo.t);
			_palette[x] = lo.c.Lerp(hi.c, u);
		}
	}

	#endregion

	#region Properties

	public float BlendFactor { get; set; } = 32f / 129f;

	#endregion

	#region Methods

	protected override void UpdateLayout()
	{
		base.UpdateLayout();
		_fire = new byte[Size.Height, Size.Width];
		Array.Clear(_fire);
	}

	protected override void DrawSelf(IRenderingContext rc)
	{
		rc.RenderFilledRect(new Rectangle(Point.Empty, Size), Color.Black);

		// Randomize the bottom row of the fire buffer.
		for (var x = 0; x < Size.Width; x++)
		{
			SetFire(x, Size.Height - 1, (byte)(Math.Abs(32768 + _random.Next()) % 256));
		}

		// Do the fire calculations for every pixel, from top to bottom.
		for (var y = 0; y < Size.Height - 1; y++)
		{
			for (var x = 0; x < Size.Width; x++)
			{
				var southWest = GetFire((x - 1 + Size.Width) % Size.Width, (y + 1) % Size.Height);
				var south = GetFire(x % Size.Width, (y + 1) % Size.Height);
				var southEast = GetFire((x + 1) % Size.Width, (y + 1) % Size.Height);
				var southSouth = GetFire(x % Size.Width, (y + 2) % Size.Height);
				SetFire(x, y, (byte)((southWest + south + southEast + southSouth) * BlendFactor));
			}
		}

		// Render fire buffer row by row using the palette.
		if (_rowBuffer.Length != Size.Width)
			_rowBuffer = new Color?[Size.Width];

		for (var y = 0; y < Size.Height; y++)
		{
			for (var x = 0; x < Size.Width; x++)
			{
				var f = GetFire(x, y);
				_rowBuffer[x] = (uint)f < (uint)_palette.Length ? _palette[f] : Color.Black;
			}
			rc.RenderSpan(0, y, _rowBuffer);
		}
	}

	private byte GetFire(int x, int y)
	{
		if (y < 0 || y >= _fire.GetLength(0) || x < 0 || x >= _fire.GetLength(1)) return 0;
		return _fire[y, x];
	}

	private void SetFire(int x, int y, byte value)
	{
		if (y < 0 || y >= _fire.GetLength(0) || x < 0 || x >= _fire.GetLength(1)) return;
		_fire[y, x] = value;
	}

	#endregion
}
