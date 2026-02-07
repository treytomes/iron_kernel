using OpenTK.Graphics.OpenGL4;

namespace IronKernel.Modules.Framebuffer.Shaders;

/// <summary>  
/// Represents an OpenGL shader object.  
/// </summary>  
public class Shader : IDisposable
{
	#region Fields

	private readonly int _id;
	private bool _disposedValue;

	#endregion

	#region Constructors

	/// <summary>  
	/// Creates a new shader of the specified type with the given source code.  
	/// </summary>  
	/// <param name="type">The type of shader to create.</param>  
	/// <param name="source">The GLSL source code for the shader.</param>  
	/// <exception cref="ArgumentNullException">Thrown when source is null.</exception>  
	/// <exception cref="Exception">Thrown when shader creation or compilation fails.</exception>  
	public Shader(ShaderType type, string source)
	{
		if (string.IsNullOrEmpty(source))
			throw new ArgumentNullException(nameof(source), "Shader source cannot be null or empty.");

		Type = type;

		_id = GL.CreateShader(type);
		if (_id == 0)
			throw new Exception($"Failed to create shader of type {type} ({GL.GetError()}): {source}");

		try
		{
			GL.ShaderSource(_id, source);
			GL.CompileShader(_id);
			CheckShaderCompileStatus();
		}
		catch
		{
			// Clean up if an exception occurs  
			GL.DeleteShader(_id);
			throw;
		}
	}

	#endregion

	#region Properties

	/// <summary>  
	/// Gets the OpenGL ID of the shader.  
	/// </summary>  
	public int Id => _id;

	/// <summary>  
	/// Gets the type of this shader.  
	/// </summary>  
	public ShaderType Type { get; }

	#endregion

	#region Methods

	/// <summary>  
	/// Creates a new shader by loading its source code from a file.  
	/// </summary>  
	/// <param name="type">The type of shader to create.</param>  
	/// <param name="path">The path to the file containing the shader source code.</param>  
	/// <returns>A new Shader instance.</returns>  
	/// <exception cref="ArgumentNullException">Thrown when path is null.</exception>  
	/// <exception cref="FileNotFoundException">Thrown when the shader file cannot be found.</exception>  
	/// <exception cref="IOException">Thrown when an I/O error occurs while reading the file.</exception>  
	/// <exception cref="Exception">Thrown when shader creation or compilation fails.</exception>  
	public static Shader FromFile(ShaderType type, string path)
	{
		if (string.IsNullOrEmpty(path))
			throw new ArgumentNullException(nameof(path), "Path cannot be null or empty.");

		Directory.SetCurrentDirectory(AppContext.BaseDirectory);
		if (!File.Exists(path))
			throw new FileNotFoundException($"Shader file not found: {path}", path);

		try
		{
			var source = File.ReadAllText(path);
			return new Shader(type, source);
		}
		catch (IOException ex)
		{
			throw new IOException($"Failed to read shader file: {path}", ex);
		}
	}

	/// <summary>  
	/// Checks if the shader was compiled successfully.  
	/// </summary>  
	/// <exception cref="Exception">Thrown when shader compilation fails.</exception>  
	private void CheckShaderCompileStatus()
	{
		GL.GetShader(_id, ShaderParameter.CompileStatus, out int status);
		if (status == 0)
		{
			string infoLog = GL.GetShaderInfoLog(_id);
			throw new Exception($"Shader compilation failed: {infoLog}");
		}
	}

	/// <summary>  
	/// Releases the unmanaged resources used by the shader.  
	/// </summary>  
	/// <param name="disposing">True if called from Dispose(), false if called from finalizer.</param>  
	protected virtual void Dispose(bool disposing)
	{
		if (!_disposedValue)
		{
			if (_id != 0)
			{
				GL.DeleteShader(_id);
			}
			_disposedValue = true;
		}
	}

	/// <summary>  
	/// Disposes the shader, releasing all resources.  
	/// </summary>  
	public void Dispose()
	{
		Dispose(disposing: true);
		GC.SuppressFinalize(this);
	}

	#endregion
}