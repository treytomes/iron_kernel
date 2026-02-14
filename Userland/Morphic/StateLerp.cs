namespace Userland.Morphic;

public sealed class StateLerp
{
	private float _value;

	public float Value => _value;

	public void Update(bool active, double deltaMs, float speed)
	{
		float target = active ? 1f : 0f;
		float delta = (float)(deltaMs * speed);

		if (_value < target)
			_value = MathF.Min(target, _value + delta);
		else if (_value > target)
			_value = MathF.Max(target, _value - delta);
	}
}