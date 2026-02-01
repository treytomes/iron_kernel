using System.Drawing;
using IronKernel.Common.ValueObjects;
using IronKernel.Userland.Gfx;
using IronKernel.Userland.Morphic.Handles;

namespace IronKernel.Userland.Morphic;

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

		AddMorph(new MoveHandleMorph(target)
		{
			Position = new Point(-4, -4)
		});

		AddMorph(new DeleteHandleMorph(target)
		{

			Position = new Point(
				target.Position.X + target.Size.Width / 2 - 4,
				target.Position.Y + target.Size.Height - 4
			)
		});

		AddMorph(new InspectHandleMorph(target)
		{

			Position = new Point(
				target.Position.X + target.Size.Width / 2 - 4,
				target.Position.Y - 4
			)
		});
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

		var wave = TriangleWave(_totalTime, AnimationSpeedFactor);
		var symmetric = (wave * 2f) - 1f;

		// Optional amplitude clamp
		_outlineColorLerpWeight = symmetric * 0.9f;
	}


	// t = time in seconds, freq = cycles per second
	static float TriangleWave(double t, double freq)
	{
		var phase = (t * freq) % 1.0;   // [0,1)
		return phase < 0.5
			? (float)(phase * 2.0)
			: (float)(2.0 - phase * 2.0);
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
			new Rectangle(new Point(0, 0), Size),
			bg,
			1);
	}

	private void UpdateFromTarget()
	{
		Position = _target.Position;
		Size = _target.Size;

		LayoutHandles();
	}

	private void LayoutHandles()
	{
		var hs = 3;

		foreach (var m in Submorphs.OfType<ResizeHandleMorph>())
		{
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

		Submorphs.OfType<MoveHandleMorph>().Single().Position = new Point(
			 -4,
			 -4
		// +Size.Width / 2 - 6,
		// -16
		);
		Submorphs.OfType<DeleteHandleMorph>().Single().Position = new Point(
			 Size.Width / 2 - 4,
			 Size.Height - 4
		);
		Submorphs.OfType<InspectHandleMorph>().Single().Position = new Point(
			 Size.Width / 2 - 4,
			 -4
		);
	}

	#endregion
}
