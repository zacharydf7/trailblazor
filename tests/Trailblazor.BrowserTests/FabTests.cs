namespace Trailblazor.BrowserTests;

public class FabTests(BrowserFixture fixture) : BrowserTest(fixture)
{
    private static ILocator Compose(IPage page) => page.GetByRole(AriaRole.Button, new() { Name = "Compose" });

    private static async Task<ILocator> DialAsync(IPage page)
    {
        var dial = page.GetByRole(AriaRole.Button, new() { Name = "Add" });
        await Expect(dial).ToHaveAttributeAsync("data-ready", "");
        return dial;
    }

    // How far the element's edges sit from the window's left, right and bottom edges, inside its scrollbars.
    private static Task<double[]> GapsAsync(ILocator element) => element.EvaluateAsync<double[]>("""
        element => {
            const box = element.getBoundingClientRect();
            const { clientWidth, clientHeight } = document.documentElement;
            return [box.left, clientWidth - box.right, clientHeight - box.bottom];
        }
        """);

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Fab_ShouldKeepTheWindowsCornerAndRunItsAction_WhenThePageScrolls(string browser)
    {
        var page = await OpenAsync(browser, "/fab");
        var before = await GapsAsync(Compose(page));

        await page.Mouse.WheelAsync(0, 2000);
        await page.WaitForFunctionAsync("() => scrollY > 0");
        await Compose(page).ClickAsync();

        Assert.Equal(before, await GapsAsync(Compose(page)));
        Assert.Equal([16, 16], before[1..]);
        await Expect(page.GetByLabel("Last action")).ToHaveTextAsync("Composing.");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Fab_ShouldSitOnTheLeft_WhenThePageIsRightToLeft(string browser)
    {
        var page = await OpenAsync(browser, "/fab");

        await page.EvaluateAsync("() => document.documentElement.dir = 'rtl'");

        // A scrollbar on the left in right-to-left text sits between the window's edge and the button.
        Assert.InRange((await GapsAsync(Compose(page)))[0], 16, 40);
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task SpeedDial_ShouldOpenItsActionsAboveTheTriggerCenteredOverIt_WhenClicked(string browser)
    {
        var page = await OpenAsync(browser, "/fab");
        var dial = await DialAsync(page);

        await dial.ClickAsync();

        var actions = page.GetByRole(AriaRole.Menuitem);
        await Expect(actions).ToHaveCountAsync(3);
        await Expect(actions.First).ToBeFocusedAsync();
        await Expect(dial.Locator("[data-part=icon]")).ToHaveCSSAsync("rotate", "45deg");
        await WaitForFadesAsync(page);
        var trigger = (await dial.BoundingBoxAsync())!;
        foreach (var icon in await page.Locator("[role=menuitem] [data-part=icon]").AllAsync())
        {
            var box = (await icon.BoundingBoxAsync())!;
            Assert.Equal(trigger.X + trigger.Width / 2, box.X + box.Width / 2, tolerance: 1);
            Assert.True(box.Y + box.Height <= trigger.Y, "Each action opens above the trigger.");
        }
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task SpeedDial_ShouldTakeTheMenuButtonsKeys_WhenUsedFromTheKeyboard(string browser)
    {
        var page = await OpenAsync(browser, "/fab");
        var dial = await DialAsync(page);
        var actions = page.GetByRole(AriaRole.Menuitem);

        await dial.FocusAsync();
        await page.Keyboard.PressAsync("ArrowUp");
        await Expect(actions.Last).ToBeFocusedAsync();
        await page.Keyboard.PressAsync("ArrowDown");
        await Expect(actions.First).ToBeFocusedAsync();
        await page.Keyboard.PressAsync("End");
        await Expect(actions.Last).ToBeFocusedAsync();
        await page.Keyboard.PressAsync("Home");
        await Expect(actions.First).ToBeFocusedAsync();
        await page.Keyboard.PressAsync("Escape");
        await Expect(dial).ToBeFocusedAsync();
        await Expect(dial).ToHaveAttributeAsync("aria-expanded", "false");

        await page.Keyboard.PressAsync("Enter");
        await Expect(actions.First).ToBeFocusedAsync();
        await page.Keyboard.PressAsync("ArrowDown");
        await page.Keyboard.PressAsync("Enter");

        await Expect(page.GetByLabel("Last action")).ToHaveTextAsync("Added a folder.");
        await Expect(dial).ToBeFocusedAsync();
        await Expect(dial).ToHaveAttributeAsync("aria-expanded", "false");
    }
}
