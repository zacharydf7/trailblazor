namespace Trailblazor.BrowserTests;

// Runs in a browser that draws its scrollbars, since a test browser hides them by default and nothing could shift.
public class ScrollLockTests(BrowserFixture fixture) : BrowserTest(fixture)
{
    private static ILocator Button(IPage page, string name) =>
        page.GetByRole(AriaRole.Button, new() { Name = name, Exact = true });

    private Task<IPage> OpenPageAsync() => OpenAsync(BrowserFixture.ChromiumWithScrollbars, "/scroll-lock");

    private static Task<double> BodyWidthAsync(IPage page) =>
        page.EvaluateAsync<double>("() => document.body.getBoundingClientRect().width");

    private static Task<double[]> BodySidesAsync(IPage page) => page.EvaluateAsync<double[]>("""
        () => {
            const box = document.body.getBoundingClientRect();
            return [box.left, box.right];
        }
        """);

    private static Task WaitForNoScrollbarAsync(IPage page) =>
        page.WaitForFunctionAsync("() => innerWidth === document.documentElement.clientWidth");

    private static Task<double> RootPaddingAsync(IPage page) =>
        page.EvaluateAsync<double>("() => parseFloat(getComputedStyle(document.documentElement).paddingRight)");

    private static Task WaitForScrollbarAsync(IPage page, bool shown) => page.WaitForFunctionAsync("""
        shown => (innerWidth > document.documentElement.clientWidth) === shown
            && (parseFloat(document.documentElement.style.getPropertyValue('--tb-page-scrollbar')) > 0) === shown
        """, shown);

    private static async Task<IPage> GrowAsync(IPage page)
    {
        await Button(page, "Grow").ClickAsync();
        await WaitForScrollbarAsync(page, true);
        return page;
    }

    private static async Task OpenModalAsync(IPage page)
    {
        await Button(page, "Open modal").ClickAsync();
        await Expect(page.GetByRole(AriaRole.Dialog)).ToBeVisibleAsync();
    }

    [Fact]
    public async Task Modal_ShouldHoldThePageStillAndCoverTheWindow_WhenItTakesAwayAScrollbar()
    {
        var page = await GrowAsync(await OpenPageAsync());
        var width = await BodyWidthAsync(page);

        await OpenModalAsync(page);
        await WaitForNoScrollbarAsync(page);

        Assert.Equal(width, await BodyWidthAsync(page));
        Assert.Equal("dialog", await page.EvaluateAsync<string?>(
            "() => document.elementFromPoint(innerWidth - 3, innerHeight / 2)?.localName"));

        await page.Keyboard.PressAsync("Escape");
        await WaitForScrollbarAsync(page, true);
        Assert.Equal(width, await BodyWidthAsync(page));
        Assert.Equal(0, await RootPaddingAsync(page));
    }

    [Fact]
    public async Task Overlay_ShouldHoldThePageStillAndCoverTheWindow_WhenItTakesAwayAScrollbar()
    {
        var page = await GrowAsync(await OpenPageAsync());
        var width = await BodyWidthAsync(page);

        await Button(page, "Open overlay").ClickAsync();
        await WaitForNoScrollbarAsync(page);

        Assert.Equal(width, await BodyWidthAsync(page));
        Assert.True(await page.Locator(".tb-overlay").EvaluateAsync<bool>(
            "overlay => overlay.getBoundingClientRect().right === innerWidth"), "The overlay should reach the edge.");
    }

    [Fact]
    public async Task Modal_ShouldKeepMakingUpForTheScrollbar_WhenTheWindowResizesWhileItsOpen()
    {
        var page = await GrowAsync(await OpenPageAsync());
        await OpenModalAsync(page);
        await WaitForNoScrollbarAsync(page);
        var padding = await RootPaddingAsync(page);
        Assert.True(padding > 0);

        await page.SetViewportSizeAsync(page.ViewportSize!.Width - 100, page.ViewportSize.Height);
        await page.EvaluateAsync("() => new Promise(done => requestAnimationFrame(() => requestAnimationFrame(done)))");

        Assert.Equal(padding, await RootPaddingAsync(page));
    }

