namespace Trailblazor.BrowserTests;

public class SegmentedControlTests(BrowserFixture fixture) : BrowserTest(fixture)
{
    private const string Clear = "rgba(0, 0, 0, 0)";

    private static ILocator Segment(IPage page, string name) =>
        page.GetByRole(AriaRole.Radio, new() { Name = name }).Locator("xpath=..");

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task SegmentedControl_ShouldFillTheChosenSegmentAndHideTheRadios_WhenRendered(string browser)
    {
        var page = await OpenAsync(browser, "/examples/segmented-control");

        await Expect(Segment(page, "Moderate")).Not.ToHaveCSSAsync("background-color", Clear);
        await Expect(Segment(page, "Easy")).ToHaveCSSAsync("background-color", Clear);
        await Expect(page.GetByRole(AriaRole.Radio, new() { Name = "Easy" })).ToHaveCSSAsync("opacity", "0");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task SegmentedControl_ShouldChooseASegment_WhenItIsClicked(string browser)
    {
        var page = await OpenAsync(browser, "/examples/segmented-control");

        await Segment(page, "Hard").ClickAsync();

        await Expect(page.GetByText("Showing hard trails.")).ToBeVisibleAsync();
        await Expect(Segment(page, "Hard")).Not.ToHaveCSSAsync("background-color", Clear);
        await Expect(Segment(page, "Moderate")).ToHaveCSSAsync("background-color", Clear);
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task SegmentedControl_ShouldMoveTheChoice_WhenAnArrowKeyIsPressed(string browser)
    {
        var page = await OpenAsync(browser, "/examples/segmented-control");
        await page.GetByRole(AriaRole.Radio, new() { Name = "Moderate" }).FocusAsync();

        await page.Keyboard.PressAsync("ArrowRight");

        await Expect(page.GetByText("Showing hard trails.")).ToBeVisibleAsync();
        await Expect(page.GetByRole(AriaRole.Radio, new() { Name = "Hard" })).ToBeFocusedAsync();
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task SegmentedControl_ShouldRingTheFocusedSegment_WhenItHasKeyboardFocus(string browser)
    {
        var page = await OpenAsync(browser, "/examples/segmented-control");

        // The control is the page's first tab stop, and Tab lands on the chosen segment.
        await page.Keyboard.PressAsync("Tab");

        await Expect(page.GetByRole(AriaRole.Radio, new() { Name = "Moderate" })).ToBeFocusedAsync();
        await Expect(Segment(page, "Moderate")).ToHaveCSSAsync("outline-style", "solid");
        await Expect(Segment(page, "Easy")).ToHaveCSSAsync("outline-style", "none");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task SegmentedControl_ShouldShadeOnlySegmentsItCanChoose_WhenHovered(string browser)
    {
        var page = await OpenAsync(browser, "/examples/segmented-control");

        await Segment(page, "Overnight").HoverAsync();
        await Expect(Segment(page, "Overnight")).ToHaveCSSAsync("background-color", Clear);

        await Segment(page, "Solo").HoverAsync();
        await Expect(Segment(page, "Solo")).Not.ToHaveCSSAsync("background-color", Clear);
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task SegmentedControl_ShouldHighlightTheChosenSegment_WhenColorsAreForced(string browser)
    {
        var page = await OpenAsync(browser, "/examples/segmented-control", new() { ForcedColors = ForcedColors.Active });
        var highlight = await SystemColorAsync(page, "Highlight");

        await Expect(Segment(page, "Moderate")).ToHaveCSSAsync("background-color", highlight);
        await Expect(Segment(page, "Easy")).Not.ToHaveCSSAsync("background-color", highlight);
    }
}
