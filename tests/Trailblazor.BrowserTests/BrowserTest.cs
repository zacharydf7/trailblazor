using Microsoft.AspNetCore.WebUtilities;
using Trailblazor.Testing.Playwright;

namespace Trailblazor.BrowserTests;

public abstract class BrowserTest(BrowserFixture fixture) : IAsyncDisposable
{
    private readonly List<IBrowserContext> _contexts = [];
    private IBrowser? _alone;

    protected BrowserFixture Fixture { get; } = fixture;

    // While one of Firefox's pages holds a mouse button down, clicks and moves meant for its other pages go astray, and
    // theirs reach it. A class with a test that holds one, itself or through one of the testing library's drags, says
    // so here, and each of its tests has a Firefox to itself while it runs. MouseHoldTests fails for a class that
    // should and doesn't.
    protected virtual bool HoldsTheMouse => false;

    // Runs before a page opens anything, for a test class that sets up every page the same way.
    protected virtual Task PreparePageAsync(IPage page) => Task.CompletedTask;

    // TRAILBLAZOR_POPPED=1 runs a page's tests against the page inside a dock panel that shows in a browser window of
    // its own, which is where a script that assumes one window gives itself away. A test that can't run there says
    // why with Assert.SkipWhen(Popped, ...).
    protected static bool Popped { get; } = Environment.GetEnvironmentVariable("TRAILBLAZOR_POPPED") == "1";

    // Why a test skips itself there.
    protected const string LeavesThePage = "It follows a link or reloads, which takes the panel's window away.";
    protected const string AtThePagesTopEdge =
        "What it looks at sits at the page's top edge, where the panel's window has its Return bar.";
    protected const string TheFocusFallsToThePanel =
        "It waits for the focus to fall to the page, and in the panel's window it falls to the panel.";
    protected const string ScrollsThePage = "It scrolls the page, and in the panel's window the panel is what scrolls.";
    protected const string PatchesTheWindow =
        "It changes a built-in of the window it is given, and the panel's elements keep the page's.";
    protected const string ChangesThePagesMain =
        "It changes the page's main element, which the panel's window doesn't have.";
    protected const string ScansAnElement =
        "axe narrows a scan to an element only from the window it runs in, and the page made the panel's elements.";
    protected const string ThePageFailsToRender = "The page fails to render on purpose, so it has no panel to open.";
    protected const string WarnsInThePagesConsole =
        "The library's scripts warn in the page's console, not the panel's window's.";
    protected const string AddsToThePagesBody =
        "The page's own script adds its popup to the page's body, which the panel's window doesn't show.";

    // Waits until the window has closed, which it may have done already.
    protected static async Task ClosedAsync(IPage window)
    {
        for (var tries = 0; !window.IsClosed && tries < 100; tries++)
        {
            await Task.Delay(50);
        }

        Assert.True(window.IsClosed);
    }

    // Waits until the element is its own page's focused one. With more than one window open, which of them a test
    // browser counts as the active one varies, and Playwright's own focus check also needs the element's window to be
    // the active one.
    protected static async Task FocusedAsync(ILocator element) => await element.Page.WaitForFunctionAsync(
        "element => document.activeElement === element", await element.ElementHandleAsync());

    protected async Task<IPage> OpenAsync(string browser, string path, BrowserNewContextOptions? options = null)
    {
        if (Popped)
        {
            return (await OpenPoppedAsync(browser, path, options)).Window;
        }

        var page = await OpenStaticAsync(browser, path, options);
        await page.Locator("main[data-interactive]").WaitForAsync(new() { State = WaitForSelectorState.Attached });
        return page;
    }

