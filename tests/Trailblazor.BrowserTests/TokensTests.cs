namespace Trailblazor.BrowserTests;

public class TokensTests(BrowserFixture fixture) : BrowserTest(fixture)
{
    // Resolves a color token on a probe inside <main>, then reads it back as sRGB through a canvas.
    private const string LuminanceScript = """
        token => {
            const probe = document.createElement('div');
            probe.style.color = `var(${token})`;
            document.querySelector('main').append(probe);
            const color = getComputedStyle(probe).color;
            probe.remove();
            const context = document.createElement('canvas').getContext('2d');
            context.fillStyle = color;
            context.fillRect(0, 0, 1, 1);
            const [r, g, b] = [...context.getImageData(0, 0, 1, 1).data].map(value => {
                const channel = value / 255;
                return channel <= 0.04045 ? channel / 12.92 : ((channel + 0.055) / 1.055) ** 2.4;
            });
            return 0.2126 * r + 0.7152 * g + 0.0722 * b;
        }
        """;

    private static readonly string[] Tones = ["primary", "neutral", "success", "warning", "danger", "info"];

    public static TheoryData<string, string> BrowsersAndThemes =>
        new(AllBrowsers.Names.SelectMany(browser => new[] { (browser, "light"), (browser, "dark") }));

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

    [Theory, MemberData(nameof(BrowsersAndThemes))]
    public async Task TextTokens_ShouldMeetContrastMinimum_WhenThemeIsApplied(string browser, string theme)
    {
        var page = await OpenAsync(browser, "/tokens");
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

    private static Task<double> GetLuminanceAsync(IPage page, string token) =>
        page.EvaluateAsync<double>(LuminanceScript, token);

    private static async Task<string> GetRootTokenAsync(IPage page, string token) =>
        (await page.EvaluateAsync<string>(
            "token => getComputedStyle(document.documentElement).getPropertyValue(token)", token)).Trim();

    private static double ContrastRatio(double first, double second) =>
        (Math.Max(first, second) + 0.05) / (Math.Min(first, second) + 0.05);
}
