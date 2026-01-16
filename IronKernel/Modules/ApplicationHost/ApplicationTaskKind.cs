namespace IronKernel.Modules.ApplicationHost;

public enum ApplicationTaskKind
{
	/// <summary>
	/// Expected to complete.
	/// Subject to slow / hung detection.
	/// </summary>
	Finite,

	/// <summary>
	/// Runs for the lifetime of the application.
	/// No hung detection.
	/// </summary>
	LongRunning
}