using IronKernel.Common.ValueObjects;
using Userland.Services;
using Userland.ValueObjects;

namespace ScriptConsole;

/// <summary>
/// Simple in-memory IFileSystem for use in the script console harness.
/// Supports files, directories, and basic CRUD operations against a
/// dictionary of file://... URLs.
/// </summary>
public sealed class InMemoryFileSystem : IFileSystem
{
    private readonly Dictionary<string, byte[]> _files = new();
    private readonly HashSet<string> _dirs = new(StringComparer.OrdinalIgnoreCase);

    public InMemoryFileSystem()
    {
        // Root always exists
        _dirs.Add("file://");
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static string Normalize(string url)
    {
        if (!url.StartsWith("file://"))
            url = "file://" + url;
        // Preserve the root "file://" — only strip trailing slashes from deeper paths
        if (url == "file://")
            return url;
        return url.TrimEnd('/');
    }

    private string? Parent(string url)
    {
        url = Normalize(url);
        var path = url["file://".Length..];
        var slash = path.LastIndexOf('/');
        return slash < 0 ? "file://" : "file://" + path[..slash];
    }

    private string Name(string url)
    {
        url = Normalize(url);
        var path = url["file://".Length..];
        var slash = path.LastIndexOf('/');
        return slash < 0 ? path : path[(slash + 1)..];
    }

    // ── IFileSystem ───────────────────────────────────────────────────────────

    public bool Exists(string url, CancellationToken ct = default)
    {
        url = Normalize(url);
        return _dirs.Contains(url) || _files.ContainsKey(url);
    }

    public Task<bool> ExistsAsync(string url, CancellationToken ct = default) =>
        Task.FromResult(Exists(url, ct));

    public DirectoryEntry CreateDirectory(string url, CancellationToken ct = default)
    {
        url = Normalize(url);
        _dirs.Add(url);
        return new DirectoryEntry(Name(url), true, null, DateTime.Now);
    }

    public Task<DirectoryEntry> CreateDirectoryAsync(string url, CancellationToken ct = default) =>
        Task.FromResult(CreateDirectory(url, ct));

    public FileReadResult Read(string url, CancellationToken ct = default)
    {
        url = Normalize(url);
        if (!_files.TryGetValue(url, out var data))
            throw new FileNotFoundException("File not found.", url);
        return new FileReadResult(data, "application/octet-stream");
    }

    public Task<FileReadResult> ReadAsync(string url, CancellationToken ct = default) =>
        Task.FromResult(Read(url, ct));

    public FileWriteResult Write(string url, byte[] data, string? mimeType = null, CancellationToken ct = default)
    {
        url = Normalize(url);
        _files[url] = data;
        // Ensure all ancestor directories exist
        var parent = Parent(url);
        while (parent != null && !_dirs.Contains(parent))
        {
            _dirs.Add(parent);
            parent = Parent(parent);
        }
        return new FileWriteResult(true, null);
    }

    public Task<FileWriteResult> WriteAsync(string url, byte[] data, string? mimeType = null, CancellationToken ct = default) =>
        Task.FromResult(Write(url, data, mimeType, ct));

    public FileDeleteResult Delete(string url, CancellationToken ct = default)
    {
        url = Normalize(url);
        if (_files.Remove(url))
            return new FileDeleteResult(true, null);
        if (_dirs.Remove(url))
            return new FileDeleteResult(true, null);
        return new FileDeleteResult(false, $"Not found: {url}");
    }

    public Task<FileDeleteResult> DeleteAsync(string url, CancellationToken ct = default) =>
        Task.FromResult(Delete(url, ct));

    public IReadOnlyList<DirectoryEntry> ListDirectory(string url, CancellationToken ct = default)
    {
        url = Normalize(url);
        if (!_dirs.Contains(url))
            throw new DirectoryNotFoundException($"Directory not found: {url}");

        var prefix = url == "file://" ? "file://" : url + "/";
        var entries = new List<DirectoryEntry>();

        foreach (var dir in _dirs)
        {
            if (dir == url) continue;
            if (!dir.StartsWith(prefix)) continue;
            var rest = dir[prefix.Length..];
            if (rest.Contains('/')) continue; // not direct child
            entries.Add(new DirectoryEntry(rest, true, null, DateTime.Now));
        }

        foreach (var file in _files.Keys)
        {
            if (!file.StartsWith(prefix)) continue;
            var rest = file[prefix.Length..];
            if (rest.Contains('/')) continue;
            entries.Add(new DirectoryEntry(rest, false, _files[file].Length, DateTime.Now));
        }

        return entries.OrderBy(e => e.Name).ToList();
    }

    public Task<IReadOnlyList<DirectoryEntry>> ListDirectoryAsync(string url, CancellationToken ct = default) =>
        Task.FromResult(ListDirectory(url, ct));
}