    [Fact]
    public async Task Modal_ShouldHoldThePageInPlace_WhenThePageRunsRightToLeft()
    {
        var page = await OpenPageAsync();
        await page.EvaluateAsync("() => document.documentElement.dir = 'rtl'");
        await GrowAsync(page);
        var sides = await BodySidesAsync(page);

        await OpenModalAsync(page);
        await WaitForNoScrollbarAsync(page);

        Assert.Equal(sides, await BodySidesAsync(page));
    }

    [Fact]
    public async Task Modal_ShouldHoldThePageStill_WhenThePageAlreadyKeepsAGutter()
    {
        var page = await OpenPageAsync();
        await page.AddStyleTagAsync(new() { Content = "html { scrollbar-gutter: stable; }" });
        await GrowAsync(page);
        var width = await BodyWidthAsync(page);

        await OpenModalAsync(page);
        await WaitForNoScrollbarAsync(page);

        Assert.Equal(width, await BodyWidthAsync(page));
    }

    [Fact]
    public async Task FixedElement_ShouldHoldStill_WhenItAddsTheLockOffset()
    {
        var page = await GrowAsync(await OpenPageAsync());
        var pinned = page.GetByTestId("pinned");
        var before = (await pinned.BoundingBoxAsync())!;

        await OpenModalAsync(page);
        await WaitForNoScrollbarAsync(page);

        Assert.Equal(before.X, (await pinned.BoundingBoxAsync())!.X);
    }

    [Fact]
    public async Task Modal_ShouldAddNoRoom_WhenThePageClipsItselfAfterHavingAScrollbar()
    {
        var page = await GrowAsync(await OpenPageAsync());
        await page.AddStyleTagAsync(new() { Content = "html { overflow: hidden; }" });
        await WaitForScrollbarAsync(page, false);

        await OpenModalAsync(page);

        Assert.Equal(0, await RootPaddingAsync(page));
    }

    [Fact]
    public async Task Modal_ShouldAddNoRoom_WhenAWidePageOnAPhoneReportsAWiderWindow()
    {
        var page = await OpenAsync("chromium", "/scroll-lock",
            new() { IsMobile = true, HasTouch = true, ViewportSize = new() { Width = 400, Height = 700 } });
        await page.AddStyleTagAsync(new() { Content = "body { min-inline-size: 900px; }" });
        await page.WaitForFunctionAsync("() => innerWidth > document.documentElement.clientWidth");
        await page.EvaluateAsync("() => new Promise(done => requestAnimationFrame(() => requestAnimationFrame(done)))");

        await Button(page, "Open modal").EvaluateAsync("button => button.click()");
        await Expect(page.GetByRole(AriaRole.Dialog)).ToBeVisibleAsync();

        Assert.Equal(0, await RootPaddingAsync(page));
    }

    [Fact]
    public async Task Page_ShouldStillKnowItsScrollbar_WhenAnEnhancedNavigationReplacesThePage()
    {
        var page = await OpenStaticAsync(BrowserFixture.ChromiumWithScrollbars, "/static-theme",
            new() { ViewportSize = new() { Width = 800, Height = 20 } });
        await WaitForScrollbarAsync(page, true);
        // A root of pinned height doesn't resize as the page changes, and an adopted sheet outlives the navigation.
        await page.EvaluateAsync("""
            () => {
                const sheet = new CSSStyleSheet();
                sheet.replaceSync('html { block-size: 100vh; }');
                document.adoptedStyleSheets = [sheet];
            }
            """);
        // A full page load would forget this, so its surviving shows the navigation was enhanced.
        await page.EvaluateAsync("() => window.stayedOnThePage = true");

        await page.GetByRole(AriaRole.Link, new() { Name = "Read-only fields" }).ClickAsync();
        await Expect(page.GetByRole(AriaRole.Checkbox, new() { Name = "Agree" })).ToBeAttachedAsync();

        Assert.True(await page.EvaluateAsync<bool>("() => window.stayedOnThePage === true"));
        await WaitForScrollbarAsync(page, true);
    }

