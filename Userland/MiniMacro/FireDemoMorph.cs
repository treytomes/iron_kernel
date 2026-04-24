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

		// Generate the palette.
		// Hue goes from 0 to 85: red to yellow (0–85/255 in 0–1 scale).
		// Saturation is always 1.0.
		// Lightness is 0..1 for x=0..128, and 1.0 for x=128..255.
		for (var x = 0; x < PALETTE_SIZE; x++)
		{
			var h = (x / 3) / 255f;
			var s = 1f;
			var l = Math.Min(1f, (x * 2) / 255f);
			_palette[x] = Color.FromHSL(h, s, l);
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
