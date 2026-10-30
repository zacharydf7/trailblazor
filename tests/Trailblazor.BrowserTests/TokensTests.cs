using System.Text.RegularExpressions;
using Trailblazor.Docs.Examples;

namespace Trailblazor.BrowserTests;

public class TokensTests(BrowserFixture fixture) : BrowserTest(fixture)
{
    private static readonly string[] Tones = ["primary", "neutral", "success", "warning", "danger", "info"];

    private static readonly string[] Palettes = [.. PaletteCatalog.All.Select(palette => palette.Name)];

    // Reads a color token on a probe inside <main> as its sRGB channels, 0 to 255.
    private const string ChannelsScript = """
        token => {
            const probe = document.createElement('div');
            probe.style.color = `var(${token})`;
            document.querySelector('main').append(probe);
            const context = document.createElement('canvas').getContext('2d');
            context.fillStyle = getComputedStyle(probe).color;
            probe.remove();
            context.fillRect(0, 0, 1, 1);
            return [...context.getImageData(0, 0, 1, 1).data].slice(0, 3);
        }
        """;

    public static TheoryData<string, string, string> BrowsersPalettesAndThemes => new(
        from browser in AllBrowsers.Names
        from palette in Palettes
        from theme in new[] { "light", "dark" }
        select (browser, palette, theme));

