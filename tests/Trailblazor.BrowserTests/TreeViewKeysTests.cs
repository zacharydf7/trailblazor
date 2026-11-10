using Trailblazor.Testing.Playwright;

namespace Trailblazor.BrowserTests;

// Keyboard tests and shared helpers for TreeViewTests; the slow-link class reruns only these.
public abstract class TreeViewKeysTests(BrowserFixture fixture) : BrowserTest(fixture)
{
    protected static ILocator Tree(IPage page, string name = "Places") =>
        page.GetByRole(AriaRole.Tree, new() { Name = name });

    // A row's name includes its end slot, so rows are found by their own text.
    protected static ILocator Row(IPage page, string text, string tree = "Places") =>
        Tree(page, tree).Locator($"[role=treeitem][data-text='{text}']");

    protected async Task<IPage> OpenTreeAsync(string browser)
    {
        var page = await OpenAsync(browser, "/tree-view");
        await Expect(Tree(page)).ToHaveAttributeAsync("data-ready", "");
        return page;
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Tree_ShouldSelectEachRowTheArrowsReachButNotWithCtrl_WhenSelectionFollowsFocus(string browser)
    {
        var page = await OpenAsync(browser, "/tree-view");
        var tree = page.GetByRole(AriaRole.Tree, new() { Name = "Outline" });
        var north = tree.GetByRole(AriaRole.Treeitem, new() { Name = "North", Exact = true });
        await Expect(tree).ToHaveAttributeAsync("data-ready", "");
        await north.ClickAsync();
        await Expect(page.GetByTestId("outline")).ToHaveTextAsync("1");

        await page.Keyboard.PressAsync("ArrowDown");
        await Expect(page.GetByTestId("outline")).ToHaveTextAsync("4");
        await page.Keyboard.PressAsync("Control+ArrowUp");

        await Expect(north).ToBeFocusedAsync();
        await Expect(page.GetByTestId("outline")).ToHaveTextAsync("4");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Tree_ShouldLeaveALetterToThePage_WhenTypeAheadIsOff(string browser)
    {
        var page = await OpenAsync(browser, "/tree-view");
        var tree = page.GetByRole(AriaRole.Tree, new() { Name = "Outline" });
        var north = tree.GetByRole(AriaRole.Treeitem, new() { Name = "North", Exact = true });
        await Expect(tree).ToHaveAttributeAsync("data-ready", "");
        await north.ClickAsync();
        await page.EvaluateAsync(
            "() => document.addEventListener('keydown', event => window.taken = event.defaultPrevented, { once: true })");

        await page.Keyboard.PressAsync("s");

        Assert.False(await page.EvaluateAsync<bool>("() => window.taken"));
        await Expect(north).ToBeFocusedAsync();
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Keys_ShouldMoveOpenAndCloseLikeAWaiAriaTree_WhenPressed(string browser)
    {
        var page = await OpenTreeAsync(browser);
        await Row(page, "North").FocusAsync();

        await page.Keyboard.PressAsync("ArrowDown");
        await Expect(Row(page, "South")).ToBeFocusedAsync();
        await page.Keyboard.PressAsync("ArrowUp");
        await page.Keyboard.PressAsync("ArrowRight");
        await Expect(Row(page, "North")).ToHaveAttributeAsync("aria-expanded", "true");
        await page.Keyboard.PressAsync("ArrowRight");
        await Expect(Row(page, "Ridge")).ToBeFocusedAsync();
        await page.Keyboard.PressAsync("ArrowLeft");
        await Expect(Row(page, "North")).ToBeFocusedAsync();
        await page.Keyboard.PressAsync("ArrowLeft");
        await Expect(Row(page, "North")).ToHaveAttributeAsync("aria-expanded", "false");
        await page.Keyboard.PressAsync("End");
        await Expect(Row(page, "South")).ToBeFocusedAsync();
        await page.Keyboard.PressAsync("Home");
        await Expect(Row(page, "North")).ToBeFocusedAsync();
        await page.Keyboard.PressAsync("s");
        await Expect(Row(page, "South")).ToBeFocusedAsync();

        await page.Keyboard.PressAsync("Enter");

        await Expect(page.GetByTestId("activated")).ToHaveTextAsync("South");
        await Expect(page.GetByTestId("selected")).ToHaveTextAsync("4");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Tree_ShouldBeOneTabStopThatRemembersTheRow_WhenTabbedThrough(string browser)
    {
        var page = await OpenTreeAsync(browser);
        await Row(page, "North").FocusAsync();
        await page.Keyboard.PressAsync("ArrowDown");
        await Expect(Row(page, "South")).ToBeFocusedAsync();

        await page.GetByRole(AriaRole.Button, new() { Name = "Before" }).FocusAsync();
        await page.Keyboard.PressAsync("Tab");

        await Expect(Row(page, "South")).ToBeFocusedAsync();
        await Expect(Tree(page).Locator("[role=treeitem][tabindex='0']")).ToHaveCountAsync(1);
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Tree_ShouldMakeAClickedRowTheTabStop_WhenTheArrowsMovedFocusFirst(string browser)
    {
        var page = await OpenTreeAsync(browser);
        await Row(page, "North").FocusAsync();
        await page.Keyboard.PressAsync("ArrowDown");
        await Expect(Row(page, "South")).ToBeFocusedAsync();

        await Row(page, "North").ClickAsync();
        await page.GetByRole(AriaRole.Button, new() { Name = "Before" }).FocusAsync();
        await page.Keyboard.PressAsync("Tab");

        await Expect(Row(page, "North")).ToBeFocusedAsync();
        await Expect(Tree(page).Locator("[role=treeitem][tabindex='0']")).ToHaveCountAsync(1);
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task ShiftArrows_ShouldExtendTheSelection_WhenTheTreeTakesSeveral(string browser)
    {
        var page = await OpenTreeAsync(browser);
        await Row(page, "North", "Gear").ClickAsync(new() { Modifiers = [KeyboardModifier.Shift] });
        await Row(page, "North", "Gear").ClickAsync();

        await page.Keyboard.PressAsync("Shift+ArrowDown");

        await Expect(page.GetByTestId("gear")).ToHaveTextAsync("1, 4");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task ShiftDown_ShouldSelectTheRowItLeftToo_WhenNothingIsChosenYet(string browser)
    {
        var page = await OpenTreeAsync(browser);
        await Row(page, "North", "Gear").FocusAsync();

        await page.Keyboard.PressAsync("Shift+ArrowDown");

        await Expect(page.GetByTestId("gear")).ToHaveTextAsync("1, 4");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task ShiftHomeAndEnd_ShouldExtendTheSelectionToTheFirstAndLastRows_WhenPressed(string browser)
    {
        var page = await OpenTreeAsync(browser);
        await page.Tb().Tree("Gear").ExpandAsync("North");
        await Row(page, "Ridge", "Gear").ClickAsync();

        await page.Keyboard.PressAsync("Shift+End");
        await Expect(page.GetByTestId("gear")).ToHaveTextAsync("2, 4, 6");
        await Expect(Row(page, "South", "Gear")).ToBeFocusedAsync();
        await page.Keyboard.PressAsync("Shift+Home");

        await Expect(page.GetByTestId("gear")).ToHaveTextAsync("1, 2");
        await Expect(Row(page, "North", "Gear")).ToBeFocusedAsync();
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task ShiftEnd_ShouldOnlyMoveFocus_WhenTheTreeSelectsOneRow(string browser)
    {
        var page = await OpenTreeAsync(browser);
        await Row(page, "North").FocusAsync();

        await page.Keyboard.PressAsync("Shift+End");
        await Expect(Row(page, "South")).ToBeFocusedAsync();

        // Opening a row goes to the server after the Shift+End, so any selection it made has arrived by then.
        await page.Keyboard.PressAsync("ArrowRight");
        await Expect(Row(page, "Lake")).ToBeVisibleAsync();
        await Expect(page.GetByTestId("selected")).ToHaveTextAsync("");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Check_ShouldFillWithThePrimaryColor_WhenItsRowIsCheckedOrMixed(string browser)
    {
        var page = await OpenTreeAsync(browser);
        await page.Tb().Tree("Gear").ExpandAsync("North");
        await Row(page, "Pass", "Gear").FocusAsync();
        await page.Keyboard.PressAsync("Space");
        await Expect(page.GetByTestId("checked")).ToHaveTextAsync("6");

        foreach (var (row, state) in new[] { ("Pass", "true"), ("North", "mixed") })
        {
            await Expect(Row(page, row, "Gear")).ToHaveAttributeAsync("aria-checked", state);
            var colors = await Row(page, row, "Gear").Locator(".tb-tree__check").EvaluateAsync<string[]>("""
                check => {
                    const probe = document.createElement('span');
                    probe.style.color = 'var(--tb-color-primary)';
                    document.body.append(probe);
                    return [getComputedStyle(check).backgroundColor, getComputedStyle(probe).color];
                }
                """);

            Assert.Equal(colors[1], colors[0]);
        }
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Space_ShouldCheckTheRowAndMarkItsParentMixed_WhenTheTreeIsCheckable(string browser)
    {
        var page = await OpenTreeAsync(browser);
        await page.Tb().Tree("Gear").ExpandAsync("North");

        await Row(page, "Pass", "Gear").FocusAsync();
        await page.Keyboard.PressAsync("Space");

        await Expect(page.GetByTestId("checked")).ToHaveTextAsync("6");
        await Expect(Row(page, "North", "Gear")).ToHaveAttributeAsync("aria-checked", "mixed");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Keyboard_ShouldReachAFailedRowRetryItAndFollowTheBranch_WhenALazyLoadFails(string browser)
    {
        var page = await OpenTreeAsync(browser);
        var tree = Tree(page, "Flaky");
        await Expect(tree).ToHaveAttributeAsync("data-ready", "");
        await Row(page, "Outpost", "Flaky").FocusAsync();

        await page.Keyboard.PressAsync("ArrowRight");
        var failed = tree.Locator("[data-part=failed]");
        await Expect(failed).ToBeVisibleAsync();
        var retry = failed.GetByRole(AriaRole.Button, new() { Name = "Retry" });
        await Expect(retry).ToHaveAttributeAsync("tabindex", "-1");
        await page.Keyboard.PressAsync("ArrowDown");
        await Expect(failed).ToBeFocusedAsync();
        await Expect(retry).ToHaveAttributeAsync("tabindex", "0");
        await page.Keyboard.PressAsync("Enter");
        await Expect(tree.Locator("[data-part=loading]")).ToBeFocusedAsync();

        await Expect(Row(page, "Cabin", "Flaky")).ToBeFocusedAsync();
        await Expect(page.Locator("[data-part=announcer] [role=status] p").First)
            .ToHaveTextAsync("Couldn't load these items.");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Keyboard_ShouldKeepATabStopInTheTree_WhenFocusLeavesALoadingRowBeforeItGoes(string browser)
    {
        var page = await OpenTreeAsync(browser);
        var tree = Tree(page, "Flaky");
        await Expect(tree).ToHaveAttributeAsync("data-ready", "");
        await Row(page, "Outpost", "Flaky").FocusAsync();
        await page.Keyboard.PressAsync("ArrowRight");
        await Expect(tree.Locator("[data-part=failed]")).ToBeVisibleAsync();
        await page.Keyboard.PressAsync("ArrowDown");
        await page.Keyboard.PressAsync("Enter");
        await Expect(tree.Locator("[data-part=loading]")).ToBeFocusedAsync();

        await page.GetByRole(AriaRole.Button, new() { Name = "Before" }).FocusAsync();

        await Expect(Row(page, "Cabin", "Flaky")).ToHaveAttributeAsync("tabindex", "0");
        await Expect(page.GetByRole(AriaRole.Button, new() { Name = "Before" })).ToBeFocusedAsync();
    }
}
