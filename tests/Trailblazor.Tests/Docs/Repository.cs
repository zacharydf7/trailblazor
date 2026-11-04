namespace Trailblazor.Tests.Docs;

// The repository's checkout, for tests that read its sources.
internal static class Repository
{
    public static string Root { get; } = FindRoot();

    private static string FindRoot()
    {
        var root = new DirectoryInfo(AppContext.BaseDirectory);
        while (!File.Exists(Path.Combine(root.FullName, "Trailblazor.slnx")))
        {
            root = root.Parent!;
        }

        return root.FullName;
    }
}
