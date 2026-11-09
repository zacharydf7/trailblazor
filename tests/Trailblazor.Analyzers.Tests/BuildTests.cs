using System.IO.Compression;
using System.Text.RegularExpressions;

namespace Trailblazor.Analyzers.Tests;

public partial class BuildTests
{
    [Fact]
    public async Task RazorBuild_ShouldReportEachRuleOnItsMarkupLine_WhenAPageMakesTheMistakes()
    {
        var fixture = Path.Combine(Dotnet.RepositoryRoot, "tests", "Trailblazor.Analyzers.Tests", "Fixture",
            "Fixture.csproj");

        // Only the fixture is compiled; the library and analyzer it references are already built.
        var output = await Dotnet.RunAsync("build", fixture, "-p:BuildProjectReferences=false");

        var reported = Reported().Matches(output).Select(match => match.Value).Distinct().Order();
        Assert.Equal(["Mistakes.razor(3,30): error TB001", "Mistakes.razor(4,20): error TB004",
            "Mistakes.razor(5,27): error TB003", "Mistakes.razor(6,11): error TB002",
            "Mistakes.razor(7,22): error TB005"], reported);
    }

    [Fact]
    public async Task Package_ShouldCarryTheAnalyzer_WhenTrailblazorIsPacked()
    {
        var folder = Directory.CreateTempSubdirectory("trailblazor-pack-");
        try
        {
            var library = Path.Combine(Dotnet.RepositoryRoot, "src", "Trailblazor", "Trailblazor.csproj");
            await Dotnet.RunAsync("pack", library, "--no-build", "--output", folder.FullName);

            using var package = ZipFile.OpenRead(Assert.Single(Directory.GetFiles(folder.FullName, "*.nupkg")));
            Assert.Contains("analyzers/dotnet/cs/Trailblazor.Analyzers.dll",
                package.Entries.Select(entry => entry.FullName));
        }
        finally
        {
            folder.Delete(recursive: true);
        }
    }

    [GeneratedRegex(@"Mistakes\.razor\(\d+,\d+\): \w+ TB\d+")]
    private static partial Regex Reported();
}
