using Deque.AxeCore.Playwright;
using Trailblazor.Testing.Playwright;

namespace Trailblazor.BrowserTests;

public class TreeViewTests(BrowserFixture fixture) : BrowserTest(fixture)
{
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

    private static ILocator Tree(IPage page, string name = "Places") =>
        page.GetByRole(AriaRole.Tree, new() { Name = name });

    // A row's name includes its end slot, so rows are found by their own text.
    private static ILocator Row(IPage page, string text, string tree = "Places") =>
        Tree(page, tree).Locator($"[role=treeitem][data-text='{text}']");

    private async Task<IPage> OpenTreeAsync(string browser)
    {
        var page = await OpenAsync(browser, "/tree-view");
        await Expect(Tree(page)).ToHaveAttributeAsync("data-ready", "");
        return page;
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
    public async Task Mouse_ShouldSelectOnClickAndActivateOnDoubleClick_WhenUsed(string browser)
    {
        var page = await OpenTreeAsync(browser);

        await Row(page, "North").Locator("[data-part=toggle]").ClickAsync();
        await Expect(Row(page, "North")).ToHaveAttributeAsync("aria-expanded", "true");
        await Expect(page.GetByTestId("selected")).ToBeEmptyAsync();
        await Row(page, "Pass").ClickAsync();
        await Expect(page.GetByTestId("selected")).ToHaveTextAsync("6");
        await Row(page, "Ridge").DblClickAsync();

        await Expect(page.GetByTestId("activated")).ToHaveTextAsync("Ridge");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Row_ShouldOpenAndCloseAndBeActivated_WhenARowWithChildrenIsDoubleClicked(string browser)
    {
        var page = await OpenTreeAsync(browser);

        await Row(page, "North").DblClickAsync();
        await Expect(Row(page, "North")).ToHaveAttributeAsync("aria-expanded", "true");
        await Expect(page.GetByTestId("activated")).ToHaveTextAsync("North");
        await Row(page, "North").DblClickAsync();

        await Expect(Row(page, "North")).ToHaveAttributeAsync("aria-expanded", "false");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task EndSlot_ShouldKeepItsClicksFromTheRow_WhenAButtonInItIsPressed(string browser)
    {
        var page = await OpenTreeAsync(browser);

        await Row(page, "South").GetByRole(AriaRole.Button, new() { Name = "Info" }).ClickAsync();

        await Expect(page.GetByTestId("info")).ToHaveTextAsync("South");
        await Expect(page.GetByTestId("selected")).ToBeEmptyAsync();
        await Expect(Row(page, "South")).ToHaveAttributeAsync("aria-selected", "false");
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
    public async Task Expand_ShouldShowLoadingThenTheChildren_WhenTheyLoadLazily(string browser)
    {
        var page = await OpenTreeAsync(browser);

        await Row(page, "Archive", "Remote").Locator("[data-part=toggle]").ClickAsync();

        await Expect(Tree(page, "Remote").Locator("[data-part=loading]")).ToBeVisibleAsync();
        await Expect(Row(page, "2026", "Remote")).ToBeVisibleAsync();
        await Expect(Tree(page, "Remote").Locator("[data-part=loading]")).ToHaveCountAsync(0);
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

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Helper_ShouldExpandSelectAndCheckByPath_WhenGivenTheRowsTexts(string browser)
    {
        var page = await OpenAsync(browser, "/tree-view");

        await page.Tb().Tree("Places").SelectAsync("North", "Ridge", "Summit");
        await page.Tb().Tree("Gear").CheckAsync("South", "Lake");
        await page.Tb().Tree("Remote").ExpandAsync("Archive");

        await Expect(page.GetByTestId("selected")).ToHaveTextAsync("3");
        await Expect(page.GetByTestId("checked")).ToHaveTextAsync("4, 5");
        await Expect(await page.Tb().Tree("Remote").ItemAsync("Archive", "2025")).ToBeVisibleAsync();
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Tree_ShouldHaveNoAccessibilityViolations_WhenOpenSelectedAndChecked(string browser)
    {
        var page = await OpenTreeAsync(browser);
        await page.Tb().Tree("Places").SelectAsync("North", "Pass");
        await page.Tb().Tree("Gear").CheckAsync("North", "Pass");

        var result = await page.RunAxe(AxeTests.WcagAA);

        Assert.Empty(result.Violations.Select(violation =>
            $"{violation.Id}: {string.Join(", ", violation.Nodes.Select(node => node.Target))}"));
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task TreeExample_ShouldShowOnlyMatchingTrails_WhenSearchTextIsTyped(string browser)
    {
        var page = await OpenAsync(browser, "/examples/tree-view");

        await page.GetByLabel("Find a trail").PressSequentiallyAsync("cedar");

        await Expect(Row(page, "Cedar Flats", "Trails by region")).ToBeVisibleAsync();
        await Expect(Row(page, "Pine Hollow loop", "Trails by region")).ToHaveCountAsync(0);
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Tree_ShouldOutlineTheSelectedRowInTheHighlight_WhenColorsAreForced(string browser)
    {
        var page = await OpenAsync(browser, "/examples/tree-view", new() { ForcedColors = ForcedColors.Active });
        var row = Row(page, "Pine Hollow loop", "Trails by region");

        await row.ClickAsync();

        await Expect(row).ToHaveAttributeAsync("aria-selected", "true");
        await Expect(row).ToHaveCSSAsync("outline-style", "solid");
        await Expect(row).ToHaveCSSAsync("outline-color", await SystemColorAsync(page, "Highlight"));
    }
}
