using System.Text;
using Userland.Services;
using Userland.ValueObjects;

namespace Userland;

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

	// -----------------------------
	// Synchronous ReadText
	// -----------------------------
	public static string ReadText(
		this IFileSystem fs,
		string url,
		CancellationToken ct = default)
	{
		// Force synchronous completion
		var result = fs.ReadAsync(url, ct)
			.ConfigureAwait(false)
			.GetAwaiter()
			.GetResult();

		return Encoding.UTF8.GetString(result.Data);
	}

	// -----------------------------
	// Synchronous WriteText
	// -----------------------------
	public static FileWriteResult WriteText(
		this IFileSystem fs,
		string url,
		string text,
		CancellationToken ct = default)
	{
		var data = Encoding.UTF8.GetBytes(text);

		return fs.WriteAsync(url, data, "text/plain", ct)
			.ConfigureAwait(false)
			.GetAwaiter()
			.GetResult();
	}
}