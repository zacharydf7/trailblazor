using Trailblazor.Testing.Playwright;

namespace Trailblazor.BrowserTests;

public class DockManagerPopOutTests(BrowserFixture fixture) : BrowserTest(fixture)
{
    // The page's outer dock. Its Map panel holds another one, which shows once the Map does.
    private static ILocator Workbench(IPage page) => page.GetByRole(AriaRole.Group, new() { Name = "Workbench" });

    private async Task<IPage> OpenDockAsync(string browser)
    {
        var page = await OpenAsync(browser, "/dock-pop-out");
        await Expect(Workbench(page)).ToHaveAttributeAsync("data-ready", "");
        return page;
    }

    // Opens a panel in a browser window of its own, from its tab's menu, and gives that window's page.
    private static async Task<IPage> PopOutAsync(IPage page, string panel)
    {
        var window = await page.RunAndWaitForPopupAsync(() =>
            page.Tb().Dock("Workbench").ChooseFromTabMenuAsync(panel, "Open in new window"));
        await window.Locator("[data-part=panel][data-popped]").WaitForAsync();
        return window;
    }

    // Waits until the element is its own page's focused one. With more than one window open, which of them a test
    // browser counts as the active one varies, and Playwright's own focus check also needs the element's window to be
    // the active one.
    private static async Task FocusedAsync(ILocator element) => await element.Page.WaitForFunctionAsync(
        "element => document.activeElement === element", await element.ElementHandleAsync());

