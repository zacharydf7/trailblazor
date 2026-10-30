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
        // A forced click doesn't wait for the menu to settle, so it waits here for the script to place it.
        await OpenPlacedAsync(page, "Actions");

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
    public async Task Menu_ShouldStayAtLeastAsWideAsItsTrigger_WhenTheFullWidthTriggerGrowsWhileItIsOpen(string browser)
    {
        var page = await OpenAsync(browser, "/menu-placement");
        await page.EvaluateAsync(
            "() => { window.errors = []; addEventListener('error', event => errors.push(event.message)); }");
        await OpenPlacedAsync(page, "Wide");

        var trigger = page.GetByRole(AriaRole.Button, new() { Name = "Wide", Exact = true });
        var width = await trigger.EvaluateAsync<int>("""
            trigger => {
                trigger.closest('.tb-menu').parentElement.style.width = '40rem';
                return trigger.offsetWidth;
            }
            """);

        var menu = page.GetByRole(AriaRole.Menu, new() { Name = "Wide", Exact = true });
        await Expect(menu).ToHaveCSSAsync("min-width", $"{width}px");
        // Following the trigger mustn't make the browser report a ResizeObserver loop, which apps may log as an error.
        await page.EvaluateAsync(
            "() => new Promise(resolve => requestAnimationFrame(() => requestAnimationFrame(resolve)))");
        Assert.Empty(await page.EvaluateAsync<string[]>("() => window.errors"));
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

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Menu_ShouldOpenFromACustomTriggerWithTheKeyboard_WhenGivenATrigger(string browser)
    {
        var page = await OpenAsync(browser, "/examples/menu");
        var account = page.GetByRole(AriaRole.Button, new() { Name = "RB account" });
        await Expect(account).ToHaveAttributeAsync("aria-expanded", "false");
        await account.FocusAsync();

        await page.Keyboard.PressAsync("ArrowDown");

        await Expect(page.GetByRole(AriaRole.Menu, new() { Name = "RB account" })).ToBeVisibleAsync();
        await Expect(Item(page, "Profile")).ToBeFocusedAsync();
        await page.Keyboard.PressAsync("Enter");
        await Expect(page.GetByTestId("last-action")).ToHaveTextAsync("Profile");
        await Expect(account).ToBeFocusedAsync();
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Menu_ShouldNotOpen_WhenDisabled(string browser)
    {
        var page = await OpenAsync(browser, "/menu");

        foreach (var name in new[] { "Locked", "Custom locked" })
        {
            var trigger = page.GetByRole(AriaRole.Button, new() { Name = name, Exact = true });
            await Expect(trigger).ToBeDisabledAsync();
            await trigger.ClickAsync(new() { Force = true });
        }

        await Expect(page.GetByRole(AriaRole.Menu)).ToHaveCountAsync(0);
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task CheckboxItem_ShouldToggleAndCloseTheMenu_WhenChosen(string browser)
    {
        var page = await OpenAsync(browser, "/examples/menu");
        var view = page.GetByRole(AriaRole.Button, new() { Name = "View" });
        var map = page.GetByRole(AriaRole.Menuitemcheckbox, new() { Name = "Map" });
        var elevation = page.GetByRole(AriaRole.Menuitemcheckbox, new() { Name = "Elevation" });
        await view.ClickAsync();
        await Expect(map).ToBeCheckedAsync();
        await Expect(map).ToBeFocusedAsync();

        await map.ClickAsync();
        await Expect(page.GetByRole(AriaRole.Menu)).ToBeHiddenAsync();
        await view.ClickAsync();
        await Expect(map).ToBeFocusedAsync();
        await page.Keyboard.PressAsync("ArrowDown");
        await Expect(elevation).ToBeFocusedAsync();
        await page.Keyboard.PressAsync(" ");

        await Expect(page.GetByTestId("view")).ToHaveTextAsync("Miles, elevation");
        await view.ClickAsync();
        await Expect(map).Not.ToBeCheckedAsync();
        await Expect(elevation).ToBeCheckedAsync();
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task RadioItem_ShouldMoveTheChoiceWithinItsNamedGroup_WhenChosen(string browser)
    {
        var page = await OpenAsync(browser, "/examples/menu");
        var view = page.GetByRole(AriaRole.Button, new() { Name = "View" });
        await view.ClickAsync();
        var units = page.GetByRole(AriaRole.Group, new() { Name = "Units" });
        await Expect(units.GetByRole(AriaRole.Menuitemradio, new() { Name = "Miles" })).ToBeCheckedAsync();
        await Expect(page.GetByRole(AriaRole.Menu, new() { Name = "View" }).GetByRole(AriaRole.Separator))
            .ToHaveCountAsync(2);

        await units.GetByRole(AriaRole.Menuitemradio, new() { Name = "Kilometers" }).ClickAsync();

        await Expect(page.GetByTestId("view")).ToHaveTextAsync("Kilometers, map");
        await view.ClickAsync();
        await Expect(units.GetByRole(AriaRole.Menuitemradio, new() { Name = "Kilometers" })).ToBeCheckedAsync();
        await Expect(units.GetByRole(AriaRole.Menuitemradio, new() { Name = "Miles" })).Not.ToBeCheckedAsync();
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Menu_ShouldHaveNoAccessibilityViolations_WhenItHoldsGroupsCheckboxesAndRadios(string browser)
    {
        var page = await OpenAsync(browser, "/examples/menu");
        await page.GetByRole(AriaRole.Button, new() { Name = "View" }).ClickAsync();
        await Expect(page.GetByRole(AriaRole.Menu, new() { Name = "View" })).ToBeVisibleAsync();

        var results = await page.Locator(".tb-menu__list:popover-open").RunAxe();

        Assert.Empty(results.Violations);
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task LinkItem_ShouldFollowItsAddress_WhenChosenWithEnterOrSpace(string browser)
    {
        var page = await OpenAsync(browser, "/menu");
        var go = page.GetByRole(AriaRole.Button, new() { Name = "Go" });

        foreach (var key in new[] { "Enter", " " })
        {
            await page.EvaluateAsync("() => history.replaceState(null, '', location.pathname)");
            await go.ClickAsync();
            await Expect(Item(page, "Follow")).ToBeFocusedAsync();
            await page.Keyboard.PressAsync(key);

            await Expect(page).ToHaveURLAsync(new Regex("/menu#followed$"));
            await Expect(page.GetByRole(AriaRole.Menu)).ToBeHiddenAsync();
        }
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task LinkItem_ShouldOpenANewTabAndSaySo_WhenNewTabIsSet(string browser)
    {
        var page = await OpenAsync(browser, "/menu");
        await page.GetByRole(AriaRole.Button, new() { Name = "Go" }).ClickAsync();
        var again = Item(page, "Again (opens in a new tab)");
        await Expect(again).ToHaveAttributeAsync("target", "_blank");
        Assert.False(await Item(page, "Nowhere").EvaluateAsync<bool>("item => item.hasAttribute('href')"));

        var popup = await page.RunAndWaitForPopupAsync(() => again.ClickAsync());

        await Expect(popup).ToHaveURLAsync(new Regex("/menu$"));
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task SubMenu_ShouldOpenWithoutTakingFocus_WhenThePointerRestsOnItsItem(string browser)
    {
        var page = await OpenAsync(browser, "/menu");
        await page.GetByRole(AriaRole.Button, new() { Name = "Insert" }).ClickAsync();
        await Expect(Item(page, "Shape")).ToBeFocusedAsync();
        await Item(page, "Table").HoverAsync();

        await Item(page, "Chart").HoverAsync();

        await Expect(page.GetByRole(AriaRole.Menu, new() { Name = "Chart" })).ToBeVisibleAsync();
        await Expect(Item(page, "Chart")).ToHaveAttributeAsync("aria-expanded", "true");
        await Expect(Item(page, "Bar")).Not.ToBeFocusedAsync();
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task SubMenu_ShouldOpenOnHoverAgain_WhenTheMenuIsReopenedOverTheSameItem(string browser)
    {
        var page = await OpenAsync(browser, "/menu");
        var insert = page.GetByRole(AriaRole.Button, new() { Name = "Insert" });
        var chart = page.GetByRole(AriaRole.Menu, new() { Name = "Chart" });
        await insert.ClickAsync();
        await Item(page, "Chart").HoverAsync();
        await Expect(chart).ToBeVisibleAsync();
        await page.Keyboard.PressAsync("Escape");
        await page.Keyboard.PressAsync("Escape");
        await Expect(page.GetByRole(AriaRole.Menu)).ToHaveCountAsync(0);

        await insert.ClickAsync();
        await Item(page, "Chart").HoverAsync();

        await Expect(chart).ToBeVisibleAsync();
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task SubMenu_ShouldClose_WhenThePointerRestsOnAnotherItem(string browser)
    {
        var page = await OpenAsync(browser, "/menu");
        await page.GetByRole(AriaRole.Button, new() { Name = "Insert" }).ClickAsync();
        await Item(page, "Chart").HoverAsync();
        await Expect(page.GetByRole(AriaRole.Menu, new() { Name = "Chart" })).ToBeVisibleAsync();

        await Item(page, "Image").HoverAsync();

        await Expect(page.GetByRole(AriaRole.Menu, new() { Name = "Chart" })).ToBeHiddenAsync();
        await Expect(page.GetByRole(AriaRole.Menu, new() { Name = "Insert" })).ToBeVisibleAsync();
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task SubMenu_ShouldStayOpen_WhenThePointerPausesOnAnotherItemOnItsWayToTheSubmenu(string browser)
    {
        var page = await OpenAsync(browser, "/menu");
        await page.GetByRole(AriaRole.Button, new() { Name = "Insert" }).ClickAsync();
        var shape = page.GetByRole(AriaRole.Menu, new() { Name = "Shape" });
        await Item(page, "Shape").HoverAsync();
        await Expect(shape).ToBeVisibleAsync();

        // From the item's middle towards the submenu's fourth item, pausing on Table, longer than the hover delay.
        var from = (await Item(page, "Shape").BoundingBoxAsync())!;
        var table = (await Item(page, "Table").BoundingBoxAsync())!;
        var star = (await Item(page, "Star").BoundingBoxAsync())!;
        var start = (X: from.X + from.Width / 2, Y: from.Y + from.Height / 2);
        var end = (X: star.X + 12, Y: star.Y + star.Height / 2);
        var pauseY = table.Y + table.Height / 2;
        var pauseX = start.X + (end.X - start.X) * (pauseY - start.Y) / (end.Y - start.Y);
        Assert.True(pauseX < table.X + table.Width, "The pause should be over Table.");

        var open = await MovePointerAsync(page, (start.X, start.Y, 0), (pauseX, pauseY, 250), (end.X, end.Y, 250));

        Assert.Contains("Shape", open);
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task SubMenu_ShouldCloseAfterTheHoverDelay_WhenThePointerHeadsAwayFromIt(string browser)
    {
        var page = await OpenAsync(browser, "/menu");
        await page.GetByRole(AriaRole.Button, new() { Name = "Insert" }).ClickAsync();
        await Item(page, "Shape").HoverAsync();
        await Expect(page.GetByRole(AriaRole.Menu, new() { Name = "Shape" })).ToBeVisibleAsync();
        var from = (await Item(page, "Shape").BoundingBoxAsync())!;
        var table = (await Item(page, "Table").BoundingBoxAsync())!;

        // Down and back towards the start, outside the way to the submenu, resting longer than the hover delay but
        // not the grace time.
        var open = await MovePointerAsync(page, (from.X + from.Width / 2, from.Y + from.Height / 2, 0),
            (table.X + 8, table.Y + table.Height / 2, 300));

        Assert.DoesNotContain("Shape", open);
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task SubMenu_ShouldNotOpen_WhenThePointerCrossesItsItemAndLeavesTheMenu(string browser)
    {
        var page = await OpenAsync(browser, "/menu");
        await page.GetByRole(AriaRole.Button, new() { Name = "Insert" }).ClickAsync();
        var chart = (await Item(page, "Chart").BoundingBoxAsync())!;
        await Item(page, "Image").HoverAsync();

        // Down across Chart and out of the menu below it, well within the hover delay.
        await page.Mouse.MoveAsync(chart.X + 8, chart.Y + chart.Height + 60, new() { Steps = 4 });
        await page.WaitForTimeoutAsync(400);

        await Expect(page.GetByRole(AriaRole.Menu, new() { Name = "Chart" })).ToBeHiddenAsync();
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task SubMenu_ShouldStayClosed_WhenEscapeClosesItBeforeTheHoverDelayEnds(string browser)
    {
        var page = await OpenAsync(browser, "/menu");
        await page.GetByRole(AriaRole.Button, new() { Name = "Insert" }).ClickAsync();
        await Expect(Item(page, "Shape")).ToBeFocusedAsync();

        // The click arrives with the pointer, so its hover is still waiting when Escape closes the submenu.
        await Item(page, "Chart").ClickAsync();
        await page.Keyboard.PressAsync("Escape");
        await page.WaitForTimeoutAsync(400);

        await Expect(page.GetByRole(AriaRole.Menu, new() { Name = "Chart" })).ToBeHiddenAsync();
        await Expect(Item(page, "Chart")).ToBeFocusedAsync();
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task SubMenu_ShouldGoIntoTheSubmenu_WhenItsItemIsClickedAfterThePointerOpenedIt(string browser)
    {
        var page = await OpenAsync(browser, "/menu");
        await page.GetByRole(AriaRole.Button, new() { Name = "Insert" }).ClickAsync();
        await Item(page, "Chart").HoverAsync();
        await Expect(page.GetByRole(AriaRole.Menu, new() { Name = "Chart" })).ToBeVisibleAsync();

        await Item(page, "Chart").ClickAsync();

        await Expect(page.GetByRole(AriaRole.Menu, new() { Name = "Chart" })).ToBeVisibleAsync();
        await Expect(Item(page, "Bar")).ToBeFocusedAsync();
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

    // Waits on the page's own timers, which fire in order with the menu's however slow the machine is.
    private static Task<string[]> MovePointerAsync(IPage page, params (double X, double Y, int Wait)[] steps) =>
        page.EvaluateAsync<string[]>("""
            async steps => {
                for (const [x, y, wait] of steps) {
                    document.elementFromPoint(x, y).dispatchEvent(new PointerEvent('pointermove',
                        { bubbles: true, clientX: x, clientY: y, pointerType: 'mouse' }));
                    await new Promise(resolve => setTimeout(resolve, wait));
                }

                return [...document.querySelectorAll('[role=menu]:popover-open')]
                    .map(menu => document.getElementById(menu.getAttribute('aria-labelledby')).textContent.trim());
            }
            """, steps.Select(step => new[] { step.X, step.Y, step.Wait }).ToArray());

    private static ILocator Item(IPage page, string name) =>
        page.GetByRole(AriaRole.Menuitem, new() { Name = name });
}
