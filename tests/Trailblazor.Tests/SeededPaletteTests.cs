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
        var cobalt = Seeded(new()
        {
            Primary = Scale("blue", 600), PrimaryDark = Scale("blue", 400),
            Secondary = Scale("rose", 600), SecondaryDark = Scale("rose", 400),
            Success = "oklch(0.52 0.17 155)", SuccessDark = "oklch(0.71 0.14 155)", Warning = "oklch(0.71 0.14 80)",
            Danger = "oklch(0.52 0.17 30)", DangerDark = "oklch(0.71 0.14 30)",
            Info = "oklch(0.52 0.17 220)", InfoDark = "oklch(0.71 0.14 220)",
        });
        var block = Regex.Match(Css, @"\[data-tb-palette=""cobalt""\] \{(.*?)\}", RegexOptions.Singleline)
            .Groups[1].Value;

        foreach (var token in cobalt.Tokens.Where(token => !token.Name.StartsWith("--tb-color-on-")))
        {
            // A built-in warning is one color for both modes.
            var declared = Regex.Match(block, $@"{token.Name}: (?:light-dark\((.+?), (.+?)\)|(oklch\([^)]*\)));");
            Assert.True(declared.Success, token.Name);
            var (light, dark) = declared.Groups[3].Success ? (declared.Groups[3].Value, declared.Groups[3].Value)
                : (declared.Groups[1].Value, declared.Groups[2].Value);
            Assert.Equal((Resolve(light), Resolve(dark)), (token.Light.ToCss(), token.Dark.ToCss()));
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
    public void ContrastProblems_ShouldBeNone_WhenSeededWithEachBuiltInPalettesStatusColors()
    {
        // The default palette reads the stylesheet's shades, and the others give their own colors.
        foreach (var block in Regex.Matches(Css, @"\[data-tb-palette=""[a-z]+""\] \{(.*?)\}", RegexOptions.Singleline)
            .Select(match => match.Groups[1].Value)
            .Where(block => block.Contains("--tb-color-success: light-dark(oklch")))
        {
            string Light(string status) =>
                Regex.Match(block, $@"--tb-color-{status}: (?:light-dark\()?(oklch\([^)]*\))").Groups[1].Value;
            string Dark(string status) =>
                Regex.Match(block, $@"--tb-color-{status}: light-dark\(oklch\([^)]*\), (oklch\([^)]*\))")
                    .Groups[1].Value;
            var palette = Seeded(new()
            {
                Primary = "#1F5FAD", Success = Light("success"), SuccessDark = Dark("success"),
                Warning = Light("warning"), Danger = Light("danger"), DangerDark = Dark("danger"),
                Info = Light("info"), InfoDark = Dark("info"),
            });

            Assert.Empty(palette.ContrastProblems());
        }
    }

    [Fact]
    public void Tokens_ShouldKeepTheWarningForDarkModeAndLightenTheOthers_WhenNoDarkFormsAreSeeded()
    {
        var palette = Seeded(new()
        {
            Primary = "#1F5FAD", Warning = "oklch(0.75 0.15 70)", Danger = "oklch(0.5 0.2 30)",
        });

        var warning = palette.Tokens.Single(token => token.Name == "--tb-color-warning");
        var danger = palette.Tokens.Single(token => token.Name == "--tb-color-danger");
        Assert.Equal(warning.Light, warning.Dark);
        Assert.Equal(0.69, danger.Dark.L, 0.001);
        Assert.Equal(OklchColor.White, palette.Tokens.Single(token => token.Name == "--tb-color-on-danger").Light);
        Assert.NotEqual(OklchColor.White, palette.Tokens.Single(token => token.Name == "--tb-color-on-warning").Light);
        // A status left out keeps the stylesheet's color, so a region with this palette doesn't take another's.
        var success = palette.Tokens.Single(token => token.Name == "--tb-color-success");
        Assert.Equal((new OklchColor(0.52, 0.17, 150), new OklchColor(0.71, 0.14, 150)), (success.Light, success.Dark));
    }

    [Fact]
    public void ContrastProblems_ShouldNameTheWarningsPress_WhenTheWarningIsTooDarkForDarkText()
    {
        // White text reads on this dark warning, but its hover and press get lighter, as a warning's do.
        var palette = Seeded(new() { Primary = "#1F5FAD", Warning = "oklch(0.55 0.15 70)" });

        Assert.Contains(palette.ContrastProblems(), problem => problem is
            { Mode: "light", Foreground: "--tb-color-on-warning", Background: "--tb-color-warning-active" });
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
