namespace IronKernel;

internal sealed class CommandLineProps
{
	public required string UserlandPath { get; set; }
	public required string ConfigFile { get; set; }
	public bool Debug { get; set; }
}