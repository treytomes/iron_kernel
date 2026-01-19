using OpenTK.Graphics.OpenGL4;

namespace IronKernel.Modules.Framebuffer;

public class Texture : IDisposable
{
	#region Fields

	public readonly int Id;
	public readonly int Width;
	public readonly int Height;
	private byte[] _data;
	private PixelFormat _format;
	private int _bpp;
	private bool _disposedValue = false;

	#endregion

	#region Constructors

	public Texture(int width, int height, bool indexed)
	{
		Width = width;
		Height = height;
		_data = [];

		// Generate the reference texture.
		Id = GL.GenTexture();
		if (Id == 0)
		{
			throw new Exception("Unable to generate palette texture.");
		}

		GL.BindTexture(TextureTarget.Texture2D, Id);
		GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Nearest);
		GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Nearest);

		_format = indexed ? PixelFormat.Red : PixelFormat.Rgb;
		_bpp = indexed ? 1 : 3;
		_data = new byte[width * height * _bpp];
	}

	#endregion

	#region Properties

	public byte[] Data
	{
		get
		{
			return _data;
		}
		set
		{
			_data = new byte[value.Length];
			value.CopyTo(_data, 0);

			Bind();
			GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgb, Width, Height, 0, _format, PixelType.UnsignedByte, _data);
		}
	}

	#endregion

	#region Methods

	public void Bind()
	{
		GL.BindTexture(TextureTarget.Texture2D, Id);
	}

	protected virtual void Dispose(bool disposing)
	{
		if (!_disposedValue)
		{
			if (disposing)
			{
				// Dispose managed state (managed objects).
			}

			// Free unmanaged resources (unmanaged objects) and override finalizer.
			if (Id != 0)
			{
				GL.DeleteTexture(Id);
			}
			_disposedValue = true;
		}
	}

	~Texture()
	{
		// Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
		Dispose(disposing: false);
	}

	public void Dispose()
	{
		// Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
		Dispose(disposing: true);
		GC.SuppressFinalize(this);
	}

	#endregion
}