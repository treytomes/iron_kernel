using OpenTK.Graphics.OpenGL4;

namespace IronKernel.Modules.Framebuffer.Shaders;

/// <summary>  
/// Represents an OpenGL shader program that can be used for rendering or compute operations.  
/// </summary>  
public class ShaderProgram : IDisposable
{
	#region Fields  

	private readonly int _id;
	private bool _disposedValue;

	#endregion

	#region Constructors  

	/// <summary>  
	/// Creates a new shader program by linking the provided shaders.  
	/// </summary>  
	/// <param name="shaders">The shaders to link into the program.</param>  
	/// <exception cref="ArgumentNullException">Thrown when shaders array is null.</exception>  
	/// <exception cref="ArgumentException">Thrown when no shaders are provided.</exception>  
	/// <exception cref="Exception">Thrown when program creation or linking fails.</exception>  
	private ShaderProgram(params Shader[] shaders)
	{
		if (shaders == null)
			throw new ArgumentNullException(nameof(shaders));

		if (shaders.Length == 0)
			throw new ArgumentException("At least one shader must be provided.", nameof(shaders));

		_id = GL.CreateProgram();
		if (_id == 0)
			throw new Exception("Failed to create shader program.");

		try
		{
			// Attach all shaders  
			foreach (var shader in shaders)
			{
				if (shader == null)
					throw new ArgumentException("Shader cannot be null.", nameof(shaders));

				GL.AttachShader(_id, shader.Id);
			}

			// Link the program  
			GL.LinkProgram(_id);
			CheckProgramLinkStatus();

			// Detach shaders after linking  
			foreach (var shader in shaders)
			{
				GL.DetachShader(_id, shader.Id);
			}
		}
		catch
		{
			// Clean up if an exception occurs  
			GL.DeleteProgram(_id);
			throw;
		}
	}

	#endregion

	#region Properties  

	/// <summary>  
	/// Gets the OpenGL ID of the shader program.  
	/// </summary>  
	public int Id => _id;

	#endregion

	#region Methods  

	/// <summary>  
	/// Creates a shader program for graphics rendering from vertex and fragment shader files.  
	/// </summary>  
	/// <param name="vertexShaderPath">Path to the vertex shader file.</param>  
	/// <param name="fragmentShaderPath">Path to the fragment shader file.</param>  
	/// <returns>A new ShaderProgram instance.</returns>  
	/// <exception cref="ArgumentNullException">Thrown when either path is null.</exception>  
	/// <exception cref="FileNotFoundException">Thrown when shader files cannot be found.</exception>  
	/// <exception cref="Exception">Thrown when shader compilation or program linking fails.</exception>  
	public static ShaderProgram ForGraphics(string vertexShaderPath, string fragmentShaderPath)
	{
		if (string.IsNullOrEmpty(vertexShaderPath))
			throw new ArgumentNullException(nameof(vertexShaderPath));

		if (string.IsNullOrEmpty(fragmentShaderPath))
			throw new ArgumentNullException(nameof(fragmentShaderPath));

		using var vertexShader = Shader.FromFile(ShaderType.VertexShader, vertexShaderPath);
		using var fragmentShader = Shader.FromFile(ShaderType.FragmentShader, fragmentShaderPath);
		return new ShaderProgram(vertexShader, fragmentShader);
	}

	/// <summary>  
	/// Creates a shader program for compute operations from a compute shader source.  
	/// </summary>  
	/// <param name="computeShaderSource">Source code of the compute shader.</param>  
	/// <returns>A new ShaderProgram instance.</returns>  
	/// <exception cref="ArgumentNullException">Thrown when the source is null.</exception>  
	/// <exception cref="NotSupportedException">Thrown when compute shaders are not supported.</exception>  
	/// <exception cref="Exception">Thrown when shader compilation or program linking fails.</exception>  
	public static ShaderProgram ForCompute(string computeShaderSource)
	{
		if (string.IsNullOrEmpty(computeShaderSource))
			throw new ArgumentNullException(nameof(computeShaderSource));

		// Check if compute shaders are supported  
		int majorVersion = GL.GetInteger(GetPName.MajorVersion);
		int minorVersion = GL.GetInteger(GetPName.MinorVersion);

		if (majorVersion < 4 || (majorVersion == 4 && minorVersion < 3))
			throw new NotSupportedException("Compute shaders require OpenGL 4.3 or higher.");

		using var computeShader = new Shader(ShaderType.ComputeShader, computeShaderSource);
		return new ShaderProgram(computeShader);
	}

	/// <summary>  
	/// Activates this shader program for rendering or compute operations.  
	/// </summary>  
	public void Use()
	{
		if (_disposedValue)
			throw new ObjectDisposedException(nameof(ShaderProgram));

		GL.UseProgram(_id);
	}

	/// <summary>  
	/// Gets the location of a uniform variable in the shader program.  
	/// </summary>  
	/// <param name="name">The name of the uniform variable.</param>  
	/// <returns>The location of the uniform variable or -1 if not found.</returns>  
	/// <exception cref="ArgumentNullException">Thrown when name is null.</exception>  
	/// <exception cref="ObjectDisposedException">Thrown when the program has been disposed.</exception>  
	public int GetUniformLocation(string name)
	{
		if (string.IsNullOrEmpty(name))
			throw new ArgumentNullException(nameof(name));

		if (_disposedValue)
			throw new ObjectDisposedException(nameof(ShaderProgram));

		return GL.GetUniformLocation(_id, name);
	}

