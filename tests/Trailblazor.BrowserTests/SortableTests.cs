using Trailblazor.Testing.Playwright;

namespace Trailblazor.BrowserTests;

public class SortableTests(BrowserFixture fixture) : BrowserTest(fixture)
{
    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Handle_ShouldMoveTheItemAfterTheOneItIsDroppedOn_WhenDraggedPastItsMiddle(string browser)
    {
        var page = await OpenReadyAsync(browser);
        var from = (await Handle(page, "Block spam").BoundingBoxAsync())!;
        var to = (await Item(page, "Star the team").BoundingBoxAsync())!;

        await page.Mouse.MoveAsync(from.X + from.Width / 2, from.Y + from.Height / 2);
        await page.Mouse.DownAsync();
        await page.Mouse.MoveAsync(to.X + to.Width / 2, to.Y + to.Height * 0.75f, new() { Steps = 10 });
        await Expect(Item(page, "Star the team")).ToHaveAttributeAsync("data-drop", "after");
        await page.Mouse.UpAsync();

        await Expect(page.GetByTestId("order")).ToHaveTextAsync("Flag invoices, Star the team, Block spam, Catch-all");
        await Expect(page.Locator("[data-drop]")).ToHaveCountAsync(0);
        await Expect(page.Locator("[data-part=announcer] [role=status] p").Last)
            .ToHaveTextAsync("Block spam moved to position 3 of 4.");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Handle_ShouldShowWhyAndLeaveTheOrder_WhenDraggedWhereTheItemMayNotGo(string browser)
    {
        var page = await OpenReadyAsync(browser);
        var from = (await Handle(page, "Catch-all").BoundingBoxAsync())!;
        var to = (await Item(page, "Block spam").BoundingBoxAsync())!;

        await page.Mouse.MoveAsync(from.X + from.Width / 2, from.Y + from.Height / 2);
        await page.Mouse.DownAsync();
        await page.Mouse.MoveAsync(to.X + to.Width / 2, to.Y + to.Height * 0.25f, new() { Steps = 10 });

        await Expect(Item(page, "Block spam")).ToHaveAttributeAsync("data-drop-refused", "");
        var rules = page.Locator(".tb-sortable",
            new() { Has = page.GetByRole(AriaRole.List, new() { Name = "Rules" }) });
        await Expect(rules.Locator("[data-part=drop-reason]")).ToHaveTextAsync("The catch-all rule stays last.");
        await page.Mouse.UpAsync();
        await Expect(page.GetByTestId("order")).ToHaveTextAsync("Block spam, Flag invoices, Star the team, Catch-all");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Keys_ShouldLeaveTheHandlesMenuClosed_WhenUpOrDownIsPressedWithAModifier(string browser)
    {
        var page = await OpenReadyAsync(browser);
        // The handle's menu starts its script on first focus, and only then answers keys.
        await Handle(page, "Block spam").FocusAsync();
        await Expect(Handle(page, "Block spam")).ToHaveAttributeAsync("aria-expanded", "false");

        foreach (var key in new[] { "Alt+ArrowDown", "Control+ArrowUp", "Meta+ArrowDown", "Shift+ArrowUp" })
        {
            await page.Keyboard.PressAsync(key);
            // A wrongly opened menu shows here at once, while focus would move into it only later.
            await Expect(page.Locator("[role=menu]:popover-open")).ToHaveCountAsync(0);
        }

        await page.Keyboard.PressAsync("ArrowDown");
        await Expect(page.GetByTestId("order")).ToHaveTextAsync("Flag invoices, Block spam, Star the team, Catch-all");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Keys_ShouldMoveTheItemAndKeepFocusOnItsHandle_WhenPressedOnTheHandle(string browser)
    {
        var page = await OpenReadyAsync(browser);
        await Handle(page, "Block spam").FocusAsync();

        await page.Keyboard.PressAsync("ArrowDown");
        await Expect(page.GetByTestId("order")).ToHaveTextAsync("Flag invoices, Block spam, Star the team, Catch-all");
        await Expect(Handle(page, "Block spam")).ToBeFocusedAsync();
        await page.Keyboard.PressAsync("Home");

        await Expect(page.GetByTestId("order")).ToHaveTextAsync("Block spam, Flag invoices, Star the team, Catch-all");
        await Expect(Handle(page, "Block spam")).ToBeFocusedAsync();
        await Expect(page.Locator("[data-part=announcer] [role=status] p").Last)
            .ToHaveTextAsync("Block spam moved to position 1 of 4.");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Menu_ShouldMoveTheItemWithOneClick_WhenAMoveIsChosenFromItsHandle(string browser)
    {
        var page = await OpenReadyAsync(browser);

        await Handle(page, "Block spam").ClickAsync();
        await page.GetByRole(AriaRole.Menuitem, new() { Name = "Move down" }).ClickAsync();

        await Expect(page.GetByTestId("order")).ToHaveTextAsync("Flag invoices, Block spam, Star the team, Catch-all");
        await Expect(page.GetByRole(AriaRole.Menu)).ToHaveCountAsync(0);
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task GridRows_ShouldReorderByKeysAndByDragging_WhenTheGridLetsRowsMove(string browser)
    {
        var page = await OpenReadyAsync(browser);
        var grid = page.GetByRole(AriaRole.Table, new() { Name = "Route stops" });
        var stops = grid.Locator("tbody td[data-column=stop]");

        await Handle(page, "Trailhead").FocusAsync();
        await page.Keyboard.PressAsync("End");
        await Expect(stops).ToHaveTextAsync(["Lake camp", "Summit Gate", "Trailhead"]);
        await Expect(Handle(page, "Trailhead")).ToBeFocusedAsync();
        var from = (await Handle(page, "Trailhead").BoundingBoxAsync())!;
        var to = (await grid.Locator("tr[data-part=row]").First.BoundingBoxAsync())!;
        await page.Mouse.MoveAsync(from.X + from.Width / 2, from.Y + from.Height / 2);
        await page.Mouse.DownAsync();
        await page.Mouse.MoveAsync(to.X + to.Width / 2, to.Y + to.Height * 0.25f, new() { Steps = 10 });
        await page.Mouse.UpAsync();

        await Expect(stops).ToHaveTextAsync(["Trailhead", "Lake camp", "Summit Gate"]);
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Keys_ShouldMoveOnlyTheInnerItem_WhenPressedOnAListInsideAGridRow(string browser)
    {
        var page = await OpenReadyAsync(browser);

        await Handle(page, "Ada").FocusAsync();
        await page.Keyboard.PressAsync("ArrowDown");
        await Expect(page.GetByTestId("crews")).ToHaveTextAsync("North crew, South crew; Ben, Ada");
        await Handle(page, "North crew").FocusAsync();
        await page.Keyboard.PressAsync("ArrowDown");

        await Expect(page.GetByTestId("crews")).ToHaveTextAsync("South crew, North crew; Ben, Ada");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Handle_ShouldChooseFromItsMenu_WhenTheTestingHelperUsesItAsThePageLoads(string browser)
    {
        // Not waiting for the page to turn interactive, so the helper meets the prerendered handle first.
        var page = await OpenStaticAsync(browser, "/sortable");

        await page.Tb().Menu("Move Block spam").ChooseAsync("Move down");

        await Expect(page.GetByTestId("order")).ToHaveTextAsync("Flag invoices, Block spam, Star the team, Catch-all");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task SortableHelpers_ShouldFindAnItemByItsLabel_WhenItHoldsAField(string browser)
    {
        var page = await OpenStaticAsync(browser, "/sortable");
        var steps = page.Tb().Sortable("Packing steps");

        await steps.MoveAsync("Stove", 0);
        Assert.Equal(["Stove", "Tent"], await steps.ItemsAsync());
        await steps.DragAfterAsync("Stove", "Tent");

        Assert.Equal(["Tent", "Stove"], await steps.ItemsAsync());
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task SortableHelpers_ShouldMoveByMenuAndStopWhereRefused_WhenMovingAnItemToAPlace(string browser)
    {
        // Skips waiting for the page to turn interactive, so the order is only read after the helpers' own waits.
        var page = await OpenStaticAsync(browser, "/sortable");
        var rules = page.Tb().Sortable("Rules");

        await rules.MoveAsync("Block spam", 2);
        Assert.Equal("Flag invoices, Star the team, Block spam, Catch-all",
            await page.GetByTestId("order").TextContentAsync());
        await rules.MoveAsync("Star the team", 0);
        Assert.Equal(["Star the team", "Flag invoices", "Block spam", "Catch-all"], await rules.ItemsAsync());
        await rules.MoveAsync("Flag invoices", 3);

        Assert.Equal(["Star the team", "Flag invoices", "Block spam", "Catch-all"], await rules.ItemsAsync());
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => rules.MoveAsync("Catch-all", 4));
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task SortableHelpers_ShouldDropBeforeOrAfterAnItem_WhenDraggingByTheHandle(string browser)
    {
        var page = await OpenStaticAsync(browser, "/sortable");
        var rules = page.Tb().Sortable("Rules");

        await rules.DragAfterAsync("Block spam", "Star the team");
        Assert.Equal("Flag invoices, Star the team, Block spam, Catch-all",
            await page.GetByTestId("order").TextContentAsync());
        await rules.DragBeforeAsync("Catch-all", "Flag invoices");
        Assert.Equal("Flag invoices, Star the team, Block spam, Catch-all",
            await page.GetByTestId("order").TextContentAsync());
        await rules.DragBeforeAsync("Star the team", "Flag invoices");

        Assert.Equal(["Star the team", "Flag invoices", "Block spam", "Catch-all"], await rules.ItemsAsync());
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task GridHelpers_ShouldMoveAndDragRows_WhenTheGridLetsRowsMove(string browser)
    {
        var page = await OpenStaticAsync(browser, "/sortable");
        var grid = page.Tb().Grid("Route stops");
        var stops = page.GetByRole(AriaRole.Table, new() { Name = "Route stops" })
            .Locator("tbody td[data-column=stop]");

        await grid.MoveRowAsync("Trailhead", 2);
        Assert.Equal(["Lake camp", "Summit Gate", "Trailhead"], await stops.AllInnerTextsAsync());
        await grid.DragRowBeforeAsync("Trailhead", "Lake camp");
        Assert.Equal(["Trailhead", "Lake camp", "Summit Gate"], await stops.AllInnerTextsAsync());
        await grid.DragRowAfterAsync("Trailhead", "Summit Gate");

        Assert.Equal(["Lake camp", "Summit Gate", "Trailhead"], await stops.AllInnerTextsAsync());
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task GridHelpers_ShouldDropInTheHandlesColumn_WhenRowsAreWiderThanTheGrid(string browser)
    {
        var page = await OpenStaticAsync(browser, "/sortable");
        var camps = page.GetByRole(AriaRole.Table, new() { Name = "Camps" }).Locator("tbody td[data-column=camp]");

        await page.Tb().Grid("Camps").DragRowAfterAsync("Pine camp", "Fern camp");

        Assert.Equal(["Fern camp", "Pine camp", "Ridge camp"], await camps.AllInnerTextsAsync());
    }

    // The lists and the grids hear drags and keys once their scripts have attached.
    private async Task<IPage> OpenReadyAsync(string browser)
    {
        var page = await OpenAsync(browser, "/sortable");
        await Expect(page.Locator(".tb-sortable:not([data-ready]), [data-part=grid]:not([data-ready])"))
            .ToHaveCountAsync(0);
        return page;
    }

    private static ILocator Item(IPage page, string text) =>
        page.Locator("[data-part=item]").Filter(new() { HasText = text });

    private static ILocator Handle(IPage page, string text) =>
        page.GetByRole(AriaRole.Button, new() { Name = $"Move {text}", Exact = true });
}
