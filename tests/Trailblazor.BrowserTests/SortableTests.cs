using Trailblazor.Testing.Playwright;

namespace Trailblazor.BrowserTests;

public class SortableTests(BrowserFixture fixture) : BrowserTest(fixture)
{
    protected override bool HoldsTheMouse => true;

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
    public async Task RefusedDrop_ShouldShowADashedMarkAndABorderedReason_WhenColorsAreForced(string browser)
    {
        var page = await OpenReadyAsync(browser, new() { ForcedColors = ForcedColors.Active });
        var from = (await Handle(page, "Catch-all").BoundingBoxAsync())!;
        var to = (await Item(page, "Block spam").BoundingBoxAsync())!;

        await page.Mouse.MoveAsync(from.X + from.Width / 2, from.Y + from.Height / 2);
        await page.Mouse.DownAsync();
        await page.Mouse.MoveAsync(to.X + to.Width / 2, to.Y + to.Height * 0.25f, new() { Steps = 10 });

        var rules = page.Locator(".tb-sortable",
            new() { Has = page.GetByRole(AriaRole.List, new() { Name = "Rules" }) });
        var reason = rules.Locator("[data-part=drop-reason]");
        await Expect(Item(page, "Block spam")).ToHaveCSSAsync("border-top-style", "dashed");
        await Expect(reason).ToBeVisibleAsync();
        await Expect(reason).ToHaveCSSAsync("border-top-style", "solid");
        await Expect(reason).ToHaveCSSAsync("border-top-width", "1px");
        await page.Mouse.UpAsync();
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task List_ShouldShowWhereToDropAndTakeAnItemOnItsEmptyContent_WhenItHasNoItems(string browser)
    {
        var page = await OpenAsync(browser, "/examples/sortable");
        var hiked = page.GetByRole(AriaRole.List, new() { Name = "Hiked" });

        await Expect(hiked.Locator("[data-part=empty]")).ToHaveTextAsync("No trails yet");
        await Expect(hiked).ToHaveCSSAsync("outline-style", "dashed");

        // The drop is at the list's middle, on its empty content.
        await page.Tb().Sortable("Planned").DragToListAsync("Pine trail", "Hiked");

        Assert.Equal(["Pine trail"], await page.Tb().Sortable("Hiked").ItemsAsync());
        await Expect(hiked.Locator("[data-part=empty]")).ToHaveCountAsync(0);
        await Expect(hiked).ToHaveCSSAsync("outline-style", "none");
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
    public async Task Handle_ShouldOpenItsMenu_WhenAFingerRestsOnItForAMomentBeforeLifting(string browser)
    {
        var page = await OpenReadyAsync(browser);

        Assert.True(await SlowTapAsync(Handle(page, "Block spam"), "[data-part=item]", "data-dragging"));

        // The tap opened the handle's menu, as a tap does, and the item was put down where it was.
        await Expect(page.GetByRole(AriaRole.Menuitem, new() { Name = "Move down" })).ToBeVisibleAsync();
        await Expect(Item(page, "Block spam")).Not.ToHaveAttributeAsync("data-dragging", "");
        await Expect(page.GetByTestId("order")).ToHaveTextAsync("Block spam, Flag invoices, Star the team, Catch-all");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task GridRows_ShouldOpenAHandlesMenu_WhenAFingerRestsOnItForAMomentBeforeLifting(string browser)
    {
        var page = await OpenReadyAsync(browser);
        var grid = page.GetByRole(AriaRole.Table, new() { Name = "Route stops" });

        Assert.True(await SlowTapAsync(Handle(page, "Trailhead"), "tr[data-part=row]", "data-dragging"));

        await Expect(page.GetByRole(AriaRole.Menuitem, new() { Name = "Move down" })).ToBeVisibleAsync();
        await Expect(grid.Locator("tbody td[data-column=stop]"))
            .ToHaveTextAsync(["Trailhead", "Lake camp", "Summit Gate"]);
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Handle_ShouldLeaveItsMenuClosed_WhenEscapeCancelsAMouseDragBeforeTheRelease(string browser)
    {
        var page = await OpenReadyAsync(browser);
        var box = (await Handle(page, "Block spam").BoundingBoxAsync())!;
        var (x, y) = (box.X + box.Width / 2, box.Y + box.Height / 2);

        // The item is carried a little way and back, so the release lands on the handle it was pressed on.
        await page.Mouse.MoveAsync(x, y);
        await page.Mouse.DownAsync();
        await page.Mouse.MoveAsync(x, y + 12, new() { Steps = 3 });
        await Expect(Item(page, "Block spam")).ToHaveAttributeAsync("data-dragging", "");
        await page.Mouse.MoveAsync(x, y, new() { Steps = 3 });
        await page.Keyboard.PressAsync("Escape");
        await page.Mouse.UpAsync();

        await Expect(Item(page, "Block spam")).Not.ToHaveAttributeAsync("data-dragging", "");
        await Expect(page.GetByRole(AriaRole.Menu)).ToHaveCountAsync(0);
        await Expect(page.GetByTestId("order")).ToHaveTextAsync("Block spam, Flag invoices, Star the team, Catch-all");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Handle_ShouldLeaveItsMenuClosed_WhenEscapeCancelsAFingersHoldBeforeItMoves(string browser)
    {
        var page = await OpenReadyAsync(browser);

        // The finger picks the item up and holds still, Escape calls the drag off, and then the finger lifts.
        var pickedUp = await Handle(page, "Block spam").EvaluateAsync<bool>("""
            async handle => {
                const box = handle.getBoundingClientRect();
                const fire = type => handle.dispatchEvent(new PointerEvent(type, { bubbles: true, pointerId: 7,
                    pointerType: 'touch', isPrimary: true, button: 0, clientX: box.left + box.width / 2,
                    clientY: box.top + box.height / 2 }));
                fire('pointerdown');
                await new Promise(resolve => setTimeout(resolve, 400));
                const carried = handle.closest('[data-part="item"]').hasAttribute('data-dragging');
                document.dispatchEvent(new KeyboardEvent('keydown', { key: 'Escape', bubbles: true,
                    cancelable: true }));
                fire('pointerup');
                handle.click();
                return carried;
            }
            """);

        Assert.True(pickedUp);
        await Expect(Item(page, "Block spam")).Not.ToHaveAttributeAsync("data-dragging", "");
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
    private async Task<IPage> OpenReadyAsync(string browser, BrowserNewContextOptions? options = null)
    {
        var page = await OpenAsync(browser, "/sortable", options);
        await Expect(page.Locator(".tb-sortable:not([data-ready]), [data-part=grid]:not([data-ready])"))
            .ToHaveCountAsync(0);
        return page;
    }

    private static ILocator Item(IPage page, string text) =>
        page.Locator("[data-part=item]").Filter(new() { HasText = text });

    private static ILocator Handle(IPage page, string text) =>
        page.GetByRole(AriaRole.Button, new() { Name = $"Move {text}", Exact = true });
}
