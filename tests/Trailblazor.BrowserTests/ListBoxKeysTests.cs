using Trailblazor.Testing.Playwright;

namespace Trailblazor.BrowserTests;

public class ListBoxKeysTests(BrowserFixture fixture) : BrowserTest(fixture)
{
    private static ILocator ListBox(IPage page, string name) =>
        page.GetByRole(AriaRole.Listbox, new() { Name = name, Exact = true });

    private static ILocator Option(IPage page, string name) =>
        page.GetByRole(AriaRole.Option, new() { Name = name, Exact = true });

    private static ILocator Active(IPage page) => page.Locator("[role=option][data-active]");

    private async Task<IPage> OpenAndFocusAsync(string browser, string name)
    {
        var page = await OpenAsync(browser, "/listbox-keys");
        await Expect(ListBox(page, name)).ToHaveAttributeAsync("data-ready", "");
        await ListBox(page, name).FocusAsync();
        return page;
    }

    private static Task<double> PageScrollAsync(IPage page) => page.EvaluateAsync<double>("() => window.scrollY");

    // The options whose middles lie inside the list's scrolling area.
    private static Task<int> OptionsShownAsync(IPage page, string name) => ListBox(page, name).EvaluateAsync<int>("""
        list => {
            const area = list.getBoundingClientRect();
            return [...list.querySelectorAll('[role=option]')].filter(each => {
                const box = each.getBoundingClientRect();
                const middle = box.top + box.height / 2;
                return middle > area.top && middle < area.bottom;
            }).length;
        }
        """);

