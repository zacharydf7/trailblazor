namespace Trailblazor.BrowserTests;

public class MenuTests(BrowserFixture fixture) : BrowserTest(fixture)
{
    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Menu_ShouldOpenWithTheFirstItemFocused_WhenTheTriggerIsClicked(string browser)
    {
        var page = await OpenAsync(browser, "/examples/menu");
        var trigger = page.GetByRole(AriaRole.Button, new() { Name = "Actions" });

        await trigger.ClickAsync();

        await Expect(page.GetByRole(AriaRole.Menu, new() { Name = "Actions" })).ToBeVisibleAsync();
        await Expect(trigger).ToHaveAttributeAsync("aria-expanded", "true");
        await Expect(page.GetByRole(AriaRole.Menuitem, new() { Name = "Duplicate" })).ToBeFocusedAsync();
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Menu_ShouldMoveFocusWithTheKeyboard_WhenOpen(string browser)
    {
        var page = await OpenAsync(browser, "/examples/menu");
        var trigger = page.GetByRole(AriaRole.Button, new() { Name = "Actions" });
        // The arrow keys work once the menu script has attached, which is when it sets aria-expanded.
        await Expect(trigger).ToHaveAttributeAsync("aria-expanded", "false");
        await trigger.FocusAsync();

        await page.Keyboard.PressAsync("ArrowUp");
        await Expect(Item(page, "Delete")).ToBeFocusedAsync();
        await page.Keyboard.PressAsync("ArrowDown");
        await Expect(Item(page, "Duplicate")).ToBeFocusedAsync();
        await page.Keyboard.PressAsync("End");
        await Expect(Item(page, "Delete")).ToBeFocusedAsync();
        await page.Keyboard.PressAsync("Home");
        await Expect(Item(page, "Duplicate")).ToBeFocusedAsync();
        await page.Keyboard.PressAsync("r");

        await Expect(Item(page, "Rename")).ToBeFocusedAsync();
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Menu_ShouldRunTheItemCloseAndRefocusTheTrigger_WhenAnItemIsChosenWithEnter(string browser)
    {
        var page = await OpenAsync(browser, "/examples/menu");
        var trigger = page.GetByRole(AriaRole.Button, new() { Name = "Actions" });
        await trigger.ClickAsync();
        await Expect(Item(page, "Duplicate")).ToBeFocusedAsync();

        await page.Keyboard.PressAsync("ArrowDown");
        await page.Keyboard.PressAsync("Enter");

        await Expect(page.GetByTestId("last-action")).ToHaveTextAsync("Rename");
        await Expect(page.GetByRole(AriaRole.Menu)).ToBeHiddenAsync();
        await Expect(trigger).ToBeFocusedAsync();
        await Expect(trigger).ToHaveAttributeAsync("aria-expanded", "false");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Menu_ShouldStayOpenAndDoNothing_WhenADisabledItemIsChosen(string browser)
    {
        var page = await OpenAsync(browser, "/examples/menu");
        await page.GetByRole(AriaRole.Button, new() { Name = "Actions" }).ClickAsync();

        // Playwright treats aria-disabled as disabled and would wait forever without Force.
        await Item(page, "Archive").ClickAsync(new() { Force = true });

        await Expect(page.GetByRole(AriaRole.Menu, new() { Name = "Actions" })).ToBeVisibleAsync();
        await Expect(page.GetByTestId("last-action")).ToBeEmptyAsync();
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Menu_ShouldFocusADisabledItemButNotChooseIt_WhenEnterOrSpaceIsPressed(string browser)
    {
        var page = await OpenAsync(browser, "/examples/menu");
        var trigger = page.GetByRole(AriaRole.Button, new() { Name = "Actions" });
        await Expect(trigger).ToHaveAttributeAsync("aria-expanded", "false");
        await trigger.FocusAsync();

        await page.Keyboard.PressAsync("ArrowDown");
        await Expect(Item(page, "Duplicate")).ToBeFocusedAsync();
        await page.Keyboard.PressAsync("ArrowDown");
        await page.Keyboard.PressAsync("ArrowDown");
        await Expect(Item(page, "Archive")).ToBeFocusedAsync();
        await page.Keyboard.PressAsync("Enter");
        // Checked between the keys: a close on Enter would refocus the trigger, and Space would then reopen it.
        await Expect(Item(page, "Archive")).ToBeFocusedAsync();
        await page.Keyboard.PressAsync("Space");

        await Expect(page.GetByRole(AriaRole.Menu, new() { Name = "Actions" })).ToBeVisibleAsync();
        await Expect(page.GetByTestId("last-action")).ToBeEmptyAsync();
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Menu_ShouldChooseTheItem_WhenSpaceIsPressed(string browser)
    {
        var page = await OpenAsync(browser, "/examples/menu");
        await page.GetByRole(AriaRole.Button, new() { Name = "Actions" }).ClickAsync();
        await Expect(Item(page, "Duplicate")).ToBeFocusedAsync();

        await page.Keyboard.PressAsync("Space");

        await Expect(page.GetByTestId("last-action")).ToHaveTextAsync("Duplicate");
        await Expect(page.GetByRole(AriaRole.Menu)).ToBeHiddenAsync();
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Menu_ShouldCloseAndRefocusTheTrigger_WhenEscapeIsPressed(string browser)
    {
        var page = await OpenAsync(browser, "/examples/menu");
        var trigger = page.GetByRole(AriaRole.Button, new() { Name = "Actions" });
        await trigger.ClickAsync();
        await Expect(Item(page, "Duplicate")).ToBeFocusedAsync();

        await page.Keyboard.PressAsync("Escape");

        await Expect(page.GetByRole(AriaRole.Menu)).ToBeHiddenAsync();
        await Expect(trigger).ToBeFocusedAsync();
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Menu_ShouldCloseAndMoveOn_WhenTabIsPressed(string browser)
    {
        var page = await OpenAsync(browser, "/examples/menu");
        await page.GetByRole(AriaRole.Button, new() { Name = "Actions" }).ClickAsync();
        await Expect(Item(page, "Duplicate")).ToBeFocusedAsync();

        await page.Keyboard.PressAsync("Tab");

        await Expect(page.GetByRole(AriaRole.Menu)).ToBeHiddenAsync();
        await Expect(page.GetByRole(AriaRole.Button, new() { Name = "More options" })).ToBeFocusedAsync();
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Menu_ShouldOpenBelowTheTrigger_WhenThereIsRoom(string browser)
    {
        var page = await OpenAsync(browser, "/examples/menu");
        var trigger = page.GetByRole(AriaRole.Button, new() { Name = "Actions" });

        await trigger.ClickAsync();

        var menu = page.GetByRole(AriaRole.Menu, new() { Name = "Actions" });
        await Expect(menu).ToBeVisibleAsync();
        var triggerBox = await trigger.BoundingBoxAsync();
        var menuBox = await menu.BoundingBoxAsync();
        Assert.True(menuBox!.Y >= triggerBox!.Y + triggerBox.Height - 1, "The menu should open below its trigger.");
        Assert.Equal(triggerBox.X, menuBox.X, tolerance: 1);
    }

    private static ILocator Item(IPage page, string name) =>
        page.GetByRole(AriaRole.Menuitem, new() { Name = name });
}
