namespace IronKernel.Userland.Services;

public interface IWindowService
{
	Task AlertAsync(string message);
	Task<string?> PromptAsync(string message, string? defaultValue = null);
	Task<bool> ConfirmAsync(string message);
}