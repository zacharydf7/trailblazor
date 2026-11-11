using System.Text.RegularExpressions;
using Trailblazor.Tests.Docs;

namespace Trailblazor.Tests;

public partial class ScriptImportsTests
{
    private static readonly Uri Initializer = new("https://app.test/_content/Trailblazor/Trailblazor.lib.module.js");

    [Fact]
    public void Initializer_ShouldImportEveryModuleThatAModuleImports_WhenCalled()
    {
        var scripts = new[] { "Trailblazor", "Trailblazor.Primitives" }.SelectMany(project =>
            Directory.GetFiles(Path.Combine(Repository.Root, "src", project, "wwwroot"), "*.js")
                .Select(file => new Uri($"https://app.test/_content/{project}/{Path.GetFileName(file)}")));

        var loadedAtStartup = ImportsOf(Initializer).ToHashSet();
        var missing = scripts.SelectMany(script => ImportsOf(script).Where(import => !loadedAtStartup.Contains(import))
            .Select(import => $"{script} imports {import}"));

        Assert.Empty(missing);
    }

    // The modules a script imports, as the browser resolves them against the script's served address.
    private static IEnumerable<Uri> ImportsOf(Uri script)
    {
        var parts = script.AbsolutePath.Split('/');
        var file = Path.Combine(Repository.Root, "src", parts[^2], "wwwroot", parts[^1]);
        return StaticImport().Matches(File.ReadAllText(file)).Select(match => new Uri(script, match.Groups[1].Value));
    }

    [GeneratedRegex("""^import\s+(?:[^'"]*?\s+from\s+)?['"]([^'"]+)['"]""", RegexOptions.Multiline)]
    private static partial Regex StaticImport();
}
