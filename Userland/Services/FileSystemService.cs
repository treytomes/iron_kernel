using IronKernel.Common;
using IronKernel.Common.ValueObjects;
using Userland.ValueObjects;

namespace Userland.Services;

public sealed class FileSystemService(IApplicationBus bus) : IFileSystem
{
	#region Fields

	private readonly IApplicationBus _bus = bus;

	#endregion

	#region Methods

	public async Task<FileReadResult> ReadAsync(string url, CancellationToken ct = default)
	{
		var response =
			await _bus.QueryAsync<
				AppFileReadQuery,
				AppFileReadResponse>(
				id => new AppFileReadQuery(id, url),
				ct);

		return new FileReadResult(
			response.Data,
			response.MimeType);
	}

	public FileReadResult Read(string url, CancellationToken ct = default)
	{
		return ReadAsync(url, ct)
			.ConfigureAwait(false)
			.GetAwaiter()
			.GetResult();
	}

	public async Task<FileWriteResult> WriteAsync(string url, byte[] data, string? mimeType = null, CancellationToken ct = default)
	{
		var response =
			await _bus.CommandAsync<
				AppFileWriteCommand,
				AppFileWriteResult>(
				id => new AppFileWriteCommand(
					id,
					url,
					data,
					mimeType),
				ct);

		return new FileWriteResult(
			response.Success,
			response.Error);
	}

	public FileWriteResult Write(string url, byte[] data, string? mimeType = null, CancellationToken ct = default)
	{
		return WriteAsync(url, data, mimeType, ct)
			.ConfigureAwait(false)
			.GetAwaiter()
			.GetResult();
	}

	public async Task<FileDeleteResult> DeleteAsync(string url, CancellationToken ct = default)
	{
		var response =
			await _bus.CommandAsync<
				AppFileDeleteCommand,
				AppFileDeleteResult>(
				id => new AppFileDeleteCommand(id, url),
				ct);

		return new FileDeleteResult(
			response.Success,
			response.Error);
	}

	public FileDeleteResult Delete(string url, CancellationToken ct = default)
	{
		return DeleteAsync(url, ct)
			.ConfigureAwait(false)
			.GetAwaiter()
			.GetResult();
	}

	public async Task<IReadOnlyList<DirectoryEntry>> ListDirectoryAsync(string url, CancellationToken ct = default)
	{
		var response =
			await _bus.QueryAsync<
				AppDirectoryListQuery,
				AppDirectoryListResponse>(
				id => new AppDirectoryListQuery(id, url),
				ct);

		return response.Entries;
	}

	public IReadOnlyList<DirectoryEntry> ListDirectory(string url, CancellationToken ct = default)
	{
		return ListDirectoryAsync(url, ct)
			.ConfigureAwait(false)
			.GetAwaiter()
			.GetResult();
	}

	#endregion
}