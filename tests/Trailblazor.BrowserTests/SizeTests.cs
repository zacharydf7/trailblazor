namespace Trailblazor.BrowserTests;

public class SizeTests(BrowserFixture fixture) : BrowserTest(fixture)
{
    // An example's sizes section names each control "<Size> <name>". The part measured is the first in each, with its
    // small, medium and large heights in comfortable density and then in compact.
    private static readonly (string Example, AriaRole Role, string Name, string Part, string[] Comfortable,
        string[] Compact)[] Parts =
    [
        ("tabs", AriaRole.Tablist, "tabs", "[role=tab]", ["32px", "40px", "48px"], ["26px", "32px", "40px"]),
        ("listbox", AriaRole.Listbox, "start time", "[role=option]", ["28px", "32px", "40px"], ["24px", "28px", "36px"]),
        ("toolbar", AriaRole.Toolbar, "map tools", "button", ["32px", "40px", "48px"], ["26px", "32px", "40px"]),
    ];

    private static readonly string[] Sizes = ["Small", "Medium", "Large"];

    public static TheoryData<string, string> PartsInEveryBrowser => new(
        from browser in AllBrowsers.Names from part in Parts select (browser, part.Example));

    [Theory, MemberData(nameof(PartsInEveryBrowser))]
    public async Task Size_ShouldStepThePartsHeightWithDensity_WhenSmallMediumOrLarge(string browser, string example)
    {
        var (_, role, name, selector, comfortable, compact) = Parts.Single(part => part.Example == example);
        var page = await OpenAsync(browser, $"/examples/{example}");
        var parts = Sizes.Select(size => page.GetByRole(role, new() { Name = $"{size} {name}", Exact = true })
            .Locator(selector).First).ToArray();

        await ExpectHeightsAsync(parts, comfortable);
        await page.EvaluateAsync("() => document.documentElement.dataset.tbDensity = 'compact'");
        await ExpectHeightsAsync(parts, compact);
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Slider_ShouldScaleTheTrackButKeepA24PixelTarget_WhenSmallMediumOrLarge(string browser)
    {
        var page = await OpenAsync(browser, "/examples/slider");
        var sliders = Sizes
            .Select(size => page.GetByRole(AriaRole.Slider, new() { Name = $"{size} volume", Exact = true })).ToArray();
        var tracks = sliders.Select(slider => page.Locator(".tb-slider").Filter(new() { Has = slider })
            .Locator("[data-part=track]")).ToArray();

        await ExpectHeightsAsync(sliders, ["24px", "24px", "24px"]);
        await ExpectHeightsAsync(tracks, ["3px", "4px", "6px"]);
    }

    private static async Task ExpectHeightsAsync(ILocator[] parts, string[] heights)
    {
        for (var i = 0; i < parts.Length; i++)
        {
            await Expect(parts[i]).ToHaveCSSAsync("height", heights[i]);
        }
    }
}
