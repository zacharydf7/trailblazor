using System.Text.RegularExpressions;
using Trailblazor.Tests.Docs;

namespace Trailblazor.Tests;

public partial class SeededPaletteTests
{
    private static readonly string Css =
        File.ReadAllText(Path.Combine(Repository.Root, "src", "Trailblazor", "wwwroot", "trailblazor.css"));

    [Fact]
    public void Tokens_ShouldMatchTheBuiltInCobalt_WhenSeededWithItsColors()
    {
        var cobalt = Seeded(new() { Primary = Scale("blue", 600), PrimaryDark = Scale("blue", 400),
            Secondary = Scale("teal", 600), SecondaryDark = Scale("teal", 400) });
        var block = Regex.Match(Css, @"\[data-tb-palette=""cobalt""\] \{(.*?)\}", RegexOptions.Singleline)
            .Groups[1].Value;

        foreach (var token in cobalt.Tokens.Where(token => !token.Name.StartsWith("--tb-color-on-")))
        {
            var declared = Regex.Match(block, $@"{token.Name}: light-dark\((.+?), (.+?)\);");
            Assert.True(declared.Success, token.Name);
            Assert.Equal((Resolve(declared.Groups[1].Value), Resolve(declared.Groups[2].Value)),
                (token.Light.ToCss(), token.Dark.ToCss()));
        }
    }

    [Fact]
    public void ToCss_ShouldStateTheStylesheetsLayerOrder_WhenCalled()
    {
        var css = Seeded(new() { Primary = "#1F5FAD" }).ToCss();

        Assert.StartsWith(LayerOrder().Match(Css).Value, css);
        Assert.Contains("@layer tb.themes {\n    [data-tb-palette=\"brand\"] {", css);
        Assert.Contains("--tb-color-primary: light-dark(oklch(", css);
    }

    [Fact]
    public void ContrastProblems_ShouldBeNone_WhenSeededLikeEachBuiltInAccent()
    {
        foreach (var hue in new[] { "blue", "teal", "violet", "rose" })
        {
            var palette = Seeded(new() { Primary = Scale(hue, 600), PrimaryDark = Scale(hue, 400),
                Neutral = "#5B6472" });

            Assert.Empty(palette.ContrastProblems());
        }
    }

    [Fact]
    public void ContrastProblems_ShouldNameThePairModeAndRatio_WhenThePrimaryIsTooLightOnWhite()
    {
        var palette = Seeded(new() { Primary = "#FFD84D" });

        var problem = Assert.Single(palette.ContrastProblems(), problem => problem is
            { Mode: "light", Foreground: "--tb-color-primary", Background: "--tb-color-background" });
        Assert.Equal(("brand", 3), (problem.Palette, problem.Minimum));
        Assert.InRange(problem.Ratio, 1, 2);
    }

    [Fact]
    public void Tokens_ShouldTintTheGraysWithTheNeutral_WhenANeutralIsSeeded()
    {
        OklchColor.TryParse("#7A5B3C", out var brown);

        var palette = Seeded(new() { Primary = "#1F5FAD", Neutral = "#7A5B3C" });

        var text = palette.Tokens.Single(token => token.Name == "--tb-color-text");
        Assert.Equal(brown.H, text.Light.H, 0.001);
        Assert.Equal(0.33, text.Light.L, 0.001);
        Assert.Equal(brown.C * 0.015 / 0.018, text.Light.C, 0.0001);
    }

    [Fact]
    public void Tokens_ShouldLightenThePrimaryForDarkMode_WhenNoDarkPrimaryIsSeeded()
    {
        var palette = Seeded(new() { Primary = "oklch(0.5 0.2 30)" });

        var primary = palette.Tokens.Single(token => token.Name == "--tb-color-primary");
        Assert.Equal(new OklchColor(0.69, 0.164, 30), primary.Dark with { L = Math.Round(primary.Dark.L, 3),
            C = Math.Round(primary.Dark.C, 3) });
        Assert.DoesNotContain(palette.Tokens, token => token.Name.Contains("secondary"));
    }

    [Fact]
    public void ToCss_ShouldKeepTheNameInsideItsString_WhenTheNameHasQuotesOrMarkup()
    {
        var css = SeededPalette.Build(new Palette("a\"b</style>") { Seeds = new() { Primary = "#1F5FAD" } }).ToCss();

        Assert.Contains("[data-tb-palette=\"a\\\"b\\3C /style>\"]", css);
        Assert.DoesNotContain("</", css);
    }

    [Theory]
    [InlineData("blue")]
    [InlineData("rgb(0 0 255)")]
    public void Seeds_ShouldRejectTheColor_WhenItIsNeitherHexNorOklch(string color)
    {
        var error = Assert.Throws<ArgumentException>(() => new PaletteSeeds { Primary = "#1F5FAD", Neutral = color });

        Assert.Equal("Neutral", error.ParamName);
    }

    private static SeededPalette Seeded(PaletteSeeds seeds) =>
        SeededPalette.Build(new Palette("brand") { Seeds = seeds });

    private static string Scale(string hue, int step) =>
        Regex.Match(Css, $@"--tb-{hue}-{step}: (oklch\([^)]*\));").Groups[1].Value;

    private static string Resolve(string value) => value switch
    {
        "white" => "oklch(1 0 0)",
        _ => Regex.Match(value, @"^var\((--tb-[a-z]+-\d+)\)$") is { Success: true } reference
            ? Regex.Match(Css, $@"{reference.Groups[1].Value}: (oklch\([^)]*\));").Groups[1].Value
            : value,
    };

    [GeneratedRegex(@"@layer tb\.reset[^;]*;")]
    private static partial Regex LayerOrder();
}
