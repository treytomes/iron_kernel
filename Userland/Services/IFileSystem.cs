using IronKernel.Common.ValueObjects;
using Userland.ValueObjects;

namespace Userland.Services;

public interface IFileSystem
{
	Task<DirectoryEntry> CreateDirectoryAsync(string url, CancellationToken ct = default);
	DirectoryEntry CreateDirectory(string url, CancellationToken ct = default);
	Task<bool> ExistsAsync(string url, CancellationToken ct = default);
	bool Exists(string url, CancellationToken ct = default);
	Task<FileReadResult> ReadAsync(string url, CancellationToken ct = default);
	FileReadResult Read(string url, CancellationToken ct = default);
	Task<FileWriteResult> WriteAsync(string url, byte[] data, string? mimeType = null, CancellationToken ct = default);
	FileWriteResult Write(string url, byte[] data, string? mimeType = null, CancellationToken ct = default);
	Task<FileDeleteResult> DeleteAsync(string url, CancellationToken ct = default);
	FileDeleteResult Delete(string url, CancellationToken ct = default);
	Task<IReadOnlyList<DirectoryEntry>> ListDirectoryAsync(string url, CancellationToken ct = default);
	IReadOnlyList<DirectoryEntry> ListDirectory(string url, CancellationToken ct = default);
}