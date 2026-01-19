using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;

namespace IronKernel.Modules.Framebuffer.Shaders;

/// <summary>  
/// Wrapper for three-component shader uniforms.  
/// </summary>  
public class ShaderUniform3 : IShaderUniform
{
	private readonly int _location;

	/// <summary>  
	/// Creates a new wrapper for a three-component shader uniform.  
	/// </summary>  
	/// <param name="location">The location of the uniform in the shader program.</param>  
	public ShaderUniform3(int location)
	{
		_location = location;
	}

	/// <inheritdoc/>  
	public int Location => _location;

	/// <inheritdoc/>  
	public bool IsValid => _location != -1;

	/// <summary>  
	/// Sets the uniform to the specified float components.  
	/// </summary>  
	/// <param name="x">The first component.</param>  
	/// <param name="y">The second component.</param>  
	/// <param name="z">The third component.</param>  
	public void Set(float x, float y, float z)
	{
		if (IsValid)
			GL.Uniform3(_location, x, y, z);
	}

	/// <summary>  
	/// Sets the uniform to the specified Vector3.  
	/// </summary>  
	/// <param name="vector">The vector containing the components.</param>  
	public void Set(Vector3 vector)
	{
		if (IsValid)
			GL.Uniform3(_location, vector);
	}

	/// <summary>  
	/// Sets the uniform to the specified Vector3i.  
	/// </summary>  
	/// <param name="vector">The vector containing the components.</param>  
	public void Set(Vector3i vector)
	{
		if (IsValid)
			GL.Uniform3(_location, vector.X, vector.Y, vector.Z);
	}

	/// <summary>  
	/// Sets the uniform to the specified color (RGB components).  
	/// </summary>  
	/// <param name="color">The color to set.</param>  
	public void Set(Color4 color)
	{
		if (IsValid)
			GL.Uniform3(_location, color.R, color.G, color.B);
	}
}
