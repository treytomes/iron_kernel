using IronKernel.Common.ValueObjects;

namespace IronKernel.Userland.Roguey;

public sealed class TileInfo
{
	public int TileIndex;
	public RadialColor ForegroundColor = RadialColor.White;
	public RadialColor BackgroundColor = RadialColor.Black;

	public bool BlocksMovement;
	public bool BlocksVision;
	public string Tag = "floor";
}