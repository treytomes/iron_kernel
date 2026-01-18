using IronKernel.Modules.OpenTKHost.ValueObjects;

namespace IronKernel.Modules.ApplicationHost;

public sealed record ApplicationUpdateTick(double TotalTime, double ElapsedTime, CancellationToken CancellationToken);
public sealed record ApplicationRenderTick(double TotalTime, double ElapsedTime, CancellationToken CancellationToken);
public sealed record ApplicationResizeEvent(int Width, int Height, CancellationToken CancellationToken);
public sealed record ApplicationShutdown(CancellationToken CancellationToken);
public sealed record ApplicationAcquiredFocus(CancellationToken CancellationToken);
public sealed record ApplicationLostFocus(CancellationToken CancellationToken);

public sealed record ApplicationMouseWheelEvent(float OffsetX, float OffsetY, CancellationToken CancellationToken);
public sealed record ApplicationMouseMoveEvent(float X, float Y, float DeltaX, float DeltaY, CancellationToken CancellationToken);
public sealed record ApplicationMouseButtonEvent(InputAction Action, MouseButton Button, KeyModifier Modifiers, CancellationToken CancellationToken);
public sealed record ApplicationKeyboardEvent(InputAction Action, KeyModifier Modifiers, Key Key, CancellationToken CancellationToken);