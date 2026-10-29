namespace Trailblazor.BrowserTests;

public class TabsTests(BrowserFixture fixture) : BrowserTest(fixture)
{
    private static ILocator Tab(IPage page, string name) =>
        page.GetByRole(AriaRole.Tab, new() { Name = name, Exact = true });

    // The arrow keys work once the tabs script has attached, which is when it sets aria-orientation.
    private static async Task WaitForKeysAsync(IPage page, string list) =>
        await Expect(page.GetByRole(AriaRole.Tablist, new() { Name = list }))
            .ToHaveAttributeAsync("aria-orientation", "horizontal");

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Tabs_ShouldMoveFocusAndSelectSkippingDisabledTabs_WhenArrowKeysArePressed(string browser)
    {
        var page = await OpenAsync(browser, "/tabs");
        await WaitForKeysAsync(page, "Product");
        await Tab(page, "Overview").FocusAsync();

        await page.Keyboard.PressAsync("ArrowRight");
        await Expect(Tab(page, "Reviews")).ToBeFocusedAsync();
        await Expect(Tab(page, "Reviews")).ToHaveAttributeAsync("aria-selected", "true");
        await Expect(page.GetByRole(AriaRole.Tabpanel, new() { Name = "Reviews" })).ToHaveTextAsync("Reviews panel");
        await Expect(page.GetByTestId("section")).ToHaveTextAsync("2");

        await page.Keyboard.PressAsync("End");
        await Expect(Tab(page, "Pricing")).ToBeFocusedAsync();
        await page.Keyboard.PressAsync("ArrowRight");
        await Expect(Tab(page, "Overview")).ToBeFocusedAsync();
        await page.Keyboard.PressAsync("ArrowLeft");
        await Expect(Tab(page, "Pricing")).ToBeFocusedAsync();
        await page.Keyboard.PressAsync("Home");
        await Expect(Tab(page, "Overview")).ToBeFocusedAsync();
        await Expect(page.GetByTestId("section")).ToHaveTextAsync("0");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Tabs_ShouldMoveForwardWithTheLeftArrow_WhenThePageIsRightToLeft(string browser)
    {
        var page = await OpenAsync(browser, "/tabs");
        await WaitForKeysAsync(page, "Product");
        await page.EvaluateAsync("() => document.documentElement.dir = 'rtl'");
        await Tab(page, "Overview").FocusAsync();

        await page.Keyboard.PressAsync("ArrowLeft");

        await Expect(Tab(page, "Reviews")).ToBeFocusedAsync();
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Tabs_ShouldOnlyMoveFocusUntilEnter_WhenActivationIsManual(string browser)
    {
        var page = await OpenAsync(browser, "/tabs");
        await WaitForKeysAsync(page, "Account");
        await Tab(page, "Profile").FocusAsync();

        await page.Keyboard.PressAsync("ArrowRight");
        await Expect(Tab(page, "Security")).ToBeFocusedAsync();
        await Expect(Tab(page, "Profile")).ToHaveAttributeAsync("aria-selected", "true");

        await page.Keyboard.PressAsync("Enter");
        await Expect(Tab(page, "Security")).ToHaveAttributeAsync("aria-selected", "true");
        await Expect(page.GetByRole(AriaRole.Tabpanel, new() { Name = "Security" })).ToHaveTextAsync("Security panel");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Tabs_ShouldKeepAPanelsState_WhenKeepAliveIsSetAndTheUserSwitchesAway(string browser)
    {
        var page = await OpenAsync(browser, "/tabs");
        await page.GetByLabel("Bio").FillAsync("likes hiking");

        await Tab(page, "Security").ClickAsync();
        await Expect(page.GetByLabel("Bio")).ToBeHiddenAsync();
        await Tab(page, "Profile").ClickAsync();

        await Expect(page.GetByLabel("Bio")).ToHaveValueAsync("likes hiking");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Tabs_ShouldMoveFocusIntoThePanel_WhenTabIsPressedOnTheSelectedTab(string browser)
    {
        var page = await OpenAsync(browser, "/tabs");
        await Tab(page, "Overview").FocusAsync();

        await page.Keyboard.PressAsync("Tab");

        await Expect(page.GetByRole(AriaRole.Tabpanel, new() { Name = "Overview" })).ToBeFocusedAsync();
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Tabs_ShouldListALateTabInMarkupOrderAndKeepTheSelection_WhenItRendersAfterOthers(string browser)
    {
        var page = await OpenAsync(browser, "/tabs");
        await WaitForKeysAsync(page, "Reports");
        await Tab(page, "Monthly").ClickAsync();
        await Expect(page.GetByTestId("report")).ToHaveTextAsync("1");

        await page.GetByRole(AriaRole.Button, new() { Name = "Add weekly" }).ClickAsync();

        await Expect(page.GetByRole(AriaRole.Tablist, new() { Name = "Reports" }).GetByRole(AriaRole.Tab))
            .ToHaveTextAsync(["Daily", "Weekly", "Monthly"]);
        await Expect(page.GetByTestId("report")).ToHaveTextAsync("2");
        await Expect(Tab(page, "Monthly")).ToHaveAttributeAsync("aria-selected", "true");
        await Tab(page, "Daily").FocusAsync();
        await page.Keyboard.PressAsync("ArrowRight");
        await Expect(Tab(page, "Weekly")).ToBeFocusedAsync();
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Tabs_ShouldKeepTheSelectedTab_WhenTabsAreSortedAfterTheParentOpenedANewLastTab(string browser)
    {
        var page = await OpenAsync(browser, "/tabs");
        await WaitForKeysAsync(page, "Regions");
        await page.GetByRole(AriaRole.Button, new() { Name = "Open south" }).ClickAsync();
        await Expect(Tab(page, "South")).ToHaveAttributeAsync("aria-selected", "true");

        await page.GetByRole(AriaRole.Button, new() { Name = "Sort regions" }).ClickAsync();

        await Expect(page.GetByRole(AriaRole.Tablist, new() { Name = "Regions" }).GetByRole(AriaRole.Tab))
            .ToHaveTextAsync(["North", "South", "West"]);
        await Expect(page.GetByTestId("region")).ToHaveTextAsync("1");
        await Expect(Tab(page, "South")).ToHaveAttributeAsync("aria-selected", "true");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Tabs_ShouldKeepFocusOnTheSameTab_WhenALateTabRendersBeforeIt(string browser)
    {
        var page = await OpenAsync(browser, "/tabs");
        await WaitForKeysAsync(page, "Plans");
        await Tab(page, "Basic").FocusAsync();

        await page.Keyboard.PressAsync("ArrowRight");

        await Expect(page.GetByRole(AriaRole.Tablist, new() { Name = "Plans" }).GetByRole(AriaRole.Tab))
            .ToHaveTextAsync(["Basic", "Plus", "Pro"]);
        await Expect(Tab(page, "Pro")).ToBeFocusedAsync();
        await Expect(Tab(page, "Pro")).ToHaveAttributeAsync("aria-selected", "true");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Tabs_ShouldSelectTheTabTheParentChose_WhenItAddsTheTabAndSetsTheIndexTogether(string browser)
    {
        var page = await OpenAsync(browser, "/tabs");
        await WaitForKeysAsync(page, "Reports");

        await page.GetByRole(AriaRole.Button, new() { Name = "Open weekly" }).ClickAsync();

        await Expect(page.GetByRole(AriaRole.Tablist, new() { Name = "Reports" }).GetByRole(AriaRole.Tab))
            .ToHaveTextAsync(["Daily", "Weekly", "Monthly"]);
        await Expect(Tab(page, "Weekly")).ToHaveAttributeAsync("aria-selected", "true");
        await Expect(page.GetByRole(AriaRole.Tabpanel, new() { Name = "Weekly" })).ToHaveTextAsync("Weekly panel");
        await Expect(page.GetByTestId("report")).ToHaveTextAsync("1");
    }
}
