using IronKernel.Common.ValueObjects;
using Userland.ValueObjects;

namespace Userland.Services;

public interface IFileSystem
{
	Task<FileReadResult> ReadAsync(string url, CancellationToken ct = default);

	Task<FileWriteResult> WriteAsync(
		string url,
		byte[] data,
		string? mimeType = null,
		CancellationToken ct = default);

	Task<FileDeleteResult> DeleteAsync(
		string url,
		CancellationToken ct = default);

	Task<IReadOnlyList<DirectoryEntry>> ListDirectoryAsync(
		string url,
		CancellationToken ct = default);
}