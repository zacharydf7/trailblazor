namespace Trailblazor.BrowserTests;

public class DrawerTests(BrowserFixture fixture) : BrowserTest(fixture)
{
    // Reduced motion opens the drawer at once, so it is measured where it rests, not partway through sliding in.
    private static readonly BrowserNewContextOptions Still = new() { ReducedMotion = ReducedMotion.Reduce };

    private static async Task<(double Left, double Top, double Right, double Bottom)> EdgesAsync(ILocator element)
    {
        var box = (await element.BoundingBoxAsync())!;
        return (box.X, box.Y, box.X + box.Width, box.Y + box.Height);
    }

    private static async Task<(double Width, double Height)> WindowAsync(IPage page)
    {
        var size = await page.EvaluateAsync<double[]>("() => [document.documentElement.clientWidth, innerHeight]");
        return (size[0], size[1]);
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Drawer_ShouldFillTheWindowsHeightAtItsEnd_WhenOpened(string browser)
    {
        var page = await OpenAsync(browser, "/examples/drawer", Still);
        await page.GetByRole(AriaRole.Button, new() { Name = "Filters" }).ClickAsync();
        var drawer = page.GetByRole(AriaRole.Dialog, new() { Name = "Trail filters" });
        await Expect(drawer).ToBeVisibleAsync();

        var (_, top, right, bottom) = await EdgesAsync(drawer);
        var (width, height) = await WindowAsync(page);

        Assert.Equal((0, width, height), (Math.Round(top), Math.Round(right), Math.Round(bottom)));
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Drawer_ShouldOpenOnTheRight_WhenItsAtTheStartOfRightToLeftText(string browser)
    {
        var page = await OpenAsync(browser, "/examples/drawer", Still);
        await page.EvaluateAsync("() => document.documentElement.dir = 'rtl'");
        await page.GetByRole(AriaRole.Button, new() { Name = "Open at the start" }).ClickAsync();
        var drawer = page.GetByRole(AriaRole.Dialog, new() { Name = "Share trail" });
        await Expect(drawer).ToBeVisibleAsync();

        var (_, _, right, _) = await EdgesAsync(drawer);
        var (width, _) = await WindowAsync(page);

        Assert.Equal(width, Math.Round(right));
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Drawer_ShouldSpanTheBottomOfTheWindow_WhenItIsABottomSheet(string browser)
    {
        var page = await OpenAsync(browser, "/examples/drawer", Still);
        await page.GetByRole(AriaRole.Button, new() { Name = "Open a bottom sheet" }).ClickAsync();
        var drawer = page.GetByRole(AriaRole.Dialog, new() { Name = "Share trail" });
        await Expect(drawer).ToBeVisibleAsync();

        var (left, top, right, bottom) = await EdgesAsync(drawer);
        var (width, height) = await WindowAsync(page);

        Assert.Equal((0, width, height), (Math.Round(left), Math.Round(right), Math.Round(bottom)));
        Assert.True(top > height / 2, $"The sheet starts at {top}, in the top half of a {height} window.");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Drawer_ShouldKeepItsActionsAtTheBottom_WhenItsContentIsShort(string browser)
    {
        var page = await OpenAsync(browser, "/examples/drawer", Still);
        await page.GetByRole(AriaRole.Button, new() { Name = "Filters" }).ClickAsync();
        var drawer = page.GetByRole(AriaRole.Dialog, new() { Name = "Trail filters" });
        await Expect(drawer).ToBeVisibleAsync();

        var (_, _, _, actionsBottom) = await EdgesAsync(drawer.Locator("[data-part=actions]"));
        var (_, _, _, drawerBottom) = await EdgesAsync(drawer);

        Assert.InRange(drawerBottom - actionsBottom, 0, 2);
    }

    public static TheoryData<string, string, string, string, string> Edges => new(
        from browser in AllBrowsers.Names
        from edge in new[]
        {
            ("Open at the start", "Share trail", "ltr", "-100%"),
            ("Open at the start", "Share trail", "rtl", "100%"),
            ("Filters", "Trail filters", "ltr", "100%"),
            ("Filters", "Trail filters", "rtl", "-100%"),
            ("Open at the top", "Share trail", "ltr", "0px -100%"),
            ("Open a bottom sheet", "Share trail", "ltr", "0px 100%"),
        }
        select (browser, edge.Item1, edge.Item2, edge.Item3, edge.Item4));

    // data-leaving sets the same translate the drawer slides in from and back to, so setting it shows which edge it is.
    [Theory, MemberData(nameof(Edges))]
    public async Task Drawer_ShouldSlideFromItsOwnEdge_WhenItOpensAndCloses(string browser, string opener,
        string title, string direction, string translate)
    {
        var page = await OpenAsync(browser, "/examples/drawer", Still);
        await page.EvaluateAsync("direction => document.documentElement.dir = direction", direction);
        await page.GetByRole(AriaRole.Button, new() { Name = opener }).ClickAsync();
        var drawer = page.GetByRole(AriaRole.Dialog, new() { Name = title });
        await Expect(drawer).ToBeVisibleAsync();

        await Expect(drawer).ToHaveCSSAsync("translate", "none");
        await drawer.EvaluateAsync("drawer => drawer.dataset.leaving = ''");

        await Expect(drawer).ToHaveCSSAsync("translate", translate);
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Drawer_ShouldKeepItsActionsInView_WhenItsContentScrolls(string browser)
    {
        var page = await OpenAsync(browser, "/examples/drawer", Still);
        await page.GetByRole(AriaRole.Button, new() { Name = "Filters" }).ClickAsync();
        var drawer = page.GetByRole(AriaRole.Dialog, new() { Name = "Trail filters" });
        await Expect(drawer).ToBeVisibleAsync();
        var body = drawer.Locator("[data-part=body]");
        await body.EvaluateAsync("body => body.prepend(Object.assign(document.createElement('div'), "
            + "{ style: 'height: 3000px' }))");
        await body.EvaluateAsync("body => body.scrollTop = body.scrollHeight / 2");

        var (_, _, _, actionsBottom) = await EdgesAsync(drawer.Locator("[data-part=actions]"));
        var (_, _, _, bodyBottom) = await EdgesAsync(body);

        Assert.InRange(bodyBottom - actionsBottom, -1, 1);
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Drawer_ShouldScrollAFocusedFieldOutFromBehindItsActions_WhenTheyCoverIt(string browser)
    {
        var page = await OpenAsync(browser, "/examples/drawer", Still);
        await page.GetByRole(AriaRole.Button, new() { Name = "Filters" }).ClickAsync();
        var drawer = page.GetByRole(AriaRole.Dialog, new() { Name = "Trail filters" });
        await Expect(drawer).ToBeVisibleAsync();
        var body = drawer.Locator("[data-part=body]");
        await body.EvaluateAsync("""
            body => {
                const field = Object.assign(document.createElement('button'), { textContent: 'Low field' });
                const filler = Object.assign(document.createElement('div'), { style: 'flex: none; height: 3000px' });
                body.querySelector('[data-part=actions]').before(filler, field);
                body.scrollTop += field.getBoundingClientRect().bottom - body.getBoundingClientRect().bottom + 2;
            }
            """);
        var field = drawer.GetByRole(AriaRole.Button, new() { Name = "Low field" });
        await drawer.GetByRole(AriaRole.Button, new() { Name = "Reset" }).FocusAsync();

        await page.Keyboard.PressAsync("Shift+Tab");

        await Expect(field).ToBeFocusedAsync();

        var (_, _, _, fieldBottom) = await EdgesAsync(field);
        var (_, actionsTop, _, _) = await EdgesAsync(drawer.Locator("[data-part=actions]"));
        Assert.True(fieldBottom <= actionsTop + 1,
            $"The field ends at {fieldBottom}, under actions from {actionsTop}.");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Drawer_ShouldCloseAndGiveFocusBack_WhenEscapeIsPressed(string browser)
    {
        var page = await OpenAsync(browser, "/examples/drawer");
        var opener = page.GetByRole(AriaRole.Button, new() { Name = "Filters" });
        await opener.ClickAsync();
        var drawer = page.GetByRole(AriaRole.Dialog, new() { Name = "Trail filters" });
        await Expect(drawer.GetByRole(AriaRole.Checkbox, new() { Name = "Dogs allowed" })).ToBeFocusedAsync();

        await page.Keyboard.PressAsync("Escape");

        await Expect(drawer).ToBeHiddenAsync();
        await Expect(opener).ToBeFocusedAsync();
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Drawer_ShouldLeaveThePageUsable_WhenItIsNotModal(string browser)
    {
        var page = await OpenAsync(browser, "/examples/drawer");
        await page.GetByRole(AriaRole.Button, new() { Name = "Ridge loop" }).ClickAsync();
        await Expect(page.GetByRole(AriaRole.Dialog, new() { Name = "Ridge loop details" })).ToBeVisibleAsync();

        await page.GetByRole(AriaRole.Button, new() { Name = "Creek path" }).ClickAsync();

        await Expect(page.GetByRole(AriaRole.Dialog, new() { Name = "Creek path details" })).ToBeVisibleAsync();
    }
}
