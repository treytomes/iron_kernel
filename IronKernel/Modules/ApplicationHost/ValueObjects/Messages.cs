using IronKernel.Modules.OpenTKHost.ValueObjects;

namespace IronKernel.Modules.ApplicationHost;

public sealed record ApplicationUpdateTick(double TotalTime, double ElapsedTime);
public sealed record ApplicationRenderTick(double TotalTime, double ElapsedTime);
public sealed record ApplicationResizeEvent(int Width, int Height);
public sealed record ApplicationShutdown();
public sealed record ApplicationAcquiredFocus();
public sealed record ApplicationLostFocus();

public sealed record ApplicationMouseWheelEvent(float OffsetX, float OffsetY);
public sealed record ApplicationMouseMoveEvent(float X, float Y, float DeltaX, float DeltaY);
public sealed record ApplicationMouseButtonEvent(InputAction Action, MouseButton Button, KeyModifier Modifiers);
public sealed record ApplicationKeyboardEvent(InputAction Action, KeyModifier Modifiers, Key Key);
