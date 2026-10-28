namespace Trailblazor.BrowserTests;

public abstract class BrowserTest(BrowserFixture fixture) : IAsyncDisposable
{
    private readonly List<IBrowserContext> _contexts = [];

    protected BrowserFixture Fixture { get; } = fixture;

    protected async Task<IPage> OpenAsync(string browser, string path, BrowserNewContextOptions? options = null)
    {
        var context = await Fixture.NewContextAsync(browser, options);
        _contexts.Add(context);
        var page = await context.NewPageAsync();
        await page.GotoAsync(new Uri(Fixture.BaseAddress, path).ToString());
        await page.Locator("main[data-interactive]").WaitForAsync(new() { State = WaitForSelectorState.Attached });
        return page;
    }

    protected static Task SetThemeAsync(IPage page, string theme) =>
        page.EvaluateAsync("theme => document.documentElement.dataset.tbTheme = theme", theme);

    public async ValueTask DisposeAsync()
    {
        foreach (var context in _contexts)
        {
            await context.CloseAsync();
        }
    }
}
