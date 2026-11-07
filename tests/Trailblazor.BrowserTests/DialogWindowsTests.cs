using Trailblazor.Testing.Playwright;

namespace Trailblazor.BrowserTests;

public class DialogWindowsTests(BrowserFixture fixture) : BrowserTest(fixture)
{
    private async Task<IPage> OpenBothAsync(string browser, string path = "/dialog-windows")
    {
        var page = await OpenAsync(browser, path);
        await page.GetByRole(AriaRole.Button, new() { Name = "Open back" }).ClickAsync();
        await Expect(Dialog(page, "Back")).ToBeVisibleAsync();
        await page.GetByRole(AriaRole.Button, new() { Name = "Open front" }).ClickAsync();
        await Expect(Dialog(page, "Front")).ToBeVisibleAsync();
        return page;
    }

    private static ILocator Dialog(IPage page, string title) =>
        page.GetByRole(AriaRole.Dialog, new() { Name = title, Exact = true });

    private static ILocator Field(IPage page) => page.GetByLabel("Back field", new() { Exact = true });

    private static ILocator Count(IPage page, string name) =>
        page.GetByRole(AriaRole.Status, new() { Name = name, Exact = true });

    private static ILocator Header(IPage page, string title) => Dialog(page, title).Locator("[data-part=header]").First;

    // Presses the back window's header at its start, or the front window's at its end, away from where they overlap.
    // The header cancels a press's default action, so focus stays where it is.
    private static async Task PressHeaderAsync(IPage page, string title)
    {
        var header = (await Header(page, title).BoundingBoxAsync())!;
        var x = title == "Front" ? header.X + header.Width - 4 : header.X + 6;
        await page.Mouse.ClickAsync(x, header.Y + header.Height / 2);
    }

    // Slides the front window off the back window's end, so each window has a part left to press.
    private static async Task<(double X, double Y)> SpreadAsync(IPage page)
    {
        var back = (await Dialog(page, "Back").BoundingBoxAsync())!;
        var front = (await Header(page, "Front").BoundingBoxAsync())!;
        await page.Mouse.MoveAsync(front.X + 6, front.Y + front.Height / 2);
        await page.Mouse.DownAsync();
        await page.Mouse.MoveAsync(back.X + back.Width - 34, front.Y + front.Height / 2, new() { Steps = 4 });
        await page.Mouse.UpAsync();

        // Firefox can still be handling the last move when the release returns.
        await page.WaitForFunctionAsync("([dialog, end]) => dialog.getBoundingClientRect().right > end",
            new object[] { await Dialog(page, "Front").ElementHandleAsync(), (double)back.X + back.Width });
        var moved = (await Dialog(page, "Front").BoundingBoxAsync())!;
        return ((moved.X + back.X + back.Width) / 2, moved.Y + moved.Height / 2);
    }

    // Waits until the element is what's drawn at its own middle, since it may still be moving into place.
    private static async Task ExpectOnTopAsync(IPage page, ILocator element)
    {
        await page.WaitForFunctionAsync("""
            element => {
                const box = element.getBoundingClientRect();
                return element.contains(document.elementFromPoint(box.x + box.width / 2, box.y + box.height / 2));
            }
            """, await element.ElementHandleAsync(), new() { Timeout = 3000 });
    }

    // Page-layer dialogs compare by level, since a modal makes the others inert and hit testing passes over them.
    private static async Task<int> LevelAsync(IPage page, string title) =>
        int.Parse(await Dialog(page, title).EvaluateAsync<string>("dialog => getComputedStyle(dialog).zIndex"));

    // The title of the window drawn at a point.
    private static Task<string?> WindowAtAsync(IPage page, (double X, double Y) point) =>
        page.EvaluateAsync<string?>("""
            ([x, y]) => document.elementFromPoint(x, y).closest('dialog')?.querySelector('h1, h2, h3, h4')?.textContent.trim()
            """, new[] { point.X, point.Y });

