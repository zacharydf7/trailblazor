namespace Trailblazor.BrowserTests.Primitives;

public class AnchoredPositionTests(BrowserFixture fixture) : BrowserTest(fixture)
{
    private const double Offset = 8;
    private const double ViewportPadding = 8;

    public static TheoryData<string, string, string> Placements => new(AllBrowsers.Names.SelectMany(browser => new[]
    {
        (browser, "Bottom", "Center"), (browser, "Top", "Start"), (browser, "Start", "Center"), (browser, "End", "End"),
    }));

    [Theory, MemberData(nameof(Placements))]
    public async Task CreateAsync_ShouldPlaceFloatingNextToAnchor_WhenThereIsRoom(string browser, string side,
        string align)
    {
        var page = await OpenAttachedAsync(browser, $"/anchored-position?side={side}&align={align}");

        var (anchor, floating) = await GetBoxesAsync(page);

        (double X, double Y) expected = (side, align) switch
        {
            ("Bottom", "Center") => (anchor.X + (anchor.Width - floating.Width) / 2, anchor.Y + anchor.Height + Offset),
            ("Top", "Start") => (anchor.X, anchor.Y - Offset - floating.Height),
            ("Start", "Center") =>
                (anchor.X - Offset - floating.Width, anchor.Y + (anchor.Height - floating.Height) / 2),
            ("End", "End") => (anchor.X + anchor.Width + Offset, anchor.Y + anchor.Height - floating.Height),
            _ => throw new ArgumentOutOfRangeException(nameof(side)),
        };
        Assert.Equal(expected.X, floating.X, 1.0);
        Assert.Equal(expected.Y, floating.Y, 1.0);
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task CreateAsync_ShouldFlipToTop_WhenThereIsNoRoomBelow(string browser)
    {
        var page = await OpenAttachedAsync(browser, "/anchored-position?side=Bottom&y=640");

        var (anchor, floating) = await GetBoxesAsync(page);

        Assert.Equal(anchor.Y - Offset - floating.Height, floating.Y, 1.0);
        await Expect(page.GetByTestId("floating")).ToHaveAttributeAsync("data-side", "top");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task CreateAsync_ShouldShiftIntoViewport_WhenAnchorIsAtTheEdge(string browser)
    {
        var page = await OpenAttachedAsync(browser, "/anchored-position?side=Bottom&align=Center&x=0");

        var (_, floating) = await GetBoxesAsync(page);

        Assert.Equal(ViewportPadding, floating.X, 1.0);
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task CreateAsync_ShouldPlaceStartSideOnTheRight_WhenDirectionIsRtl(string browser)
    {
        var page = await OpenAttachedAsync(browser, "/anchored-position?side=Start&rtl=true");

        var (anchor, floating) = await GetBoxesAsync(page);

        Assert.Equal(anchor.X + anchor.Width + Offset, floating.X, 1.0);
        await Expect(page.GetByTestId("floating")).ToHaveAttributeAsync("data-side", "start");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Scroll_ShouldKeepFloatingAttached_WhenScrollContainerScrolls(string browser)
    {
        var page = await OpenAttachedAsync(browser, "/anchored-position?side=Bottom");

        await page.GetByTestId("scroller").EvaluateAsync("scroller => scroller.scrollTop = 100");
        await WaitForFramesAsync(page);

        var (anchor, floating) = await GetBoxesAsync(page);
        Assert.Equal(200, anchor.Y, 1.0);
        Assert.Equal(anchor.Y + anchor.Height + Offset, floating.Y, 1.0);
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Resize_ShouldRecenterFloating_WhenAnchorSizeChanges(string browser)
    {
        var page = await OpenAttachedAsync(browser, "/anchored-position?side=Bottom&align=Center");

        await page.GetByTestId("anchor").EvaluateAsync("anchor => anchor.style.width = '300px'");
        await WaitForFramesAsync(page);

        var (anchor, floating) = await GetBoxesAsync(page);
        Assert.Equal(anchor.X + (300 - floating.Width) / 2, floating.X, 1.0);
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task DisposeAsync_ShouldStopFollowingAnchor_WhenCalled(string browser)
    {
        var page = await OpenAttachedAsync(browser, "/anchored-position?side=Bottom");
        await page.GetByTestId("detach").ClickAsync();
        await Expect(page.GetByTestId("attached")).ToHaveTextAsync("False");
        var (_, before) = await GetBoxesAsync(page);

        await page.GetByTestId("scroller").EvaluateAsync("scroller => scroller.scrollTop = 100");
        await WaitForFramesAsync(page);

        var (_, after) = await GetBoxesAsync(page);
        Assert.Equal(before.Y, after.Y, 1.0);
    }

    private async Task<IPage> OpenAttachedAsync(string browser, string path)
    {
        var page = await OpenAsync(browser, path);
        await Expect(page.GetByTestId("attached")).ToHaveTextAsync("True");
        return page;
    }

    private static async Task<(LocatorBoundingBoxResult Anchor, LocatorBoundingBoxResult Floating)> GetBoxesAsync(
        IPage page)
    {
        var anchor = await page.GetByTestId("anchor").BoundingBoxAsync();
        var floating = await page.GetByTestId("floating").BoundingBoxAsync();
        return (anchor!, floating!);
    }

    // Scroll events and ResizeObserver callbacks run during the next rendering update.
    private static Task WaitForFramesAsync(IPage page) =>
        page.EvaluateAsync("() => new Promise(resolve => requestAnimationFrame(() => requestAnimationFrame(resolve)))");
}
