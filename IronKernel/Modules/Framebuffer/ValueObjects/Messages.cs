using IronKernel.Common.ValueObjects;

namespace IronKernel.Modules.Framebuffer.ValueObjects;

// Input

public sealed record FbWriteSpan(int X, int Y, IReadOnlyList<RadialColor> Data);
public sealed record FbClear(RadialColor Color);
public sealed record FbSetBorder(RadialColor Color);

// Output

public sealed record FbInfo(int Width, int Height, int PaletteSize);

// A required for FbInfo to be placed on the bus.
public sealed record FbInfoQuery();
