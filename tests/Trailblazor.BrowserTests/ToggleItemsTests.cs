namespace Trailblazor.BrowserTests;

public class ToggleItemsTests(BrowserFixture fixture) : BrowserTest(fixture)
{
    private static ILocator Group(IPage page, string name) => page.GetByRole(AriaRole.Group, new() { Name = name });

    private static ILocator Item(IPage page, string group, string name) =>
        Group(page, group).GetByRole(AriaRole.Button, new() { Name = name, Exact = true });

    private async Task<IPage> OpenReadyAsync(string browser, string group = "Layers")
    {
        var page = await OpenAsync(browser, "/toggle-items");
        await Expect(Group(page, group)).ToHaveAttributeAsync("data-ready", "");
        return page;
    }

    // The item's fill once it has finished fading, which is what tells pressed, hovered and off apart.
    private static Task<string> FillAsync(ILocator item) => item.EvaluateAsync<string>(
        "async item => { await Promise.all(item.getAnimations().map(fade => fade.finished)); "
        + "return getComputedStyle(item).backgroundColor; }");

    // The name of the item on top at a point, where two joined items overlap by their border.
    private static Task<string?> NameOnTopAsync(IPage page, double x, double y) => page.EvaluateAsync<string?>(
        "([x, y]) => document.elementFromPoint(x, y).closest('button').getAttribute('aria-label')", new[] { x, y });

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Item_ShouldLookDifferentFromAPressedOne_WhenItIsHovered(string browser)
    {
        var page = await OpenReadyAsync(browser);
        var pressed = await FillAsync(Item(page, "Layers", "Roads"));

        await Item(page, "Layers", "Rivers").HoverAsync();

        Assert.NotEqual(pressed, await FillAsync(Item(page, "Layers", "Rivers")));
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Item_ShouldShowItTurnedOff_WhenClickedWithThePointerStillOnIt(string browser)
    {
        var page = await OpenReadyAsync(browser);
        var item = Item(page, "Layers", "Roads");
        await item.HoverAsync();
        var pressed = await FillAsync(item);

        await item.ClickAsync();
        await Expect(item).ToHaveAttributeAsync("aria-pressed", "false");

        Assert.NotEqual(pressed, await FillAsync(item));
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Item_ShouldShowItsName_WhenAnIconOnlyItemIsHovered(string browser)
    {
        var page = await OpenReadyAsync(browser);

        await Item(page, "Layers", "Rivers").HoverAsync();

        await Expect(page.GetByRole(AriaRole.Tooltip)).ToHaveTextAsync("Rivers");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Item_ShouldShowItsName_WhenTheKeysMoveFocusToAnIconOnlyItem(string browser)
    {
        var page = await OpenReadyAsync(browser);
        await Item(page, "Layers", "Roads").FocusAsync();

        await page.Keyboard.PressAsync("ArrowRight");

        await Expect(Item(page, "Layers", "Rivers")).ToBeFocusedAsync();
        await Expect(page.GetByRole(AriaRole.Tooltip)).ToHaveTextAsync("Rivers");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Item_ShouldStayJoinedToItsNeighbors_WhenInARow(string browser)
    {
        var page = await OpenReadyAsync(browser);
        var first = (await Item(page, "Layers", "Roads").BoundingBoxAsync())!;
        var second = (await Item(page, "Layers", "Rivers").BoundingBoxAsync())!;

        Assert.Equal(first.X + first.Width - 1, second.X, 0.5f);
        await Expect(Item(page, "Layers", "Roads")).ToHaveCSSAsync("border-top-right-radius", "0px");
        await Expect(Item(page, "Layers", "Rivers")).ToHaveCSSAsync("border-top-left-radius", "0px");
        await Expect(Item(page, "Layers", "Rivers")).ToHaveCSSAsync("border-top-right-radius", "0px");
        await Expect(Item(page, "Layers", "Towns")).ToHaveCSSAsync("border-bottom-left-radius", "0px");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Item_ShouldStayJoinedToItsNeighbors_WhenInAColumn(string browser)
    {
        var page = await OpenReadyAsync(browser, "Side");
        var first = (await Item(page, "Side", "Top").BoundingBoxAsync())!;
        var second = (await Item(page, "Side", "Middle of the side").BoundingBoxAsync())!;

        Assert.Equal(first.Y + first.Height - 1, second.Y, 0.5f);
        Assert.Equal(first.Width, second.Width, 0.5f);
        await Expect(Item(page, "Side", "Top")).ToHaveCSSAsync("border-bottom-left-radius", "0px");
        await Expect(Item(page, "Side", "Middle of the side")).ToHaveCSSAsync("border-top-left-radius", "0px");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Item_ShouldLiftAPressedOneOverItsNeighbors_WhenJoined(string browser)
    {
        var page = await OpenReadyAsync(browser);
        var pressed = (await Item(page, "Layers", "Roads").BoundingBoxAsync())!;

        var onTop = await NameOnTopAsync(page, pressed.X + pressed.Width - 0.5, pressed.Y + pressed.Height / 2);

        Assert.Equal("Roads", onTop);
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Item_ShouldLiftAFocusedOneOverItsNeighbors_WhenJoined(string browser)
    {
        var page = await OpenReadyAsync(browser);
        var focused = Item(page, "Layers", "Rivers");
        await Item(page, "Layers", "Roads").FocusAsync();
        await page.Keyboard.PressAsync("ArrowRight");
        await Expect(focused).ToBeFocusedAsync();
        var box = (await focused.BoundingBoxAsync())!;

        var onTop = await NameOnTopAsync(page, box.X + box.Width - 0.5, box.Y + box.Height / 2);

        Assert.Equal("Rivers", onTop);
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task ToggleGroup_ShouldKeepATabStop_WhenTheFocusedItemTurnsOffWhileHidden(string browser)
    {
        var page = await OpenReadyAsync(browser, "Side");
        var middle = Item(page, "Side", "Middle of the side");
        await middle.FocusAsync();
        await Expect(middle).ToHaveAttributeAsync("tabindex", "0");

        await Group(page, "Side").EvaluateAsync("group => group.style.display = 'none'");
        await page.GetByRole(AriaRole.Button, new() { Name = "Turn middle off" }).ClickAsync();
        // A hidden group has no role to find it by.
        var group = page.Locator("[aria-label='Side']");
        await Expect(group.Locator("button", new() { HasText = "Middle of the side" })).ToBeDisabledAsync();
        await group.EvaluateAsync("group => group.style.display = ''");

        await Expect(Item(page, "Side", "Top")).ToHaveAttributeAsync("tabindex", "0");
        await Expect(Group(page, "Side").Locator("[tabindex='0']")).ToHaveCountAsync(1);
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task ThemePicker_ShouldShowAChoicesNameAndTellItFromThePressedOne_WhenHovered(string browser)
    {
        var page = await OpenReadyAsync(browser, "Theme");
        var picker = Group(page, "Theme");
        var pressed = picker.Locator("[aria-pressed=true]");
        await Expect(pressed).ToHaveCountAsync(1);
        var fill = await FillAsync(pressed);
        var other = picker.Locator("[aria-pressed=false]").First;

        await other.HoverAsync();

        Assert.NotEqual(fill, await FillAsync(other));
        await Expect(page.GetByRole(AriaRole.Tooltip))
            .ToHaveTextAsync((await other.GetAttributeAsync("aria-label"))!);
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task ThemePicker_ShouldShowTheNextChoicesName_WhenTheKeysMoveFocus(string browser)
    {
        var page = await OpenReadyAsync(browser, "Theme");
        var picker = Group(page, "Theme");
        var pressed = picker.Locator("[aria-pressed=true]");
        await Expect(pressed).ToHaveCountAsync(1);
        var start = (await pressed.GetAttributeAsync("aria-label"))!;
        await pressed.FocusAsync();

        await page.Keyboard.PressAsync("ArrowRight");

        var focused = picker.Locator("button:focus");
        await Expect(focused).Not.ToHaveAttributeAsync("aria-label", start);
        await Expect(page.GetByRole(AriaRole.Tooltip))
            .ToHaveTextAsync((await focused.GetAttributeAsync("aria-label"))!);
    }
}
