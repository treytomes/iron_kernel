using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;

namespace IronKernel.Modules.Framebuffer.Shaders;

/// <summary>  
/// Wrapper for four-component shader uniforms.  
/// </summary>  
public class ShaderUniform4 : IShaderUniform
{
	private readonly int _location;

	/// <summary>  
	/// Creates a new wrapper for a four-component shader uniform.  
	/// </summary>  
	/// <param name="location">The location of the uniform in the shader program.</param>  
	public ShaderUniform4(int location)
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
	/// <param name="w">The fourth component.</param>  
	public void Set(float x, float y, float z, float w)
	{
		if (IsValid)
			GL.Uniform4(_location, x, y, z, w);
	}

	/// <summary>  
	/// Sets the uniform to the specified integer components.  
	/// </summary>  
	/// <param name="x">The first component.</param>  
	/// <param name="y">The second component.</param>  
	/// <param name="z">The third component.</param>  
	/// <param name="w">The fourth component.</param>  
	public void Set(int x, int y, int z, int w)
	{
		if (IsValid)
			GL.Uniform4(_location, x, y, z, w);
	}

	/// <summary>  
	/// Sets the uniform to the specified Vector4.  
	/// </summary>  
	/// <param name="vector">The vector containing the components.</param>  
	public void Set(Vector4 vector)
	{
		if (IsValid)
			GL.Uniform4(_location, vector);
	}

	/// <summary>  
	/// Sets the uniform to the specified Vector4i.  
	/// </summary>  
	/// <param name="vector">The vector containing the components.</param>  
	public void Set(Vector4i vector)
	{
		if (IsValid)
			GL.Uniform4(_location, vector.X, vector.Y, vector.Z, vector.W);
	}

	/// <summary>  
	/// Sets the uniform to the specified color (RGBA components).  
	/// </summary>  
	/// <param name="color">The color to set.</param>  
	public void Set(Color4 color)
	{
		if (IsValid)
			GL.Uniform4(_location, color);
	}
}