using System.Drawing;
using IronKernel.Common;
using IronKernel.Common.ValueObjects;

namespace IronKernel.Modules.Framebuffer.ValueObjects;

// Input

public sealed record FbWriteSpan(int X, int Y, IReadOnlyList<RadialColor> Data, bool IsComplete);
public sealed record FbSetBorder(RadialColor Color);
public sealed record FbFrameReady(ulong FrameId);

// Output

public sealed record FbInfoResponse(Guid CorrelationID, Size Size) : Response(CorrelationID);
public sealed record FbInfoQuery(Guid CorrelationID) : Query(CorrelationID);
