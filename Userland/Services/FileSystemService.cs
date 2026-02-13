using IronKernel.Common;
using IronKernel.Common.ValueObjects;
using Userland.ValueObjects;

namespace Userland.Services;

public sealed class FileSystemService : IFileSystem
{
	private readonly IApplicationBus _bus;

	public FileSystemService(IApplicationBus bus)
	{
		_bus = bus;
	}

	#region Read

	public async Task<FileReadResult> ReadAsync(
		string url,
		CancellationToken ct = default)
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

	#endregion

	#region Write

	public async Task<FileWriteResult> WriteAsync(
		string url,
		byte[] data,
		string? mimeType = null,
		CancellationToken ct = default)
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

	#endregion

	#region Delete

	public async Task<FileDeleteResult> DeleteAsync(
		string url,
		CancellationToken ct = default)
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

	#endregion

	#region Directory Listing

	public async Task<IReadOnlyList<DirectoryEntry>> ListDirectoryAsync(
		string url,
		CancellationToken ct = default)
	{
		var response =
			await _bus.QueryAsync<
				AppDirectoryListQuery,
				AppDirectoryListResponse>(
				id => new AppDirectoryListQuery(id, url),
				ct);

		return response.Entries;
	}

	#endregion
}