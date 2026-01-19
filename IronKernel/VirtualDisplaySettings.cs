namespace IronKernel;

/// <summary>  
/// Settings for configuring a virtual display.  
/// </summary>  
public class VirtualDisplaySettings
{
	/// <summary>  
	/// The width of the virtual display in pixels.  
	/// </summary>  
	public int Width { get; set; } = 320;

	/// <summary>  
	/// The height of the virtual display in pixels.  
	/// </summary>  
	public int Height { get; set; } = 240;

	/// <summary>  
	/// Path to the vertex shader file.  
	/// </summary>  
	public string VertexShaderPath { get; set; } = "assets/shaders/vertex.glsl";

	/// <summary>  
	/// Path to the fragment shader file.  
	/// </summary>  
	public string FragmentShaderPath { get; set; } = "assets/shaders/fragment.glsl";
}