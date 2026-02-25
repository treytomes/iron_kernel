using Microsoft.Extensions.DependencyInjection;
using Userland.MiniMacro;
using Userland.Morphic;
using Userland.Morphic.Inspector;

namespace Userland.Services;

public sealed class WindowService : IWindowService
{
	private readonly WorldMorph _world;
	private readonly IServiceProvider _services;

	public WindowService(WorldMorph world, IServiceProvider services)
	{
		_world = world;
		_services = services;
	}

	public Task AlertAsync(string message)
	{
		var tcs = new TaskCompletionSource(
			TaskCreationOptions.RunContinuationsAsynchronously);

		var window = new AlertWindowMorph(
			message,
			onClose: () => tcs.SetResult()
		);
		_world.AddMorph(window);
		window.CenterOnOwner();
		return tcs.Task;
	}

	public Task<string?> PromptAsync(string message, string? defaultValue = null)
	{
		var tcs = new TaskCompletionSource<string?>(
			TaskCreationOptions.RunContinuationsAsynchronously);

		var window = new PromptWindowMorph(
			message,
			defaultValue,
			tcs.SetResult
		);
		_world.AddMorph(window);
		window.CenterOnOwner();
		return tcs.Task;
	}

	public Task<bool> ConfirmAsync(string message)
	{
		var tcs = new TaskCompletionSource<bool>(
			TaskCreationOptions.RunContinuationsAsynchronously);

		var window = new ConfirmWindowMorph(
			message,
			tcs.SetResult
		);

		_world.AddMorph(window);
		window.CenterOnOwner();
		return tcs.Task;
	}

	public async Task EditFileAsync(string? filename)
	{
		// Create the editor window (DI / factory style depends on your setup)
		var editor = _services.GetRequiredService<TextEditorWindowMorph>();

		_world.AddMorph(editor);

		// Kick off async file open.
		if (!string.IsNullOrWhiteSpace(filename))
		{
			await editor.OpenFileAsync(filename);
		}
	}

	/// <summary>
	/// Open an object inspector.
	/// This mirrors the behavior of clicking the inspect halo handle.
	/// </summary>
	public void Inspect(object target)
	{
		if (target == null)
			return;

		_world.ClearSelection();

		var inspector = new InspectorMorph(target);
		_world.AddMorph(inspector);
		inspector.CenterOnOwner();
	}
}