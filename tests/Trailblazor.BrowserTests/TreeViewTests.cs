using Deque.AxeCore.Playwright;
using Trailblazor.Testing.Playwright;

namespace Trailblazor.BrowserTests;

public class TreeViewTests(BrowserFixture fixture) : TreeViewKeysTests(fixture)
{
    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task TreeLocator_ShouldFindTheTreeAndReturn_WhenItHasNoRowsToExpand(string browser)
    {
        var page = await OpenAsync(browser, "/examples/tree-view");

        await page.Tb().Tree("Closed regions").ExpandAllAsync();

        await Expect(page.GetByRole(AriaRole.Group, new() { Name = "Closed regions" })).ToBeVisibleAsync();
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
    public async Task Rows_ShouldHighlightNoText_WhenDoubleClickedOrShiftClicked(string browser)
    {
        var page = await OpenTreeAsync(browser);

        await Row(page, "South", "Places").DblClickAsync();
        Assert.True(await page.EvaluateAsync<bool>("() => document.getSelection().isCollapsed"));
        await Row(page, "North", "Gear").ClickAsync();
        await Row(page, "South", "Gear").ClickAsync(new() { Modifiers = [KeyboardModifier.Shift] });
        await Expect(page.GetByTestId("gear")).ToHaveTextAsync("1, 4");

        Assert.True(await page.EvaluateAsync<bool>("() => document.getSelection().isCollapsed"));
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
    public async Task Helper_ShouldCollapseAndActivateByPath_WhenGivenTheRowsTexts(string browser)
    {
        var page = await OpenAsync(browser, "/tree-view");
        var places = page.Tb().Tree("Places");
        await places.ExpandAsync("North");

        await places.CollapseAsync("North");
        var collapsed = await places.TextsAsync();
        await places.ActivateAsync("North", "Ridge", "Summit");

        Assert.Equal(["North", "South"], collapsed);
        await Expect(page.GetByTestId("activated")).ToHaveTextAsync("Summit");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Helper_ShouldStartPathsInsideTheRow_WhenGivenABranch(string browser)
    {
        var page = await OpenAsync(browser, "/tree-view");
        var places = page.Tb().Tree("Places");
        var north = places.Branch("North");

        await north.ExpandAllAsync();
        var opened = await places.TextsAsync();
        await places.ExpandAsync("South");
        await north.Branch("Ridge").SelectAsync("Summit");
        var summit = await (await north.RowAsync("Summit")).GetAttributeAsync("data-key");
        await north.CollapseAsync("Ridge");

        Assert.Equal(["North", "Ridge", "Summit", "Pass", "South"], opened);
        Assert.Equal("3", summit);
        await Expect(page.GetByTestId("selected")).ToHaveTextAsync("3");
        await Assert.ThrowsAsync<InvalidOperationException>(() => north.RowAsync("Lake"));
        Assert.Equal(["Ridge", "Pass"], await north.TextsAsync());
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
