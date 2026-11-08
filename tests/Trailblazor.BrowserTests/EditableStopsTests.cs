namespace Trailblazor.BrowserTests;

public class EditableStopsTests(BrowserFixture fixture) : BrowserTest(fixture)
{
    private static ILocator Button(IPage page, string name) =>
        page.GetByRole(AriaRole.Button, new() { Name = name, Exact = true });

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task FocusScope_ShouldWrapBothWays_WhenAnEditableRegionEndsIt(string browser)
    {
        var page = await OpenAsync(browser, "/editable-stops");
        await Button(page, "Open editable last").ClickAsync();
        await Expect(Button(page, "Before")).ToBeFocusedAsync();

        await page.Keyboard.PressAsync("Shift+Tab");
        await Expect(page.GetByTestId("notes")).ToBeFocusedAsync();
        await page.Keyboard.PressAsync("Tab");

        await Expect(Button(page, "Before")).ToBeFocusedAsync();
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task FocusScope_ShouldWrapBackwardToThePlayer_WhenAPlayerWithControlsEndsIt(string browser)
    {
        var page = await OpenAsync(browser, "/editable-stops");
        await Button(page, "Open player last").ClickAsync();
        await Expect(Button(page, "Before")).ToBeFocusedAsync();

        await page.Keyboard.PressAsync("Shift+Tab");

        await Expect(page.GetByTestId("player")).ToBeFocusedAsync();
    }

    // Presses the key until focus reaches the target, and counts the presses that left it on the player.
    private static async Task<int> PressPastThePlayerAsync(IPage page, string key, ILocator target)
    {
        for (var presses = 0; presses < 12; presses++)
        {
            await page.Keyboard.PressAsync(key);
            if (await target.EvaluateAsync<bool>("element => element === document.activeElement"))
            {
                return presses;
            }

            await Expect(page.GetByTestId("player")).ToBeFocusedAsync();
        }

        throw new InvalidOperationException($"{key} never reached the target.");
    }

    // Makes everything outside the scope inert, as a blocking overlay does, so the page has nowhere else to Tab to.
    private static Task BlockThePageAsync(IPage page) => page.GetByTestId("player").EvaluateAsync("""
        player => {
            for (let node = player.parentElement; node.parentElement; node = node.parentElement) {
                for (const sibling of node.parentElement.children) {
                    sibling.inert = sibling !== node;
                }
            }
        }
        """);

    public static TheoryData<string, bool> BrowsersWithAndWithoutABlockedPage => new(
        from browser in AllBrowsers.Names
        from blocked in new[] { false, true }
        select (browser, blocked));

    // WebKit gives a player no controls of its own to Tab through, so focus goes straight past it there.
    [Theory, MemberData(nameof(BrowsersWithAndWithoutABlockedPage))]
    public async Task FocusScope_ShouldTabThroughThePlayersControlsBeforeWrapping_WhenAPlayerEndsIt(string browser,
        bool blocked)
    {
        var page = await OpenAsync(browser, "/editable-stops");
        await Button(page, "Open player last").ClickAsync();
        await Expect(Button(page, "Before")).ToBeFocusedAsync();
        if (blocked)
        {
            await BlockThePageAsync(page);
        }

        await page.Keyboard.PressAsync("Shift+Tab");
        await Expect(page.GetByTestId("player")).ToBeFocusedAsync();

        var stays = await PressPastThePlayerAsync(page, "Tab", Button(page, "Before"));

        Assert.True(stays > 0 || browser == "webkit",
            "Tab wrapped straight off the player without going through its controls.");
    }

    [Theory, MemberData(nameof(BrowsersWithAndWithoutABlockedPage))]
    public async Task FocusScope_ShouldShiftTabThroughThePlayersControlsBeforeWrapping_WhenAPlayerStartsIt(string browser,
        bool blocked)
    {
        var page = await OpenAsync(browser, "/editable-stops");
        await Button(page, "Open player first").ClickAsync();
        await Expect(page.GetByTestId("player")).ToBeFocusedAsync();
        if (blocked)
        {
            await BlockThePageAsync(page);
        }

        await Button(page, "After").FocusAsync();
        await page.Keyboard.PressAsync("Shift+Tab");
        await Expect(page.GetByTestId("player")).ToBeFocusedAsync();

        var stays = await PressPastThePlayerAsync(page, "Shift+Tab", Button(page, "After"));

        Assert.True(stays > 0 || browser == "webkit",
            "Shift+Tab wrapped straight off the player without going through its controls.");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Focus_ShouldReturnToThePlayer_WhenMovedOutsideFromThePlayersControls(string browser)
    {
        Assert.SkipWhen(browser == "webkit", "WebKit gives a player no controls of its own to Tab into.");
        var page = await OpenAsync(browser, "/editable-stops");
        await Button(page, "Open player last").ClickAsync();
        await Expect(Button(page, "Before")).ToBeFocusedAsync();
        await page.Keyboard.PressAsync("Shift+Tab");
        await page.Keyboard.PressAsync("Tab");
        await Expect(page.GetByTestId("player")).ToBeFocusedAsync();

        await Button(page, "Open editable last").EvaluateAsync("button => button.focus()");

        await Expect(page.GetByTestId("player")).ToBeFocusedAsync();
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Dialog_ShouldStartOnTheEditableRegion_WhenItComesFirst(string browser)
    {
        var page = await OpenAsync(browser, "/editable-stops");

        await Button(page, "Open editable first").ClickAsync();

        await Expect(page.GetByTestId("draft")).ToBeFocusedAsync();
    }
}
