using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;

namespace IronKernel.Modules.Framebuffer.Shaders;

/// <summary>  
/// Wrapper for two-component shader uniforms.  
/// </summary>  
public class ShaderUniform2 : IShaderUniform
{
	private readonly int _location;

	/// <summary>  
	/// Creates a new wrapper for a two-component shader uniform.  
	/// </summary>  
	/// <param name="location">The location of the uniform in the shader program.</param>  
	public ShaderUniform2(int location)
	{
		_location = location;
	}

	/// <inheritdoc/>  
	public int Location => _location;

	/// <inheritdoc/>  
	public bool IsValid => _location != -1;

	/// <summary>  
	/// Sets the uniform to the specified integer components.  
	/// </summary>  
	/// <param name="x">The first component.</param>  
	/// <param name="y">The second component.</param>  
	public void Set(int x, int y)
	{
		if (IsValid)
			GL.Uniform2(_location, x, y);
	}

	/// <summary>  
	/// Sets the uniform to the specified float components.  
	/// </summary>  
	/// <param name="x">The first component.</param>  
	/// <param name="y">The second component.</param>  
	public void Set(float x, float y)
	{
		if (IsValid)
			GL.Uniform2(_location, x, y);
	}

	/// <summary>  
	/// Sets the uniform to the specified Vector2.  
	/// </summary>  
	/// <param name="vector">The vector containing the components.</param>  
	public void Set(Vector2 vector)
	{
		if (IsValid)
			GL.Uniform2(_location, vector);
	}

	/// <summary>  
	/// Sets the uniform to the specified Vector2i.  
	/// </summary>  
	/// <param name="vector">The vector containing the components.</param>  
	public void Set(Vector2i vector)
	{
		if (IsValid)
			GL.Uniform2(_location, vector.X, vector.Y);
	}
}
