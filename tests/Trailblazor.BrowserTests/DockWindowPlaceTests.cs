using Trailblazor.Testing.Playwright;

namespace Trailblazor.BrowserTests;

public class DockWindowPlaceTests(BrowserFixture fixture) : BrowserTest(fixture)
{
    // Notes what the page asks of the browser for each window it opens: the window's size and place.
    private const string NoteAsked = """
        () => {
            window.asked = [];
            const open = window.open;
            window.open = (address, name, features) => {
                window.asked.push(features);
                return open.call(window, address, name, features);
            };
        }
        """;

    private const string Box = "() => [innerWidth, innerHeight, screenX, screenY]";

    private static ILocator Workbench(IPage page) => page.GetByRole(AriaRole.Group, new() { Name = "Workbench" });

    private async Task<IPage> OpenDockAsync(string browser, string query = "")
    {
        var page = await OpenAsync(browser, $"/dock-pop-out{query}");
        await Expect(Workbench(page)).ToHaveAttributeAsync("data-ready", "");
        await page.EvaluateAsync(NoteAsked);
        return page;
    }

    private static async Task<IPage> PopOutAsync(IPage page, string panel) =>
        (await page.Tb().Dock("Workbench").PopOutAsync(panel)).Page;

    private static async Task<string> AskedAsync(IPage page, int window) =>
        (await page.EvaluateAsync<string[]>("() => window.asked"))[window];

    // A window hears of its new size with its next frame, and the dock notes it then.
    private static Task FrameAsync(IPage window) => window.EvaluateAsync(
        "() => new Promise(done => requestAnimationFrame(() => requestAnimationFrame(done)))");

    private static string Asking(int[] box) => $"popup,width={box[0]},height={box[1]},left={box[2]},top={box[3]}";

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Window_ShouldOpenWhereThePanelsLastOneWas_WhenThePanelOpensInAWindowAgain(string browser)
    {
        var page = await OpenDockAsync(browser);
        var window = await PopOutAsync(page, "Notes");
        await window.SetViewportSizeAsync(520, 410);
        var box = await window.EvaluateAsync<int[]>(Box);
        await FrameAsync(window);

        await window.GetByRole(AriaRole.Button, new() { Name = "Return to the dock" }).ClickAsync();
        await Expect(page.GetByRole(AriaRole.Tab, new() { Name = "Notes" })).ToBeVisibleAsync();
        var again = await PopOutAsync(page, "Notes");

        // The first window opened over the dock, and the second where the first was when the panel left it.
        Assert.Equal([520, 410], box[..2]);
        Assert.NotEqual(Asking(box), await AskedAsync(page, 0));
        Assert.Equal(Asking(box), await AskedAsync(page, 1));

        // A window the user closes is remembered just like one left by its Return button.
        await again.SetViewportSizeAsync(610, 380);
        box = await again.EvaluateAsync<int[]>(Box);
        await FrameAsync(again);
        await again.CloseAsync();
        await Expect(page.GetByRole(AriaRole.Tab, new() { Name = "Notes" })).ToBeVisibleAsync();
        await PopOutAsync(page, "Notes");

        Assert.Equal(Asking(box), await AskedAsync(page, 2));

        // Each panel has its own place: one that has had no window yet opens over the dock, at half the dock's size.
        await PopOutAsync(page, "Gear");

        Assert.StartsWith("popup,width=384,height=240,", await AskedAsync(page, 3));
    }

