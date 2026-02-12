namespace IronKernel.Userland.Services;

using IronKernel.Common;
using IronKernel.Modules.ApplicationHost;

public sealed class ClipboardService : IClipboardService
{
	private readonly IApplicationBus _bus;
	private string? _localText;

	public ClipboardService(IApplicationBus bus)
	{
		_bus = bus;
	}

	public string? LocalText => _localText;

	public async Task<string?> GetTextAsync()
	{
		var response = await _bus.QueryAsync<
			AppClipboardGetQuery,
			AppClipboardGetResponse>(
				id => new AppClipboardGetQuery(id));

		// Always keep a local mirror
		_localText = response.Text;
		return response.Text;
	}

	public void SetText(string text)
	{
		_localText = text;

		var id = Guid.NewGuid();
		_bus.Publish(new AppClipboardSetCommand(id, text));
	}
}