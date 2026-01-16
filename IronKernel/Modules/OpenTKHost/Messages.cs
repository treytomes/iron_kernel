namespace IronKernel.Modules.OpenTKHost;

public sealed record HostUpdateTick(double TotalTime, double ElapsedTime);
public sealed record HostRenderTick(double TotalTime, double ElapsedTime);
public sealed record HostResizeEvent(int Width, int Height);
public sealed record HostShutdownEvent();
