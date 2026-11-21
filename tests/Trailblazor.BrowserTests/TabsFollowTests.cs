namespace Trailblazor.BrowserTests;

// A selected tab that goes away hands its focus to the tab selected in its place, which the app can choose as it
// removes the tab, and focus gets there in one move.
public class TabsFollowTests(BrowserFixture fixture) : BrowserTest(fixture)
{
    // The app selects Last, which was there already, or New, a tab it adds as it removes the other.
    public static TheoryData<string, string, string, string> Ways => new(
        from browser in AllBrowsers.Names
        from bound in new[] { "key", "index" }
        from way in new[] { "the page's key", "Delete", "the close mark" }
        from chosen in new[] { "Last", "New" }
        select (browser, bound, way, chosen));

    public static TheoryData<string, string> Bindings => new(
        from browser in AllBrowsers.Names
        from bound in new[] { "key", "index" }
        select (browser, bound));

    private static ILocator Tab(ILocator list, string name) => list.GetByRole(AriaRole.Tab, new() { Name = name });

    private async Task<ILocator> OpenTabsAsync(string browser, string bound, string query = "")
    {
        var page = await OpenAsync(browser, "/tabs-follow" + query);
        var list = page.GetByRole(AriaRole.Tablist, new() { Name = $"By {bound}" });
        await Expect(list).ToHaveAttributeAsync("data-ready", "");
        return list;
    }

    // Selects the tab and gives it focus, and starts keeping the ids of the tabs that take focus from then on.
    private static async Task SelectAsync(ILocator tab)
    {
        await tab.ClickAsync();
        await Expect(tab).ToHaveAttributeAsync("aria-selected", "true");
        // Safari doesn't focus a button when it's clicked.
        await tab.FocusAsync();
        await tab.EvaluateAsync("""
            tab => {
                window.focusedTabs = [];
                tab.parentElement.addEventListener('focusin', event => focusedTabs.push(event.target.id));
            }
            """);
    }

    [Theory, MemberData(nameof(Ways))]
    public async Task Tabs_ShouldMoveFocusOnceToTheTabTheAppSelects_WhenTheSelectedTabGoesAway(string browser,
        string bound, string way, string chosen)
    {
        var list = await OpenTabsAsync(browser, bound, chosen == "New" ? "?add=true" : "");
        var second = Tab(list, $"Second {bound}");
        await SelectAsync(second);

        if (way == "the close mark")
        {
            await second.Locator("[data-part=close]").ClickAsync();
        }
        else
        {
            await list.Page.Keyboard.PressAsync(way == "Delete" ? "Delete" : "x");
        }

        // Third, beside the removed tab, is where focus would go if it didn't follow the selection.
        var selected = Tab(list, $"{chosen} {bound}");
        await Expect(second).ToHaveCountAsync(0);
        await Expect(selected).ToHaveAttributeAsync("aria-selected", "true");
        await Expect(selected).ToBeFocusedAsync();
        Assert.Equal([await selected.GetAttributeAsync("id")], await list.Page.EvaluateAsync<string[]>("focusedTabs"));
    }

    [Theory, MemberData(nameof(Bindings))]
    public async Task Tabs_ShouldFocusTheTabBesideIt_WhenAFocusedTabThatIsNotSelectedGoesAsTheAppSelectsAnother(
        string browser, string bound)
    {
        var list = await OpenTabsAsync(browser, bound);
        await Tab(list, $"Second {bound}").FocusAsync();

        await list.Page.Keyboard.PressAsync("u");

        await Expect(Tab(list, $"Second {bound}")).ToHaveCountAsync(0);
        await Expect(Tab(list, $"Last {bound}")).ToHaveAttributeAsync("aria-selected", "true");
        await Expect(Tab(list, $"Third {bound}")).ToBeFocusedAsync();
    }

    [Theory, MemberData(nameof(Bindings))]
    public async Task Tabs_ShouldFocusTheTabBesideIt_WhenTheSelectedTabGoesAwayAndTheAppSelectsADisabledTab(
        string browser, string bound)
    {
        var list = await OpenTabsAsync(browser, bound);
        await SelectAsync(Tab(list, $"Second {bound}"));

        await list.Page.Keyboard.PressAsync("d");

        await Expect(Tab(list, $"Second {bound}")).ToHaveCountAsync(0);
        await Expect(Tab(list, $"Shut {bound}")).ToHaveAttributeAsync("aria-selected", "true");
        await Expect(Tab(list, $"Third {bound}")).ToBeFocusedAsync();
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Tabs_ShouldFocusTheTabBesideIt_WhenTheSelectedTabGoesAwayAndTheAppSelectsNone(string browser)
    {
        var list = await OpenTabsAsync(browser, "key");
        await SelectAsync(Tab(list, "Second key"));

        await list.Page.Keyboard.PressAsync("n");

        await Expect(Tab(list, "Second key")).ToHaveCountAsync(0);
        await Expect(list.Locator("[aria-selected=true]")).ToHaveCountAsync(0);
        await Expect(Tab(list, "Third key")).ToBeFocusedAsync();
    }
}
