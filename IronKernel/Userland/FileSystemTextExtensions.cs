using System.Text;
using IronKernel.Userland.Services;
using IronKernel.Userland.ValueObjects;

namespace IronKernel.Userland;

public static class FileSystemTextExtensions
{
	public static async Task<string> ReadTextAsync(
		this IFileSystem fs,
		string url,
		CancellationToken ct = default)
	{
		var result = await fs.ReadAsync(url, ct);
		return Encoding.UTF8.GetString(result.Data);
	}

	public static Task<FileWriteResult> WriteTextAsync(
		this IFileSystem fs,
		string url,
		string text,
		CancellationToken ct = default)
	{
		var data = Encoding.UTF8.GetBytes(text);
		return fs.WriteAsync(url, data, "text/plain", ct);
	}
}