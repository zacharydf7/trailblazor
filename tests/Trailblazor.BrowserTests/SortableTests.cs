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
    public async Task MoveAsync_ShouldMoveTheItemAndSaySo_WhenAnAppsButtonCallsIt(string browser)
    {
        var page = await OpenReadyAsync(browser);

        await page.GetByRole(AriaRole.Button, new() { Name = "Send the first rule down" }).ClickAsync();

        await Expect(page.GetByTestId("order")).ToHaveTextAsync("Flag invoices, Star the team, Block spam, Catch-all");
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