    [Fact]
    public async Task ModelessDialog_ShouldLeaveTheScrollbarAlone_WhenThePageScrolls()
    {
        var page = await GrowAsync(await OpenPageAsync());

        await Button(page, "Open modeless").ClickAsync();
        await Expect(page.GetByRole(AriaRole.Dialog)).ToBeVisibleAsync();

        await WaitForScrollbarAsync(page, true);
        Assert.Equal(0, await RootPaddingAsync(page));
    }

    [Fact]
    public async Task Modal_ShouldAddNoRoom_WhenThePageHasNoScrollbar()
    {
        var page = await OpenPageAsync();
        var width = await BodyWidthAsync(page);

        await OpenModalAsync(page);

        Assert.Equal(width, await BodyWidthAsync(page));
        Assert.Equal(0, await RootPaddingAsync(page));
    }

    [Fact]
    public async Task Modal_ShouldHoldThePageStill_WhenAShortPageForcesItsScrollbar()
    {
        var page = await OpenPageAsync();
        await page.AddStyleTagAsync(new() { Content = "html { overflow-y: scroll; }" });
        await WaitForScrollbarAsync(page, true);
        var width = await BodyWidthAsync(page);

        await OpenModalAsync(page);
        await WaitForNoScrollbarAsync(page);

        Assert.Equal(width, await BodyWidthAsync(page));
    }

    [Fact]
    public async Task Modal_ShouldHoldThePageStill_WhenAResetSetsTheRootsPaddingAndSizing()
    {
        var page = await OpenPageAsync();
        await page.AddStyleTagAsync(new() { Content = "html { padding: 0; box-sizing: content-box; inline-size: 100%; }" });
        await GrowAsync(page);
        var width = await BodyWidthAsync(page);

        await OpenModalAsync(page);
        await WaitForNoScrollbarAsync(page);

        Assert.Equal(width, await BodyWidthAsync(page));
    }

    [Fact]
    public async Task Modal_ShouldAddNoRoom_WhenThePageClipsItsOwnOverflow()
    {
        var page = await OpenPageAsync();
        await page.AddStyleTagAsync(new() { Content = "html { overflow: hidden; }" });
        await Button(page, "Grow").ClickAsync();
        var width = await BodyWidthAsync(page);

        await OpenModalAsync(page);

        Assert.Equal(width, await BodyWidthAsync(page));
        Assert.Equal(0, await RootPaddingAsync(page));
    }

    [Fact]
    public async Task Modal_ShouldHoldThePageStill_WhenContentOverflowsABodySizedToTheWindow()
    {
        var page = await OpenPageAsync();
        await page.AddStyleTagAsync(new() { Content = "html, body { block-size: 100%; }" });
        await GrowAsync(page);
        var width = await BodyWidthAsync(page);

        await OpenModalAsync(page);

        Assert.Equal(width, await BodyWidthAsync(page));
    }

    [Fact]
    public async Task Modal_ShouldAddNoRoom_WhenThePageHasShrunkSinceItHadAScrollbar()
    {
        var page = await GrowAsync(await OpenPageAsync());
        await Button(page, "Shrink").ClickAsync();
        await WaitForScrollbarAsync(page, false);

        await OpenModalAsync(page);

        Assert.Equal(0, await RootPaddingAsync(page));
    }

    [Fact]
    public async Task Modal_ShouldHoldThePageStill_WhenTheWindowShrinkingGaveThePageAScrollbar()
    {
        var page = await OpenPageAsync();
        await page.SetViewportSizeAsync(page.ViewportSize!.Width, 40);
        await WaitForScrollbarAsync(page, true);
        var width = await BodyWidthAsync(page);

        await Button(page, "Open modal").EvaluateAsync("button => button.click()");
        await Expect(page.GetByRole(AriaRole.Dialog)).ToBeVisibleAsync();

        Assert.Equal(width, await BodyWidthAsync(page));
    }
}
