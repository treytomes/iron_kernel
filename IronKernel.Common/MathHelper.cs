namespace IronKernel.Common;

public static class MathHelper
{
	public static float Lerp(float start, float end, float t)
	{
		return start + (end - start) * t;
	}

	// t = time in seconds, freq = cycles per second
	public static float TriangleWave(double t, double freq)
	{
		var phase = (t * freq) % 1.0;   // [0,1)
		return phase < 0.5
			? (float)(phase * 2.0)
			: (float)(2.0 - phase * 2.0);
	}
}