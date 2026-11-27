using System.Text.RegularExpressions;
using Trailblazor.Tests.Docs;

namespace Trailblazor.Tests;

// The built-in palettes, as the stylesheet declares them, differ in more than their primary color.
public class PaletteCssTests
{
    private static readonly Dictionary<string, string> Blocks = Regex.Matches(
            File.ReadAllText(Path.Combine(Repository.Root, "src", "Trailblazor", "wwwroot", "trailblazor.css")),
            @"\[data-tb-palette=""([a-z]+)""\] \{(.*?)\}", RegexOptions.Singleline)
        .ToDictionary(match => match.Groups[1].Value, match => match.Groups[2].Value);

    private static readonly string[] Statuses = ["success", "warning", "danger", "info"];

    [Fact]
    public void Palettes_ShouldEachDeclareTheirStatusColorsAndTheTextOnThem_WhenDeclared()
    {
        // A palette region inside another palette takes nothing of the other's status colors.
        Assert.All(Blocks, block => Assert.All(Statuses.SelectMany(status => new[] { $"--tb-color-{status}",
            $"--tb-color-on-{status}" }), token => Assert.NotEmpty(Declared(block.Value, token))));
    }

    [Fact]
    public void Palettes_ShouldEachSetTheirOwnStatusColors_WhenDeclared()
    {
        var statuses = Blocks.Select(block =>
            string.Join(" | ", Statuses.Select(status => Declared(block.Value, $"--tb-color-{status}")))).ToList();

        Assert.Equal(statuses.Count, statuses.Distinct().Count());
    }

    [Fact]
    public void Palettes_ShouldShareASecondaryWithAtMostOneOther_WhenDeclared()
    {
        var shared = Blocks.GroupBy(block => Declared(block.Value, "--tb-color-secondary"))
            .Where(group => group.Count() > 2).Select(group => string.Join(", ", group.Select(block => block.Key)));

        Assert.Empty(shared);
    }

    [Fact]
    public void Palettes_ShouldTintTheirLightBackground_WhenTheyAreWarm()
    {
        Assert.All(new[] { "sand", "clay" }, name =>
            Assert.DoesNotContain("light-dark(white,", Declared(Blocks[name], "--tb-color-background")));
    }

    private static string Declared(string block, string token) =>
        Regex.Match(block, $@"{token}: ([^;]+);").Groups[1].Value;
}