    public static TheoryData<string, string> BrowsersAndThemes => new(
        from browser in AllBrowsers.Names
        from theme in new[] { "light", "dark" }
        select (browser, theme));

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Tokens_ShouldUseLightColors_WhenNoThemeIsSetAndSystemPrefersDark(string browser)
    {
        var page = await OpenAsync(browser, "/tokens", new() { ColorScheme = ColorScheme.Dark });

        Assert.True(await GetLuminanceAsync(page, "--tb-color-surface") > 0.9);
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Tokens_ShouldUseDarkColors_WhenThemeIsSystemAndSystemPrefersDark(string browser)
    {
        var page = await OpenAsync(browser, "/tokens", new() { ColorScheme = ColorScheme.Dark });

        await SetThemeAsync(page, "system");

        Assert.True(await GetLuminanceAsync(page, "--tb-color-surface") < 0.1);
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Tokens_ShouldUseLightColors_WhenThemeIsSystemAndSystemPrefersLight(string browser)
    {
        var page = await OpenAsync(browser, "/tokens", new() { ColorScheme = ColorScheme.Light });

        await SetThemeAsync(page, "system");

        Assert.True(await GetLuminanceAsync(page, "--tb-color-surface") > 0.9);
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Tokens_ShouldUseDarkColors_WhenThemeIsDark(string browser)
    {
        var page = await OpenAsync(browser, "/tokens", new() { ColorScheme = ColorScheme.Light });

        await SetThemeAsync(page, "dark");

        Assert.True(await GetLuminanceAsync(page, "--tb-color-surface") < 0.1);
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Tokens_ShouldUseDarkColors_WhenAnAncestorHasDarkTheme(string browser)
    {
        var page = await OpenAsync(browser, "/tokens");

        await page.EvalOnSelectorAsync("main", "main => main.dataset.tbTheme = 'dark'");

        Assert.True(await GetLuminanceAsync(page, "--tb-color-surface") < 0.1);
    }

    [Theory, MemberData(nameof(BrowsersPalettesAndThemes))]
    public async Task TextTokens_ShouldMeetContrastMinimum_WhenAPaletteAndThemeAreApplied(string browser,
        string palette, string theme)
    {
        var page = await OpenAsync(browser, "/tokens");
        await page.EvaluateAsync("palette => document.documentElement.dataset.tbPalette = palette", palette);
        await SetThemeAsync(page, theme);
        var pairs = new List<(string Foreground, string Background)>();
        foreach (var background in new[] { "background", "surface", "surface-raised", "surface-sunken" })
        {
            pairs.Add(("--tb-color-text", $"--tb-color-{background}"));
            pairs.Add(("--tb-color-text-muted", $"--tb-color-{background}"));
        }

        foreach (var tone in Tones)
        {
            pairs.Add(($"--tb-color-on-{tone}", $"--tb-color-{tone}"));
            pairs.Add(($"--tb-color-on-{tone}", $"--tb-color-{tone}-hover"));
            pairs.Add(($"--tb-color-on-{tone}", $"--tb-color-{tone}-active"));
            pairs.Add(($"--tb-color-{tone}-text", "--tb-color-surface"));
            pairs.Add(($"--tb-color-{tone}-text", $"--tb-color-{tone}-subtle"));
        }

        var failures = new List<string>();
        foreach (var (foreground, background) in pairs)
        {
            var ratio = ContrastRatio(await GetLuminanceAsync(page, foreground),
                await GetLuminanceAsync(page, background));
            if (ratio < 4.5)
            {
                failures.Add($"{foreground} on {background}: {ratio:F2}");
            }
        }

        Assert.Empty(failures);
    }

    [Theory, MemberData(nameof(BrowsersPalettesAndThemes))]
    public async Task NonTextTokens_ShouldMeetContrastMinimum_WhenAPaletteAndThemeAreApplied(string browser,
        string palette, string theme)
    {
        var page = await OpenAsync(browser, "/tokens");
        await page.EvaluateAsync("palette => document.documentElement.dataset.tbPalette = palette", palette);
        await SetThemeAsync(page, theme);

        var failures = new List<string>();
        foreach (var foreground in new[] { "--tb-focus-ring-color", "--tb-border-strong" })
        {
            foreach (var background in new[] { "background", "surface", "surface-raised", "surface-sunken" })
            {
                var ratio = ContrastRatio(await GetLuminanceAsync(page, foreground),
                    await GetLuminanceAsync(page, $"--tb-color-{background}"));
                if (ratio < 3)
                {
                    failures.Add($"{foreground} on --tb-color-{background}: {ratio:F2}");
                }
            }
        }

        Assert.Empty(failures);
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Palette_ShouldBeGraphiteInk_WhenNoPaletteIsSet(string browser)
    {
        var page = await OpenAsync(browser, "/tokens");

        var (red, green, blue) = await GetChannelsAsync(page, "--tb-color-primary");

        Assert.True(Math.Max(red, Math.Max(green, blue)) - Math.Min(red, Math.Min(green, blue)) < 12,
            $"Primary should be gray ink, not rgb({red}, {green}, {blue}).");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Palette_ShouldTurnPrimaryBlue_WhenAnAncestorChoosesCobalt(string browser)
    {
        var page = await OpenAsync(browser, "/tokens");

        await page.EvalOnSelectorAsync("main", "main => main.dataset.tbPalette = 'cobalt'");

        var (red, _, blue) = await GetChannelsAsync(page, "--tb-color-primary");
        Assert.True(blue - red > 80, $"Primary should be blue, not rgb with red {red} and blue {blue}.");
    }

    [Fact]
    public async Task PaletteCatalog_ShouldListEveryPaletteInTheStylesheet_WhenCalled()
    {
        using var client = new HttpClient { BaseAddress = Fixture.BaseAddress };

        var css = await client.GetStringAsync("_content/Trailblazor/trailblazor.css",
            TestContext.Current.CancellationToken);

        var declared = Regex.Matches(css, "data-tb-palette=\"([a-z]+)\"").Select(match => match.Groups[1].Value);
        Assert.Equal(declared.Distinct().Order(), Palettes.Order());
    }

    [Theory, MemberData(nameof(BrowsersAndThemes))]
    public async Task Palettes_ShouldKeepPrimaryApartFromTheStatusColors_WhenTheirPrimaryIsColored(string browser,
        string theme)
    {
        var page = await OpenAsync(browser, "/tokens");
        await SetThemeAsync(page, theme);

        var failures = new List<string>();
        foreach (var palette in Palettes)
        {
            await page.EvaluateAsync("palette => document.documentElement.dataset.tbPalette = palette", palette);
            var (chroma, hue) = ToChromaAndHue(await GetChannelsAsync(page, "--tb-color-primary"));
            // An ink primary has no hue to mistake for a status color.
            if (chroma < 0.04)
            {
                continue;
            }

            foreach (var tone in new[] { "success", "warning", "danger", "info" })
            {
                var (_, toneHue) = ToChromaAndHue(await GetChannelsAsync(page, $"--tb-color-{tone}"));
                var apart = Math.Abs(hue - toneHue) is var gap && gap > 180 ? 360 - gap : gap;
                if (apart < 20)
                {
                    failures.Add($"{palette} primary is {apart:F0} degrees from {tone}");
                }
            }
        }

        Assert.Empty(failures);
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task ConsumerCss_ShouldOverrideToken_WhenUnlayeredRuleHasLowerSpecificity(string browser)
    {
        var page = await OpenAsync(browser, "/tokens");

        await page.AddStyleTagAsync(new() { Content = "html { --tb-color-primary: rgb(0, 128, 128); }" });

        await Expect(page.GetByTestId("primary-probe")).ToHaveCSSAsync("color", "rgb(0, 128, 128)");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task MotionTokens_ShouldKeepDurations_WhenNoMotionPreferenceIsSet(string browser)
    {
        var page = await OpenAsync(browser, "/tokens", new() { ReducedMotion = ReducedMotion.NoPreference });

        Assert.Equal("200ms", await GetRootTokenAsync(page, "--tb-motion-duration-normal"));
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task MotionTokens_ShouldBeZero_WhenReducedMotionIsPreferred(string browser)
    {
        var page = await OpenAsync(browser, "/tokens", new() { ReducedMotion = ReducedMotion.Reduce });

        Assert.Equal("0ms", await GetRootTokenAsync(page, "--tb-motion-duration-normal"));
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task ControlHeight_ShouldShrink_WhenDensityIsCompact(string browser)
    {
        var page = await OpenAsync(browser, "/tokens");
        var probe = page.GetByTestId("probe");
        await Expect(probe).ToHaveCSSAsync("height", "40px");

        await page.EvaluateAsync("() => document.documentElement.dataset.tbDensity = 'compact'");

        await Expect(probe).ToHaveCSSAsync("height", "32px");
    }

    private static async Task<double> GetLuminanceAsync(IPage page, string token)
    {
        var (red, green, blue) = await GetChannelsAsync(page, token);
        return 0.2126 * Linear(red) + 0.7152 * Linear(green) + 0.0722 * Linear(blue);
    }

    private static double Linear(int channel)
    {
        var value = channel / 255.0;
        return value <= 0.04045 ? value / 12.92 : Math.Pow((value + 0.055) / 1.055, 2.4);
    }

    // OKLCH chroma and hue, in degrees, of an sRGB color, by way of OKLab.
    private static (double Chroma, double Hue) ToChromaAndHue((int Red, int Green, int Blue) color)
    {
        var (red, green, blue) = (Linear(color.Red), Linear(color.Green), Linear(color.Blue));
        var l = Math.Cbrt(0.4122214708 * red + 0.5363325363 * green + 0.0514459929 * blue);
        var m = Math.Cbrt(0.2119034982 * red + 0.6806995451 * green + 0.1073969566 * blue);
        var s = Math.Cbrt(0.0883024619 * red + 0.2817188376 * green + 0.6299787005 * blue);
        var a = 1.9779984951 * l - 2.4285922050 * m + 0.4505937099 * s;
        var b = 0.0259040371 * l + 0.7827717662 * m - 0.8086757660 * s;
        return (Math.Sqrt(a * a + b * b), (Math.Atan2(b, a) * 180 / Math.PI + 360) % 360);
    }

    private static async Task<(int Red, int Green, int Blue)> GetChannelsAsync(IPage page, string token)
    {
        var channels = await page.EvaluateAsync<int[]>(ChannelsScript, token);
        return (channels[0], channels[1], channels[2]);
    }

    private static async Task<string> GetRootTokenAsync(IPage page, string token) =>
        (await page.EvaluateAsync<string>(
            "token => getComputedStyle(document.documentElement).getPropertyValue(token)", token)).Trim();

    private static double ContrastRatio(double first, double second) =>
        (Math.Max(first, second) + 0.05) / (Math.Min(first, second) + 0.05);
}
