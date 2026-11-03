using System.Diagnostics;

namespace Trailblazor.Analyzers.Tests;

// Runs the dotnet CLI on the repository's projects, which are already built with the tests in the same configuration.
internal static class Dotnet
{
#if DEBUG
    public const string Configuration = "Debug";
#else
    public const string Configuration = "Release";
#endif

    public static string RepositoryRoot { get; } = FindRoot();

    public static async Task<string> RunAsync(params string[] arguments)
    {
        var start = new ProcessStartInfo("dotnet", [.. arguments, "--configuration", Configuration,
            "--disable-build-servers"])
        {
            RedirectStandardOutput = true,
        };
        using var process = Process.Start(start)!;
        var output = await process.StandardOutput.ReadToEndAsync(TestContext.Current.CancellationToken);
        await process.WaitForExitAsync(TestContext.Current.CancellationToken);
        return output;
    }

    private static string FindRoot()
    {
        var root = AppContext.BaseDirectory;
        while (!File.Exists(Path.Combine(root, "Trailblazor.slnx")))
        {
            root = Path.GetDirectoryName(root)!;
        }

        return root;
    }
}
