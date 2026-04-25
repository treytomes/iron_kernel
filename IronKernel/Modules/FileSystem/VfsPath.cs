namespace IronKernel.Modules.FileSystem;

/// <summary>
/// Shared URL-to-disk-path resolution for sys:// and file:// schemes.
/// </summary>
internal static class VfsPath
{
    internal static bool TryResolve(
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
}
