using IronKernel.Modules.FileSystem;

namespace IronKernel.Tests;

/// <summary>
/// Tests for the sys:// path resolution logic extracted from FileSystemModule.
/// We test the static resolution rules directly without spinning up the full module.
/// </summary>
public class SysProtocolTests
{
    // Mirrors the resolution logic in FileSystemModule.TryResolvePath.
    private static bool TryResolve(
        string url,
        string userRoot,
        string sysRoot,
        out string fullPath,
        out string? error)
    {
        fullPath = string.Empty;
        error = null;

        string scheme, root;
        if (url.StartsWith("sys://", StringComparison.OrdinalIgnoreCase))
        {
            scheme = "sys://";
            root = sysRoot;
        }
        else if (url.StartsWith("file://", StringComparison.OrdinalIgnoreCase))
        {
            scheme = "file://";
            root = userRoot;
        }
        else
        {
            error = "Unsupported URL scheme.";
            return false;
        }

        var relative = url[scheme.Length..]
            .Replace('/', Path.DirectorySeparatorChar)
            .TrimStart(Path.DirectorySeparatorChar);

        if (relative.Contains(".."))
        {
            error = "Path traversal is not allowed.";
            return false;
        }

        fullPath = Path.GetFullPath(Path.Combine(root, relative));

        if (!fullPath.StartsWith(root, StringComparison.Ordinal))
        {
            error = "Resolved path escapes root.";
            return false;
        }

        return true;
    }

    private static readonly string UserRoot = Path.GetFullPath("/tmp/iron_test_user");
    private static readonly string SysRoot = Path.GetFullPath("/tmp/iron_test_sys");

    [Fact]
    public void SysUrl_ResolvesToSysRoot()
    {
        TryResolve("sys://sounds/blipA4.wav", UserRoot, SysRoot, out var path, out _);
        Assert.StartsWith(SysRoot, path);
        Assert.EndsWith("blipA4.wav", path);
    }

    [Fact]
    public void FileUrl_ResolvesToUserRoot()
    {
        TryResolve("file://notes.txt", UserRoot, SysRoot, out var path, out _);
        Assert.StartsWith(UserRoot, path);
        Assert.EndsWith("notes.txt", path);
    }

    [Fact]
    public void SysUrl_RootOnly_ResolvesToSysRoot()
    {
        var ok = TryResolve("sys://", UserRoot, SysRoot, out var path, out _);
        Assert.True(ok);
        Assert.Equal(SysRoot, path);
    }

    [Fact]
    public void SysUrl_PathTraversal_Rejected()
    {
        var ok = TryResolve("sys://../etc/passwd", UserRoot, SysRoot, out _, out var error);
        Assert.False(ok);
        Assert.Contains("traversal", error, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void FileUrl_PathTraversal_Rejected()
    {
        var ok = TryResolve("file://../secret", UserRoot, SysRoot, out _, out var error);
        Assert.False(ok);
        Assert.Contains("traversal", error, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void UnknownScheme_Rejected()
    {
        var ok = TryResolve("http://example.com", UserRoot, SysRoot, out _, out var error);
        Assert.False(ok);
        Assert.Contains("Unsupported", error);
    }

    [Fact]
    public void SysUrl_NestedPath_ResolvesCorrectly()
    {
        TryResolve("sys://fonts/subdir/file.bmf", UserRoot, SysRoot, out var path, out _);
        var expected = Path.GetFullPath(Path.Combine(SysRoot, "fonts", "subdir", "file.bmf"));
        Assert.Equal(expected, path);
    }
}
