namespace IronKernel.Userland;

public interface IAssetService
{
	Task<RenderImage> LoadImageAsync(string assetId);
}