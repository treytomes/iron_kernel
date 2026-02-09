namespace IronKernel;

internal sealed class AppSettings
{
	public required string AssetRoot { get; init; }
	public required string UserFileRoot { get; init; }
	public required AssetDirectory Assets { get; init; }
	public bool Debug { get; init; } = false;
	public required WindowSettings Window { get; init; }
	public required VirtualDisplaySettings VirtualDisplay { get; init; }

	internal sealed class AssetDirectory
	{
		public required IReadOnlyDictionary<string, string> Image { get; init; }
	}

	/// <summary>  
	/// Settings for configuring a virtual display.  
	/// </summary>  
	internal sealed class VirtualDisplaySettings
	{
		/// <summary>  
		/// The width of the virtual display in pixels.  
		/// </summary>  
		public int Width { get; init; } = 320;

		/// <summary>  
		/// The height of the virtual display in pixels.  
		/// </summary>  
		public int Height { get; init; } = 240;

		/// <summary>  
		/// Path to the vertex shader file.  
		/// </summary>  
		public required string VertexShaderPath { get; init; } = "assets/shaders/vertex.glsl";

		/// <summary>  
		/// Path to the fragment shader file.  
		/// </summary>  
		public required string FragmentShaderPath { get; init; } = "assets/shaders/fragment.glsl";
	}

	public class WindowSettings
	{
		public int Width { get; init; }
		public int Height { get; init; }
		public required string Title { get; init; }
		public bool Fullscreen { get; init; }
		public bool Maximize { get; init; }
	}
}