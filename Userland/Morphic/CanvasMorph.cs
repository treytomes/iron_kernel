using System.Drawing;
using IronKernel.Common.ValueObjects;
using Userland.Gfx;

namespace Userland.Morphic;

/// <summary>
/// A Morph that owns a writable pixel buffer.
/// Intended as a script-driven raster surface.
/// </summary>
public sealed class CanvasMorph : Morph
{
	#region Fields

	private RenderImage? _buffer = null;
	// private bool _bufferDirty = true;

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
		_buffer.Clear(RadialColor.Black);

		// _bufferDirty = true;
	}

	protected override void UpdateLayout()
	{
		base.UpdateLayout();
		AllocateBuffer();
	}

	#endregion

	#region Public API (script-callable)

	public void Clear(RadialColor color)
	{
		if (_buffer == null) return;

		Clear(color);

		// _bufferDirty = true;
		Invalidate();
	}

	public void WritePixels(RadialColor?[] pixels)
	{
		_buffer?.WritePixels(pixels);
		Invalidate();
	}

	public void SetPixel(int x, int y, RadialColor color)
	{
		_buffer?.SetPixel(x, y, color);
		// _bufferDirty = true;
	}

	public RadialColor GetPixel(int x, int y)
	{
		return _buffer?.GetPixel(x, y) ?? RadialColor.Black;
	}

	#endregion

	#region Rendering

	protected override void DrawSelf(IRenderingContext rc)
	{
		_buffer?.Render(rc, Point.Empty, RenderImage.RenderFlag.None);
		// _bufferDirty = false;
	}

	#endregion
}