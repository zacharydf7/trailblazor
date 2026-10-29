using System.Text.RegularExpressions;
using Deque.AxeCore.Playwright;
using Box = Microsoft.Playwright.LocatorBoundingBoxResult;

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

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task SubMenu_ShouldOpenBesideItsItemWithTheFirstItemFocused_WhenItsItemIsClicked(string browser)
    {
        var page = await OpenAsync(browser, "/examples/menu");
        await page.GetByRole(AriaRole.Button, new() { Name = "More options" }).ClickAsync();

        await Item(page, "Share").ClickAsync();

        await Expect(Item(page, "Email")).ToBeFocusedAsync();
        await Expect(Item(page, "Share")).ToHaveAttributeAsync("aria-expanded", "true");
        await Expect(page.GetByRole(AriaRole.Menu, new() { Name = "More options" })).ToBeVisibleAsync();
        var itemBox = await Item(page, "Share").BoundingBoxAsync();
        var submenuBox = await page.GetByRole(AriaRole.Menu, new() { Name = "Share" }).BoundingBoxAsync();
        Assert.Equal(itemBox!.X + itemBox.Width, submenuBox!.X, tolerance: 1);
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task SubMenu_ShouldKeepTheKeysWithinEachList_WhenOpenedAndClosedWithTheArrowKeys(string browser)
    {
        var page = await OpenAsync(browser, "/examples/menu");
        var trigger = page.GetByRole(AriaRole.Button, new() { Name = "More options" });
        await Expect(trigger).ToHaveAttributeAsync("aria-expanded", "false");
        await trigger.FocusAsync();
        await page.Keyboard.PressAsync("ArrowUp");
        await Expect(Item(page, "Share")).ToBeFocusedAsync();

        await page.Keyboard.PressAsync("ArrowRight");
        await Expect(Item(page, "Email")).ToBeFocusedAsync();
        await page.Keyboard.PressAsync("ArrowUp");
        await Expect(Item(page, "Copy link")).ToBeFocusedAsync();
        await page.Keyboard.PressAsync("ArrowDown");
        await Expect(Item(page, "Email")).ToBeFocusedAsync();
        await page.Keyboard.PressAsync("ArrowLeft");

        await Expect(page.GetByRole(AriaRole.Menu, new() { Name = "Share" })).ToBeHiddenAsync();
        await Expect(Item(page, "Share")).ToBeFocusedAsync();
        await Expect(Item(page, "Share")).ToHaveAttributeAsync("aria-expanded", "false");
        await page.Keyboard.PressAsync("e");
        await Expect(Item(page, "Export")).ToBeFocusedAsync();
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task SubMenu_ShouldOpenWithTheLeftArrow_WhenThePageIsRightToLeft(string browser)
    {
        var page = await OpenAsync(browser, "/examples/menu");
        var trigger = page.GetByRole(AriaRole.Button, new() { Name = "More options" });
        await Expect(trigger).ToHaveAttributeAsync("aria-expanded", "false");
        await page.EvaluateAsync("() => document.documentElement.dir = 'rtl'");
        await trigger.FocusAsync();
        await page.Keyboard.PressAsync("ArrowUp");
        await Expect(Item(page, "Share")).ToBeFocusedAsync();

        await page.Keyboard.PressAsync("ArrowLeft");
        await Expect(Item(page, "Email")).ToBeFocusedAsync();
        await page.Keyboard.PressAsync("ArrowRight");

        await Expect(Item(page, "Share")).ToBeFocusedAsync();
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task SubMenu_ShouldCloseOneLevelAtATime_WhenEscapeIsPressed(string browser)
    {
        var page = await OpenAsync(browser, "/examples/menu");
        var trigger = page.GetByRole(AriaRole.Button, new() { Name = "More options" });
        await trigger.ClickAsync();
        await Item(page, "Share").ClickAsync();
        await Expect(Item(page, "Email")).ToBeFocusedAsync();

        await page.Keyboard.PressAsync("Escape");
        await Expect(page.GetByRole(AriaRole.Menu, new() { Name = "Share" })).ToBeHiddenAsync();
        await Expect(page.GetByRole(AriaRole.Menu, new() { Name = "More options" })).ToBeVisibleAsync();
        await Expect(Item(page, "Share")).ToBeFocusedAsync();
        await page.Keyboard.PressAsync("Escape");

        await Expect(page.GetByRole(AriaRole.Menu, new() { Name = "More options" })).ToBeHiddenAsync();
        await Expect(trigger).ToBeFocusedAsync();
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task SubMenu_ShouldRunTheItemAndCloseEveryMenu_WhenOneOfItsItemsIsChosen(string browser)
    {
        var page = await OpenAsync(browser, "/examples/menu");
        var trigger = page.GetByRole(AriaRole.Button, new() { Name = "More options" });
        await trigger.ClickAsync();
        await Item(page, "Share").ClickAsync();

        await Item(page, "Email").ClickAsync();

        await Expect(page.GetByTestId("last-action")).ToHaveTextAsync("Email");
        await Expect(page.GetByRole(AriaRole.Menu)).ToHaveCountAsync(0);
        await Expect(trigger).ToBeFocusedAsync();
        await Expect(trigger).ToHaveAttributeAsync("aria-expanded", "false");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task SubMenu_ShouldHaveNoAccessibilityViolations_WhenOpen(string browser)
    {
        var page = await OpenAsync(browser, "/examples/menu");
        await page.GetByRole(AriaRole.Button, new() { Name = "More options" }).ClickAsync();
        await Item(page, "Share").ClickAsync();
        await Expect(Item(page, "Email")).ToBeFocusedAsync();

        var result = await page.RunAxe(AxeTests.WcagAA);

        Assert.Empty(result.Violations.Select(violation =>
            $"{violation.Id}: {string.Join(", ", violation.Nodes.Select(node => node.Target))}"));
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Menu_ShouldOpenAboveLinedUpWithTheTriggersEnd_WhenPlacedOnTopAlignedToTheEnd(string browser)
    {
        var page = await OpenAsync(browser, "/menu-placement");

        var (trigger, menu) = await OpenPlacedAsync(page, "Above");

        Assert.True(menu.Y + menu.Height <= trigger.Y + 1, "The menu should open above its trigger.");
        Assert.Equal(trigger.X + trigger.Width, menu.X + menu.Width, tolerance: 1);
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Menu_ShouldOpenBesideCenteredOnTheTrigger_WhenPlacedAtTheEndAlignedToTheCenter(string browser)
    {
        var page = await OpenAsync(browser, "/menu-placement");

        var (trigger, menu) = await OpenPlacedAsync(page, "Beside");

        Assert.True(menu.X >= trigger.X + trigger.Width - 1, "The menu should open after its trigger.");
        Assert.Equal(trigger.Y + trigger.Height / 2, menu.Y + menu.Height / 2, tolerance: 1);
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Menu_ShouldStretchTheTriggerAndOpenAtLeastAsWide_WhenFullWidth(string browser)
    {
        var page = await OpenAsync(browser, "/menu-placement");

        var (trigger, menu) = await OpenPlacedAsync(page, "Wide");

        Assert.Equal(480, trigger.Width, tolerance: 1);
        Assert.True(menu.Width >= trigger.Width - 1, $"The menu is {menu.Width}px wide, under its trigger's 480px.");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Menu_ShouldKeepItsOwnMinimumWidth_WhenFullWidthInANarrowContainer(string browser)
    {
        var page = await OpenAsync(browser, "/menu-placement");

        var (trigger, menu) = await OpenPlacedAsync(page, "Narrow");

        Assert.Equal(128, trigger.Width, tolerance: 1);
        Assert.True(menu.Width >= 160, $"The menu is {menu.Width}px wide, under its own 10rem minimum.");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Menu_ShouldOpenAtItsOwnWidth_WhenFullWidthIsTurnedOffAfterItOpened(string browser)
    {
        var page = await OpenAsync(browser, "/menu-placement");
        await OpenPlacedAsync(page, "Wide");
        await page.Keyboard.PressAsync("Escape");

        await page.GetByRole(AriaRole.Button, new() { Name = "Fit the wide menu" }).ClickAsync();
        // The server's render has dropped Wide's class once only Narrow is still full width.
        await Expect(page.Locator(".tb-menu--full-width")).ToHaveCountAsync(1);
        await page.GetByRole(AriaRole.Button, new() { Name = "Wide", Exact = true }).ClickAsync();

        // The stylesheet's 10rem, rather than the 480px trigger's width left from the first open.
        var menu = page.GetByRole(AriaRole.Menu, new() { Name = "Wide", Exact = true });
        await Expect(menu).ToHaveCSSAsync("min-width", "160px");
    }

    // Opens the menu and measures it once the positioning script has placed it.
    private static async Task<(Box Trigger, Box Menu)> OpenPlacedAsync(IPage page, string label)
    {
        var trigger = page.GetByRole(AriaRole.Button, new() { Name = label, Exact = true });
        var menu = page.GetByRole(AriaRole.Menu, new() { Name = label, Exact = true });
        await trigger.ClickAsync();
        await Expect(menu).ToHaveAttributeAsync("style", new Regex("--tb-anchored-offset"));
        return ((await trigger.BoundingBoxAsync())!, (await menu.BoundingBoxAsync())!);
    }

    private static ILocator Item(IPage page, string name) =>
        page.GetByRole(AriaRole.Menuitem, new() { Name = name });
}
