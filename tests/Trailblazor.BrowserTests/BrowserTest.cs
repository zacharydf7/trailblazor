namespace Trailblazor.BrowserTests;

public abstract class BrowserTest(BrowserFixture fixture) : IAsyncDisposable
{
    private readonly List<IBrowserContext> _contexts = [];

    protected BrowserFixture Fixture { get; } = fixture;

    // Runs before a page opens anything, for a test class that sets up every page the same way.
    protected virtual Task PreparePageAsync(IPage page) => Task.CompletedTask;

    protected async Task<IPage> OpenAsync(string browser, string path, BrowserNewContextOptions? options = null)
    {
        var page = await OpenStaticAsync(browser, path, options);
        await page.Locator("main[data-interactive]").WaitForAsync(new() { State = WaitForSelectorState.Attached });
        return page;
    }

    // Opens an interactive page with the script matching the glob, such as "**/menu.*js", held back until the returned
    // source is set, so a test can act before the component's script has attached.
    protected async Task<(IPage Page, TaskCompletionSource Script)> OpenWithAScriptHeldAsync(string browser,
        string path, string script)
    {
        var held = new TaskCompletionSource();
        var page = await OpenStaticAsync(browser, "about:blank");
        await page.RouteAsync(script, async route =>
        {
            await held.Task;
            await route.ContinueAsync();
        });
        await page.GotoAsync(new Uri(Fixture.BaseAddress, path).ToString());
        // A click before this is lost anyway, when the interactive render replaces the prerendered page.
        await page.Locator("main[data-interactive]").WaitForAsync(new() { State = WaitForSelectorState.Attached });
        return (page, held);
    }

    // For pages rendered as static SSR, which never become interactive.
    protected Task<IPage> OpenStaticAsync(string browser, string path, BrowserNewContextOptions? options = null) =>
        OpenAtAsync(browser, new Uri(Fixture.BaseAddress, path), options);

    // For the published docs site, a WebAssembly app that has loaded once its page heading shows.
    protected async Task<IPage> OpenDocsAsync(string browser, string path, BrowserNewContextOptions? options = null)
    {
        var page = await OpenAtAsync(browser, new Uri(Fixture.DocsAddress, path), options);
        await page.Locator(".docs-main h1").First.WaitForAsync();
        return page;
    }

    private async Task<IPage> OpenAtAsync(string browser, Uri address, BrowserNewContextOptions? options)
    {
        var context = await Fixture.NewContextAsync(browser, options);
        _contexts.Add(context);
        var page = await context.NewPageAsync();
        await PreparePageAsync(page);
        await page.GotoAsync(address.ToString());
        return page;
    }

    // Colors transition when the theme flips, so wait until no CSS transition is still running. A transition another
    // replaces, as when a component re-renders for the new theme, ends canceled, so the wait goes on to its successor.
    protected static Task SetThemeAsync(IPage page, string theme) =>
        page.EvaluateAsync("""
            async theme => {
                document.documentElement.dataset.tbTheme = theme;
                await new Promise(resolve => requestAnimationFrame(resolve));
                const running = () => document.getAnimations().filter(animation => animation instanceof CSSTransition);
                for (let transitions = running(); transitions.length > 0; transitions = running()) {
                    await Promise.allSettled(transitions.map(transition => transition.finished));
                }
            }
            """, theme);

    // Waits out fades and color transitions before a check reads colors, but not infinite animations like a spinner.
    protected static Task WaitForFadesAsync(IPage page) => page.WaitForFunctionAsync("""
        () => document.getAnimations().every(animation => animation.playState !== 'running'
            || animation.effect?.getComputedTiming().iterations === Infinity)
        """);

    // What a system color, such as Highlight, computes to in this browser while colors are forced.
    protected static Task<string> SystemColorAsync(IPage page, string color) => page.EvaluateAsync<string>("""
        color => {
            const probe = document.body.appendChild(document.createElement('div'));
            probe.style.cssText = `forced-color-adjust: none; color: ${color}`;
            const computed = getComputedStyle(probe).color;
            probe.remove();
            return computed;
        }
        """, color);

    // The muted text color in the form getComputedStyle reports, so it compares with an element's color.
    protected static Task<string> MutedColorAsync(IPage page) => page.EvaluateAsync<string>("""
        () => {
            const probe = document.body.appendChild(document.createElement('span'));
            probe.style.color = 'var(--tb-color-text-muted)';
            const color = getComputedStyle(probe).color;
            probe.remove();
            return color;
        }
        """);

    public async ValueTask DisposeAsync()
    {
        foreach (var context in _contexts)
        {
            await context.CloseAsync();
        }
    }
}
