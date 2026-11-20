using Trailblazor.Testing.Playwright;

namespace Trailblazor.BrowserTests;

public class RibbonCollapseTests(BrowserFixture fixture) : BrowserTest(fixture)
{
    // Narrow enough that every group that can collapse has.
    private const int EveryGroupCollapsed = 240;

    private static ILocator Commands(IPage page) => page.GetByRole(AriaRole.Toolbar, new() { Name = "Home" });

    private static ILocator Group(IPage page, string label) =>
        page.GetByRole(AriaRole.Group, new() { Name = label, Exact = true });

    private static ILocator GroupButton(IPage page, string label) =>
        Group(page, label).Locator("> [data-part=group-button]");

    private static ILocator Button(IPage page, string name) =>
        page.GetByRole(AriaRole.Button, new() { Name = name, Exact = true });

    private async Task<IPage> OpenReadyAsync(string browser)
    {
        var page = await OpenAsync(browser, "/ribbon-collapse");
        await Expect(Commands(page)).ToHaveAttributeAsync("data-ready", "");
        return page;
    }

    // The row fits again a frame after its box changes size, so this waits out that fit.
    private static async Task SetWidthAsync(IPage page, int width)
    {
        await page.GetByTestId("box").EvaluateAsync("""
            (box, width) => {
                box.style.inlineSize = `${width}px`;
                return new Promise(done => requestAnimationFrame(() => requestAnimationFrame(done)));
            }
            """, width);
        await Expect(Commands(page)).Not.ToHaveAttributeAsync("data-fitting", "");
    }

    private static async Task<string[]> CollapsedAsync(IPage page) => await Commands(page).EvaluateAsync<string[]>(
        "row => [...row.querySelectorAll('[data-collapsed] > [data-part=group-label]')].map(label => label.textContent)");

