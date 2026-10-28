namespace Trailblazor.BrowserTests;

public abstract class BrowserTest(BrowserFixture fixture) : IAsyncDisposable
{
    private readonly List<IBrowserContext> _contexts = [];

    protected BrowserFixture Fixture { get; } = fixture;

    protected async Task<IPage> OpenAsync(string browser, string path, BrowserNewContextOptions? options = null)
    {
        var page = await OpenStaticAsync(browser, path, options);
        await page.Locator("main[data-interactive]").WaitForAsync(new() { State = WaitForSelectorState.Attached });
        return page;
    }

    // For pages rendered as static SSR, which never become interactive.
    protected async Task<IPage> OpenStaticAsync(string browser, string path, BrowserNewContextOptions? options = null)
    {
        var context = await Fixture.NewContextAsync(browser, options);
        _contexts.Add(context);
        var page = await context.NewPageAsync();
        await page.GotoAsync(new Uri(Fixture.BaseAddress, path).ToString());
        return page;
    }

    // Colors transition when the theme flips, so wait until no CSS transition is still running.
    protected static Task SetThemeAsync(IPage page, string theme) =>
        page.EvaluateAsync("""
            async theme => {
                document.documentElement.dataset.tbTheme = theme;
                await new Promise(resolve => requestAnimationFrame(resolve));
                const transitions = document.getAnimations().filter(animation => animation instanceof CSSTransition);
                await Promise.all(transitions.map(transition => transition.finished));
            }
            """, theme);

    public async ValueTask DisposeAsync()
    {
        foreach (var context in _contexts)
        {
            await context.CloseAsync();
        }
    }
}
