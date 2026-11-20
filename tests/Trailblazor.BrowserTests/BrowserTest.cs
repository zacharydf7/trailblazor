using Microsoft.AspNetCore.WebUtilities;

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
        var (page, held) = await OpenWithRequestsHeldAsync(browser, path, script);
        // A click before this is lost anyway, when the interactive render replaces the prerendered page.
        await page.Locator("main[data-interactive]").WaitForAsync(new() { State = WaitForSelectorState.Attached });
        return (page, held);
    }

    // Opens a page with its interactive session held back until the returned source is set, so a test can act before
    // the session's components first render.
    protected Task<(IPage Page, TaskCompletionSource Session)> OpenWithTheSessionHeldAsync(string browser,
        string path) => OpenWithRequestsHeldAsync(browser, path, "**/_blazor/negotiate**");

    private async Task<(IPage Page, TaskCompletionSource Held)> OpenWithRequestsHeldAsync(string browser, string path,
        string requests)
    {
        var held = new TaskCompletionSource();
        var page = await OpenStaticAsync(browser, "about:blank");
        await page.RouteAsync(requests, async route =>
        {
            await held.Task;
            await route.ContinueAsync();
        });
        await page.GotoAsync(HostAddress(path).ToString());
        return (page, held);
    }

    // The page stays prerendered until Blazor's script is let through, which the test does once the helper is waiting.
    protected async Task<(IPage Page, TaskCompletionSource Held)> OpenHeldAsync(string browser, string path)
    {
        var held = new TaskCompletionSource();
        var page = await OpenStaticAsync(browser, path);
        await page.RouteAsync("**/_framework/blazor.web*.js", async route =>
        {
            await held.Task;
            await route.ContinueAsync();
        });
        await page.ReloadAsync(new() { WaitUntil = WaitUntilState.Commit });
        return (page, held);
    }

    // For pages rendered as static SSR, which never become interactive.
    protected Task<IPage> OpenStaticAsync(string browser, string path, BrowserNewContextOptions? options = null) =>
        OpenAtAsync(browser, HostAddress(path), options);

    // For the published docs site, a WebAssembly app that has loaded once its page heading shows.
    protected async Task<IPage> OpenDocsAsync(string browser, string path, BrowserNewContextOptions? options = null)
    {
        var page = await OpenAtAsync(browser, new Uri(Fixture.DocsAddress, path), options);
        await page.Locator(".docs-main h1").First.WaitForAsync();
        return page;
    }

    // The trimmed app isn't prerendered, so its buttons appear only after WebAssembly starts.
    protected async Task<IPage> OpenTrimmedAppAsync(string browser)
    {
        var page = await OpenAtAsync(browser, Fixture.TrimmedAppAddress, null);
        await page.GetByRole(AriaRole.Button).First.WaitForAsync();
        return page;
    }

    // TRAILBLAZOR_RENDER_MODE=webassembly or auto runs the host's pages that way, unless a test names its own ?mode=.
    private Uri HostAddress(string path)
    {
        var address = new Uri(Fixture.BaseAddress, path);
        var mode = Environment.GetEnvironmentVariable("TRAILBLAZOR_RENDER_MODE");
        if (string.IsNullOrEmpty(mode) || QueryHelpers.ParseQuery(address.Query).ContainsKey("mode"))
        {
            return address;
        }

        return new Uri(QueryHelpers.AddQueryString(address.ToString(), "mode", mode));
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

    // The browser fires a popover's toggle event a task after it closes. Holding that event back stands in for a slow
    // machine, where a test or the app focuses something else before the component hears the close. This also adds a
    // button named Elsewhere for the test to focus.
    protected static Task HoldBackTheClosedToggleAsync(IPage page, string popover) => page.EvaluateAsync("""
        popover => {
            document.addEventListener('toggle', event => {
                if (event.isTrusted && event.newState === 'closed' && event.target.matches(popover)) {
                    event.stopImmediatePropagation();
                    window.hearTheClose = () => event.target.dispatchEvent(
                        new ToggleEvent('toggle', { oldState: 'open', newState: 'closed' }));
                }
            }, true);
            const other = document.createElement('button');
            other.textContent = 'Elsewhere';
            document.body.append(other);
        }
        """, popover);

    // Focuses the Elsewhere button once the popover has closed, and only then lets the component hear the close.
    protected static async Task<ILocator> FocusElsewhereBeforeTheCloseIsHeardAsync(IPage page)
    {
        var elsewhere = page.GetByRole(AriaRole.Button, new() { Name = "Elsewhere" });
        await page.WaitForFunctionAsync("() => typeof window.hearTheClose === 'function'");
        await elsewhere.FocusAsync();
        await page.EvaluateAsync("() => window.hearTheClose()");
        return elsewhere;
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

    // The page's timers then fire only when the test runs the clock. The installed clock runs until the pause, which
    // can't lie in its past, so the pause leaves room for a slow machine.
    protected static async Task HoldTimersAsync(IPage page)
    {
        var now = DateTime.Now;
        await page.Clock.InstallAsync(new() { TimeDate = now });
        await page.Clock.PauseAtAsync(now + PauseMargin);
    }

    private static readonly TimeSpan PauseMargin = TimeSpan.FromSeconds(5);

    // A slow tap: a finger holds still on the target past the moment that picks a draggable item up, then lifts where
    // it was. Returns whether the hold picked the item up: whether the target, or the ancestor it is carried with, had
    // the attribute that marks a carried item. The target is brought to the middle of the window first: a finger held
    // near the top or bottom of what scrolls keeps it scrolling, which counts as a move.
    protected static Task<bool> SlowTapAsync(ILocator target, string carrier, string attribute) =>
        target.EvaluateAsync<bool>("""
            async (target, { carrier, attribute }) => {
                target.scrollIntoView({ block: 'center', behavior: 'instant' });
                const box = target.getBoundingClientRect();
                const fire = type => target.dispatchEvent(new PointerEvent(type, { bubbles: true, pointerId: 7,
                    pointerType: 'touch', isPrimary: true, button: 0, clientX: box.left + box.width / 2,
                    clientY: box.top + box.height / 2 }));
                fire('pointerdown');
                await new Promise(resolve => setTimeout(resolve, 400));
                const carried = target.closest(carrier).hasAttribute(attribute);
                fire('pointerup');
                target.click();
                return carried;
            }
            """, new { carrier, attribute });

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
