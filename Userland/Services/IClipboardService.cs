namespace IronKernel.Userland.Services;

public interface IClipboardService
{
	Task<string?> GetTextAsync();
	void SetText(string text);
}