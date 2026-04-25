namespace IronKernel.Tests;

internal static class TestContext
{
    /// <summary>Walks up from the test assembly output dir to find the repo root (contains IronKernel.sln).</summary>
    public static string RepoRoot { get; } = FindRepoRoot();

    private static string FindRepoRoot()
    {
        var dir = AppContext.BaseDirectory;
        while (dir != null)
        {
            if (File.Exists(Path.Combine(dir, "IronKernel.sln")))
                return dir;
            dir = Path.GetDirectoryName(dir);
        }
        throw new InvalidOperationException("Could not locate repo root (IronKernel.sln not found).");
    }
}
