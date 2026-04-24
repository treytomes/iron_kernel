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
	private bool _isIndexed;

	#endregion

	#region Constructors

	public Texture(int width, int height, bool indexed)
	{
		Width = width;
		Height = height;
		_isIndexed = indexed;
		_data = [];

		Id = GL.GenTexture();
		if (Id == 0)
			throw new Exception("Unable to generate palette texture.");

		GL.BindTexture(TextureTarget.Texture2D, Id);
		GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Nearest);
		GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Nearest);

		if (indexed)
		{
			// 16-bit unsigned single-channel — holds indices up to 65535
			_format = PixelFormat.RedInteger;
			_bpp = 2;
			_data = new byte[width * height * _bpp];
			GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.R16ui, width, height, 0, _format, PixelType.UnsignedShort, _data);
		}
		else
		{
			_format = PixelFormat.Rgb;
			_bpp = 3;
			_data = new byte[width * height * _bpp];
			GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgb, width, height, 0, _format, PixelType.UnsignedByte, _data);
		}
	}

	#endregion

	#region Properties

	public byte[] Data
	{
		get => _data;
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

	public void UploadData(ushort[] data)
	{
		Bind();
		GL.TexSubImage2D(TextureTarget.Texture2D, 0, 0, 0, Width, Height, _format, PixelType.UnsignedShort, data);
	}

	protected virtual void Dispose(bool disposing)
	{
		if (!_disposedValue)
		{
			if (Id != 0)
				GL.DeleteTexture(Id);
			_disposedValue = true;
		}
	}

	~Texture()
	{
		Dispose(disposing: false);
	}

	public void Dispose()
	{
		Dispose(disposing: true);
		GC.SuppressFinalize(this);
	}

	#endregion
}