    // The title of the window drawn at the middle of a window that the others overlap.
    private static async Task<string?> WindowOverAsync(IPage page, string title)
    {
        var box = (await Dialog(page, title).BoundingBoxAsync())!;
        return await WindowAtAsync(page, (box.X + box.Width / 2, box.Y + box.Height / 2));
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Window_ShouldComeToTheFront_WhenPressed(string browser)
    {
        var page = await OpenBothAsync(browser);
        Assert.Equal("Front", await WindowOverAsync(page, "Front"));

        await PressHeaderAsync(page, "Back");

        Assert.Equal("Back", await WindowOverAsync(page, "Front"));
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Window_ShouldComeToTheFront_WhenFocusMovesIntoIt(string browser)
    {
        var page = await OpenBothAsync(browser);

        await Field(page).FocusAsync();

        Assert.Equal("Back", await WindowOverAsync(page, "Front"));
        await Expect(Field(page)).ToBeFocusedAsync();
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Window_ShouldKeepFocusAndRaiseNoFieldEvents_WhenWindowsSwapPlaces(string browser)
    {
        var page = await OpenBothAsync(browser);
        var overlap = await SpreadAsync(page);
        await Field(page).FocusAsync();
        await Expect(Count(page, "Focuses")).ToHaveTextAsync("1");
        Assert.Equal("Back", await WindowAtAsync(page, overlap));

        await PressHeaderAsync(page, "Front");
        Assert.Equal("Front", await WindowAtAsync(page, overlap));
        await PressHeaderAsync(page, "Back");
        Assert.Equal("Back", await WindowAtAsync(page, overlap));
        await PressHeaderAsync(page, "Front");
        Assert.Equal("Front", await WindowAtAsync(page, overlap));
        await page.Keyboard.TypeAsync("x");

        await Expect(Field(page)).ToBeFocusedAsync();
        await Expect(Field(page)).ToHaveValueAsync("x");
        await Expect(Count(page, "Focuses")).ToHaveTextAsync("1");
        await Expect(Count(page, "Blurs")).ToHaveTextAsync("0");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Window_ShouldComeToTheFrontAndKeepFocusElsewhere_WhenItsGripIsDragged(string browser)
    {
        var page = await OpenBothAsync(browser);
        var overlap = await SpreadAsync(page);
        await Field(page).FocusAsync();
        Assert.Equal("Back", await WindowAtAsync(page, overlap));

        var grip = (await Dialog(page, "Front").Locator("[data-part=resize] [data-part=trigger]").BoundingBoxAsync())!;
        await page.Mouse.MoveAsync(grip.X + grip.Width / 2, grip.Y + grip.Height / 2);
        await page.Mouse.DownAsync();
        await page.Mouse.MoveAsync(grip.X + grip.Width / 2 + 12, grip.Y + grip.Height / 2 + 12, new() { Steps = 3 });
        await page.Mouse.UpAsync();

        Assert.Equal("Front", await WindowAtAsync(page, overlap));
        await Expect(Field(page)).ToBeFocusedAsync();
        await Expect(Count(page, "Blurs")).ToHaveTextAsync("0");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Window_ShouldStillTakeTheClick_WhenTheClickBringsItForward(string browser)
    {
        var page = await OpenBothAsync(browser);
        await SpreadAsync(page);

        await Dialog(page, "Back").GetByRole(AriaRole.Button, new() { Name = "Open inner" }).ClickAsync();

        await Expect(Dialog(page, "Inner")).ToBeVisibleAsync();
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Window_ShouldFocusTheFieldOnce_WhenClickingItBringsItForward(string browser)
    {
        var page = await OpenBothAsync(browser);
        var overlap = await SpreadAsync(page);

        await Field(page).ClickAsync();
        await page.Keyboard.TypeAsync("x");

        Assert.Equal("Back", await WindowAtAsync(page, overlap));
        await Expect(Field(page)).ToHaveValueAsync("x");
        await Expect(Count(page, "Focuses")).ToHaveTextAsync("1");
        await Expect(Count(page, "Blurs")).ToHaveTextAsync("0");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Window_ShouldKeepItsNestedWindowAboveIt_WhenPressed(string browser)
    {
        var page = await OpenAsync(browser, "/dialog-windows");
        await page.GetByRole(AriaRole.Button, new() { Name = "Open back" }).ClickAsync();
        await Dialog(page, "Back").GetByRole(AriaRole.Button, new() { Name = "Open inner" }).ClickAsync();
        await Expect(Dialog(page, "Inner")).ToBeVisibleAsync();
        await page.GetByRole(AriaRole.Button, new() { Name = "Open front" }).EvaluateAsync("button => button.click()");
        await Expect(Dialog(page, "Front")).ToBeVisibleAsync();
        Assert.Equal("Front", await WindowOverAsync(page, "Inner"));

        await PressHeaderAsync(page, "Back");

        Assert.Equal("Inner", await WindowOverAsync(page, "Inner"));
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Window_ShouldComeToTheFront_WhenFocusMovesIntoAWindowNestedInIt(string browser)
    {
        var page = await OpenBothAsync(browser);
        var overlap = await SpreadAsync(page);
        Assert.Equal("Front", await WindowAtAsync(page, overlap));

        await Dialog(page, "Back").GetByRole(AriaRole.Button, new() { Name = "Open inner" }).EvaluateAsync("button => button.click()");
        await Expect(Dialog(page, "Inner")).ToBeVisibleAsync();

        Assert.Equal("Back", await WindowAtAsync(page, overlap));
    }

    // An inert window is passed over by hit testing, so this checks that the window wasn't moved above its modal.
    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Window_ShouldStayBehind_WhenAModalOpensInIt(string browser)
    {
        var page = await OpenBothAsync(browser);
        await Dialog(page, "Back").EvaluateAsync(
            "dialog => dialog.addEventListener('beforetoggle', () => window.shownAgain = true)");

        await page.GetByRole(AriaRole.Button, new() { Name = "Open held" }).EvaluateAsync("button => button.click()");
        await Expect(Dialog(page, "Held")).ToBeVisibleAsync();

        Assert.False(await page.EvaluateAsync<bool>("() => !!window.shownAgain"));
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Tooltip_ShouldStayAboveItsWindow_WhenTheWindowComesToTheFront(string browser)
    {
        var page = await OpenBothAsync(browser);
        await SpreadAsync(page);
        await Field(page).FocusAsync();
        await page.Keyboard.PressAsync("Tab");
        var tooltip = page.GetByRole(AriaRole.Tooltip);
        await Expect(tooltip).ToBeVisibleAsync();
        await PressHeaderAsync(page, "Front");

        await PressHeaderAsync(page, "Back");

        await ExpectOnTopAsync(page, tooltip);
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Popover_ShouldStayOnTop_WhenOpenFromAWindowBehindAnother(string browser)
    {
        var page = await OpenBothAsync(browser);

        await page.GetByRole(AriaRole.Button, new() { Name = "Open share" }).EvaluateAsync("button => button.click()");

        var panel = page.GetByText("Share words");
        await Expect(panel).ToBeFocusedAsync();
        // Until the panel lies over its window, the window can't hide it and the on-top check would prove nothing.
        await page.WaitForFunctionAsync("""
            ([panel, window]) => {
                const box = panel.getBoundingClientRect();
                const area = window.getBoundingClientRect();
                return box.y > area.y && box.bottom < area.bottom;
            }
            """, new object[] { await panel.ElementHandleAsync(), await Dialog(page, "Back").ElementHandleAsync() });
        await ExpectOnTopAsync(page, panel);

        var header = (await Header(page, "Back").BoundingBoxAsync())!;
        await page.Mouse.MoveAsync(header.X + 6, header.Y + header.Height / 2);
        await page.Mouse.DownAsync();

        await ExpectOnTopAsync(page, panel);
        await page.Mouse.UpAsync();
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Window_ShouldNotBeShownAgain_WhenItIsAlreadyInFront(string browser)
    {
        var page = await OpenAsync(browser, "/dialog-windows");
        await page.GetByRole(AriaRole.Button, new() { Name = "Open back" }).ClickAsync();
        await Expect(Dialog(page, "Back")).ToBeVisibleAsync();
        await Dialog(page, "Back").EvaluateAsync(
            "dialog => dialog.addEventListener('beforetoggle', () => window.shownAgain = true)");

        await PressHeaderAsync(page, "Back");
        await Field(page).FocusAsync();

        Assert.False(await page.EvaluateAsync<bool>("() => !!window.shownAgain"));
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Toast_ShouldStayAboveAWindow_WhenTheWindowComesToTheFront(string browser)
    {
        var page = await OpenBothAsync(browser);
        await page.GetByRole(AriaRole.Button, new() { Name = "Show toast" }).EvaluateAsync("button => button.click()");
        var toast = page.Locator("[data-part=toaster]").GetByText("Saved");
        await Expect(toast).ToBeVisibleAsync();

        // The press brings the window forward, and the drag takes it under the toast.
        var place = (await toast.BoundingBoxAsync())!;
        var header = (await Header(page, "Back").BoundingBoxAsync())!;
        await page.Mouse.MoveAsync(header.X + 6, header.Y + header.Height / 2);
        await page.Mouse.DownAsync();
        await page.Mouse.MoveAsync(header.X + 6 + place.X, place.Y + place.Height / 2, new() { Steps = 4 });
        await page.Mouse.UpAsync();

        await page.WaitForFunctionAsync("""
            ([dialog, x, y]) => {
                const box = dialog.getBoundingClientRect();
                return box.right > x && box.top < y && box.left < x;
            }
            """, new object[] { await Dialog(page, "Back").ElementHandleAsync(), (double)place.X + place.Width / 2,
                (double)place.Y + place.Height / 2 });
        await ExpectOnTopAsync(page, toast);
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Window_ShouldComeToTheFront_WhenPressedOnThePageLayer(string browser)
    {
        var page = await OpenBothAsync(browser, "/dialog-windows?OnPage=true");
        Assert.Equal("Front", await WindowOverAsync(page, "Front"));

        await PressHeaderAsync(page, "Back");

        Assert.Equal("Back", await WindowOverAsync(page, "Front"));
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Window_ShouldOpenAboveARaisedWindow_WhenOnThePageLayer(string browser)
    {
        var page = await OpenBothAsync(browser, "/dialog-windows?OnPage=true");
        await PressHeaderAsync(page, "Back");

        await page.GetByRole(AriaRole.Button, new() { Name = "Open last" }).EvaluateAsync("button => button.click()");
        await Expect(Dialog(page, "Last")).ToBeVisibleAsync();

        Assert.Equal("Last", await WindowOverAsync(page, "Last"));
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Modal_ShouldOpenAboveARaisedWindow_WhenOnThePageLayer(string browser)
    {
        var page = await OpenBothAsync(browser, "/dialog-windows?OnPage=true");
        await PressHeaderAsync(page, "Back");
        Assert.Equal("Back", await WindowOverAsync(page, "Front"));

        await page.GetByRole(AriaRole.Button, new() { Name = "Open modal" }).EvaluateAsync("button => button.click()");
        await Expect(Dialog(page, "Modal")).ToBeVisibleAsync();

        Assert.True(await LevelAsync(page, "Modal") > await LevelAsync(page, "Back"));
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Modal_ShouldStayBelowALaterWindow_WhenPressedOnThePageLayer(string browser)
    {
        var page = await OpenAsync(browser, "/dialog-windows?OnPage=true");
        await page.GetByRole(AriaRole.Button, new() { Name = "Open modal" }).ClickAsync();
        await Expect(Dialog(page, "Modal")).ToBeVisibleAsync();
        await page.GetByRole(AriaRole.Button, new() { Name = "Open last" }).EvaluateAsync("button => button.click()");
        await Expect(Dialog(page, "Last")).ToBeVisibleAsync();

        // Slid aside, the later window leaves the modal's header free to press.
        var header = (await Header(page, "Last").BoundingBoxAsync())!;
        await page.Mouse.MoveAsync(header.X + 6, header.Y + header.Height / 2);
        await page.Mouse.DownAsync();
        await page.Mouse.MoveAsync(header.X + 6, header.Y + header.Height * 4, new() { Steps = 4 });
        await page.Mouse.UpAsync();
        await PressHeaderAsync(page, "Modal");

        Assert.True(await LevelAsync(page, "Modal") < await LevelAsync(page, "Last"));
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Modal_ShouldDrawAboveALaterWindow_WhenRestoredOnThePageLayer(string browser)
    {
        var page = await OpenAsync(browser, "/dialog-windows?OnPage=true");
        await page.GetByRole(AriaRole.Button, new() { Name = "Open modal" }).ClickAsync();
        await Dialog(page, "Modal").GetByRole(AriaRole.Button, new() { Name = "Minimize" }).ClickAsync();
        await Expect(Dialog(page, "Modal")).Not.ToHaveAttributeAsync("aria-modal", "true");
        await page.GetByRole(AriaRole.Button, new() { Name = "Open last" }).ClickAsync();
        await Expect(Dialog(page, "Last")).ToBeVisibleAsync();

        // From code, with no press in the dialog to bring it forward first.
        await page.GetByRole(AriaRole.Button, new() { Name = "Restore oldest" }).EvaluateAsync("button => button.click()");
        await Expect(Dialog(page, "Modal")).ToHaveAttributeAsync("aria-modal", "true");

        Assert.True(await LevelAsync(page, "Modal") > await LevelAsync(page, "Last"));
    }
}
