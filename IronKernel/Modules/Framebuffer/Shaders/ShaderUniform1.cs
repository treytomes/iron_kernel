using OpenTK.Graphics.OpenGL4;

namespace IronKernel.Modules.Framebuffer.Shaders;

/// <summary>  
/// Wrapper for single-value shader uniforms.  
/// </summary>  
public class ShaderUniform1 : IShaderUniform
{
	private readonly int _location;

	/// <summary>  
	/// Creates a new wrapper for a single-value shader uniform.  
	/// </summary>  
	/// <param name="location">The location of the uniform in the shader program.</param>  
	public ShaderUniform1(int location)
	{
		_location = location;
	}

	/// <inheritdoc/>  
	public int Location => _location;

	/// <inheritdoc/>  
	public bool IsValid => _location != -1;

	/// <summary>  
	/// Sets the uniform value to the specified integer.  
	/// </summary>  
	/// <param name="value">The value to set.</param>  
	public void Set(int value)
	{
		if (IsValid)
			GL.Uniform1(_location, value);
	}

	/// <summary>  
	/// Sets the uniform value to the specified float.  
	/// </summary>  
	/// <param name="value">The value to set.</param>  
	public void Set(float value)
	{
		if (IsValid)
			GL.Uniform1(_location, value);
	}

	/// <summary>  
	/// Sets the uniform value to the specified double.  
	/// </summary>  
	/// <param name="value">The value to set.</param>  
	public void Set(double value)
	{
		if (IsValid)
			GL.Uniform1(_location, (float)value);
	}

	/// <summary>  
	/// Sets the uniform value to the specified boolean.  
	/// </summary>  
	/// <param name="value">The value to set (converted to 1 for true, 0 for false).</param>  
	public void Set(bool value)
	{
		if (IsValid)
			GL.Uniform1(_location, value ? 1 : 0);
	}
}
