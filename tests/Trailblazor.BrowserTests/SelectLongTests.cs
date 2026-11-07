using Trailblazor.Testing.Playwright;

namespace Trailblazor.BrowserTests;

public class SelectLongTests(BrowserFixture fixture) : BrowserTest(fixture)
{
    private static ILocator Combobox(IPage page, string name) =>
        page.GetByRole(AriaRole.Combobox, new() { Name = name, Exact = true });

    private static ILocator Filter(IPage page) => page.Locator("[data-part=filter]");

    private static ILocator Active(IPage page) => page.Locator("[role=option][data-active]");

    private static ILocator Option(IPage page, string name) =>
        page.GetByRole(AriaRole.Option, new() { Name = name, Exact = true });

    private async Task<IPage> OpenListAsync(string browser, string name)
    {
        var page = await OpenAsync(browser, "/select-long");
        await Expect(Combobox(page, name)).ToHaveAttributeAsync("data-ready", "");
        await Combobox(page, name).ClickAsync();
        await Expect(Active(page)).ToBeVisibleAsync();
        return page;
    }

    // The options above the active one, among those not disabled.
    private static Task<int> ActiveIndexAsync(IPage page) => Active(page).EvaluateAsync<int>("""
        option => [...option.closest('[role=listbox]').querySelectorAll('[role=option]:not([aria-disabled=true])')]
            .indexOf(option)
        """);

    private static Task<double> PageScrollAsync(IPage page) => page.EvaluateAsync<double>("() => window.scrollY");

    // The options whose middles lie inside the open list's scrolling area.
    private static Task<int> OptionsShownAsync(IPage page) => Active(page).EvaluateAsync<int>("""
        option => {
            const list = option.closest('[role=listbox]');
            const area = list.getBoundingClientRect();
            return [...list.querySelectorAll('[role=option]')].filter(each => {
                const box = each.getBoundingClientRect();
                const middle = box.top + box.height / 2;
                return middle > area.top && middle < area.bottom;
            }).length;
        }
        """);

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Select_ShouldMoveByTheRowsShownAndLeaveThePageAlone_WhenPageKeysArePressed(string browser)
    {
        var page = await OpenListAsync(browser, "Plain");
        var shown = await OptionsShownAsync(page);

        await page.Keyboard.PressAsync("PageDown");

        var step = await ActiveIndexAsync(page);
        Assert.InRange(step, shown - 2, shown + 2);
        await Expect(Active(page)).ToBeInViewportAsync();
        await page.Keyboard.PressAsync("PageDown");
        Assert.Equal(step * 2, await ActiveIndexAsync(page));
        await page.Keyboard.PressAsync("PageUp");
        Assert.Equal(step, await ActiveIndexAsync(page));
        Assert.Equal(0, await PageScrollAsync(page));
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Select_ShouldStopAtTheEndsOfTheList_WhenPageKeysReachThem(string browser)
    {
        var page = await OpenListAsync(browser, "Plain");

        await page.Keyboard.PressAsync("ArrowDown");
        await page.Keyboard.PressAsync("PageUp");
        await Expect(Active(page)).ToHaveTextAsync("Plain 1");
        await page.Keyboard.PressAsync("End");
        await page.Keyboard.PressAsync("ArrowUp");
        await page.Keyboard.PressAsync("PageDown");

        await Expect(Active(page)).ToHaveTextAsync("Plain 40");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Select_ShouldMoveByTheRowsShownAndLeaveThePageAlone_WhenPageKeysArePressedInTheFilter(string browser)
    {
        var page = await OpenListAsync(browser, "Filtered");
        await Expect(Filter(page)).ToBeFocusedAsync();
        var shown = await OptionsShownAsync(page);

        await page.Keyboard.PressAsync("PageDown");

        Assert.InRange(await ActiveIndexAsync(page), shown - 2, shown + 2);
        Assert.Equal(0, await PageScrollAsync(page));
        await page.Keyboard.PressAsync("PageUp");
        await Expect(Active(page)).ToHaveTextAsync("Filtered 1");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Select_ShouldMoveByAboutAPage_WhenOptionsArriveAfterTheListOpensWithNoneActive(string browser)
    {
        var page = await OpenAsync(browser, "/select-long");
        await Expect(Combobox(page, "Late")).ToHaveAttributeAsync("data-ready", "");
        await Combobox(page, "Late").ClickAsync();
        await Expect(Combobox(page, "Late")).ToHaveAttributeAsync("aria-expanded", "true");
        await page.GetByRole(AriaRole.Button, new() { Name = "Load" }).EvaluateAsync("button => button.click()");
        await Expect(Option(page, "Late 40")).ToHaveCountAsync(1);

        await page.Keyboard.PressAsync("PageDown");

        await Expect(Active(page)).ToBeVisibleAsync();
        Assert.InRange(await ActiveIndexAsync(page), 1, 20);
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Select_ShouldKeepFocusAndItsKeys_WhenADisabledOptionIsPressed(string browser)
    {
        var page = await OpenListAsync(browser, "Plain");

        // Hovering waits for the list to settle, and unlike a click it doesn't refuse a disabled option.
        await Option(page, "Plain disabled").HoverAsync();
        await page.Mouse.DownAsync();
        await page.Mouse.UpAsync();

        await Expect(Combobox(page, "Plain")).ToBeFocusedAsync();
        await page.Keyboard.PressAsync("ArrowDown");
        await Expect(Active(page)).ToHaveTextAsync("Plain 2");
        await page.Keyboard.PressAsync("Escape");
        await Expect(Combobox(page, "Plain")).ToHaveAttributeAsync("aria-expanded", "false");
        await Expect(Combobox(page, "Plain")).ToBeFocusedAsync();
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Select_ShouldKeepFocusInTheFilter_WhenAGroupHeadingIsPressed(string browser)
    {
        var page = await OpenListAsync(browser, "Filtered");

        await page.GetByText("Filtered group").ClickAsync();

        await Expect(Filter(page)).ToBeFocusedAsync();
        await page.Keyboard.TypeAsync("40");
        await Expect(Filter(page)).ToHaveValueAsync("40");
        await Expect(Active(page)).ToHaveTextAsync("Filtered 40");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Filter_ShouldMoveItsCaret_WhenItsTextBoxIsPressed(string browser)
    {
        var page = await OpenListAsync(browser, "Filtered");
        await page.Keyboard.TypeAsync("40");
        await Filter(page).EvaluateAsync("filter => filter.setSelectionRange(0, 0)");

        // Playwright clicks the box's center, far past the text, so the caret lands at its end.
        await Filter(page).ClickAsync();

        Assert.Equal(2, await Filter(page).EvaluateAsync<int>("filter => filter.selectionStart"));
    }
}
