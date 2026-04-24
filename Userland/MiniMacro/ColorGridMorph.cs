using System.Drawing;
using IronKernel.Common.ValueObjects;
using Userland.Gfx;
using Userland.Morphic;
using Userland.Morphic.Events;
using Color = IronKernel.Common.ValueObjects.Color;

namespace Userland.MiniMacro;

/// <summary>
/// A 2D color spectrum grid. X axis controls one channel, Y axis controls another.
/// The third channel is fixed at 0. Click anywhere to cycle through axis pairs.
/// </summary>
public sealed class ColorGridMorph : Morph
{
	private enum Axis { RG, RB, GB }

	private Axis _axis = Axis.RG;
	private Color?[] _rowBuffer = [];

	public ColorGridMorph()
	{
		Size = new Size(320, 240);
		IsSelectable = true;
	}

	protected override void DrawSelf(IRenderingContext rc)
	{
		var w = Size.Width;
		var h = Size.Height;

		if (_rowBuffer.Length != w)
			_rowBuffer = new Color?[w];

		for (var py = 0; py < h; py++)
		{
			var v = 1f - py / (h - 1f);
			for (var px = 0; px < w; px++)
			{
				var u = px / (w - 1f);
				_rowBuffer[px] = _axis switch
				{
					Axis.RG => new Color(u, v, 0f),
					Axis.RB => new Color(u, 0f, v),
					Axis.GB => new Color(0f, u, v),
					_       => Color.Black,
				};
			}
			rc.RenderSpan(0, py, _rowBuffer);
		}
	}

	public override void OnPointerDown(PointerDownEvent e)
	{
		_axis = _axis switch
		{
			Axis.RG => Axis.RB,
			Axis.RB => Axis.GB,
			_       => Axis.RG,
		};
		Invalidate();
	}
}
