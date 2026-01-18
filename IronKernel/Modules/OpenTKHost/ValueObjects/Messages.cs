namespace IronKernel.Modules.OpenTKHost.ValueObjects;

public sealed record HostUpdateTick(double TotalTime, double ElapsedTime);
public sealed record HostRenderTick(double TotalTime, double ElapsedTime);
public sealed record HostResizeEvent(int Width, int Height);
public sealed record HostShutdown();
public sealed record HostAcquiredFocus();
public sealed record HostLostFocus();

public sealed record HostMouseWheelEvent(float OffsetX, float OffsetY);
public sealed record HostMouseMoveEvent(float X, float Y, float DeltaX, float DeltaY);
public sealed record HostMouseButtonEvent(InputAction Action, MouseButton Button, KeyModifier Modifiers);
public sealed record HostKeyboardEvent(
	InputAction Action,
	KeyModifier Modifiers,
	Key Key
);