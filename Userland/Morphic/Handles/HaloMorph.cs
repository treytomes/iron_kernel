using IronKernel.Common;
using IronKernel.Common.ValueObjects;
using System.Drawing;
using Userland.Gfx;

namespace Userland.Morphic.Halo;

public sealed class HaloMorph : Morph
{
	#region Fields

	private readonly Morph _target;
	private float _outlineColorLerpWeight = 0.0f;
	private double _totalTime = 0.0;

	#endregion

	#region Constructors

	public HaloMorph(Morph target)
	{
		_target = target;
		Visible = true;
		IsSelectable = false;

		// AddMorph(new ResizeHandleMorph(target, ResizeHandle.TopLeft));
		AddMorph(new ResizeHandleMorph(target, ResizeHandle.TopRight));
		AddMorph(new ResizeHandleMorph(target, ResizeHandle.BottomLeft));
		AddMorph(new ResizeHandleMorph(target, ResizeHandle.BottomRight));

		AddMorph(new MoveHandleMorph(target));
		AddMorph(new DeleteHandleMorph(target));
		AddMorph(new InspectHandleMorph(target));
	}

	#endregion

	#region Properties

	private double AnimationSpeedFactor { get; set; } = 1.0;

	#endregion

	#region Methods

	public override void Update(double deltaTime)
	{
		base.Update(deltaTime);

		_totalTime += deltaTime;

		var wave = MathHelper.TriangleWave(_totalTime, AnimationSpeedFactor);
		var symmetric = (wave * 2f) - 1f;

		// Optional amplitude clamp
		_outlineColorLerpWeight = symmetric * 0.9f;
	}

	protected override void DrawSelf(IRenderingContext rc)
	{
		base.DrawSelf(rc);

		if (!TryGetWorld(out var world)) return;
		if (Style == null) return;

		UpdateFromTarget();

		var bg = (_outlineColorLerpWeight > 0)
			? Style.HaloOutline.Lerp(RadialColor.White, _outlineColorLerpWeight)
			: Style.HaloOutline.Lerp(RadialColor.Black, -_outlineColorLerpWeight);

		rc.RenderRect(
			new Rectangle(Point.Empty, Size),
			bg,
			1);
	}

	protected override void UpdateLayout()
	{
		base.UpdateLayout();
		UpdateFromTarget();
	}

	private void UpdateFromTarget()
	{
		if (_target.Owner == null) return;

		Position = _target.Position;
		Size = _target.Size;

		LayoutHandles();
	}

	private void LayoutHandles()
	{
		foreach (var m in Submorphs.OfType<ResizeHandleMorph>())
		{
			var hs = m.Size.Width / 2;

			m.Position = m.Kind switch
			{
				// ResizeHandle.TopLeft =>
				// 	new Point(-hs, -hs),

				ResizeHandle.TopRight =>
					new Point(+Size.Width - hs, -hs),

				ResizeHandle.BottomLeft =>
					new Point(-hs, +Size.Height - hs),

				ResizeHandle.BottomRight =>
					new Point(+Size.Width - hs, +Size.Height - hs),

				_ => m.Position
			};
		}

		var move = Submorphs.OfType<MoveHandleMorph>().FirstOrDefault();
		if (move != null)
		{
			var hs = move.Size.Width / 2;
			move.Position = new Point(-hs, -hs);
		}

		var delete = Submorphs.OfType<DeleteHandleMorph>().FirstOrDefault();
		if (delete != null)
		{
			var hs = delete.Size.Width / 2;
			delete.Position = new Point(Size.Width / 2 - hs, Size.Height - hs);
		}

		var resize = Submorphs.OfType<InspectHandleMorph>().FirstOrDefault();
		if (resize != null)
		{
			var hs = resize.Size.Width / 2;
			resize.Position = new Point(Size.Width / 2 - hs, -hs);
		}
	}

	#endregion
}