    // Waits until the window has closed, which it may have done already.
    private static async Task ClosedAsync(IPage window)
    {
        for (var tries = 0; !window.IsClosed && tries < 100; tries++)
        {
            await Task.Delay(50);
        }

        Assert.True(window.IsClosed);
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Panel_ShouldKeepItsStateAndWork_WhenItOpensInAWindowOfItsOwn(string browser)
    {
        var page = await OpenDockAsync(browser);
        await page.GetByLabel("Note", new() { Exact = true }).FillAsync("Pack the stove");
        await page.GetByRole(AriaRole.Button, new() { Name = "Count", Exact = true }).ClickAsync();
        await Expect(page.GetByTestId("count")).ToHaveTextAsync("Count: 1");
        await page.GetByRole(AriaRole.Combobox, new() { Name = "Size" }).ClickAsync();
        await page.GetByRole(AriaRole.Option, new() { Name = "Small" }).ClickAsync();
        await Expect(page.GetByTestId("size")).ToHaveTextAsync("Size: Small");

        var window = await PopOutAsync(page, "Notes");

        // The panel left the page with what it held, and the dock shows no tab for it.
        await Expect(window).ToHaveTitleAsync("Notes");
        Assert.Equal("CSS1Compat", await window.EvaluateAsync<string>("document.compatMode"));
        await Expect(window.GetByRole(AriaRole.Region, new() { Name = "Notes" })).ToBeVisibleAsync();
        await Expect(window.GetByLabel("Note", new() { Exact = true })).ToHaveValueAsync("Pack the stove");
        await Expect(window.GetByTestId("count")).ToHaveTextAsync("Count: 1");
        await Expect(window.GetByRole(AriaRole.Combobox, new() { Name = "Size" })).ToHaveTextAsync("Small");
        await Expect(page.GetByRole(AriaRole.Tab, new() { Name = "Notes" })).ToHaveCountAsync(0);
        await Expect(page.GetByTestId("count")).ToHaveCountAsync(0);
        await page.Tb().Announcements.ExpectAnnouncedAsync("Notes opened in a new window.");

        // It still renders, from the page and from its own window.
        await page.GetByRole(AriaRole.Button, new() { Name = "Count from the page" }).ClickAsync();
        await Expect(window.GetByTestId("count")).ToHaveTextAsync("Count: 2");
        await window.GetByRole(AriaRole.Button, new() { Name = "Count", Exact = true }).ClickAsync();
        await Expect(window.GetByTestId("count")).ToHaveTextAsync("Count: 3");
        await window.GetByLabel("Note", new() { Exact = true }).FillAsync("Pack the stove and the fuel");
        await Expect(window.GetByTestId("note")).ToHaveTextAsync("Note: Pack the stove and the fuel");
        // The focus, which doesn't bubble, was heard once in the page and once here. Blazor keeps a form's sending
        // from leaving the page, and that holds in the window too, which would otherwise load another page.
        await Expect(window.GetByTestId("focused")).ToHaveTextAsync("Focused: 2");
        await window.GetByLabel("Note", new() { Exact = true }).SelectTextAsync();
        await Expect(window.GetByTestId("selected")).Not.ToHaveTextAsync("Selected: 0");
        await window.GetByRole(AriaRole.Button, new() { Name = "Send" }).ClickAsync();
        await Expect(window.GetByTestId("sent")).ToHaveTextAsync("Sent: 1");
        await Expect(window.GetByTestId("count")).ToHaveTextAsync("Count: 3");
        // An event of the library's own, which a text field sends for Enter, reaches Blazor from the window too.
        await window.GetByLabel("Tag").FillAsync("stove");
        await window.Keyboard.PressAsync("Enter");
        await Expect(window.GetByTestId("entered")).ToHaveTextAsync("Entered: 1");
        await window.GetByRole(AriaRole.Combobox, new() { Name = "Size" }).ClickAsync();
        // The list is placed by its own window's size, not the page's, so it stays inside the window.
        await window.WaitForFunctionAsync("(() => { const box = document.querySelector('[role=listbox]')"
            + ".getBoundingClientRect(); return box.top >= 0 && box.bottom <= innerHeight; })()");
        await window.GetByRole(AriaRole.Option, new() { Name = "Medium" }).ClickAsync();
        await Expect(window.GetByTestId("size")).ToHaveTextAsync("Size: Medium");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Panel_ShouldComeBackToAFloatingWindowWithItsState_WhenItsWindowClosesOrReturnIsPressed(
        string browser)
    {
        var page = await OpenDockAsync(browser);
        var window = await PopOutAsync(page, "Notes");
        await window.GetByLabel("Note", new() { Exact = true }).FillAsync("Fuel");

        await window.GetByRole(AriaRole.Button, new() { Name = "Return to the dock" }).ClickAsync();

        await Expect(page.GetByLabel("Note", new() { Exact = true })).ToHaveValueAsync("Fuel");
        await FocusedAsync(page.GetByRole(AriaRole.Tab, new() { Name = "Notes" }));
        Assert.Equal(["Notes"], await page.Tb().Dock("Workbench").FloatingAsync());
        await page.Tb().Announcements.ExpectAnnouncedAsync("Notes is back in the dock.");
        Assert.True(window.IsClosed);

        // Closing the window brings it back too.
        var again = await PopOutAsync(page, "Notes");
        await again.GetByLabel("Note", new() { Exact = true }).FillAsync("Fuel and matches");
        await again.CloseAsync();

        await Expect(page.GetByLabel("Note", new() { Exact = true })).ToHaveValueAsync("Fuel and matches");
        await Expect(page.GetByRole(AriaRole.Tab, new() { Name = "Notes" })).ToBeVisibleAsync();
        Assert.Equal(["Notes"], await page.Tb().Dock("Workbench").FloatingAsync());
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Windows_ShouldFollowTheDock_WhenAPanelIsAddedOrClosedOrThePageGoesAway(string browser)
    {
        var page = await OpenDockAsync(browser);
        var notes = await PopOutAsync(page, "Notes");
        var gear = await PopOutAsync(page, "Gear");

        // A panel that opens while two are away lands in the dock, not in one of their windows.
        await page.GetByRole(AriaRole.Button, new() { Name = "Open log" }).ClickAsync();
        await Expect(page.GetByRole(AriaRole.Tab, new() { Name = "Log" })).ToBeVisibleAsync();
        await Expect(page.GetByText("Log lines")).ToBeVisibleAsync();
        await Expect(notes.GetByText("Log lines")).ToHaveCountAsync(0);

        // A panel the app closes while it is away closes its window.
        await page.GetByRole(AriaRole.Button, new() { Name = "Close notes" }).ClickAsync();
        await ClosedAsync(notes);
        await Expect(page.GetByLabel("Note", new() { Exact = true })).ToHaveCountAsync(0);

        // The windows go with the dock, here as the app moves to another page.
        await page.EvaluateAsync("Blazor.navigateTo('select')");
        await ClosedAsync(gear);
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Windows_ShouldClose_WhenThePageGoesAway(string browser)
    {
        var page = await OpenDockAsync(browser);
        var notes = await PopOutAsync(page, "Notes");

        await page.GotoAsync("about:blank");

        await ClosedAsync(notes);
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task List_ShouldMoveToTheSideWithRoom_WhenItsWindowGetsTaller(string browser)
    {
        var page = await OpenDockAsync(browser);
        var window = await PopOutAsync(page, "Notes");
        // In a short window the select is scrolled into view at the bottom, with room for its list only above it.
        await window.SetViewportSizeAsync(400, 260);
        await window.GetByRole(AriaRole.Combobox, new() { Name = "Size" }).ClickAsync();
        var list = window.Locator("[data-side]").Filter(new() { Has = window.GetByRole(AriaRole.Listbox) })
            .Or(window.Locator("[role=listbox][data-side]"));
        await Expect(list).ToHaveAttributeAsync("data-side", "top");

        await window.SetViewportSizeAsync(400, 700);

        // The list follows its own window's size, which the page's window never hears of.
        await Expect(list).ToHaveAttributeAsync("data-side", "bottom");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Dock_ShouldTakeTheFocus_WhenItsLastTabClosesWhileAnotherPanelIsInAWindowOfItsOwn(string browser)
    {
        var page = await OpenDockAsync(browser);
        var dock = page.Tb().Dock("Workbench");
        await dock.PopOutAsync("Notes");
        await dock.PopOutAsync("Map");
        await page.BringToFrontAsync();
        await page.GetByRole(AriaRole.Tab, new() { Name = "Gear" }).FocusAsync();

        await page.Keyboard.PressAsync("Delete");

        // The panels that are away have no tabs to take the focus.
        await Expect(page.GetByRole(AriaRole.Tab)).ToHaveCountAsync(0);
        await FocusedAsync(Workbench(page));
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Dock_ShouldTakeTheFocus_WhenAnAutoHiddenPanelClosesWhileTheOthersAreInWindowsOfTheirOwn(
        string browser)
    {
        var page = await OpenDockAsync(browser);
        var dock = page.Tb().Dock("Workbench");
        await dock.AutoHideAsync("Gear", DockPosition.Start);
        await dock.PopOutAsync("Notes");
        await dock.PopOutAsync("Map");
        await page.BringToFrontAsync();
        await dock.RevealAsync("Gear");
        await FocusedAsync(page.GetByRole(AriaRole.Tab, new() { Name = "Gear" }));

        await page.Keyboard.PressAsync("Delete");

        // No button is left on the strip and no tab shows in the dock, so the dock takes the focus.
        await Expect(page.GetByRole(AriaRole.Tab)).ToHaveCountAsync(0);
        await FocusedAsync(Workbench(page));
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task List_ShouldFollowItsOwnWindow_WhenTheWindowGrowsItsPanelScrollsOrItsFieldNarrows(string browser)
    {
        var page = await OpenDockAsync(browser);
        var window = (await page.Tb().Dock("Workbench").PopOutAsync("Gear")).Page;
        await window.SetViewportSizeAsync(400, 150);
        await window.GetByRole(AriaRole.Combobox, new() { Name = "Pace" }).ClickAsync();
        var list = window.Locator("[data-side]").Filter(new() { Has = window.GetByRole(AriaRole.Listbox) })
            .Or(window.Locator("[role=listbox][data-side]"));
        await Expect(list).ToBeVisibleAsync();
        const string Box = "list => { const box = list.getBoundingClientRect(); return [box.top, box.height]; }";
        var capped = await list.EvaluateAsync<double[]>(Box);

        // The window grows, with nothing scrolled: the list takes the room it now has below its field.
        await window.SetViewportSizeAsync(400, 600);
        await window.WaitForFunctionAsync("height => document.querySelector('[data-side]')"
            + ".getBoundingClientRect().height > height + 20", capped[1]);

        // The panel scrolls, with the window as it is: the list stays by its field.
        var before = await list.EvaluateAsync<double[]>(Box);
        await window.Locator("[data-part=panel]").EvaluateAsync("panel => panel.scrollTop = 20");
        await window.WaitForFunctionAsync("top => Math.abs(document.querySelector('[data-side]')"
            + ".getBoundingClientRect().top - (top - 20)) < 1", before[0]);

        // The field narrows, with nothing else changing: the list is told its field's new width.
        const string Width = "document.querySelector('[data-side]').style.getPropertyValue('--tb-anchor-width')";
        var wide = await window.EvaluateAsync<string>(Width);
        await window.Locator("[data-part=panel]").EvaluateAsync("""
            panel => {
                const field = [...panel.children].find(child => child.querySelector('[role=combobox]'));
                field.style.inlineSize = '10rem';
            }
            """);
        await window.WaitForFunctionAsync($"wide => {Width} !== wide", wide);
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task PopOut_ShouldLeaveThePanelFloatingAndSaySo_WhenTheBrowserRefusesTheWindow(string browser)
    {
        var page = await OpenDockAsync(browser);
        await page.EvaluateAsync("window.open = () => null");

        await page.Tb().Dock("Workbench").ChooseFromTabMenuAsync("Notes", "Open in new window");

        await page.Tb().Announcements.ExpectAnnouncedAsync("The browser blocked the new window for Notes.");
        Assert.Equal(["Notes"], await page.Tb().Dock("Workbench").FloatingAsync());
        await Expect(page.GetByRole(AriaRole.Tab, new() { Name = "Notes" })).ToBeFocusedAsync();
        await Expect(page.GetByRole(AriaRole.Tabpanel, new() { Name = "Notes" })).ToBeVisibleAsync();
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Window_ShouldLookLikeThePage_WhenThePageSetsAThemeAndADensity(string browser)
    {
        var page = await OpenDockAsync(browser);
        await SetThemeAsync(page, "dark");
        await page.EvaluateAsync("document.documentElement.dataset.probe = 'on'");

        var window = await PopOutAsync(page, "Notes");

        var region = window.GetByRole(AriaRole.Region, new() { Name = "Notes" });
        Assert.Equal(await page.EvaluateAsync<string>("document.documentElement.dataset.tbTheme"),
            await window.EvaluateAsync<string>("document.documentElement.dataset.tbTheme"));
        Assert.Equal("on", await window.EvaluateAsync<string>("document.documentElement.dataset.probe"));
        // What is set around the dock, and not on the page's root, goes on the window's body: the nearer of two
        // values, and not the theme, which the root has.
        Assert.Equal(("compact", "7px", "tb-dock-window", "none"), (
            await window.EvaluateAsync<string>("document.body.dataset.tbDensity"),
            await window.EvaluateAsync<string>("document.body.style.getPropertyValue('--probe-gap')"),
            await window.EvaluateAsync<string>("document.body.className"),
            await window.EvaluateAsync<string>("document.body.dataset.tbTheme ?? 'none'")));
        const string Background = "element => getComputedStyle(element).backgroundColor";
        Assert.Equal(await Workbench(page).EvaluateAsync<string>(Background),
            await region.EvaluateAsync<string>(Background));
        // The panel fills its window, which is half as wide as the dock and, the dock being low, as tall as the
        // smallest window the dock opens.
        var box = (await region.BoundingBoxAsync())!;
        var size = await window.EvaluateAsync<int[]>("[innerWidth, innerHeight]");
        Assert.Equal((0, 0, size[0], size[1]), (box.X, box.Y, box.Width, box.Height));
        Assert.Equal((384, 240), (size[0], size[1]));

        // The root's attributes and the style sheets keep following the page.
        await SetThemeAsync(page, "light");
        await window.WaitForFunctionAsync("document.documentElement.dataset.tbTheme === 'light'");
        await page.EvaluateAsync("delete document.documentElement.dataset.probe");
        await window.WaitForFunctionAsync("!('probe' in document.documentElement.dataset)");
        await page.EvaluateAsync("""
            () => {
                const sheet = document.head.appendChild(document.createElement('style'));
                sheet.textContent = '[data-testid=count] { letter-spacing: 3px }';
            }
            """);
        var count = window.GetByTestId("count");
        await Expect(count).ToHaveCSSAsync("letter-spacing", "3px");
        // A sheet the page puts before its others comes before them in the window too, where it loses to them still.
        // The copies that were in place stay there: a linked sheet that moves loads again.
        await window.EvaluateAsync("""
            () => {
                window.taken = 0;
                new MutationObserver(records => records.forEach(record => window.taken += record.removedNodes.length))
                    .observe(document.head, { childList: true });
            }
            """);
        await page.EvaluateAsync("""
            () => {
                const first = document.head.insertBefore(document.createElement('style'), document.head.firstChild);
                first.textContent = '[data-testid=count] { letter-spacing: 8px; text-transform: uppercase }';
            }
            """);
        await Expect(count).ToHaveCSSAsync("text-transform", "uppercase");
        await Expect(count).ToHaveCSSAsync("letter-spacing", "3px");
        Assert.Equal(0, await window.EvaluateAsync<int>("window.taken"));
        await page.EvaluateAsync("document.head.firstChild.remove()");
        await Expect(count).ToHaveCSSAsync("text-transform", "none");
        // Its text changes in place, as Blazor changes a style element it renders.
        await page.EvaluateAsync(
            "document.head.lastElementChild.firstChild.data = '[data-testid=count] { letter-spacing: 5px }'");
        await Expect(count).ToHaveCSSAsync("letter-spacing", "5px");
        await page.EvaluateAsync("document.head.lastElementChild.media = 'print'");
        await Expect(count).ToHaveCSSAsync("letter-spacing", "normal");
        await page.EvaluateAsync("document.head.lastElementChild.media = 'all'");
        await Expect(count).ToHaveCSSAsync("letter-spacing", "5px");
        await page.EvaluateAsync("document.head.lastElementChild.remove()");
        await Expect(count).ToHaveCSSAsync("letter-spacing", "normal");
        // A linked sheet follows its address, as a page that switches themes by one changes it.
        await page.EvaluateAsync("""
            () => {
                const link = document.head.appendChild(document.createElement('link'));
                link.rel = 'stylesheet';
                link.href = 'data:text/css,[data-testid=count]{word-spacing:4px}';
            }
            """);
        await Expect(count).ToHaveCSSAsync("word-spacing", "4px");
        await page.EvaluateAsync(
            "document.head.lastElementChild.href = 'data:text/css,[data-testid=count]{word-spacing:6px}'");
        await Expect(count).ToHaveCSSAsync("word-spacing", "6px");
        // A sheet the page turns off is off in the window, and on again with the page.
        await page.EvaluateAsync("document.head.lastElementChild.disabled = true");
        await Expect(count).ToHaveCSSAsync("word-spacing", "0px");
        await page.EvaluateAsync("document.head.lastElementChild.disabled = false");
        await Expect(count).ToHaveCSSAsync("word-spacing", "6px");
        // A sheet the page only offers as an alternate is an alternate in the window too, so it isn't applied there.
        await page.EvaluateAsync("document.head.lastElementChild.insertAdjacentHTML('beforebegin', '<link "
            + "rel=\"alternate stylesheet\" title=\"Other\" "
            + "href=\"data:text/css,[data-testid=count]{text-indent:9px}\">')");
        await window.WaitForFunctionAsync("[...document.head.querySelectorAll('link')].some(link => "
            + "link.title === 'Other' && link.rel === 'alternate stylesheet')");
        await page.EvaluateAsync(
            "document.head.lastElementChild.href = 'data:text/css,[data-testid=count]{word-spacing:7px}'");
        await Expect(count).ToHaveCSSAsync("word-spacing", "7px");
        await Expect(count).ToHaveCSSAsync("text-indent", "0px");

        const string Address = "link => link.href";
        Assert.Equal(new Uri(new Uri(page.Url), "dock-pop-out#top").ToString(),
            await window.GetByRole(AriaRole.Link, new() { Name = "Top" }).EvaluateAsync<string>(Address));

        // The window keeps the panel's title as it changes.
        await page.GetByRole(AriaRole.Button, new() { Name = "Rename notes" }).ClickAsync();
        await Expect(window).ToHaveTitleAsync("Field notes");
        await Expect(window.GetByRole(AriaRole.Region, new() { Name = "Field notes" })).ToBeVisibleAsync();
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Dock_ShouldOpenOneWindowForItsOwnPanel_WhenItIsNestedInAnotherDocksPanel(string browser)
    {
        var page = await OpenDockAsync(browser);
        var (outer, inner) = (page.Tb().Dock("Workbench"), page.Tb().Dock("Layers"));
        await outer.SelectAsync("Map");
        await Expect(page.GetByRole(AriaRole.Group, new() { Name = "Layers" })).ToHaveAttributeAsync("data-ready", "");

        var roads = await inner.PopOutAsync("Roads");

        // The outer dock leaves the click to the inner one, so no second, empty window opens.
        await Expect(roads).ToContainTextAsync("Road lines");
        Assert.Equal(2, page.Context.Pages.Count);

        // Each dock's helpers find its own panels, whichever windows are open.
        await outer.PopOutAsync("Notes");
        Assert.Equal(["Notes"], await outer.PoppedOutAsync());
        Assert.Equal(["Roads"], await inner.PoppedOutAsync());
        await inner.ReturnAsync("Roads");
        Assert.Equal(["Notes"], await outer.PoppedOutAsync());
        Assert.Empty(await inner.PoppedOutAsync());
        Assert.Equal(["Roads"], await inner.FloatingAsync());
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Window_ShouldTakeTheFocusOnItsPanelBeforeTheReturnButton_WhenItOpensFromTheKeyboard(
        string browser)
    {
        Assert.SkipWhen(browser == "webkit", "WebKit often leaves the focus on the new window's body (#586).");
        var page = await OpenDockAsync(browser);
        await page.GetByRole(AriaRole.Tab, new() { Name = "Notes" }).FocusAsync();

        // The menu's item is chosen with the keyboard, which lets a page open a window as a click does.
        var window = await page.RunAndWaitForPopupAsync(async () =>
        {
            await page.Keyboard.PressAsync("Shift+F10");
            await page.GetByRole(AriaRole.Menuitem, new() { Name = "Open in new window" }).FocusAsync();
            await page.Keyboard.PressAsync("Enter");
        });

        await FocusedAsync(window.GetByRole(AriaRole.Region, new() { Name = "Notes" }));
        await window.BringToFrontAsync();
        await window.Keyboard.PressAsync("Tab");
        await FocusedAsync(window.GetByRole(AriaRole.Button, new() { Name = "Return to the dock" }));
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Window_ShouldCloseAfterTenSecondsUnlessItHoldsAPanel_WhenTheDockNeverFillsIt(string browser)
    {
        var page = await OpenDockAsync(browser);
        await HoldTimersAsync(page);

        // A click the dock's script takes for the menu's item, with no dock behind it to ask for the window.
        const string Click = """
            dock => {
                const item = dock.appendChild(document.createElement('button'));
                item.dataset.move = 'pop-out';
                item.click();
                item.remove();
            }
            """;
        var first = await page.RunAndWaitForPopupAsync(() => Workbench(page).EvaluateAsync(Click));
        var window = await page.RunAndWaitForPopupAsync(() => Workbench(page).EvaluateAsync(Click));

        // The second click's window takes the first one's place at once.
        await ClosedAsync(first);
        await page.Clock.FastForwardAsync(8000);
        Assert.False(window.IsClosed);

        await page.Clock.FastForwardAsync(3000);

        await ClosedAsync(window);

        // A window that holds its panel stays open past that limit.
        var notes = await PopOutAsync(page, "Notes");
        await page.Clock.FastForwardAsync(11000);
        Assert.False(notes.IsClosed);
        await Expect(notes.GetByRole(AriaRole.Region, new() { Name = "Notes" })).ToBeVisibleAsync();
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Helpers_ShouldOpenAPanelsWindowAndBringThePanelBack_WhenUsed(string browser)
    {
        var page = await OpenDockAsync(browser);
        var dock = page.Tb().Dock("Workbench");

        var gear = await dock.PopOutAsync("Gear");
        var notes = await dock.PopOutAsync("Notes");

        // Each helper gives the panel in its window, whose page is the window's.
        await Expect(gear.GetByRole(AriaRole.Button, new() { Name = "Add gear" })).ToBeVisibleAsync();
        await Expect(notes.Page).ToHaveTitleAsync("Notes");
        Assert.Equal(["Gear", "Notes"], await dock.PoppedOutAsync());
        Assert.Equal(["Map"], await dock.PanelsAsync());

        await dock.ReturnAsync("Gear");

        Assert.Equal(["Gear"], await dock.FloatingAsync());
        Assert.Equal(["Notes"], await dock.PoppedOutAsync());
        await ClosedAsync(gear.Page);
        Assert.Equal("The panel \"Gear\" isn't in a browser window of its own.",
            (await Assert.ThrowsAsync<InvalidOperationException>(() => dock.ReturnAsync("Gear"))).Message);

        // A dock that doesn't allow it has no such item, and the helper says so.
        var plain = await OpenAsync(browser, "/dock-manager");
        await Expect(plain.Locator(".tb-dock")).ToHaveAttributeAsync("data-ready", "");
        Assert.Equal("The dock doesn't open panels in windows of their own. Turn on its AllowPopOut.",
            (await Assert.ThrowsAsync<InvalidOperationException>(() => plain.Tb().Dock("Hike planner")
                .PopOutAsync("Notes"))).Message);
    }
}