	/// <summary>  
	/// Gets a uniform wrapper for a single-value uniform.  
	/// </summary>  
	/// <param name="name">The name of the uniform variable.</param>  
	/// <returns>A ShaderUniform1 instance for the specified uniform.</returns>  
	public ShaderUniform1 GetUniform1(string name)
	{
		int location = GetUniformLocation(name);
		if (location == -1)
			Console.WriteLine($"Warning: Uniform '{name}' not found in shader program {_id}.");

		return new ShaderUniform1(location);
	}

	/// <summary>  
	/// Gets a uniform wrapper for a two-component uniform.  
	/// </summary>  
	/// <param name="name">The name of the uniform variable.</param>  
	/// <returns>A ShaderUniform2 instance for the specified uniform.</returns>  
	public ShaderUniform2 GetUniform2(string name)
	{
		int location = GetUniformLocation(name);
		if (location == -1)
			Console.WriteLine($"Warning: Uniform '{name}' not found in shader program {_id}.");

		return new ShaderUniform2(location);
	}

	/// <summary>  
	/// Gets a uniform wrapper for a three-component uniform.  
	/// </summary>  
	/// <param name="name">The name of the uniform variable.</param>  
	/// <returns>A ShaderUniform3 instance for the specified uniform.</returns>  
	public ShaderUniform3 GetUniform3(string name)
	{
		int location = GetUniformLocation(name);
		if (location == -1)
			Console.WriteLine($"Warning: Uniform '{name}' not found in shader program {_id}.");

		return new ShaderUniform3(location);
	}

	/// <summary>  
	/// Gets a uniform wrapper for a four-component uniform.  
	/// </summary>  
	/// <param name="name">The name of the uniform variable.</param>  
	/// <returns>A ShaderUniform4 instance for the specified uniform.</returns>  
	public ShaderUniform4 GetUniform4(string name)
	{
		int location = GetUniformLocation(name);
		if (location == -1)
			Console.WriteLine($"Warning: Uniform '{name}' not found in shader program {_id}.");

		return new ShaderUniform4(location);
	}

	/// <summary>  
	/// Gets a uniform wrapper for a matrix uniform.  
	/// </summary>  
	/// <param name="name">The name of the uniform variable.</param>  
	/// <returns>A ShaderUniformMatrix instance for the specified uniform.</returns>  
	public ShaderUniformMatrix GetUniformMatrix(string name)
	{
		int location = GetUniformLocation(name);
		if (location == -1)
			Console.WriteLine($"Warning: Uniform '{name}' not found in shader program {_id}.");

		return new ShaderUniformMatrix(location);
	}

	/// <summary>  
	/// Gets a uniform wrapper of the specified type.  
	/// </summary>  
	/// <typeparam name="T">The type of uniform wrapper to return.</typeparam>  
	/// <param name="name">The name of the uniform variable.</param>  
	/// <returns>A uniform wrapper of type T for the specified uniform.</returns>  
	/// <exception cref="ArgumentException">Thrown when T is not a supported uniform type.</exception>  
	public T GetUniform<T>(string name) where T : class, IShaderUniform
	{
		int location = GetUniformLocation(name);
		if (location == -1)
			Console.WriteLine($"Warning: Uniform '{name}' not found in shader program {_id}.");

		// Use pattern matching to create the correct type  
		if (typeof(T) == typeof(ShaderUniform1))
			return (T)(IShaderUniform)new ShaderUniform1(location);
		else if (typeof(T) == typeof(ShaderUniform2))
			return (T)(IShaderUniform)new ShaderUniform2(location);
		else if (typeof(T) == typeof(ShaderUniform3))
			return (T)(IShaderUniform)new ShaderUniform3(location);
		else if (typeof(T) == typeof(ShaderUniform4))
			return (T)(IShaderUniform)new ShaderUniform4(location);
		else if (typeof(T) == typeof(ShaderUniformMatrix))
			return (T)(IShaderUniform)new ShaderUniformMatrix(location);
		else
			throw new ArgumentException($"Unsupported uniform type: {typeof(T).Name}");
	}

	/// <summary>  
	/// Checks if the program was linked successfully.  
	/// </summary>  
	/// <exception cref="Exception">Thrown when program linking fails.</exception>  
	private void CheckProgramLinkStatus()
	{
		GL.GetProgram(_id, GetProgramParameterName.LinkStatus, out int status);
		if (status == 0)
		{
			string infoLog = GL.GetProgramInfoLog(_id);
			throw new Exception($"Program linking failed: {infoLog}");
		}
	}

	/// <summary>  
	/// Releases the unmanaged resources used by the shader program.  
	/// </summary>  
	/// <param name="disposing">True if called from Dispose(), false if called from finalizer.</param>  
	protected virtual void Dispose(bool disposing)
	{
		if (!_disposedValue)
		{
			if (_id != 0)
			{
				GL.DeleteProgram(_id);
			}
			_disposedValue = true;
		}
	}

	/// <summary>  
	/// Disposes the shader program, releasing all resources.  
	/// </summary>  
	public void Dispose()
	{
		Dispose(disposing: true);
		GC.SuppressFinalize(this);
	}

	#endregion
}