    private static Task ShiftClickAsync(IPage page, string name) =>
        Option(page, name).ClickAsync(new() { Modifiers = [KeyboardModifier.Shift] });

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task ListBox_ShouldMoveByTheRowsShownAndChoose_WhenPageKeysArePressed(string browser)
    {
        var page = await OpenAndFocusAsync(browser, "Single");
        var single = page.GetByTestId("single");
        var shown = await OptionsShownAsync(page, "Single");

        await page.Keyboard.PressAsync("PageDown");

        await Expect(single).Not.ToHaveTextAsync("");
        var step = int.Parse((await single.TextContentAsync())!.Split(' ')[1]) - 1;
        Assert.Equal(shown, step);
        await Expect(Active(page)).ToBeInViewportAsync();
        await page.Keyboard.PressAsync("PageDown");
        await Expect(single).ToHaveTextAsync($"Single {1 + step * 2}");
        await page.Keyboard.PressAsync("PageUp");
        await Expect(single).ToHaveTextAsync($"Single {1 + step}");
        Assert.Equal(0, await PageScrollAsync(page));
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task ListBox_ShouldStopAtTheEndsOfTheList_WhenPageKeysReachThem(string browser)
    {
        var page = await OpenAndFocusAsync(browser, "Single");
        var single = page.GetByTestId("single");

        await page.Keyboard.PressAsync("End");
        await Expect(single).ToHaveTextAsync("Single 30");
        await page.Keyboard.PressAsync("ArrowUp");
        await Expect(single).ToHaveTextAsync("Single 29");
        await page.Keyboard.PressAsync("PageDown");
        await Expect(single).ToHaveTextAsync("Single 30");
        await page.Keyboard.PressAsync("Home");
        await Expect(single).ToHaveTextAsync("Single 1");
        await page.Keyboard.PressAsync("ArrowDown");
        await Expect(single).ToHaveTextAsync("Single 2");
        await page.Keyboard.PressAsync("PageUp");

        await Expect(single).ToHaveTextAsync("Single 1");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task MultiListBox_ShouldMoveWithoutChoosing_WhenPageKeysArePressed(string browser)
    {
        var page = await OpenAndFocusAsync(browser, "Several");
        await Expect(Active(page)).ToHaveTextAsync("Alder");

        await page.Keyboard.PressAsync("PageDown");

        await Expect(Active(page)).Not.ToHaveTextAsync("Alder");
        await page.GetByRole(AriaRole.Button, new() { Name = "Flush" }).EvaluateAsync("button => button.click()");
        await Expect(page.GetByTestId("flushes")).ToHaveTextAsync("1");
        await Expect(page.GetByTestId("several")).ToHaveTextAsync("");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task MultiListBox_ShouldChooseEveryEnabledOptionAsOneChange_WhenCtrlAIsPressed(string browser)
    {
        var page = await OpenAndFocusAsync(browser, "Several");
        await Option(page, "Cedar").ClickAsync();
        await Expect(page.GetByTestId("changes")).ToHaveTextAsync("1");

        await page.Keyboard.PressAsync("ControlOrMeta+a");

        await Expect(page.GetByTestId("several"))
            .ToHaveTextAsync("Cedar, Alder, Dogwood, Elm, Fir, Gum, Hazel");
        await Expect(page.GetByTestId("changes")).ToHaveTextAsync("2");
        Assert.Equal("", await page.EvaluateAsync<string>("() => getSelection().toString()"));
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task MultiListBox_ShouldLeaveTheValueAlone_WhenItIsReadOnlyAndCtrlAIsPressed(string browser)
    {
        var page = await OpenAndFocusAsync(browser, "Locked");

        await page.Keyboard.PressAsync("ControlOrMeta+a");

        await page.GetByRole(AriaRole.Button, new() { Name = "Flush" }).EvaluateAsync("button => button.click()");
        await Expect(page.GetByTestId("flushes")).ToHaveTextAsync("1");
        await Expect(page.GetByTestId("locked")).ToHaveTextAsync("0");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task ListBox_ShouldLeaveCtrlAToThePage_WhenOnlyOneOptionCanBeChosen(string browser)
    {
        var page = await OpenAndFocusAsync(browser, "Single");
        await page.EvaluateAsync("""
            () => document.addEventListener('keydown', event => {
                if (event.key === 'a') {
                    window.taken = event.defaultPrevented;
                }
            })
            """);

        await page.Keyboard.PressAsync("ControlOrMeta+a");

        Assert.False(await page.EvaluateAsync<bool>("() => window.taken"));
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task MultiListBox_ShouldLeaveCtrlShiftAToThePage_WhenItIsPressed(string browser)
    {
        var page = await OpenAndFocusAsync(browser, "Several");
        await page.EvaluateAsync("""
            () => document.addEventListener('keydown', event => {
                if (event.key.toLowerCase() === 'a') {
                    window.taken = event.defaultPrevented;
                }
            })
            """);

        await page.Keyboard.PressAsync("ControlOrMeta+Shift+a");

        Assert.False(await page.EvaluateAsync<bool>("() => window.taken"));
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task MultiListBox_ShouldChooseTheRangeAsOneChange_WhenAnOptionIsShiftClicked(string browser)
    {
        var page = await OpenAndFocusAsync(browser, "Several");
        await Option(page, "Alder").ClickAsync();
        await Expect(page.GetByTestId("changes")).ToHaveTextAsync("1");

        await ShiftClickAsync(page, "Dogwood");

        await Expect(page.GetByTestId("several")).ToHaveTextAsync("Alder, Cedar, Dogwood");
        await Expect(page.GetByTestId("changes")).ToHaveTextAsync("2");
        await Expect(Active(page)).ToHaveTextAsync("Dogwood");
        Assert.Equal("", await page.EvaluateAsync<string>("() => getSelection().toString()"));
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task MultiListBox_ShouldChooseTheRangeUpward_WhenTheShiftClickedOptionIsAboveTheLastOne(string browser)
    {
        var page = await OpenAndFocusAsync(browser, "Several");
        await Option(page, "Elm").ClickAsync();
        await Expect(page.GetByTestId("several")).ToHaveTextAsync("Elm");

        await ShiftClickAsync(page, "Cedar");

        await Expect(page.GetByTestId("several")).ToHaveTextAsync("Elm, Cedar, Dogwood");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task MultiListBox_ShouldKeepWhatIsChosen_WhenARangeIsShiftClickedOverIt(string browser)
    {
        var page = await OpenAndFocusAsync(browser, "Several");
        await Option(page, "Cedar").ClickAsync();
        await Expect(page.GetByTestId("several")).ToHaveTextAsync("Cedar");
        await Option(page, "Alder").ClickAsync();
        await Expect(page.GetByTestId("several")).ToHaveTextAsync("Cedar, Alder");

        await ShiftClickAsync(page, "Dogwood");

        await Expect(page.GetByTestId("several")).ToHaveTextAsync("Cedar, Alder, Dogwood");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task MultiListBox_ShouldChooseEveryOption_WhenCtrlAIsPressedOnAVeryLongList(string browser)
    {
        var page = await OpenAndFocusAsync(browser, "Huge");

        await page.Keyboard.PressAsync("ControlOrMeta+a");

        await Expect(page.GetByTestId("huge")).ToHaveTextAsync("1000");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task MultiListBox_ShouldChooseTheRange_WhenItCrossesAVeryLongList(string browser)
    {
        var page = await OpenAndFocusAsync(browser, "Huge");
        await Option(page, "Huge 1").ClickAsync();
        await Expect(page.GetByTestId("huge")).ToHaveTextAsync("1");

        await ShiftClickAsync(page, "Huge 1000");

        await Expect(page.GetByTestId("huge")).ToHaveTextAsync("1000");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task ListBox_ShouldChooseTheOption_WhenASingleListIsShiftClicked(string browser)
    {
        var page = await OpenAndFocusAsync(browser, "Single");

        await ShiftClickAsync(page, "Single 2");

        await Expect(page.GetByTestId("single")).ToHaveTextAsync("Single 2");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task ListBox_ShouldPutTheActiveOptionOnTheChosenOne_WhenFocusComesBackAfterTheValueChanged(string browser)
    {
        var page = await OpenAndFocusAsync(browser, "Single");
        await page.Keyboard.PressAsync("ArrowDown");
        await Expect(page.GetByTestId("single")).Not.ToHaveTextAsync("");

        await page.GetByRole(AriaRole.Button, new() { Name = "Choose from code" }).ClickAsync();
        await Expect(page.GetByTestId("single")).ToHaveTextAsync("Single 20");
        await ListBox(page, "Single").FocusAsync();

        await Expect(Active(page)).ToHaveTextAsync("Single 20");
        await Expect(Active(page)).ToBeInViewportAsync();
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task ListBox_ShouldPutTheActiveOptionOnTheChosenOne_WhenADisabledOptionIsPressed(string browser)
    {
        var page = await OpenAndFocusAsync(browser, "Single");
        await page.Keyboard.PressAsync("ArrowDown");
        await Expect(page.GetByTestId("single")).Not.ToHaveTextAsync("");
        await page.GetByRole(AriaRole.Button, new() { Name = "Choose from code" }).ClickAsync();
        await Expect(page.GetByTestId("single")).ToHaveTextAsync("Single 20");

        // Hovering scrolls it into view, and unlike a click it doesn't refuse a disabled option.
        await Option(page, "Single off").HoverAsync();
        await page.Mouse.DownAsync();
        await page.Mouse.UpAsync();

        await Expect(ListBox(page, "Single")).ToBeFocusedAsync();
        await Expect(Active(page)).ToHaveTextAsync("Single 20");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task ListBox_ShouldPutTheActiveOptionOnTheChosenOne_WhenFocusComesBackAfterAnEarlierPress(string browser)
    {
        var page = await OpenAndFocusAsync(browser, "Single");
        await Option(page, "Single 2").ClickAsync();
        await Expect(page.GetByTestId("single")).ToHaveTextAsync("Single 2");
        await page.GetByRole(AriaRole.Button, new() { Name = "Choose from code" }).ClickAsync();
        await Expect(page.GetByTestId("single")).ToHaveTextAsync("Single 20");

        await ListBox(page, "Single").FocusAsync();

        await Expect(Active(page)).ToHaveTextAsync("Single 20");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task ListBox_ShouldPutTheActiveOptionOnTheChosenOne_WhenAnEarlierPressBecameAScroll(string browser)
    {
        var page = await OpenAndFocusAsync(browser, "Single");
        await page.Keyboard.PressAsync("ArrowDown");
        await Expect(page.GetByTestId("single")).Not.ToHaveTextAsync("");
        await page.GetByRole(AriaRole.Button, new() { Name = "Choose from code" }).ClickAsync();
        await Expect(page.GetByTestId("single")).ToHaveTextAsync("Single 20");
        await Option(page, "Single 2").EvaluateAsync("""
            option => {
                option.dispatchEvent(new PointerEvent('pointerdown', { bubbles: true }));
                option.dispatchEvent(new PointerEvent('pointercancel', { bubbles: true }));
            }
            """);

        await ListBox(page, "Single").FocusAsync();

        await Expect(Active(page)).ToHaveTextAsync("Single 20");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task ListBox_ShouldKeepItsScrollPosition_WhenFocusComesBackWithTheValueUnchanged(string browser)
    {
        var page = await OpenAndFocusAsync(browser, "Single");
        await page.Keyboard.PressAsync("ArrowDown");
        await Expect(page.GetByTestId("single")).Not.ToHaveTextAsync("");
        await ListBox(page, "Single").EvaluateAsync("list => list.blur()");
        await ListBox(page, "Single").EvaluateAsync("list => list.scrollTop = list.scrollHeight");
        var scrolled = await ListBox(page, "Single").EvaluateAsync<double>("list => list.scrollTop");
        Assert.True(scrolled > 0);

        await ListBox(page, "Single").FocusAsync();

        Assert.Equal(scrolled, await ListBox(page, "Single").EvaluateAsync<double>("list => list.scrollTop"));
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task ListBox_ShouldChooseThePressedOption_WhenAPressBringsFocusBackAfterTheValueChanged(string browser)
    {
        var page = await OpenAndFocusAsync(browser, "Single");
        await page.Keyboard.PressAsync("ArrowDown");
        await Expect(page.GetByTestId("single")).Not.ToHaveTextAsync("");
        await page.GetByRole(AriaRole.Button, new() { Name = "Choose from code" }).ClickAsync();
        await Expect(page.GetByTestId("single")).ToHaveTextAsync("Single 20");

        await Option(page, "Single 19").ClickAsync();

        await Expect(page.GetByTestId("single")).ToHaveTextAsync("Single 19");
        await Expect(Active(page)).ToHaveTextAsync("Single 19");
    }
}
