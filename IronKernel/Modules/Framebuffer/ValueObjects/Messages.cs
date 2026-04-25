using System.Drawing;
using IronKernel.Common;
using IronKernel.Common.ValueObjects;
using Color = IronKernel.Common.ValueObjects.Color;

namespace IronKernel.Modules.Framebuffer.ValueObjects;

// Input

public sealed record FbWriteRect(
	int X,
	int Y,
	int Width,
	int Height,
	Color[] Data,
	bool IsComplete);
public sealed record FbSetBorder(Color Color);
public sealed record FbFrameReady(ulong FrameId);

// Output

public sealed record FbInfoResponse(Guid CorrelationID, Size Size, int ColorDepth) : Response(CorrelationID);
public sealed record FbInfoQuery(Guid CorrelationID) : Query(CorrelationID);
