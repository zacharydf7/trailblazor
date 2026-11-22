using System.Text.RegularExpressions;
using Deque.AxeCore.Playwright;
using Trailblazor.Testing.Playwright;

namespace Trailblazor.BrowserTests;

public class DockConnectionTests(BrowserFixture fixture) : BrowserTest(fixture)
{
    private const string Lost = "The page lost its connection to the server. This panel works again when the page "
        + "reconnects.";

    // What an app that handles reconnecting itself sends to tell the dock of each state.
    private const string Tell = "state => document.dispatchEvent("
        + "new CustomEvent('components-reconnect-state-changed', { detail: { state } }))";

    private readonly List<IWebSocketRoute> _links = [];
    private bool _down;

    // The page's link to its circuit goes through the test, which can cut it and refuse a new one.
    protected override async Task PreparePageAsync(IPage page)
    {
        await page.RouteWebSocketAsync(new Regex("/_blazor"), link =>
        {
            if (_down)
            {
                _ = link.CloseAsync();
                return;
            }

            _links.Add(link);
            _links.Add(link.ConnectToServer());
        });
        // Without its WebSocket, the page would fall back to long polling.
        await page.RouteAsync(new Regex("/_blazor/negotiate"),
            route => _down ? route.AbortAsync() : route.ContinueAsync());
    }

    private async Task DropAsync()
    {
        _down = true;
        foreach (var link in _links.ToList())
        {
            await link.CloseAsync();
        }

        _links.Clear();
    }

    private async Task<IPage> OpenDockAsync(string browser, string query = "")
    {
        var page = await OpenAsync(browser, $"/dock-pop-out{query}");
        await ReadyAsync(page);
        return page;
    }

    private static Task ReadyAsync(IPage page) =>
        Expect(page.GetByRole(AriaRole.Group, new() { Name = "Workbench" })).ToHaveAttributeAsync("data-ready", "");

    private static async Task<IPage> PopOutNotesAsync(IPage page) =>
        (await page.Tb().Dock("Workbench").PopOutAsync("Notes")).Page;

    private static ILocator Notice(IPage window) => window.Locator("[data-part=connection-lost]");

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Window_ShouldSayTheConnectionIsLostAndTakeNoInput_WhenThePagesCircuitDrops(string browser)
    {
        var window = await PopOutNotesAsync(await OpenDockAsync(browser));
        await window.GetByRole(AriaRole.Button, new() { Name = "Count", Exact = true }).ClickAsync();
        await Expect(window.GetByTestId("count")).ToHaveTextAsync("Count: 1");
        await window.GetByLabel("Note", new() { Exact = true }).FocusAsync();

        await DropAsync();

        // The window says what the page's reconnect UI says there, and what it holds takes no input meanwhile.
        await Expect(Notice(window)).ToHaveTextAsync(Lost);
        await Expect(Notice(window)).ToHaveRoleAsync(AriaRole.Alert);
        await Expect(window.Locator("[data-part=panel]")).ToHaveJSPropertyAsync("inert", true);

        _down = false;

        // The page tries again by itself, at most a few seconds apart, and the panel works as before, with the focus
        // where it was.
        await Expect(Notice(window)).ToHaveCountAsync(0, new() { Timeout = 30000 });
        await Expect(window.Locator("[data-part=panel]")).ToHaveJSPropertyAsync("inert", false);
        await FocusedAsync(window.GetByLabel("Note", new() { Exact = true }));
        await window.GetByRole(AriaRole.Button, new() { Name = "Count", Exact = true }).ClickAsync();
        await Expect(window.GetByTestId("count")).ToHaveTextAsync("Count: 2");
    }

