using System.Drawing;
using IronKernel.Common.ValueObjects;
using Userland.Gfx;
using Color = IronKernel.Common.ValueObjects.Color;

namespace Userland.Morphic;

/// <summary>
/// A Morph that owns a writable pixel buffer.
/// Intended as a script-driven raster surface.
/// </summary>
public sealed class CanvasMorph : Morph
{
	#region Fields

	private RenderImage? _buffer = null;

	#endregion

	#region Constructors

	public CanvasMorph()
	{
		IsSelectable = true;
	}

	public CanvasMorph(Size size)
	{
		IsSelectable = true;
		Size = size;
		AllocateBuffer();
	}

	#endregion

	#region Buffer management

	private void AllocateBuffer()
	{
		if (Size == _buffer?.Size) return;

		var w = Math.Max(0, Size.Width);
		var h = Math.Max(0, Size.Height);

		_buffer = new RenderImage(w, h);
		_buffer.Clear(Color.Black);
	}

	protected override void UpdateLayout()
	{
		base.UpdateLayout();
		AllocateBuffer();
	}

	#endregion

	#region Public API (script-callable)

	public void Clear(Color? color)
	{
		if (_buffer == null) return;
		_buffer.Clear(color);
		Invalidate();
	}

	public void WritePixels(Color?[] pixels)
	{
		_buffer?.WritePixels(pixels);
		Invalidate();
	}

	public void SetPixel(int x, int y, Color? color)
	{
		_buffer?.SetPixel(x, y, color);
	}

	public Color? GetPixel(int x, int y)
	{
		return _buffer?.GetPixel(x, y);
	}

	#endregion

	#region Rendering

	protected override void DrawSelf(IRenderingContext rc)
	{
		_buffer?.Render(rc, Point.Empty, RenderImage.RenderFlag.None);
	}

	#endregion
}