    // The box's width at which the groups just fit, so a little less collapses the first of them.
    private static Task<int> FittingWidthAsync(IPage page) => Commands(page).EvaluateAsync<int>("""
        row => {
            const groups = row.lastElementChild.getBoundingClientRect().right - row.getBoundingClientRect().left;
            const around = document.querySelector('[data-testid=box]').getBoundingClientRect().width - row.clientWidth;
            return Math.ceil(groups + around);
        }
        """);

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Ribbon_ShouldCollapseOrderedGroupsFirstThenFromTheLast_WhenItNarrows(string browser)
    {
        var page = await OpenReadyAsync(browser);
        Assert.Empty(await CollapsedAsync(page));
        var width = await FittingWidthAsync(page) - 2;
        var steps = new List<string>();

        while (width > EveryGroupCollapsed)
        {
            await SetWidthAsync(page, width);
            var collapsed = string.Join(", ", await CollapsedAsync(page));
            if (steps.LastOrDefault() != collapsed)
            {
                steps.Add(collapsed);
            }

            width -= 24;
        }

        // Share says it goes first; the rest go from the last, except Marked places, which is narrower than the
        // button it would collapse into. Each list is in the groups' own order.
        Assert.Equal(["Share", "Share, Notes", "Share, View, Notes", "Write, Share, View, Notes"], steps);
        await Expect(GroupButton(page, "Share")).ToBeVisibleAsync();
        await Expect(Button(page, "Send")).ToBeHiddenAsync();
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Ribbon_ShouldFitOnce_WhenItsWidthChanges(string browser)
    {
        var page = await OpenReadyAsync(browser);
        // A fit that came again a frame later would close a group's controls opened in between.
        await Commands(page).EvaluateAsync("""
            row => {
                window.fits = 0;
                new MutationObserver(() => window.fits += row.hasAttribute('data-fitting') ? 1 : 0)
                    .observe(row, { attributes: true, attributeFilter: ['data-fitting'] });
            }
            """);

        await SetWidthAsync(page, EveryGroupCollapsed);
        await page.EvaluateAsync("""
            () => new Promise(done => requestAnimationFrame(() => requestAnimationFrame(
                () => requestAnimationFrame(done))))
            """);

        Assert.Equal(4, (await CollapsedAsync(page)).Length);
        Assert.Equal(1, await page.EvaluateAsync<int>("() => window.fits"));
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Ribbon_ShouldKeepItsFit_WhenAnotherTabIsShownAndItComesBack(string browser)
    {
        var page = await OpenReadyAsync(browser);
        await SetWidthAsync(page, EveryGroupCollapsed);

        await page.GetByRole(AriaRole.Tab, new() { Name = "Review" }).ClickAsync();
        await Expect(Button(page, "Spelling")).ToBeVisibleAsync();
        // A hidden row has no width, and fitting it then would bring every group back.
        await page.EvaluateAsync(
            "() => new Promise(done => requestAnimationFrame(() => requestAnimationFrame(done)))");
        var collapsedWhileHidden = await page.Locator("[aria-label=Home][role=toolbar] > [data-collapsed]").CountAsync();
        await page.GetByRole(AriaRole.Tab, new() { Name = "Home" }).ClickAsync();

        Assert.Equal(4, collapsedWhileHidden);
        await Expect(GroupButton(page, "Share")).ToBeVisibleAsync();
        Assert.Equal(4, (await CollapsedAsync(page)).Length);
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Ribbon_ShouldBringItsGroupsBack_WhenItWidensAgain(string browser)
    {
        var page = await OpenReadyAsync(browser);
        await SetWidthAsync(page, EveryGroupCollapsed);
        Assert.Equal(4, (await CollapsedAsync(page)).Length);

        await SetWidthAsync(page, 1100);

        Assert.Empty(await CollapsedAsync(page));
        await Expect(GroupButton(page, "Share")).ToBeHiddenAsync();
        await Expect(Button(page, "Send")).ToBeVisibleAsync();
        await Expect(Group(page, "Share").Locator("[data-part=group-label]")).ToBeVisibleAsync();
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Group_ShouldOpenItsControlsUnderItsButtonAndRunThem_WhenItHasCollapsed(string browser)
    {
        var page = await OpenReadyAsync(browser);
        await SetWidthAsync(page, EveryGroupCollapsed);
        var button = GroupButton(page, "Write");

        await button.ClickAsync();

        await Expect(button).ToHaveAttributeAsync("aria-expanded", "true");
        await Expect(Button(page, "New note")).ToBeFocusedAsync();
        var (opener, popup) = ((await button.BoundingBoxAsync())!,
            (await Group(page, "Write").Locator("[data-part=items]").BoundingBoxAsync())!);
        Assert.True(popup.Y >= opener.Y + opener.Height, "The group's controls should open under its button.");
        await Button(page, "Find").ClickAsync();
        await Expect(page.GetByTestId("last")).ToHaveTextAsync("Find");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Ribbon_ShouldReachACollapsedGroupsButtonAndItsOpenControls_WhenTheArrowKeysMoveAlongIt(
        string browser)
    {
        var page = await OpenReadyAsync(browser);
        await SetWidthAsync(page, await FittingWidthAsync(page) - 2);
        Assert.Equal(["Share"], await CollapsedAsync(page));
        await Button(page, "Filter").FocusAsync();

        await page.Keyboard.PressAsync("ArrowRight");
        await Expect(GroupButton(page, "Share")).ToBeFocusedAsync();
        await page.Keyboard.PressAsync("Enter");
        await Expect(Button(page, "Send")).ToBeFocusedAsync();
        await page.Keyboard.PressAsync("ArrowRight");
        await Expect(Button(page, "Print")).ToBeFocusedAsync();
        await page.Keyboard.PressAsync("Escape");

        await Expect(Button(page, "Print")).ToBeHiddenAsync();
        await Expect(GroupButton(page, "Share")).ToBeFocusedAsync();
        await Expect(GroupButton(page, "Share")).ToHaveAttributeAsync("aria-expanded", "false");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Group_ShouldGiveFocusToItsButton_WhenItsControlsWereOpenedFromCodeAndEscapeClosesThem(
        string browser)
    {
        var page = await OpenReadyAsync(browser);
        await SetWidthAsync(page, EveryGroupCollapsed);
        // Nothing has focus as the controls open, so the browser has nowhere of its own to put focus back.
        await Group(page, "Share").Locator("[data-part=items]").EvaluateAsync("popup => popup.showPopover()");
        await Expect(Button(page, "Send")).ToBeFocusedAsync();

        await page.Keyboard.PressAsync("Escape");

        await Expect(Button(page, "Send")).ToBeHiddenAsync();
        await Expect(GroupButton(page, "Share")).ToBeFocusedAsync();
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Group_ShouldLeaveFocusWhereItWent_WhenAnotherControlTakesItBeforeTheGroupHearsItClosed(
        string browser)
    {
        var page = await OpenReadyAsync(browser);
        await SetWidthAsync(page, EveryGroupCollapsed);
        await GroupButton(page, "Share").FocusAsync();
        await page.Keyboard.PressAsync(" ");
        await Expect(Button(page, "Send")).ToBeFocusedAsync();
        await HoldBackTheClosedToggleAsync(page, "[data-part=items]");

        await page.Keyboard.PressAsync("Escape");
        await Expect(Button(page, "Send")).ToBeHiddenAsync();
        var elsewhere = await FocusElsewhereBeforeTheCloseIsHeardAsync(page);

        await Expect(GroupButton(page, "Share")).ToHaveAttributeAsync("aria-expanded", "false");
        await Expect(elsewhere).ToBeFocusedAsync();
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Group_ShouldKeepWhatItsControlsHold_WhenItCollapsesOpensAndComesBack(string browser)
    {
        var page = await OpenReadyAsync(browser);
        var title = page.GetByRole(AriaRole.Textbox, new() { Name = "Title" });
        await title.FillAsync("Ridge loop");

        await SetWidthAsync(page, EveryGroupCollapsed);
        await GroupButton(page, "Notes").ClickAsync();
        await Expect(title).ToBeVisibleAsync();
        await Expect(title).ToHaveValueAsync("Ridge loop");
        await SetWidthAsync(page, 1100);

        await Expect(title).ToHaveValueAsync("Ridge loop");
        // Back in the row, not where the popup was.
        var (row, field) = ((await Commands(page).BoundingBoxAsync())!, (await title.BoundingBoxAsync())!);
        Assert.True(field.Y >= row.Y && field.Y + field.Height <= row.Y + row.Height, "The field should be in the row.");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Group_ShouldCloseItsControlsAndStayCollapsed_WhenTheRibbonChangesWidthWhileTheyAreOpen(
        string browser)
    {
        var page = await OpenReadyAsync(browser);
        await SetWidthAsync(page, EveryGroupCollapsed);
        await GroupButton(page, "Share").ClickAsync();
        await Expect(Button(page, "Send")).ToBeFocusedAsync();

        await SetWidthAsync(page, EveryGroupCollapsed - 10);

        await Expect(Button(page, "Send")).ToBeHiddenAsync();
        await Expect(GroupButton(page, "Share")).ToHaveAttributeAsync("aria-expanded", "false");
        await Expect(GroupButton(page, "Share")).ToBeFocusedAsync();
        Assert.Contains("Share", await CollapsedAsync(page));
        await SetWidthAsync(page, 1100);
        await Expect(Group(page, "Share").Locator("[data-part=items]")).Not.ToHaveCSSAsync("position", "fixed");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Ribbon_ShouldMoveFocusToTheGroupsFirstControl_WhenTheFocusedButtonsGroupComesBack(string browser)
    {
        var page = await OpenReadyAsync(browser);
        await SetWidthAsync(page, EveryGroupCollapsed);
        await GroupButton(page, "Share").FocusAsync();

        await SetWidthAsync(page, 1100);

        await Expect(Button(page, "Send")).ToBeFocusedAsync();
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Ribbon_ShouldKeepFocusOnAGroupsButton_WhenItsWidthChangesAndTheGroupStaysCollapsed(string browser)
    {
        var page = await OpenReadyAsync(browser);
        await SetWidthAsync(page, EveryGroupCollapsed);
        await GroupButton(page, "Share").FocusAsync();

        await SetWidthAsync(page, EveryGroupCollapsed - 10);

        await Expect(GroupButton(page, "Share")).ToBeFocusedAsync();
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Ribbon_ShouldMoveFocusToTheGroupsButton_WhenTheFocusedControlsGroupCollapses(string browser)
    {
        var page = await OpenReadyAsync(browser);
        await Button(page, "Send").FocusAsync();

        await SetWidthAsync(page, await FittingWidthAsync(page) - 2);

        await Expect(GroupButton(page, "Share")).ToBeFocusedAsync();
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Group_ShouldCloseItsControls_WhenFocusLeavesThem(string browser)
    {
        var page = await OpenReadyAsync(browser);
        await SetWidthAsync(page, EveryGroupCollapsed);
        await GroupButton(page, "Share").ClickAsync();
        await Expect(Button(page, "Send")).ToBeFocusedAsync();

        await page.Keyboard.PressAsync("Tab");

        await Expect(Button(page, "After")).ToBeFocusedAsync();
        await Expect(Button(page, "Send")).ToBeHiddenAsync();
        await Expect(GroupButton(page, "Share")).ToHaveAttributeAsync("aria-expanded", "false");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Ribbon_ShouldScrollSidewaysAndBringTheFocusedButtonIntoView_WhenEvenTheButtonsDoNotFit(
        string browser)
    {
        var page = await OpenReadyAsync(browser);
        await SetWidthAsync(page, 150);
        Assert.True(await Commands(page).EvaluateAsync<bool>("row => row.scrollWidth > row.clientWidth"));
        // The page itself doesn't scroll sideways.
        Assert.False(await page.EvaluateAsync<bool>(
            "() => document.documentElement.scrollWidth > document.documentElement.clientWidth"));
        await GroupButton(page, "Write").FocusAsync();

        await page.Keyboard.PressAsync("End");

        await Expect(GroupButton(page, "Notes")).ToBeFocusedAsync();
        await Expect(GroupButton(page, "Notes")).ToBeInViewportAsync(new() { Ratio = 1 });
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Helper_ShouldOpenACollapsedGroupToClickItsButton_WhenTheButtonIsOutOfSight(string browser)
    {
        var page = await OpenReadyAsync(browser);
        await SetWidthAsync(page, EveryGroupCollapsed);

        await page.Tb().Ribbon("Planner").ClickAsync("Print");

        await Expect(page.GetByTestId("last")).ToHaveTextAsync("Print");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Helper_ShouldWaitOutTheFit_WhenTheRibbonNarrowedJustBeforeTheClick(string browser)
    {
        var page = await OpenReadyAsync(browser);
        // Not SetWidthAsync, which waits for the fit itself.
        await page.GetByTestId("box").EvaluateAsync(
            "(box, width) => { box.style.inlineSize = `${width}px`; }", EveryGroupCollapsed);

        await page.Tb().Ribbon("Planner").ClickAsync("Print");

        await Expect(page.GetByTestId("last")).ToHaveTextAsync("Print");
        Assert.Contains("Share", await CollapsedAsync(page));
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Helper_ShouldClickTheCommand_WhenItsGroupHasTheSameNameCollapsedOrNot(string browser)
    {
        var page = await OpenReadyAsync(browser);

        await page.Tb().Ribbon("Planner").ClickAsync("View");
        await Expect(page.GetByTestId("last")).ToHaveTextAsync("View");
        await page.Tb().Ribbon("Planner").ClickAsync("Find");
        await SetWidthAsync(page, EveryGroupCollapsed);
        await page.Tb().Ribbon("Planner").ClickAsync("View");

        await Expect(page.GetByTestId("last")).ToHaveTextAsync("View");
    }
}
