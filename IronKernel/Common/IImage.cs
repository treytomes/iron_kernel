namespace IronKernel.Common;

public interface IImage<TImage>
	where TImage : IImage<TImage>
{
	int Width { get; }
	int Height { get; }
	TImage Crop(int x, int y, int width, int height);

	/// <summary>
	/// Generate a new <typeparamref name="TImage"/> scaled by <paramref name="factor"/>.
	/// </summary>
	TImage Scale(int factor);
}

public interface IImage<TImage, TPixel> : IImage<TImage>
	where TImage : IImage<TImage, TPixel>
{
	TPixel this[int x, int y] { get; set; }

	TPixel GetPixel(int x, int y);
	void SetPixel(int x, int y, TPixel color);
}