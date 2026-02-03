using System.Drawing;
using IronKernel.Common;
using IronKernel.Common.ValueObjects;

namespace IronKernel.Modules.ApplicationHost;

public sealed record AppUpdateTick(double TotalTime, double ElapsedTime);

// The render tick is effectively the vsync signal.
public sealed record AppRenderTick(ulong FrameId, double TotalTime, double ElapsedTime);
// public sealed record AppFrameReady(ulong FrameId);

public sealed record AppResizeEvent(int Width, int Height);
public sealed record AppShutdown();
public sealed record AppAcquiredFocus();
public sealed record AppLostFocus();

public sealed record AppMouseWheelEvent(int OffsetX, int OffsetY);
public sealed record AppMouseMoveEvent(int X, int Y, int DeltaX, int DeltaY);
public sealed record AppMouseButtonEvent(InputAction Action, MouseButton Button, KeyModifier Modifiers);
public sealed record AppKeyboardEvent(InputAction Action, KeyModifier Modifiers, Key Key);


public sealed record AppFbWriteSpan(int X, int Y, IReadOnlyList<RadialColor> Data, bool IsComplete);
public sealed record AppFbWriteRect(
	int X,
	int Y,
	int Width,
	int Height,
	RadialColor[] Data,
	bool IsComplete);
public sealed record AppFbSetBorder(RadialColor Color);

public sealed record AppFbInfoQuery(Guid CorrelationID) : Query(CorrelationID);
public sealed record AppFbInfoResponse(Guid CorrelationID, Size Size) : Response<Size>(CorrelationID, Size);

public sealed record AppAssetImageQuery(Guid CorrelationID, string AssetId) : Query(CorrelationID);
public sealed record AppAssetImageResponse(Guid CorrelationID, string AssetId, Image Image) : Response<Image>(CorrelationID, Image);