    // Blazor leaves its reconnect UI in the page once it was needed, so a dock that comes later finds it there.
    [Fact]
    public async Task Window_ShouldSayTheConnectionIsLost_WhenAnEarlierLossCameBeforeTheDockWasOnThePage()
    {
        var page = await OpenAsync("chromium", "/rich-text-mentions");
        // The editor can be typed in once its script runs, which the page's circuit starts.
        await Expect(page.GetByRole(AriaRole.Textbox).First).ToHaveAttributeAsync("contenteditable", "true");
        await DropAsync();
        await Expect(page.Locator("#components-reconnect-modal")).ToHaveCSSAsync("display", "block");
        _down = false;
        await Expect(page.Locator("#components-reconnect-modal"))
            .ToHaveCSSAsync("display", "none", new() { Timeout = 30000 });

        await page.EvaluateAsync("Blazor.navigateTo('dock-pop-out')");
        await ReadyAsync(page);
        var window = await PopOutNotesAsync(page);
        await DropAsync();

        await Expect(Notice(window)).ToHaveTextAsync(Lost);
        _down = false;
        await Expect(Notice(window)).ToHaveCountAsync(0, new() { Timeout = 30000 });
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Window_ShouldSayTheConnectionIsLost_WhenAnAppsOwnReconnectUiIsToldSo(string browser)
    {
        var page = await OpenDockAsync(browser);
        var window = await PopOutNotesAsync(page);
        // An app's reconnect UI is an element the framework tells of each state; the event doesn't bubble.
        const string TellUi = """
            state => {
                const ui = document.getElementById('own') ?? document.body.appendChild(document.createElement('div'));
                ui.id = 'own';
                ui.dispatchEvent(new CustomEvent('components-reconnect-state-changed', { detail: { state } }));
            }
            """;

        await page.EvaluateAsync(TellUi, "show");

        await Expect(Notice(window)).ToHaveTextAsync(Lost);
        var scan = await window.RunAxe(AxeTests.WcagAA);
        Assert.Empty(scan.Violations.Select(violation =>
            $"{violation.Id}: {string.Join(", ", violation.Nodes.Select(node => node.Target))}"));

        // Every state but `hide` means the connection is still lost.
        await page.EvaluateAsync(TellUi, "retrying");
        await Expect(Notice(window)).ToHaveCountAsync(1);
        await page.EvaluateAsync(TellUi, "hide");

        await Expect(Notice(window)).ToHaveCountAsync(0);
        await Expect(window.Locator("[data-part=panel]")).ToHaveJSPropertyAsync("inert", false);
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Window_ShouldSayTheConnectionIsLost_WhenItsPanelArrivesWhileItIs(string browser)
    {
        var page = await OpenDockAsync(browser);
        await page.EvaluateAsync(Tell, "show");

        var window = await PopOutNotesAsync(page);

        await Expect(Notice(window)).ToHaveTextAsync(Lost);
        await page.EvaluateAsync(Tell, "hide");
        await Expect(Notice(window)).ToHaveCountAsync(0);
        // The panel has the focus a window's panel gets when it arrives.
        await FocusedAsync(window.Locator("[data-part=panel]"));
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Window_ShouldSayNothingOfTheConnection_WhenItsDockRunsInWebAssembly(string browser)
    {
        var page = await OpenDockAsync(browser, "?mode=webassembly");
        var window = await PopOutNotesAsync(page);

        // The dock hears of the state as the event is sent, so a notice would be there by the time this returns.
        await page.EvaluateAsync(Tell, "show");

        await Expect(Notice(window)).ToHaveCountAsync(0);
        await Expect(window.Locator("[data-part=panel]")).ToHaveJSPropertyAsync("inert", false);
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Panel_ShouldShowInTheDockAgain_WhenItsWindowClosesWhileTheCircuitIsDown(string browser)
    {
        var page = await OpenDockAsync(browser);
        var window = await PopOutNotesAsync(page);
        await DropAsync();
        await Expect(Notice(window)).ToHaveCountAsync(1);

        await window.CloseAsync();
        _down = false;

        // The dock couldn't be told while the circuit was down, and hears of the return once it is back.
        await Expect(page.GetByRole(AriaRole.Tabpanel, new() { Name = "Notes" }))
            .ToBeVisibleAsync(new() { Timeout = 30000 });
        Assert.Empty(await page.Tb().Dock("Workbench").PoppedOutAsync());
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Panel_ShouldComeBackTakingInput_WhenItsWindowClosesWhileTheConnectionIsLost(string browser)
    {
        var page = await OpenDockAsync(browser);
        var window = await PopOutNotesAsync(page);
        await page.EvaluateAsync(Tell, "show");
        await Expect(Notice(window)).ToHaveCountAsync(1);

        await window.CloseAsync();

        await Expect(page.Locator("[data-part=panel][data-panel=notes]")).ToHaveJSPropertyAsync("inert", false);
    }
}