    // Chromium moves a window when a script asks it to, and reports where the window is.
    [Fact]
    public async Task Window_ShouldOpenWhereItWasMovedTo_WhenItIsClosedThere()
    {
        var page = await OpenDockAsync("chromium");
        var window = await PopOutAsync(page, "Notes");
        var before = await window.EvaluateAsync<int[]>(Box);

        await window.EvaluateAsync("() => window.moveTo(screenX + 60, screenY + 40)");
        await window.WaitForFunctionAsync("left => screenX === left", before[2] + 60);
        var box = await window.EvaluateAsync<int[]>(Box);
        // The dock notes a window's place from time to time, and only what it keeps says that it has.
        await page.WaitForFunctionAsync(
            "left => JSON.parse(sessionStorage.getItem('tb-dock-window::notes'))?.left === left", box[2]);
        await window.CloseAsync();
        await Expect(page.GetByRole(AriaRole.Tab, new() { Name = "Notes" })).ToBeVisibleAsync();
        var again = await PopOutAsync(page, "Notes");

        Assert.Equal(Asking(box), await AskedAsync(page, 1));
        Assert.Equal(box, await again.EvaluateAsync<int[]>(Box));
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Window_ShouldOpenWhereThePanelsLastOneWas_WhenOpenedFromATabWhosePanelHasNotShownYet(
        string browser)
    {
        var page = await OpenDockAsync(browser);
        var window = await PopOutAsync(page, "Map");
        await window.SetViewportSizeAsync(520, 410);
        var box = await window.EvaluateAsync<int[]>(Box);
        await FrameAsync(window);
        await window.GetByRole(AriaRole.Button, new() { Name = "Return to the dock" }).ClickAsync();
        await Expect(page.GetByRole(AriaRole.Tab, new() { Name = "Map", Exact = true })).ToBeVisibleAsync();

        // A panel is first drawn when it first shows, so after a reload the map's tab is all there is of it.
        await page.ReloadAsync();
        await Expect(Workbench(page)).ToHaveAttributeAsync("data-ready", "");
        await page.EvaluateAsync(NoteAsked);
        await Expect(page.Locator("[data-part=panel][data-panel=map]")).ToHaveCountAsync(0);
        var again = await page.RunAndWaitForPopupAsync(() =>
            page.Tb().Dock("Workbench").ChooseFromTabMenuAsync("Map", "Open in new window"));
        await again.Locator("[data-part=panel][data-popped]").WaitForAsync();

        Assert.Equal(Asking(box), await AskedAsync(page, 0));
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Window_ShouldOpenWhereItWasLastInView_WhenItsPanelLeavesItOutOfView(string browser)
    {
        var page = await OpenDockAsync(browser);
        var window = await PopOutAsync(page, "Notes");
        await window.SetViewportSizeAsync(520, 410);
        var box = await window.EvaluateAsync<int[]>(Box);
        await FrameAsync(window);

        // A minimized window says it is out of view, and may say it is another size or somewhere else.
        await window.EvaluateAsync("() => Object.defineProperty(document, 'visibilityState', { get: () => 'hidden' })");
        await window.SetViewportSizeAsync(610, 380);
        await FrameAsync(window);
        await window.GetByRole(AriaRole.Button, new() { Name = "Return to the dock" }).ClickAsync();
        await Expect(page.GetByRole(AriaRole.Tab, new() { Name = "Notes" })).ToBeVisibleAsync();
        await PopOutAsync(page, "Notes");

        Assert.Equal(Asking(box), await AskedAsync(page, 1));
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Window_ShouldOpenWhereItWasInAnEarlierSession_WhenTheDockHasAPersistenceKey(string browser)
    {
        foreach (var (query, kept) in new[] { ("?key=trip", true), ("", false) })
        {
            var page = await OpenDockAsync(browser, query);
            var window = await PopOutAsync(page, "Notes");
            await window.SetViewportSizeAsync(520, 410);
            var box = await window.EvaluateAsync<int[]>(Box);
            await FrameAsync(window);
            await window.GetByRole(AriaRole.Button, new() { Name = "Return to the dock" }).ClickAsync();
            await Expect(page.GetByRole(AriaRole.Tab, new() { Name = "Notes" })).ToBeVisibleAsync();

            // Another tab shares what the browser keeps for the site, and not what it keeps for the session.
            var later = await page.Context.NewPageAsync();
            await later.GotoAsync(page.Url);
            await Expect(Workbench(later)).ToHaveAttributeAsync("data-ready", "");
            await later.EvaluateAsync(NoteAsked);
            await PopOutAsync(later, "Notes");

            Assert.Equal(kept, Asking(box) == await AskedAsync(later, 0));
            await page.Context.CloseAsync();
        }
    }
}