    // Opens the page in a dock panel's own browser window, and gives the page that holds the dock too.
    protected async Task<(IPage Host, IPage Window)> OpenPoppedAsync(string browser, string path,
        BrowserNewContextOptions? options = null)
    {
        var host = await OpenStaticAsync(browser, $"/popped{path}", options);
        await host.Locator("main[data-interactive]").WaitForAsync(new() { State = WaitForSelectorState.Attached });
        var window = (await host.Tb().Dock("Popped host").PopOutAsync("Qz")).Page;
        var size = options?.ViewportSize ?? new() { Width = 1280, Height = 720 };
        await window.SetViewportSizeAsync(size.Width, size.Height);
        return (host, window);
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

    // Opens a page under a new run id, so a count the page keeps by run is this test's alone, holds it before it
    // turns interactive, and from when its prerendered content shows, notes in window.sawLoading whether a loading row
    // ever shows.
    protected async Task<(IPage Page, TaskCompletionSource Held)> OpenPrerenderedAsync(string browser, string path,
        string query, Func<IPage, ILocator> prerendered)
    {
        var (page, held) = await OpenHeldAsync(browser, path);
        await page.GotoAsync(new Uri(new Uri(page.Url), $"{path}?run={Guid.NewGuid():N}&{query}").ToString(),
            new() { WaitUntil = WaitUntilState.Commit });
        await Expect(prerendered(page)).ToBeVisibleAsync();
        await page.EvaluateAsync("""
            () => {
                window.sawLoading = false;
                new MutationObserver(() => window.sawLoading ||= document.querySelector('[data-part=loading]') !== null)
                    .observe(document.documentElement, { childList: true, subtree: true });
            }
            """);
        return (page, held);
    }

    // Such a page shows the count it had when it last drew, so the count is read once a click on its Count button
    // has drawn it again.
    protected static async Task<string> CallsAsync(IPage page, int counted)
    {
        await page.Tb().Button("Count").ClickAsync();
        await Expect(page.GetByTestId("counted")).ToHaveTextAsync($"{counted}");
        return (await page.GetByTestId("calls").TextContentAsync())!;
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

    private async Task<IBrowserContext> NewContextAsync(string browser, BrowserNewContextOptions? options)
    {
        if (!HoldsTheMouse || browser != "firefox")
        {
            return await Fixture.NewContextAsync(browser, options);
        }

        _alone ??= await Fixture.TakeAFirefoxAsync();
        return await _alone.NewContextAsync(options);
    }

    private async Task<IPage> OpenAtAsync(string browser, Uri address, BrowserNewContextOptions? options)
    {
        var context = await NewContextAsync(browser, options);
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
    // Where a label's text ends: its last line's right edge, top and bottom. A label's own box can run past its text,
    // as when it keeps room for an info button, so the text is measured on its own.
    protected static Task<double[]> LastLineAsync(ILocator text) => text.EvaluateAsync<double[]>(
        "e => { const text = document.createRange(); text.selectNodeContents(e); "
        + "const line = [...text.getClientRects()].at(-1); return [line.right, line.top, line.bottom]; }");

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

    // Centers the field holding this element, as the docs site's example frame does, so it takes its content's width.
    protected static Task CenterFieldAsync(ILocator inField) =>
        StyleFieldsParentAsync(inField, new { display = "grid", justifyItems = "center" });

    // Sets these styles, named as in JavaScript, on the parent of the field holding this element.
    protected static Task StyleFieldsParentAsync(ILocator inField, object styles) => inField.EvaluateAsync(
        "(element, styles) => Object.assign(element.closest('.tb-field').parentElement.style, styles)", styles);

    // Whether a text box is wide enough to show its whole value.
    protected static Task<bool> ShowsWholeValueAsync(ILocator input) => input.EvaluateAsync<bool>("""
        input => {
            const style = getComputedStyle(input);
            const probe = document.body.appendChild(document.createElement('span'));
            for (const name of ['fontFamily', 'fontSize', 'fontWeight', 'letterSpacing', 'fontVariantNumeric']) {
                probe.style[name] = style[name];
            }
            probe.style.whiteSpace = 'pre';
            probe.textContent = input.value;
            const needed = probe.getBoundingClientRect().width;
            probe.remove();
            const shown = input.clientWidth - parseFloat(style.paddingLeft) - parseFloat(style.paddingRight);
            return input.value.length > 0 && shown >= needed;
        }
        """);

    public async ValueTask DisposeAsync()
    {
        foreach (var context in _contexts)
        {
            await context.CloseAsync();
        }

        if (_alone is not null)
        {
            Fixture.HandBack(_alone);
        }
    }
}
