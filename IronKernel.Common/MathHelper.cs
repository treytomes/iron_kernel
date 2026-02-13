namespace IronKernel.Common;

public static class MathHelper
{
	public static float Lerp(float start, float end, float t)
	{
		return start + (end - start) * t;
	}
}