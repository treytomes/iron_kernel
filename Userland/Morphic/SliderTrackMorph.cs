using System.Drawing;
using Userland.Gfx;

namespace Userland.Morphic.Controls;

sealed class SliderTrackMorph : Morph
{
	private readonly SliderThumbMorph _thumb;
	private readonly Func<float> _getNormalized;

	public float Min { get; set; } = 0f;
	public float Max { get; set; } = 1f;
	public float Step { get; set; } = 0f;

	public SliderTrackMorph(
		Func<float> getNormalized,
		Action<float> setNormalized)
	{
		_getNormalized = getNormalized;
		_thumb = new SliderThumbMorph(getNormalized, setNormalized);
		AddMorph(_thumb);

		// Reasonable default; HorizontalStackMorph can override
		Size = new Size(80, _thumb.Size.Height);
	}

	protected override void UpdateLayout()
	{
		base.UpdateLayout();

		// Center the track vertically in its owner
		if (Owner != null)
		{
			int y = (Owner.Size.Height - Size.Height) / 2;
			Position = new Point(Position.X, y);
		}

		// Position thumb horizontally
		float t = Math.Clamp(_getNormalized(), 0f, 1f);
		int x = (int)((Size.Width - _thumb.Size.Width) * t);
		_thumb.Position = new Point(x, -_thumb.Size.Height / 2);
	}

	protected override void DrawSelf(IRenderingContext rc)
	{
		if (Style == null)
			return;

		var s = Style.Semantic;
		int midY = Size.Height / 2;

		// Track line
		rc.RenderLine(
			new Point(0, midY),
			new Point(Size.Width, midY),
			s.Border);

		// Tick marks (only if stepped)
		if (Step > 0f && Max > Min)
		{
			int steps = (int)((Max - Min) / Step);
			if (steps > 0)
			{
				for (int i = 0; i <= steps; i++)
				{
					float t = i / (float)steps;
					int x = (int)(t * Size.Width);

					rc.RenderLine(
						new Point(x, midY - 2),
						new Point(x, midY + 2),
						s.MutedText);
				}
			}
		}
	}
}