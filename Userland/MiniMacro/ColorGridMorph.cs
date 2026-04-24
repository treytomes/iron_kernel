using System.Drawing;
using IronKernel.Common.ValueObjects;
using Userland.Gfx;
using Userland.Morphic;
using Userland.Morphic.Events;
using Color = IronKernel.Common.ValueObjects.Color;

namespace Userland.MiniMacro;

public sealed class ColorGridMorph : WindowMorph
{
	#region Inner types

	private enum Axis { RG, RB, GB }

	private sealed class GridSurface : Morph
	{
		private Axis _axis = Axis.RG;
		private Color?[] _rowBuffer = [];

		public Axis CurrentAxis
		{
			get => _axis;
			set { _axis = value; Invalidate(); }
		}

		public Action? OnPointerDownCallback { get; set; }

		public override void OnPointerDown(PointerDownEvent e)
		{
			base.OnPointerDown(e);
			OnPointerDownCallback?.Invoke();
		}

		protected override void DrawSelf(IRenderingContext rc)
		{
			var w = Size.Width;
			var h = Size.Height;
			if (w <= 0 || h <= 0) return;

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
	}

	#endregion

	#region Fields

	private static readonly (string xLabel, string yLabel, string title)[] _axisInfo =
	[
		("R →", "G ↑", "R / G"),
		("R →", "B ↑", "R / B"),
		("G →", "B ↑", "G / B"),
	];

	private Axis _axis = Axis.RG;
	private readonly GridSurface _grid;
	private readonly LabelMorph _xLabel;
	private readonly LabelMorph _yLabel;

	#endregion

	#region Constructor

	public ColorGridMorph()
		: base(Point.Empty, new Size(240, 260), "Color Grid — R / G")
	{
		_grid = new GridSurface
		{
			Size = new Size(224, 224),
			IsSelectable = true,
		};

		_xLabel = new LabelMorph { IsSelectable = false, BackgroundColor = null };
		_yLabel = new LabelMorph { IsSelectable = false, BackgroundColor = null };

		Content.AddMorph(_grid);
		Content.AddMorph(_xLabel);
		Content.AddMorph(_yLabel);

		_grid.OnPointerDownCallback = CycleAxis;

		UpdateLabels();
	}

	#endregion

	#region Layout

	protected override void UpdateLayout()
	{
		base.UpdateLayout();

		var cw = Content.Size.Width;
		var ch = Content.Size.Height;
		if (cw <= 0 || ch <= 0) return;

		var labelHeight = _xLabel.Size.Height;
		var gridSize = Math.Min(cw, ch - labelHeight * 2) - 2;
		if (gridSize < 8) return;

		// Grid centered horizontally, below Y label, above X label
		var gx = (cw - gridSize) / 2;
		var gy = labelHeight + 1;
		_grid.Size = new Size(gridSize, gridSize);
		_grid.Position = new Point(gx, gy);

		// X axis label centered below grid
		_xLabel.Position = new Point((cw - _xLabel.Size.Width) / 2, gy + gridSize + 1);

		// Y axis label centered above grid
		_yLabel.Position = new Point((cw - _yLabel.Size.Width) / 2, 1);
	}

	#endregion

	#region Helpers

	private void CycleAxis()
	{
		_axis = _axis switch
		{
			Axis.RG => Axis.RB,
			Axis.RB => Axis.GB,
			_       => Axis.RG,
		};
		_grid.CurrentAxis = _axis;
		UpdateLabels();
		InvalidateLayout();
	}

	private void UpdateLabels()
	{
		var info = _axisInfo[(int)_axis];
		_xLabel.Text = info.xLabel;
		_yLabel.Text = info.yLabel;
		Title = $"Color Grid — {info.title}  (click to cycle)";
	}

	#endregion
}
