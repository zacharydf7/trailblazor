using System.Globalization;

namespace Trailblazor.BrowserTests;

public class BrowserServiceTests(BrowserFixture fixture) : BrowserTest(fixture)
{
    private const string Notes = "Pack water and a map.";

    // Stands in for the browser's save dialog, which a test can't drive, and keeps what was written.
    private const string SavePicker = """
        failure => {
            window.saved = null;
            window.showSaveFilePicker = async options => {
                if (failure) {
                    throw new DOMException('', failure);
                }

                window.suggestedName = options.suggestedName;
                const chunks = [];
                return {
                    createWritable: async () => new WritableStream({
                        write: chunk => { chunks.push(chunk); },
                        close: async () => { window.saved = await new Blob(chunks).text(); },
                    }),
                };
            };
        }
        """;

    private static ILocator Button(IPage page, string name) => page.GetByRole(AriaRole.Button, new() { Name = name });

    private static ILocator Result(IPage page) => page.GetByTestId("result");

    // WebKit on Windows pastes from the system's clipboard rather than the one its own writes go to.
    [Theory]
    [InlineData("chromium")]
    [InlineData("firefox")]
    public async Task CopyText_ShouldPutTheTextOnTheClipboard_WhenCalledFromAClick(string browser)
    {
        // Only Chromium asks for permission; Firefox allows writing after a click.
        var page = await OpenAsync(browser, "/browser", browser == "chromium"
            ? new() { Permissions = ["clipboard-read", "clipboard-write"] } : null);

        await Button(page, "Copy").ClickAsync();
        await Expect(Result(page)).ToHaveTextAsync("copied True");
        var paste = page.GetByRole(AriaRole.Textbox, new() { Name = "Paste here" });
        await paste.FocusAsync();
        await page.Keyboard.PressAsync("ControlOrMeta+V");

        await Expect(paste).ToHaveValueAsync(Notes);
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task CopyText_ShouldReturnFalse_WhenTheBrowserRefusesTheClipboard(string browser)
    {
        var page = await OpenAsync(browser, "/browser");
        await page.EvaluateAsync("""
            () => { navigator.clipboard.writeText = () => Promise.reject(new DOMException('', 'NotAllowedError')); }
            """);

        await Button(page, "Copy").ClickAsync();

        await Expect(Result(page)).ToHaveTextAsync("copied False");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Download_ShouldDownloadTheStreamUnderTheFileName_WhenCalled(string browser)
    {
        var page = await OpenAsync(browser, "/browser");

        var download = await page.RunAndWaitForDownloadAsync(() => Button(page, "Download").ClickAsync());

        Assert.Equal("notes.txt", download.SuggestedFilename);
        Assert.Equal(Notes, await File.ReadAllTextAsync(await download.PathAsync(),
            TestContext.Current.CancellationToken));
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Save_ShouldWriteToTheChosenFile_WhenTheBrowserHasASaveDialog(string browser)
    {
        var page = await OpenAsync(browser, "/browser");
        await page.EvaluateAsync(SavePicker, null);

        await Button(page, "Save").ClickAsync();

        await Expect(Result(page)).ToHaveTextAsync("saved True");
        Assert.Equal(Notes, await page.EvaluateAsync<string>("window.saved"));
        Assert.Equal("notes.txt", await page.EvaluateAsync<string>("window.suggestedName"));
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Save_ShouldReturnFalse_WhenTheUserCancelsTheSaveDialog(string browser)
    {
        var page = await OpenAsync(browser, "/browser");
        await page.EvaluateAsync(SavePicker, "AbortError");

        await Button(page, "Save").ClickAsync();

        await Expect(Result(page)).ToHaveTextAsync("saved False");
    }

    [Theory]
    [InlineData("chromium", "delete window.showSaveFilePicker")]
    [InlineData("firefox", "delete window.showSaveFilePicker")]
    [InlineData("webkit", "delete window.showSaveFilePicker")]
    [InlineData("chromium", "SecurityError")]
    public async Task Save_ShouldDownloadTheFile_WhenTheBrowserCantOpenASaveDialog(string browser, string setup)
    {
        var page = await OpenAsync(browser, "/browser");
        await (setup == "SecurityError" ? page.EvaluateAsync(SavePicker, setup) : page.EvaluateAsync(setup));

        var download = await page.RunAndWaitForDownloadAsync(() => Button(page, "Save").ClickAsync());

        Assert.Equal("notes.txt", download.SuggestedFilename);
        await Expect(Result(page)).ToHaveTextAsync("saved True");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task PickColor_ShouldReturnTheColor_WhenTheUserPicksOne(string browser)
    {
        var page = await OpenAsync(browser, "/browser");
        await page.EvaluateAsync("""
            () => { window.EyeDropper = class { open() { return Promise.resolve({ sRGBHex: '#336699' }); } }; }
            """);

        await Button(page, "Pick a color").ClickAsync();

        await Expect(Result(page)).ToHaveTextAsync("can pick True, picked #336699");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task PickColor_ShouldReturnNull_WhenTheUserCancels(string browser)
    {
        var page = await OpenAsync(browser, "/browser");
        await page.EvaluateAsync("""
            () => {
                window.EyeDropper = class { open() { return Promise.reject(new DOMException('', 'AbortError')); } };
            }
            """);

        await Button(page, "Pick a color").ClickAsync();

        await Expect(Result(page)).ToHaveTextAsync("can pick True, picked nothing");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task PickColor_ShouldReturnNull_WhenTheBrowserHasNoEyedropper(string browser)
    {
        var page = await OpenAsync(browser, "/browser");
        await page.EvaluateAsync("delete window.EyeDropper");

        await Button(page, "Pick a color").ClickAsync();

        await Expect(Result(page)).ToHaveTextAsync("can pick False, picked nothing");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task ScrollIntoView_ShouldBringTheElementIntoView_WhenCalled(string browser)
    {
        var page = await OpenAsync(browser, "/browser");
        var end = page.GetByText("The end", new() { Exact = true });
        await Expect(end).Not.ToBeInViewportAsync();

        await Button(page, "Scroll to the end").ClickAsync();

        await Expect(end).ToBeInViewportAsync();
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task ObserveViewport_ShouldReportTheSize_WhenTheViewportIsResized(string browser)
    {
        var page = await OpenAsync(browser, "/browser");
        await Expect(page.GetByTestId("viewport")).ToHaveTextAsync("1280x720 Xl");

        await page.SetViewportSizeAsync(700, 500);

        await Expect(page.GetByTestId("viewport")).ToHaveTextAsync("700x500 Sm");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task ObserveBreakpoint_ShouldChangeAtEachBreakpointToken_WhenTheViewportIsResized(string browser)
    {
        var page = await OpenAsync(browser, "/browser");
        var breakpoint = page.GetByTestId("breakpoint");
        var names = new[] { "sm", "md", "lg", "xl" };
        var widths = await page.EvaluateAsync<string[]>("""
            names => names.map(name =>
                getComputedStyle(document.documentElement).getPropertyValue(`--tb-breakpoint-${name}`))
            """, names);

        for (var index = 0; index < names.Length; index++)
        {
            var width = int.Parse(widths[index].Trim().TrimEnd('p', 'x'), CultureInfo.InvariantCulture);
            await page.SetViewportSizeAsync(width - 1, 600);
            await Expect(breakpoint).ToHaveTextAsync(((Breakpoint)index).ToString());
            await page.SetViewportSizeAsync(width, 600);
            await Expect(breakpoint).ToHaveTextAsync(((Breakpoint)index + 1).ToString());
        }
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task ObserveBreakpoint_ShouldReportOnce_WhenTheViewportCrossesSeveralBreakpointsAtOnce(string browser)
    {
        var page = await OpenAsync(browser, "/browser");
        var reports = page.GetByTestId("breakpoint-reports");
        await Expect(reports).ToHaveTextAsync("1");

        await page.SetViewportSizeAsync(400, 600);
        await Expect(page.GetByTestId("breakpoint")).ToHaveTextAsync("Xs");
        await page.SetViewportSizeAsync(500, 600);
        await Expect(page.GetByTestId("viewport")).ToHaveTextAsync("500x600 Xs");

        await Expect(reports).ToHaveTextAsync("2");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Observe_ShouldStopReporting_WhenTheObservationIsDisposed(string browser)
    {
        var page = await OpenAsync(browser, "/browser");
        var errors = new List<string>();
        page.Console += (_, message) => { if (message.Type == "error") { errors.Add(message.Text); } };
        page.PageError += (_, error) => errors.Add(error);
        await Expect(page.GetByTestId("viewport")).ToHaveTextAsync("1280x720 Xl");

        await Button(page, "Stop observing").ClickAsync();
        await Expect(Result(page)).ToHaveTextAsync("stopped");
        await page.SetViewportSizeAsync(700, 500);
        await Expect(page.GetByTestId("viewport-control")).ToHaveTextAsync("700x500");

        await Expect(page.GetByTestId("viewport")).ToHaveTextAsync("1280x720 Xl");
        await Expect(page.GetByTestId("breakpoint")).ToHaveTextAsync("Xl");
        Assert.Empty(errors);
    }
}
