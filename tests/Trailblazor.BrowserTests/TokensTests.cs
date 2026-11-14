using System.Text.RegularExpressions;
using Trailblazor.Docs.Examples;

namespace Trailblazor.BrowserTests;

public class TokensTests(BrowserFixture fixture) : BrowserTest(fixture)
{
    private static readonly string[] Tones =
        ["primary", "secondary", "neutral", "success", "warning", "danger", "info"];

    private static readonly string[] Palettes = [.. PaletteCatalog.All.Select(palette => palette.Name)];

    // Reads a color token on a probe inside <main>, or inside an element with the given classes, as its sRGB
    // channels, 0 to 255. When under is given, the token is painted over that color token first, so a translucent
    // one reads as it shows.
    private const string ChannelsScript = """
        ([token, classes, under]) => {
            const host = document.createElement('div');
            host.className = classes ?? '';
            const probe = document.createElement('div');
            host.append(probe);
            document.querySelector('main').append(host);
            const context = document.createElement('canvas').getContext('2d');
            if (under) {
                probe.style.color = `var(${under})`;
                context.fillStyle = getComputedStyle(probe).color;
                context.fillRect(0, 0, 1, 1);
            }
            probe.style.color = `var(${token})`;
            const color = getComputedStyle(probe).color;
            host.remove();
            // A color the canvas can't parse leaves its last fill, which would read as two equal colors.
            context.fillStyle = '#010203';
            context.fillStyle = color;
            if (context.fillStyle === '#010203') {
                throw new Error(`The canvas can't paint ${token}, computed as "${color}".`);
            }
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
        // The host puts the app's default theme on <html>, so the attribute comes off for a page with none.
        await page.EvaluateAsync("() => delete document.documentElement.dataset.tbTheme");

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
    public async Task SoftNeutralButton_ShouldNotLookDisabled_WhenAPaletteAndThemeAreApplied(string browser,
        string palette, string theme)
    {
        var page = await OpenAsync(browser, "/tokens");
        await page.EvaluateAsync("palette => document.documentElement.dataset.tbPalette = palette", palette);
        await SetThemeAsync(page, theme);
        const string Soft = "tb-button tb-button--neutral tb-button--soft";

        var text = await GetLuminanceAsync(page, "--tb-button-text", Soft);
        var fill = await GetLuminanceAsync(page, "--tb-button-subtle", Soft);
        var background = await GetLuminanceAsync(page, "--tb-color-background");

        // Below these, a soft neutral button reads as disabled beside other buttons.
        Assert.True(ContrastRatio(text, fill) >= 6.5, $"Text on fill: {ContrastRatio(text, fill):F2}");
        Assert.True(ContrastRatio(fill, background) >= 1.3, $"Fill on page: {ContrastRatio(fill, background):F3}");
    }

    [Theory, MemberData(nameof(BrowsersPalettesAndThemes))]
    public async Task SubtleFills_ShouldStandOutFromEverySurfaceAndEachOther_WhenAPaletteAndThemeAreApplied(
        string browser, string palette, string theme)
    {
        var page = await OpenAsync(browser, "/tokens");
        await page.EvaluateAsync("palette => document.documentElement.dataset.tbPalette = palette", palette);
        await SetThemeAsync(page, theme);

        // A hover fill, a selection fill or a divider the same as the panel under it can't be seen, a selected row must
        // not look hovered, and a sunken well must not vanish into the page. Two fills differ by lightness, at least
        // the 1.04:1 of neighboring light steps, by how colorful they are, or by hue when both have color.
        string[] surfaces = ["--tb-color-background", "--tb-color-surface", "--tb-color-surface-raised",
            "--tb-color-surface-sunken"];
        var pairs = new[] { "--tb-color-neutral-subtle", "--tb-color-primary-subtle", "--tb-border-subtle" }
            .SelectMany(fill => surfaces.Select(surface => (fill, surface)))
            .Append(("--tb-color-primary-subtle", "--tb-color-neutral-subtle"))
            .Append(("--tb-color-surface-sunken", "--tb-color-background"));
        var failures = new List<string>();
        foreach (var (fill, under) in pairs)
        {
            var ratio = ContrastRatio(await GetLuminanceAsync(page, fill), await GetLuminanceAsync(page, under));
            var (chroma, hue) = ToChromaAndHue(await GetChannelsAsync(page, fill));
            var (underChroma, underHue) = ToChromaAndHue(await GetChannelsAsync(page, under));
            var apart = Math.Abs(hue - underHue) is var gap && gap > 180 ? 360 - gap : gap;
            var colorful = Math.Abs(chroma - underChroma) >= 0.02
                || (Math.Min(chroma, underChroma) >= 0.02 && apart >= 20);
            if (ratio < 1.04 && !colorful)
            {
                failures.Add($"{fill} on {under}: {ratio:F3}, {apart:F0} degrees apart");
            }
        }

        // Hover and press are translucent, so each is painted over the surface: hover must stand out from the bare
        // surface, and press from hover.
        foreach (var surface in surfaces)
        {
            var bare = await GetLuminanceAsync(page, surface);
            var hovered = await GetLuminanceAsync(page, "--tb-color-hover", under: surface);
            var pressed = await GetLuminanceAsync(page, "--tb-color-pressed", under: surface);
            foreach (var (layer, ratio) in new[] { ("hover", ContrastRatio(hovered, bare)),
                ("pressed", ContrastRatio(pressed, hovered)) })
            {
                if (ratio < 1.04)
                {
                    failures.Add($"{layer} on {surface}: {ratio:F3}");
                }
            }
        }

        Assert.Empty(failures);
    }

    // A case's name, the component token it sets to 37px, the markup whose last element reads it, the property read
    // there and the value expected.
    private static readonly (string Name, string Token, string Html, string Property, string Expected)[]
        ComponentTokens =
    [
        ("button radius", "--tb-button-radius", "<button class='tb-button'>Go</button>", "border-top-left-radius",
            "37px"),
        ("button height", "--tb-button-height", "<button class='tb-button'>Go</button>", "min-height", "37px"),
        ("icon button width", "--tb-button-height", "<button class='tb-button tb-icon-button'></button>", "min-width",
            "37px"),
        ("pagination page width", "--tb-button-height", "<button class='tb-button tb-pagination__page'>1</button>",
            "min-inline-size", "37px"),
        ("field radius", "--tb-field-radius", "<div class='tb-field__control'></div>", "border-top-left-radius",
            "37px"),
        ("field height", "--tb-field-height", "<div class='tb-field__control'></div>", "min-height", "37px"),
        ("field border width", "--tb-field-border-width", "<div class='tb-field__control'></div>",
            "border-bottom-width", "37px"),
        ("floating button radius", "--tb-fab-radius", "<button class='tb-button tb-fab'>Go</button>",
            "border-top-left-radius", "37px"),
        ("floating button offset", "--tb-fab-offset", "<button class='tb-button tb-fab tb-fab--bottomend'>Go</button>",
            "bottom", "37px"),
        ("menu radius", "--tb-menu-radius", "<div class='tb-menu__list'></div>", "border-top-left-radius", "37px"),
        ("menu item height", "--tb-menu-item-height", "<div class='tb-menu__item'></div>", "min-height", "37px"),
        ("popover radius", "--tb-popover-radius", "<div class='tb-popover__panel'></div>", "border-top-left-radius",
            "37px"),
        ("dialog radius", "--tb-dialog-radius", "<dialog class='tb-dialog'></dialog>", "border-top-left-radius",
            "37px"),
        ("end drawer radius", "--tb-dialog-radius", "<dialog class='tb-dialog tb-drawer tb-drawer--end'></dialog>",
            "border-top-left-radius", "37px"),
        ("start drawer radius", "--tb-dialog-radius", "<dialog class='tb-dialog tb-drawer tb-drawer--start'></dialog>",
            "border-top-right-radius", "37px"),
        ("top drawer radius", "--tb-dialog-radius", "<dialog class='tb-dialog tb-drawer tb-drawer--top'></dialog>",
            "border-bottom-left-radius", "37px"),
        ("bottom drawer radius", "--tb-dialog-radius",
            "<dialog class='tb-dialog tb-drawer tb-drawer--bottom'></dialog>", "border-top-left-radius", "37px"),
        ("select popup radius", "--tb-popover-radius", "<div class='tb-select__popup'></div>", "border-top-left-radius",
            "37px"),
        ("date range panel radius", "--tb-popover-radius", "<div class='tb-date-range__panel'></div>",
            "border-top-left-radius", "37px"),
        ("tree item height", "--tb-tree-item-height", "<div class='tb-tree__item'></div>", "min-block-size", "37px"),
        ("tree item radius", "--tb-tree-item-radius", "<div class='tb-tree__item'></div>", "border-top-left-radius",
            "37px"),
        ("table row height", "--tb-table-row-height", "<table class='tb-table'><tr><td>Row</td></tr></table>",
            "height", "37px"),
        ("grid row height", "--tb-data-grid-row-height", Grid, "height", "37px"),
        ("grid ignores table row height", "--tb-table-row-height", Grid, "height", "44px"),
    ];

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task FieldTokens_ShouldColorTheFieldsEdgeAndFill_WhenSetAroundIt(string browser)
    {
        var page = await OpenAsync(browser, "/tokens");

        var outlined = await FieldStyleAsync(page, "tb-field");
        var filled = await FieldStyleAsync(page, "tb-field tb-field--filled");

        Assert.Equal(["0px", "1px", "rgb(1, 2, 3)", "rgb(4, 5, 6)"], outlined);
        Assert.Equal(["0px", "1px", "rgb(1, 2, 3)", "rgb(4, 5, 6)"], filled);
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task FieldTokens_ShouldKeepEveryEdge_WhenColorsAreForced(string browser)
    {
        var page = await OpenAsync(browser, "/tokens", new() { ForcedColors = ForcedColors.Active });

        var style = await FieldStyleAsync(page, "tb-field", width: "0");

        Assert.Equal(["1px", "1px"], style[..2]);
    }

    // The field's top and bottom edge widths, bottom edge color and fill, with the field tokens set around it.
    private static Task<string[]> FieldStyleAsync(IPage page, string fieldClass, string width = "0 0 1px") =>
        page.EvaluateAsync<string[]>("""
            ([fieldClass, width]) => {
                const host = document.querySelector('main').appendChild(document.createElement('div'));
                host.style.setProperty('--tb-field-border-width', width);
                host.style.setProperty('--tb-field-border-color', 'rgb(1, 2, 3)');
                host.style.setProperty('--tb-field-background', 'rgb(4, 5, 6)');
                host.innerHTML = `<div class='${fieldClass}'><div class='tb-field__control'></div></div>`;
                const style = getComputedStyle(host.querySelector('.tb-field__control'));
                const values = [style.borderTopWidth, style.borderBottomWidth, style.borderBottomColor,
                    style.backgroundColor];
                host.remove();
                return values;
            }
            """, new[] { fieldClass, width });

    private const string Grid =
        "<div class='tb-data-grid'><table class='tb-data-grid__table'><tr><td>Row</td></tr></table></div>";

    public static TheoryData<string, string> ComponentTokensInEveryBrowser => new(
        from browser in AllBrowsers.Names from entry in ComponentTokens select (browser, entry.Name));

    [Theory, MemberData(nameof(ComponentTokensInEveryBrowser))]
    public async Task ComponentToken_ShouldRestyleOnlyItsComponent_WhenSetAroundIt(string browser, string name)
    {
        var page = await OpenAsync(browser, "/tokens");
        var (_, token, html, property, expected) = ComponentTokens.Single(entry => entry.Name == name);

        var value = await page.EvaluateAsync<string>("""
            ([token, html, property]) => {
                const host = document.querySelector('main').appendChild(document.createElement('div'));
                host.style.setProperty(token, '37px');
                host.innerHTML = html;
                const part = [...host.querySelectorAll('*')].at(-1);
                const value = getComputedStyle(part).getPropertyValue(property);
                host.remove();
                return value;
            }
            """, new[] { token, html, property });

        Assert.Equal(expected, value);
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task PrimaryStates_ShouldFollowAPrimarySetOnARegion_WhenOnlyThePrimaryIsSet(string browser)
    {
        var page = await OpenAsync(browser, "/tokens");

        // Each state's hue under a region with a teal primary, and the hover under one that also pins it.
        const string teal = "--tb-color-primary: oklch(0.52 0.12 190)";
        var hues = await RegionHuesAsync(page, (teal, "--tb-color-primary-hover"), (teal, "--tb-color-primary-active"),
            (teal, "--tb-color-primary-subtle"), (teal, "--tb-color-primary-text"), (teal, "--tb-focus-ring-color"),
            ($"{teal}; --tb-color-primary-hover: oklch(0.5 0.1 30)", "--tb-color-primary-hover"));

        Assert.All(hues[..5], hue => Assert.Equal(190, hue, 1.0));
        Assert.Equal(30, hues[5], 1.0);
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task SecondaryStates_ShouldFollowASecondarySetOnARegion_WhenOnlyTheSecondaryIsSet(string browser)
    {
        var page = await OpenAsync(browser, "/tokens");

        // Each state's hue under a region with a rose secondary, and the hover under one that also pins it.
        const string rose = "--tb-color-secondary: oklch(0.52 0.15 10)";
        var hues = await RegionHuesAsync(page, (rose, "--tb-color-secondary-hover"),
            (rose, "--tb-color-secondary-active"), (rose, "--tb-color-secondary-subtle"),
            (rose, "--tb-color-secondary-text"),
            ($"{rose}; --tb-color-secondary-hover: oklch(0.5 0.1 250)", "--tb-color-secondary-hover"));

        Assert.All(hues[..4], hue => Assert.Equal(10, hue, 1.0));
        Assert.Equal(250, hues[4], 1.0);
    }

    // The hue each token takes inside a region with that inline style.
    private static Task<double[]> RegionHuesAsync(IPage page, params (string Style, string Token)[] reads) =>
        page.EvaluateAsync<double[]>("""
            reads => reads.map(([style, token]) => {
                const host = document.querySelector('main').appendChild(document.createElement('div'));
                host.setAttribute('style', style);
                const probe = host.appendChild(document.createElement('div'));
                probe.style.color = `oklch(from var(${token}) l c h)`;
                const hue = Number(getComputedStyle(probe).color.match(/oklch\(\S+ \S+ ([\d.]+)/)[1]);
                host.remove();
                return hue;
            })
            """, reads.Select(read => new[] { read.Style, read.Token }).ToArray());

    [Theory, MemberData(nameof(BrowsersAndThemes))]
    public async Task PrimaryStates_ShouldMoveAndKeepTheFocusRingVisible_WhenARegionSetsAnyPrimary(string browser,
        string theme)
    {
        var page = await OpenAsync(browser, "/tokens");

        // For primaries from near black to near white: the lightness of the primary, hover and press, then the focus
        // ring and surface as sRGB channels.
        var regions = await page.EvaluateAsync<double[][]>("""
            theme => [0.12, 0.18, 0.3, 0.5, 0.7, 0.85, 0.92, 0.97].map(lightness => {
                const host = document.querySelector('main').appendChild(document.createElement('div'));
                host.dataset.tbTheme = theme;
                host.setAttribute('style', `--tb-color-primary: oklch(${lightness} 0.1 85)`);
                const probe = host.appendChild(document.createElement('div'));
                const context = document.createElement('canvas').getContext('2d');
                const read = color => { probe.style.color = color; return getComputedStyle(probe).color; };
                const l = token => Number(read(`oklch(from var(${token}) l c h)`).match(/oklch\(([\d.]+)/)[1]);
                const rgb = token => {
                    // A color the canvas can't parse leaves its last fill, which would read as a passing color.
                    context.fillStyle = '#010203';
                    context.fillStyle = read(`var(${token})`);
                    if (context.fillStyle === '#010203') {
                        throw new Error(`The canvas can't paint ${token}.`);
                    }
                    context.fillRect(0, 0, 1, 1);
                    return [...context.getImageData(0, 0, 1, 1).data].slice(0, 3);
                };
                const values = [l('--tb-color-primary'), l('--tb-color-primary-hover'),
                    l('--tb-color-primary-active'), ...rgb('--tb-focus-ring-color'), ...rgb('--tb-color-surface')];
                host.remove();
                return values;
            })
            """, theme);

        Assert.All(regions, region =>
        {
            Assert.True(Math.Abs(region[1] - region[0]) >= 0.05, $"hover {region[1]} from {region[0]}");
            Assert.True(Math.Abs(region[2] - region[0]) >= 0.05, $"press {region[2]} from {region[0]}");
            Assert.True(Math.Sign(region[1] - region[0]) == Math.Sign(region[2] - region[0]),
                $"hover {region[1]} and press {region[2]} move in opposite directions from {region[0]}");
            var ring = ContrastRatio(Luminance(region[3..6]), Luminance(region[6..9]));
            Assert.True(ring >= 3, $"focus ring {ring:F2} for a primary at {region[0]}");
        });
    }

    [Theory, MemberData(nameof(BrowsersPalettesAndThemes))]
    public async Task PressedToggle_ShouldStandOutFromThePageAndAHoveredOne_WhenAPaletteAndThemeAreApplied(
        string browser, string palette, string theme)
    {
        var page = await OpenAsync(browser, "/tokens", new() { ReducedMotion = ReducedMotion.Reduce });
        await page.EvaluateAsync("palette => document.documentElement.dataset.tbPalette = palette", palette);
        await SetThemeAsync(page, theme);
        // The default outlined neutral group and the quiet text one, each with a pressed item and one to hover, and a
        // pressed text toggle on its own.
        await page.EvaluateAsync("""
            () => {
                ['outlined', 'text'].forEach(variant => {
                    const group = document.createElement('div');
                    group.className = 'tb-toggle-group';
                    for (const on of [true, false]) {
                        const item = group.appendChild(document.createElement('button'));
                        item.className = `tb-button tb-button--${variant} tb-button--neutral`;
                        item.setAttribute('aria-pressed', String(on));
                        item.dataset.testid = `${variant}-${on ? 'on' : 'off'}`;
                        item.textContent = 'Trails';
                    }
                    document.querySelector('main').append(group);
                });
                const alone = document.querySelector('main').appendChild(document.createElement('button'));
                alone.className = 'tb-button tb-button--text tb-button--neutral';
                alone.setAttribute('aria-pressed', 'true');
                alone.dataset.testid = 'alone';
                alone.textContent = 'Pin';
            }
            """);
        var background = await GetLuminanceAsync(page, "--tb-color-background");
        var failures = new List<string>();

        // The default look meets the 3:1 a control's state needs; the quiet look is a tint, below that by design.
        foreach (var (variant, minimum) in new[] { ("outlined", 3.0), ("text", 1.25) })
        {
            var pressed = await FillLuminanceAsync(page, $"{variant}-on");
            await page.GetByTestId($"{variant}-off").HoverAsync();
            var hovered = await FillLuminanceAsync(page, $"{variant}-off");
            var (onPage, onHover) = (ContrastRatio(pressed, background), ContrastRatio(pressed, hovered));
            if (onPage < minimum || onHover < minimum)
            {
                failures.Add($"{variant}: {onPage:F2} on the page, {onHover:F2} beside a hovered item");
            }
        }

        // Out of a group, a text toggle keeps an edge that tells pressed from off.
        var edge = ContrastRatio(await FillLuminanceAsync(page, "alone", "borderTopColor"), background);
        if (edge < 3)
        {
            failures.Add($"a text toggle on its own: its edge is {edge:F2} on the page");
        }

        Assert.Empty(failures);
    }

    // An element's fill painted over the page background, since a translucent fill lets the page show through.
    private static async Task<double> FillLuminanceAsync(IPage page, string testId,
        string property = "backgroundColor") =>
        Luminance(await page.EvaluateAsync<double[]>("""
            ([testId, property]) => {
                const probe = document.querySelector('main').appendChild(document.createElement('div'));
                probe.style.color = 'var(--tb-color-background)';
                const context = document.createElement('canvas').getContext('2d');
                context.fillStyle = getComputedStyle(probe).color;
                probe.remove();
                context.fillRect(0, 0, 1, 1);
                context.fillStyle = getComputedStyle(document.querySelector(`[data-testid=${testId}]`))[property];
                context.fillRect(0, 0, 1, 1);
                return [...context.getImageData(0, 0, 1, 1).data].slice(0, 3);
            }
            """, new[] { testId, property }));

    private static double Luminance(double[] channels) =>
        0.2126 * Linear((int)channels[0]) + 0.7152 * Linear((int)channels[1]) + 0.0722 * Linear((int)channels[2]);

    [Theory, MemberData(nameof(BrowsersPalettesAndThemes))]
    public async Task WarningDot_ShouldReachThreeToOneOnItsSurface_WhenAPaletteAndThemeAreApplied(string browser,
        string palette, string theme)
    {
        var page = await OpenAsync(browser, "/tokens");
        await page.EvaluateAsync("palette => document.documentElement.dataset.tbPalette = palette", palette);
        await SetThemeAsync(page, theme);

        var dot = await GetLuminanceAsync(page, "--tb-badge-fill", "tb-badge tb-badge--dot tb-badge--warning");
        var ratio = ContrastRatio(dot, await GetLuminanceAsync(page, "--tb-color-surface"));

        Assert.True(ratio >= 3, $"{ratio:F2}");
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

    [Theory, MemberData(nameof(BrowsersPalettesAndThemes))]
    public async Task ChoiceFills_ShouldStandOutFromThePageAndTheirMarks_WhenAPaletteAndThemeAreApplied(string browser,
        string palette, string theme)
    {
        var page = await OpenAsync(browser, "/tokens");
        await page.EvaluateAsync("palette => document.documentElement.dataset.tbPalette = palette", palette);
        await SetThemeAsync(page, theme);

        var failures = new List<string>();
        foreach (var block in new[] { "tb-checkbox", "tb-switch", "tb-radio-group", "tb-checkbox-group" })
        {
            foreach (var tone in Tones)
            {
                var classes = $"{block} {block}--{tone}";
                var fill = await GetLuminanceAsync(page, "--tb-choice-fill", classes);
                foreach (var other in new[]
                {
                    "--tb-color-background", "--tb-color-surface", "--tb-color-surface-raised",
                    "--tb-color-surface-sunken", "--tb-choice-on-fill",
                })
                {
                    var ratio = ContrastRatio(fill, await GetLuminanceAsync(page, other, classes));
                    if (ratio < 3)
                    {
                        failures.Add($"{classes} fill on {other}: {ratio:F2}");
                    }
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
    public async Task Palette_ShouldNameEveryPaletteInTheStylesheet_WhenCalled()
    {
        using var client = new HttpClient { BaseAddress = Fixture.BaseAddress };

        var css = await client.GetStringAsync("_content/Trailblazor/trailblazor.css",
            TestContext.Current.CancellationToken);

        var declared = Regex.Matches(css, "data-tb-palette=\"([a-z]+)\"").Select(match => match.Groups[1].Value);
        Assert.Equal(declared.Distinct().Order(), Palettes.Order());
    }

    [Theory, MemberData(nameof(BrowsersAndThemes))]
    public async Task Palettes_ShouldKeepPrimaryAndSecondaryApartFromTheStatusColors_WhenTheyAreColored(
        string browser, string theme)
    {
        var page = await OpenAsync(browser, "/tokens");
        await SetThemeAsync(page, theme);

        var failures = new List<string>();
        foreach (var palette in Palettes)
        {
            await page.EvaluateAsync("palette => document.documentElement.dataset.tbPalette = palette", palette);
            foreach (var brand in new[] { "primary", "secondary" })
            {
                var (chroma, hue) = ToChromaAndHue(await GetChannelsAsync(page, $"--tb-color-{brand}"));
                // An ink color has no hue to mistake for a status color.
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
                        failures.Add($"{palette} {brand} is {apart:F0} degrees from {tone}");
                    }
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

        Assert.Equal("0ms", await GetRootTokenAsync(page, "--tb-motion-duration-fast"));
        Assert.Equal("0ms", await GetRootTokenAsync(page, "--tb-motion-duration-normal"));
        Assert.Equal("0ms", await GetRootTokenAsync(page, "--tb-motion-duration-slow"));
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

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task ControlHeight_ShouldFollowTheNearestDensity_WhenRegionsSetTheirOwn(string browser)
    {
        var page = await OpenAsync(browser, "/tokens");

        await Expect(page.GetByTestId("compact-probe")).ToHaveCSSAsync("height", "32px");
        await Expect(page.GetByTestId("comfortable-probe")).ToHaveCSSAsync("height", "40px");
    }

    private static async Task<double> GetLuminanceAsync(IPage page, string token, string? classes = null,
        string? under = null)
    {
        var (red, green, blue) = await GetChannelsAsync(page, token, classes, under);
        return Luminance([red, green, blue]);
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

    private static async Task<(int Red, int Green, int Blue)> GetChannelsAsync(IPage page, string token,
        string? classes = null, string? under = null)
    {
        var channels = await page.EvaluateAsync<int[]>(ChannelsScript, new[] { token, classes, under });
        return (channels[0], channels[1], channels[2]);
    }

    private static async Task<string> GetRootTokenAsync(IPage page, string token) =>
        (await page.EvaluateAsync<string>(
            "token => getComputedStyle(document.documentElement).getPropertyValue(token)", token)).Trim();

    private static double ContrastRatio(double first, double second) =>
        (Math.Max(first, second) + 0.05) / (Math.Min(first, second) + 0.05);
}
