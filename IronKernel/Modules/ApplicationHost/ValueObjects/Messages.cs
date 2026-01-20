using System.Drawing;
using IronKernel.Common.ValueObjects;

namespace IronKernel.Modules.ApplicationHost;

public sealed record AppUpdateTick(double TotalTime, double ElapsedTime);

// The render tick is effectively the vsync signal.
public sealed record AppRenderTick(double TotalTime, double ElapsedTime);

public sealed record AppResizeEvent(int Width, int Height);
public sealed record AppShutdown();
public sealed record AppAcquiredFocus();
public sealed record AppLostFocus();

public sealed record AppMouseWheelEvent(float OffsetX, float OffsetY);
public sealed record AppMouseMoveEvent(float X, float Y, float DeltaX, float DeltaY);
public sealed record AppMouseButtonEvent(InputAction Action, MouseButton Button, KeyModifier Modifiers);
public sealed record AppKeyboardEvent(InputAction Action, KeyModifier Modifiers, Key Key);


public sealed record AppFbWriteSpan(int X, int Y, IReadOnlyList<RadialColor> Data);
public sealed record AppFbClear(RadialColor Color);
public sealed record AppFbSetBorder(RadialColor Color);
public sealed record AppFbInfo(int Width, int Height, int PaletteSize, Point Padding, float Scale);
public sealed record AppFbInfoQuery();
