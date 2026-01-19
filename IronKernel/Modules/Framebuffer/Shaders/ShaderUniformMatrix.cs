using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;

namespace IronKernel.Modules.Framebuffer.Shaders;

/// <summary>  
/// Wrapper for matrix uniform variables in shaders.  
/// </summary>  
public class ShaderUniformMatrix : IShaderUniform
{
	private readonly int _location;

	/// <summary>  
	/// Creates a new wrapper for a matrix shader uniform.  
	/// </summary>  
	/// <param name="location">The location of the uniform in the shader program.</param>  
	public ShaderUniformMatrix(int location)
	{
		_location = location;
	}

	/// <inheritdoc/>  
	public int Location => _location;

	/// <inheritdoc/>  
	public bool IsValid => _location != -1;

	/// <summary>  
	/// Sets the uniform to the specified 2x2 matrix.  
	/// </summary>  
	/// <param name="matrix">The matrix to set.</param>  
	/// <param name="transpose">Whether to transpose the matrix before setting.</param>  
	public void Set(Matrix2 matrix, bool transpose = false)
	{
		if (IsValid)
			GL.UniformMatrix2(_location, 1, transpose, ref matrix.Row0.X);
	}

	/// <summary>  
	/// Sets the uniform to the specified 3x3 matrix.  
	/// </summary>  
	/// <param name="matrix">The matrix to set.</param>  
	/// <param name="transpose">Whether to transpose the matrix before setting.</param>  
	public void Set(Matrix3 matrix, bool transpose = false)
	{
		if (IsValid)
			GL.UniformMatrix3(_location, 1, transpose, ref matrix.Row0.X);
	}

	/// <summary>  
	/// Sets the uniform to the specified 4x4 matrix.  
	/// </summary>  
	/// <param name="matrix">The matrix to set.</param>  
	/// <param name="transpose">Whether to transpose the matrix before setting.</param>  
	public void Set(Matrix4 matrix, bool transpose = false)
	{
		if (IsValid)
			GL.UniformMatrix4(_location, 1, transpose, ref matrix.Row0.X);
	}
}