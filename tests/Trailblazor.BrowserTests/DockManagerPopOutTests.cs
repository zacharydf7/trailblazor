using Deque.AxeCore.Playwright;
using Trailblazor.Testing.Playwright;

namespace Trailblazor.BrowserTests;

public class DockManagerPopOutTests(BrowserFixture fixture) : BrowserTest(fixture)
{
    protected override bool HoldsTheMouse => true;

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
    public async Task Panel_ShouldComeBackWhereItWasDockedWithItsState_WhenItsWindowClosesOrReturnIsPressed(
        string browser)
    {
        var page = await OpenDockAsync(browser);
        var window = await PopOutAsync(page, "Notes");
        await window.GetByLabel("Note", new() { Exact = true }).FillAsync("Fuel");

        await window.GetByRole(AriaRole.Button, new() { Name = "Return to the dock" }).ClickAsync();

        await Expect(page.GetByLabel("Note", new() { Exact = true })).ToHaveValueAsync("Fuel");
        await FocusedAsync(page.GetByRole(AriaRole.Tab, new() { Name = "Notes" }));
        Assert.Empty(await page.Tb().Dock("Workbench").FloatingAsync());
        Assert.Equal(["Notes", "Map", "Gear"], await page.Tb().Dock("Workbench").PanelsAsync());
        await page.Tb().Announcements.ExpectAnnouncedAsync("Notes is back in the dock.");
        Assert.True(window.IsClosed);

        // Closing the window brings it back too.
        var again = await PopOutAsync(page, "Notes");
        await again.GetByLabel("Note", new() { Exact = true }).FillAsync("Fuel and matches");
        await again.CloseAsync();

        await Expect(page.GetByLabel("Note", new() { Exact = true })).ToHaveValueAsync("Fuel and matches");
        await Expect(page.GetByRole(AriaRole.Tab, new() { Name = "Notes" })).ToBeVisibleAsync();
        Assert.Empty(await page.Tb().Dock("Workbench").FloatingAsync());
        Assert.Equal(["Notes", "Map", "Gear"], await page.Tb().Dock("Workbench").PanelsAsync());
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Page_ShouldKeepAWayBackToThePanel_WhenThePanelShowsInAWindowOfItsOwn(string browser)
    {
        var page = await OpenDockAsync(browser);
        var window = await PopOutAsync(page, "Notes");
        // Counts the times the page brings the panel's window to the front.
        await window.EvaluateAsync("() => { window.shown = 0; const focus = window.focus.bind(window); "
            + "window.focus = () => { window.shown++; focus(); }; }");
        var away = page.GetByRole(AriaRole.Group, new() { Name = "Panels in their own windows" });
        var notes = away.GetByRole(AriaRole.Button, new() { Name = "Notes" });
        await Expect(notes).ToHaveAccessibleDescriptionAsync("In its own browser window");
        var result = await away.RunAxe(AxeTests.WcagAA);
        Assert.Empty(result.Violations.Select(violation =>
            $"{violation.Id}: {string.Join(", ", violation.Nodes.Select(node => node.Target))}"));

        await notes.ClickAsync();
        await page.GetByRole(AriaRole.Menuitem, new() { Name = "Show its window" }).ClickAsync();

        Assert.Equal(1, await window.EvaluateAsync<int>("() => window.shown"));
        Assert.False(window.IsClosed);

        await notes.ClickAsync();
        await page.GetByRole(AriaRole.Menuitem, new() { Name = "Return to the dock" }).ClickAsync();

        await FocusedAsync(page.GetByRole(AriaRole.Tab, new() { Name = "Notes" }));
        await Expect(away).ToHaveCountAsync(0);
        await ClosedAsync(window);
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
    public async Task Panel_ShouldShowWhereItWasDocked_WhenTheAppTakesItAwayInItsWindowAndMakesItAgain(string browser)
    {
        var page = await OpenAsync(browser, "/dock-short-lived");
        await Expect(page.GetByRole(AriaRole.Group, new() { Name = "Trips" })).ToHaveAttributeAsync("data-ready", "");
        var dock = page.Tb().Dock("Trips");
        var editor = await dock.PopOutAsync("Editor");

        // The task ends in the panel's window: the app takes the panel away, and its window closes.
        await editor.GetByRole(AriaRole.Button, new() { Name = "Done" }).ClickAsync();
        await ClosedAsync(editor.Page);
        Assert.Equal(["List", "Map"], await dock.PanelsAsync());

        await page.GetByRole(AriaRole.Button, new() { Name = "Edit", Exact = true }).ClickAsync();

        // The panel is between the list and the map again, not in a window over them, and no new window opens.
        await Expect(page.GetByRole(AriaRole.Tabpanel, new() { Name = "Editor" })).ToBeVisibleAsync();
        Assert.Equal(["List", "Editor", "Map"], await dock.PanelsAsync());
        Assert.Empty(await dock.FloatingAsync());
        Assert.Empty(await dock.PoppedOutAsync());
        Assert.Single(page.Context.Pages);
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task PopOut_ShouldPutThePanelBackAndSaySo_WhenTheBrowserRefusesTheWindow(string browser)
    {
        var page = await OpenDockAsync(browser);
        await page.EvaluateAsync("window.open = () => null");

        await page.Tb().Dock("Workbench").ChooseFromTabMenuAsync("Notes", "Open in new window");

        await page.Tb().Announcements.ExpectAnnouncedAsync("The browser blocked the new window for Notes.");
        Assert.Empty(await page.Tb().Dock("Workbench").FloatingAsync());
        Assert.Equal(["Notes", "Map", "Gear"], await page.Tb().Dock("Workbench").PanelsAsync());
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

    // Adds a linked sheet to the page and waits until it has loaded.
    private static Task AddASheetAsync(IPage page, string id, string css) => page.EvaluateAsync("""
        ([id, css]) => new Promise(loaded => {
            const link = Object.assign(document.createElement('link'),
                { id, rel: 'stylesheet', href: `data:text/css,${css}`, onload: loaded });
            document.head.append(link);
        })
        """, new[] { id, css });

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Window_ShouldApplyTheSheetsThePageApplies_WhenASheetIsSwitchedWithoutAnAttribute(string browser)
    {
        var page = await OpenDockAsync(browser);
        // A linked sheet is turned off, through the sheet itself, before the window opens.
        await AddASheetAsync(page, "early", "html{text-indent:9px}");
        await page.EvaluateAsync("document.getElementById('early').sheet.disabled = true");
        await page.EvaluateAsync(
            "document.head.insertAdjacentHTML('beforeend', '<style id=inline>html { letter-spacing: 3px }</style>')");
        // A sheet with a `disabled` property of the app's own keeps it.
        await page.EvaluateAsync("""
            () => {
                document.head.insertAdjacentHTML('beforeend', '<style id=own></style>');
                Object.defineProperty(document.getElementById('own').sheet, 'disabled',
                    { configurable: true, value: false, writable: true });
            }
            """);
        var window = await PopOutAsync(page, "Notes");
        var (pageRoot, windowRoot) = (page.Locator("html"), window.Locator("html"));
        await Expect(windowRoot).ToHaveCSSAsync("letter-spacing", "3px");
        await Expect(windowRoot).ToHaveCSSAsync("text-indent", "0px");
        await page.EvaluateAsync("document.getElementById('early').sheet.disabled = false");
        await Expect(windowRoot).ToHaveCSSAsync("text-indent", "9px");

        // Turned off and on as from `document.styleSheets`.
        await page.EvaluateAsync(
            "[...document.styleSheets].find(sheet => sheet.ownerNode.id === 'inline').disabled = true");
        await Expect(pageRoot).ToHaveCSSAsync("letter-spacing", "normal");
        await Expect(windowRoot).ToHaveCSSAsync("letter-spacing", "normal");
        await page.EvaluateAsync("document.getElementById('inline').sheet.disabled = false");
        await Expect(windowRoot).ToHaveCSSAsync("letter-spacing", "3px");

        // A sheet that loads after the window opened is followed too.
        await AddASheetAsync(page, "late", "html{word-spacing:4px}");
        await Expect(windowRoot).ToHaveCSSAsync("word-spacing", "4px");
        await page.EvaluateAsync("document.getElementById('late').sheet.disabled = true");
        await Expect(windowRoot).ToHaveCSSAsync("word-spacing", "0px");

        // The usual way to turn on an alternate sheet: off and on again, which leaves its attributes as they were.
        await page.EvaluateAsync("""
            () => {
                document.head.insertAdjacentHTML('beforeend', '<link id=other rel="alternate stylesheet" '
                    + 'title="Other" href="data:text/css,html{text-transform:uppercase}">');
                return new Promise(loaded => document.getElementById('other').onload = loaded);
            }
            """);
        await Expect(windowRoot).ToHaveCSSAsync("text-transform", "none");
        await page.EvaluateAsync("""
            () => {
                const other = document.getElementById('other');
                other.disabled = true;
                other.disabled = false;
            }
            """);
        await Expect(pageRoot).ToHaveCSSAsync("text-transform", "uppercase");
        await Expect(windowRoot).ToHaveCSSAsync("text-transform", "uppercase");
        // So is one that is added and turned on in the same go, before the window has a copy of it.
        await page.EvaluateAsync("""
            () => {
                document.head.insertAdjacentHTML('beforeend', '<link id=another rel="alternate stylesheet" '
                    + 'title="Another" href="data:text/css,html{font-style:italic}">');
                const another = document.getElementById('another');
                another.disabled = true;
                another.disabled = false;
            }
            """);
        await Expect(pageRoot).ToHaveCSSAsync("font-style", "italic");
        await Expect(windowRoot).ToHaveCSSAsync("font-style", "italic");

        // A second window goes on following after the first one closes.
        var second = await PopOutAsync(page, "Gear");
        await window.CloseAsync();
        await ClosedAsync(window);
        await page.EvaluateAsync("document.getElementById('late').sheet.disabled = false");
        await Expect(second.Locator("html")).ToHaveCSSAsync("word-spacing", "4px");

        // With the last window gone, the page's sheets are as they were before the first one opened.
        await second.CloseAsync();
        await ClosedAsync(second);
        Assert.Equal(["own"], await page.EvaluateAsync<string[]>("""
            () => [...document.styleSheets].filter(sheet => Object.hasOwn(sheet, 'disabled'))
                .map(sheet => sheet.ownerNode.id)
            """));
    }

    // Adds a sheet that the page only offers as an alternate, and waits until it has loaded.
    private static Task AddAnAlternateAsync(IPage page, string id, string address,
        string rel = "alternate stylesheet") => page.EvaluateAsync("""
        ([id, href, rel]) => new Promise(loaded => {
            const link = Object.assign(document.createElement('link'), { id, rel, title: id, href, onload: loaded });
            document.head.append(link);
        })
        """, new[] { id, address, rel });

    private const string TurnOn = """
        id => {
            const link = document.getElementById(id);
            link.disabled = true;
            link.disabled = false;
        }
        """;

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Window_ShouldApplyAnAlternateSheet_WhenAScriptTurnedItOnBeforeTheWindowOpened(string browser)
    {
        var page = await OpenDockAsync(browser);
        await AddAnAlternateAsync(page, "chosen", "data:text/css,html{text-transform:uppercase}");
        await AddAnAlternateAsync(page, "offered", "data:text/css,html{font-style:italic}");
        // A link's types count whatever case they are written in.
        await AddAnAlternateAsync(page, "shouted", "data:text/css,html{text-indent:9px}", "Alternate StyleSheet");
        await page.EvaluateAsync(TurnOn, "chosen");
        await page.EvaluateAsync(TurnOn, "shouted");
        await Expect(page.Locator("html")).ToHaveCSSAsync("text-transform", "uppercase");
        await Expect(page.Locator("html")).ToHaveCSSAsync("text-indent", "9px");
        await page.EvaluateAsync("""
            () => {
                window.asked = [];
                const insert = CSSStyleSheet.prototype.insertRule;
                CSSStyleSheet.prototype.insertRule = function (...rule) {
                    asked.push(this.ownerNode.id || this.ownerNode.localName);
                    return insert.apply(this, rule);
                };
            }
            """);

        var window = await PopOutAsync(page, "Notes");

        var windowRoot = window.Locator("html");
        await Expect(windowRoot).ToHaveCSSAsync("text-transform", "uppercase");
        await Expect(windowRoot).ToHaveCSSAsync("text-indent", "9px");
        await Expect(windowRoot).ToHaveCSSAsync("font-style", "normal");
        // Only the alternates were asked, each once, and asking left the page's sheets as they were.
        Assert.Equal(["chosen", "offered", "shouted"], await page.EvaluateAsync<string[]>("asked"));
        var left = await page.EvaluateAsync<int[]>("""
            () => [...asked.map(id => document.getElementById(id).sheet.cssRules.length),
                getComputedStyle(document.head).getPropertyValue('--tb-sheet-applies').length]
            """);
        Assert.Equal([1, 1, 1, 0], left);
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Window_ShouldApplyAnAlternateSheet_WhenItsLinkLeavesTheHeadAndComesBack(string browser)
    {
        var page = await OpenDockAsync(browser);
        await AddAnAlternateAsync(page, "chosen", "data:text/css,html{text-transform:uppercase}");
        await page.EvaluateAsync(TurnOn, "chosen");
        var window = await PopOutAsync(page, "Notes");
        var (pageRoot, windowRoot) = (page.Locator("html"), window.Locator("html"));
        await Expect(windowRoot).ToHaveCSSAsync("text-transform", "uppercase");
        await page.EvaluateAsync("() => { window.gone = document.getElementById('chosen'); gone.remove(); }");
        await Expect(windowRoot).ToHaveCSSAsync("text-transform", "none");

        await page.EvaluateAsync("document.head.append(gone)");

        // The link is still on when it is back, and so is the new copy the window makes of it.
        await Expect(pageRoot).ToHaveCSSAsync("text-transform", "uppercase");
        await Expect(windowRoot).ToHaveCSSAsync("text-transform", "uppercase");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Window_ShouldApplyAnAlternateSheet_WhenItWasTurnedOnAndIsStillLoadingAsTheWindowOpens(
        string browser)
    {
        var page = await OpenDockAsync(browser);
        // The test host holds the sheet back until its gate is opened, which is after the window has opened.
        var gate = Guid.NewGuid().ToString("N");
        await page.EvaluateAsync("""
            gate => {
                const css = encodeURIComponent('html{text-transform:uppercase}');
                const link = Object.assign(document.createElement('link'), { id: 'slow', rel: 'alternate stylesheet',
                    title: 'slow', href: `slow.css?wait=0&gate=${gate}&css=${css}` });
                document.head.append(link);
                link.disabled = true;
                link.disabled = false;
            }
            """, gate);

        var window = await page.RunAndWaitForPopupAsync(() =>
            page.Tb().Dock("Workbench").ChooseFromTabMenuAsync("Notes", "Open in new window"));

        Assert.True(await page.EvaluateAsync<bool>("document.getElementById('slow').sheet === null"));
        var open = new Uri(Fixture.BaseAddress, $"/open-gate?gate={gate}").ToString();
        Assert.True((await page.Context.APIRequest.GetAsync(open)).Ok, "The gate didn't open.");
        await Expect(window.Locator("html")).ToHaveCSSAsync("text-transform", "uppercase");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Window_ShouldLeaveAnAlternateSheetOff_WhenItComesFromAnotherOriginAndWasNeverTurnedOn(
        string browser)
    {
        var page = await OpenDockAsync(browser);
        // The test host under another name is another origin, so the page's scripts can't read its sheet's rules.
        var address = new UriBuilder(page.Url)
            { Host = "localhost", Path = "/slow.css", Query = "wait=0&css=html%7Btext-transform:uppercase%7D" };
        await AddAnAlternateAsync(page, "other", address.Uri.ToString());

        var window = await PopOutAsync(page, "Notes");

        await window.WaitForFunctionAsync("document.querySelector('link[title=other]')?.sheet");
        await Expect(window.Locator("html")).ToHaveCSSAsync("text-transform", "none");
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
        Assert.Empty(await inner.FloatingAsync());
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Window_ShouldTakeTheFocusOnItsPanelBeforeTheReturnButton_WhenItOpensFromTheKeyboard(
        string browser)
    {
        var page = await OpenDockAsync(browser);
        await page.GetByRole(AriaRole.Tab, new() { Name = "Notes" }).FocusAsync();

        // The menu's item is chosen with the keyboard, which lets a page open a window as a click does.
        var window = await page.RunAndWaitForPopupAsync(async () =>
        {
            await page.Keyboard.PressAsync("Shift+F10");
            await page.GetByRole(AriaRole.Menuitem, new() { Name = "Open in new window" }).FocusAsync();
            await page.Keyboard.PressAsync("Enter");
        });

        var notes = window.GetByRole(AriaRole.Region, new() { Name = "Notes" });
        await FocusedAsync(notes);
        // The panel starts with its Return button there, so it is no tab stop, though it has the focus.
        await Expect(notes).ToHaveAttributeAsync("tabindex", "-1");
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

        // The notes have gone to a window of their own since, so the gear goes beside the map, which was next to it.
        Assert.Empty(await dock.FloatingAsync());
        Assert.Equal(["Map", "Gear"], await dock.PanelsAsync());
        Assert.Equal(["Notes"], await dock.PoppedOutAsync());
        await ClosedAsync(gear.Page);
        Assert.Equal("The panel \"Gear\" isn't in a browser window of its own.",
            (await Assert.ThrowsAsync<InvalidOperationException>(() => dock.ReturnAsync("Gear"))).Message);

        // A dock that doesn't allow it has no such item, and the helper says so.
        var plain = await OpenAsync(browser, "/dock-manager");
        await Expect(plain.Locator(".tb-dock")).ToHaveAttributeAsync("data-ready", "");
        Assert.Equal("The panel \"Notes\" can't open in a window of its own: the dock's AllowPopOut is off, or the "
            + "panel's is.",
            (await Assert.ThrowsAsync<InvalidOperationException>(() => plain.Tb().Dock("Hike planner")
                .PopOutAsync("Notes"))).Message);
    }

    // Opens the page whose Tools panel holds components that lean on their scripts, with that panel in a window of
    // its own, narrower than the page's.
    private async Task<(IPage Page, IPage Window)> OpenToolsInAWindowAsync(string browser,
        string path = "/dock-popped-parts")
    {
        var page = await OpenAsync(browser, path);
        await Expect(page.GetByRole(AriaRole.Group, new() { Name = "Bench" })).ToHaveAttributeAsync("data-ready", "");
        var window = (await page.Tb().Dock("Bench").PopOutAsync("Tools")).Page;
        await window.SetViewportSizeAsync(900, 600);
        return (page, window);
    }

    private static async Task<(float Top, float Bottom, float Left, float Right)> EdgesAsync(ILocator element)
    {
        var box = (await element.BoundingBoxAsync())!;
        return (box.Y, box.Y + box.Height, box.X, box.X + box.Width);
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Menu_ShouldOpenBelowItsTriggerAndKeepTheFocus_WhenFirstUsedInAPanelsWindow(string browser)
    {
        var (page, window) = await OpenToolsInAWindowAsync(browser);
        // A window that is covered or minimized draws no frames, and the page's own may be.
        await page.EvaluateAsync("() => window.requestAnimationFrame = () => 0");
        var trigger = window.GetByRole(AriaRole.Button, new() { Name = "Insert" });

        await trigger.ClickAsync();

        var menu = window.GetByRole(AriaRole.Menu, new() { Name = "Insert" });
        await Expect(menu).ToBeVisibleAsync();
        await FocusedAsync(window.GetByRole(AriaRole.Menuitem, new() { Name = "Shape" }));
        Assert.InRange((await EdgesAsync(menu)).Top - (await EdgesAsync(trigger)).Bottom, 0, 12);

        // The keys work on the focus in the panel's window: into the submenu, back out, and back to the trigger.
        await window.Keyboard.PressAsync("ArrowRight");
        await FocusedAsync(window.GetByRole(AriaRole.Menuitem, new() { Name = "Circle" }));
        await window.Keyboard.PressAsync("Escape");
        await FocusedAsync(window.GetByRole(AriaRole.Menuitem, new() { Name = "Shape" }));
        await window.Keyboard.PressAsync("ArrowDown");
        await window.Keyboard.PressAsync("Enter");
        await Expect(window.GetByTestId("chosen")).ToHaveTextAsync("Table");
        await Expect(menu).ToBeHiddenAsync();
        await FocusedAsync(trigger);
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Window_ShouldHaveNoAccessibilityViolations_WhenItShowsAPanel(string browser)
    {
        var (_, window) = await OpenToolsInAWindowAsync(browser);

        var result = await window.RunAxe(AxeTests.WcagAA);

        Assert.Empty(result.Violations.Select(violation =>
            $"{violation.Id}: {string.Join(", ", violation.Nodes.Select(node => node.Target))}"));
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Components_ShouldGetTheirScripts_WhenFirstDrawnInAPanelsWindow(string browser)
    {
        var (page, window) = await OpenToolsInAWindowAsync(browser);

        await window.GetByRole(AriaRole.Button, new() { Name = "Show more" }).ClickAsync();

        // The field that asks for the focus gets it, the select's script opens its list, and the tooltip describes
        // its trigger and shows.
        await FocusedAsync(window.GetByLabel("Late note"));
        await window.GetByRole(AriaRole.Combobox, new() { Name = "Late size" }).ClickAsync();
        await window.GetByRole(AriaRole.Option, new() { Name = "Large" }).ClickAsync();
        await Expect(window.GetByTestId("size")).ToHaveTextAsync("Large");
        var share = window.GetByRole(AriaRole.Button, new() { Name = "Share" });
        await Expect(share).ToHaveAccessibleDescriptionAsync("Sends it on");
        await share.HoverAsync();
        await Expect(window.GetByRole(AriaRole.Tooltip)).ToHaveTextAsync("Sends it on");

        // Blazor finds those elements through the page's own `querySelector`, which looks in a window only for
        // Blazor's marks, and is the browser's again once no panel is away.
        const string own = "() => Object.hasOwn(document, 'querySelector')";
        Assert.True(await page.EvaluateAsync<bool>(own));
        Assert.False(await page.EvaluateAsync<bool>("() => document.querySelector('[data-part=return-bar]') !== null"));
        await page.Tb().Dock("Bench").ReturnAsync("Tools");
        Assert.False(await page.EvaluateAsync<bool>(own));
    }

    // Gives the page a linked sheet that takes this long to load. The browser hands a window the sheets the page has
    // already loaded right away, so only one that is still loading keeps a window waiting. It is a sheet for print,
    // since a page that waits for one of its screen's sheets draws no frames, which Playwright's waits count on.
    private static Task AddASlowSheetAsync(IPage page, int milliseconds) => page.EvaluateAsync("""
        wait => document.head.append(Object.assign(document.createElement('link'),
            { rel: 'stylesheet', href: `slow.css?wait=${wait}`, media: 'print' }))
        """, milliseconds);

    // Chooses Open in new window for the panel and gives the window as soon as it opens. The dock's helper would wait
    // for the panel to arrive.
    private static Task<IPage> StartOpeningAsync(IPage page, string title) => page.RunAndWaitForPopupAsync(async () =>
    {
        await page.GetByRole(AriaRole.Button, new() { Name = $"Options for {title}" }).ClickAsync();
        await page.GetByRole(AriaRole.Menuitem, new() { Name = "Open in new window" }).ClickAsync();
    });

    private static Task<IPage> StartOpeningToolsAsync(IPage page) => StartOpeningAsync(page, "Tools");

    // Waits until the dock has hidden the panel that is to leave, and then long enough for a panel that didn't wait
    // for its window's sheets to have left.
    private static async Task<ILocator> AboutToLeaveAsync(IPage page, string name)
    {
        var panel = page.Locator($"[data-part=panel][data-panel={name}]");
        await Expect(panel).ToHaveAttributeAsync("data-popped", "");
        await page.WaitForTimeoutAsync(300);
        return panel;
    }

    private static Task<ILocator> ToolsAboutToLeaveAsync(IPage page) => AboutToLeaveAsync(page, "tools");

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Panel_ShouldArriveInItsWindowStyled_WhenTheWindowsSheetsLoadSlowly(string browser)
    {
        var page = await OpenAsync(browser, "/dock-popped-parts");
        await Expect(page.GetByRole(AriaRole.Group, new() { Name = "Bench" })).ToHaveAttributeAsync("data-ready", "");
        await AddASlowSheetAsync(page, 1500);

        var window = await StartOpeningToolsAsync(page);

        // The panel waits on the page, out of sight, and moves in once its window can lay it out.
        var tools = await ToolsAboutToLeaveAsync(page);
        Assert.Equal(1, await tools.CountAsync());
        await Expect(tools).ToBeHiddenAsync();
        await Expect(window.Locator("[data-part=return-bar]")).ToHaveCSSAsync("position", "sticky");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Panel_ShouldArriveInItsWindowAnyway_WhenTheWindowsSheetsNeverLoad(string browser)
    {
        var page = await OpenAsync(browser, "/dock-popped-parts");
        await Expect(page.GetByRole(AriaRole.Group, new() { Name = "Bench" })).ToHaveAttributeAsync("data-ready", "");
        await AddASlowSheetAsync(page, 60_000);

        var window = await StartOpeningToolsAsync(page);

        await Expect(window.GetByRole(AriaRole.Region, new() { Name = "Tools" })).ToBeAttachedAsync();
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Panel_ShouldArriveInItsWindowWithoutWaiting_WhenASheetFailsToLoadOrIsTurnedOff(string browser)
    {
        var page = await OpenAsync(browser, "/dock-popped-parts");
        await Expect(page.GetByRole(AriaRole.Group, new() { Name = "Bench" })).ToHaveAttributeAsync("data-ready", "");
        await page.EvaluateAsync("""
            () => document.head.append(
                Object.assign(document.createElement('link'), { rel: 'stylesheet', href: 'no-such-sheet.css' }),
                Object.assign(document.createElement('link'),
                    { rel: 'stylesheet', href: 'slow.css?wait=60000', disabled: true }))
            """);

        var window = await StartOpeningToolsAsync(page);

        // Well inside the three seconds a sheet that never answers is given.
        await Expect(window.GetByRole(AriaRole.Region, new() { Name = "Tools" }))
            .ToBeAttachedAsync(new() { Timeout = 1500 });
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Panel_ShouldStayInTheDock_WhenItsWindowIsClosedBeforeItArrives(string browser)
    {
        var page = await OpenAsync(browser, "/dock-popped-parts");
        await Expect(page.GetByRole(AriaRole.Group, new() { Name = "Bench" })).ToHaveAttributeAsync("data-ready", "");
        await AddASlowSheetAsync(page, 2000);
        var window = await StartOpeningToolsAsync(page);
        var tools = await ToolsAboutToLeaveAsync(page);

        await window.CloseAsync();

        // The slow sheet arrives, and the dock learns that the window went: the panel is back where it was docked,
        // and nothing is said of a refused window: that would be said in the same render, so checking now is enough.
        await Expect(tools).ToBeVisibleAsync();
        Assert.Empty(await page.Tb().Dock("Bench").FloatingAsync());
        await Expect(page.Locator("[data-part=announcer]")).Not.ToContainTextAsync("blocked");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Window_ShouldClose_WhenTheAppClosesThePanelBeforeItArrives(string browser)
    {
        var page = await OpenDockAsync(browser);
        await AddASlowSheetAsync(page, 60_000);
        var window = await StartOpeningAsync(page, "Notes");
        await AboutToLeaveAsync(page, "notes");

        await page.GetByRole(AriaRole.Button, new() { Name = "Close notes" }).ClickAsync();

        await ClosedAsync(window);
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Window_ShouldClose_WhenThePageGoesAwayBeforeThePanelArrives(string browser)
    {
        var page = await OpenAsync(browser, "/dock-popped-parts");
        await Expect(page.GetByRole(AriaRole.Group, new() { Name = "Bench" })).ToHaveAttributeAsync("data-ready", "");
        await AddASlowSheetAsync(page, 60_000);
        var window = await StartOpeningToolsAsync(page);
        await ToolsAboutToLeaveAsync(page);

        await page.GotoAsync("about:blank");

        await ClosedAsync(window);
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Panel_ShouldKeepItsScrollPosition_WhenItGoesToAWindowAndComesBack(string browser)
    {
        var page = await OpenAsync(browser, "/dock-popped-parts");
        await Expect(page.GetByRole(AriaRole.Group, new() { Name = "Bench" })).ToHaveAttributeAsync("data-ready", "");
        var dock = page.Tb().Dock("Bench");
        await dock.PanelElement("Tools").EvaluateAsync("panel => panel.scrollTop = 240");

        var popped = await dock.PopOutAsync("Tools");

        Assert.Equal(240, await popped.EvaluateAsync<int>("panel => panel.scrollTop"));

        await popped.EvaluateAsync("panel => panel.scrollTop = 120");
        // Clicked from script: Playwright's own click would first scroll the panel back to its top, where the bar
        // sits in the layout, and lose the position under test.
        await popped.Locator("[data-part=return]").EvaluateAsync("button => button.click()");

        var tools = page.Locator("[data-part=panel][data-panel=tools]");
        await Expect(tools).ToBeVisibleAsync();
        Assert.Equal(120, await tools.EvaluateAsync<int>("panel => panel.scrollTop"));

        // A window that is closed brings its panel back hidden, until the dock has drawn it as its own again.
        popped = await dock.PopOutAsync("Tools");
        await popped.EvaluateAsync("panel => panel.scrollTop = 60");
        await popped.Page.CloseAsync();

        await Expect(tools).ToBeVisibleAsync();
        await page.WaitForFunctionAsync("panel => panel.scrollTop === 60", await tools.ElementHandleAsync());
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Panel_ShouldKeepItsScrollPosition_WhenThePageBringsItBack(string browser)
    {
        var page = await OpenAsync(browser, "/dock-popped-parts");
        await Expect(page.GetByRole(AriaRole.Group, new() { Name = "Bench" })).ToHaveAttributeAsync("data-ready", "");
        var popped = await page.Tb().Dock("Bench").PopOutAsync("Tools");
        await popped.EvaluateAsync("panel => panel.scrollTop = 120");
        var away = page.GetByRole(AriaRole.Group, new() { Name = "Panels in their own windows" });

        await away.GetByRole(AriaRole.Button, new() { Name = "Tools" }).ClickAsync();
        await page.GetByRole(AriaRole.Menuitem, new() { Name = "Return to the dock" }).ClickAsync();

        var tools = page.Locator("[data-part=panel][data-panel=tools]");
        await Expect(tools).ToBeVisibleAsync();
        await page.WaitForFunctionAsync("panel => panel.scrollTop === 120", await tools.ElementHandleAsync());
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Panel_ShouldKeepItsScrollPosition_WhenAnotherPanelComesBackWhileItIsOnItsWay(string browser)
    {
        var page = await OpenAsync(browser, "/dock-popped-parts");
        await Expect(page.GetByRole(AriaRole.Group, new() { Name = "Bench" })).ToHaveAttributeAsync("data-ready", "");
        var dock = page.Tb().Dock("Bench");
        var other = await dock.PopOutAsync("Other");
        await dock.PanelElement("Tools").EvaluateAsync("panel => panel.scrollTop = 240");
        await AddASlowSheetAsync(page, 1500);
        var window = await StartOpeningToolsAsync(page);
        await ToolsAboutToLeaveAsync(page);

        await other.Page.CloseAsync();

        var tools = window.GetByRole(AriaRole.Region, new() { Name = "Tools" });
        await Expect(tools).ToBeAttachedAsync();
        Assert.Equal(240, await tools.EvaluateAsync<int>("panel => panel.scrollTop"));
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Panel_ShouldKeepItsScrollPosition_WhenAnotherPanelLeavesWhileItIsOnItsWay(string browser)
    {
        var page = await OpenAsync(browser, "/dock-popped-parts");
        await Expect(page.GetByRole(AriaRole.Group, new() { Name = "Bench" })).ToHaveAttributeAsync("data-ready", "");
        await page.Tb().Dock("Bench").PanelElement("Tools").EvaluateAsync("panel => panel.scrollTop = 240");
        await AddASlowSheetAsync(page, 1500);
        var window = await StartOpeningToolsAsync(page);
        await ToolsAboutToLeaveAsync(page);

        await StartOpeningAsync(page, "Other");

        var tools = window.GetByRole(AriaRole.Region, new() { Name = "Tools" });
        await Expect(tools).ToBeAttachedAsync();
        Assert.Equal(240, await tools.EvaluateAsync<int>("panel => panel.scrollTop"));
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task RelativeAddress_ShouldCountFromTheAppsBase_WhenThePanelsWindowOpensFromADeeperPage(string browser)
    {
        var (page, window) = await OpenToolsInAWindowAsync(browser, "/dock-popped-parts/deeper");

        var address = await window.GetByRole(AriaRole.Link, new() { Name = "Guide" })
            .EvaluateAsync<string>("link => link.href");

        Assert.Equal(new Uri(new Uri(page.Url), "/guides/start").ToString(), address);
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Announcement_ShouldBeSaidInThePanelsWindowToo_WhenMadeWhileThePanelIsThere(string browser)
    {
        var page = await OpenAsync(browser, "/dock-popped-parts");
        await Expect(page.GetByRole(AriaRole.Group, new() { Name = "Bench" })).ToHaveAttributeAsync("data-ready", "");
        await page.GetByRole(AriaRole.Button, new() { Name = "Move Rule 1", Exact = true }).FocusAsync();
        await page.Keyboard.PressAsync("ArrowDown");
        await page.Tb().Announcements.ExpectAnnouncedAsync("Rule 1 moved to position 2 of 3.");
        await AddASlowSheetAsync(page, 1500);

        var window = await StartOpeningToolsAsync(page);
        await ToolsAboutToLeaveAsync(page);

        // The window's live regions are there before its panel and before any message, and empty, without what the
        // page was told earlier: a region that arrives holding text isn't spoken.
        var regions = window.Locator("[data-part=announcer] > [aria-live]");
        Assert.Equal(["status:", "alert:"], await regions.EvaluateAllAsync<string[]>(
            "regions => regions.map(region => `${region.getAttribute('role')}:${region.textContent}`)"));

        // The dock's own message, made as the panel arrives, and then a component's.
        await window.Tb().Announcements.ExpectAnnouncedAsync("Tools opened in a new window.");
        await window.GetByRole(AriaRole.Button, new() { Name = "Move Rule 1", Exact = true }).FocusAsync();
        await window.Keyboard.PressAsync("ArrowUp");

        await window.Tb().Announcements.ExpectAnnouncedAsync("Rule 1 moved to position 1 of 3.");
        await page.Tb().Announcements.ExpectAnnouncedAsync("Rule 1 moved to position 1 of 3.");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Field_ShouldSayTheFocusLeftOnlyWhenItLeavesTheField_WhenItIsInAPanelsWindow(string browser)
    {
        var (_, window) = await OpenToolsInAWindowAsync(browser);
        await window.GetByLabel("Code", new() { Exact = true }).FocusAsync();

        // The focus goes to the field's own Clear button, which isn't leaving the field, and then out of it.
        await window.GetByRole(AriaRole.Button, new() { Name = "Clear", Exact = true }).FocusAsync();
        await window.GetByTestId("first").FocusAsync();

        await Expect(window.GetByTestId("code-blurs")).ToHaveTextAsync("1");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Page_ShouldNeitherScrollNorGoBack_WhenKeysThatDoSoArePressedInAPanelsWindow(string browser)
    {
        var (page, window) = await OpenToolsInAWindowAsync(browser);
        await page.EvaluateAsync("""
            () => {
                const tall = document.createElement('div');
                tall.style.blockSize = '300vh';
                document.body.append(tall);
                history.pushState(null, '', '#keys');
            }
            """);
        var code = window.GetByLabel("Code", new() { Exact = true });
        await code.FocusAsync();

        foreach (var key in new[] { "End", "Space", "PageDown", "ArrowDown", "Backspace", "Backspace" })
        {
            await window.Keyboard.PressAsync(key);
        }

        // The keys did their work in the field, and the page is where it was: not scrolled, and not a page back.
        await Expect(code).ToHaveValueAsync("A");
        Assert.Equal("#keys 0", await page.EvaluateAsync<string>("""
            () => new Promise(done => requestAnimationFrame(() => requestAnimationFrame(
                () => done(`${location.hash} ${scrollY}`))))
            """));
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Key_ShouldDoNothing_WhenAListenerOnThePagePreventsItFromAPanelsWindow(string browser)
    {
        var (page, window) = await OpenToolsInAWindowAsync(browser);
        // The two ways a listener has to prevent a key: the method, and the older property.
        await page.EvaluateAsync("""
            () => document.addEventListener('keydown', event => {
                if (event.key === 'x') {
                    event.preventDefault();
                } else if (event.key === 'y') {
                    event.returnValue = false;
                }
            })
            """);
        var code = window.GetByLabel("Code", new() { Exact = true });
        await code.FocusAsync();
        await window.Keyboard.PressAsync("End");

        await window.Keyboard.TypeAsync("xyz");

        await Expect(code).ToHaveValueAsync("A1z");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Key_ShouldTellThePageWhetherItWasPrevented_WhenAListenerInThePanelsWindowPreventsIt(
        string browser)
    {
        var (page, window) = await OpenToolsInAWindowAsync(browser);
        var code = window.GetByLabel("Code", new() { Exact = true });
        await code.EvaluateAsync(
            "input => input.addEventListener('keydown', event => event.key === 'w' && event.preventDefault())");
        await page.EvaluateAsync("""
            () => document.addEventListener('keydown', event => {
                if (event.key.length === 1) {
                    document.body.dataset.told = `${document.body.dataset.told ?? ''}${event.key} ` +
                        `${event.defaultPrevented} ${event.returnValue}, `;
                }
            })
            """);
        await code.FocusAsync();
        await window.Keyboard.PressAsync("End");

        await window.Keyboard.TypeAsync("wv");

        await Expect(code).ToHaveValueAsync("A1v");
        await Expect(page.Locator("body")).ToHaveAttributeAsync("data-told", "w true false, v false true, ");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Select_ShouldDrawTheOptionsItScrollsTo_WhenItsLongListIsFirstDrawnInAPanelsWindow(
        string browser)
    {
        var (_, window) = await OpenToolsInAWindowAsync(browser);
        await window.GetByRole(AriaRole.Button, new() { Name = "Show more" }).ClickAsync();
        var late = window.GetByRole(AriaRole.Combobox, new() { Name = "Late trail", Exact = true });
        await Expect(late).ToHaveAttributeAsync("data-ready", "");
        await late.FocusAsync();

        // Blazor's Virtualize hears of the scroll from an observer, which Firefox runs only in a window that draws.
        await late.PressAsync("End");

        await Expect(window.Locator("[role=option][data-text='Trail 1000']")).ToBeAttachedAsync();
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task IntersectionObserver_ShouldWatchEachTargetInItsOwnWindow_WhenItsTargetsAreInTwoWindows(
        string browser)
    {
        var page = await OpenAsync(browser, "/dock-popped-parts");
        await Expect(page.GetByRole(AriaRole.Group, new() { Name = "Bench" })).ToHaveAttributeAsync("data-ready", "");
        // One observer of the button at the end of the panel's content and of a mark below the page's own fold. It
        // notes what it is told of each, and each time the button came into view.
        await page.GetByTestId("last").EvaluateAsync("""
            last => {
                const root = document.documentElement;
                const mark = document.body.appendChild(document.createElement('p'));
                mark.dataset.testid = 'mark';
                mark.style.marginBlockStart = '200vh';
                mark.textContent = 'Mark';
                window.watcher = new IntersectionObserver(entries => {
                    for (const entry of entries) {
                        root.dataset[entry.target.dataset.testid] = entry.isIntersecting;
                        if (entry.target === last && entry.isIntersecting) {
                            root.dataset.shown = `${root.dataset.shown ?? ''}+`;
                        }
                    }
                });
                watcher.observe(last);
                watcher.observe(mark);
            }
            """);
        var seen = page.Locator("html");
        await Expect(seen).ToHaveAttributeAsync("data-mark", "false");
        var window = (await page.Tb().Dock("Bench").PopOutAsync("Tools")).Page;

        await page.BringToFrontAsync();
        await page.GetByTestId("mark").ScrollIntoViewIfNeededAsync();
        await Expect(seen).ToHaveAttributeAsync("data-mark", "true");

        await window.BringToFrontAsync();
        await window.GetByTestId("last").ScrollIntoViewIfNeededAsync();
        await Expect(seen).ToHaveAttributeAsync("data-last", "true");
        // The page's window, which still watches the mark, no longer watches the button that left it.
        await page.BringToFrontAsync();
        Assert.Equal("+", await page.EvaluateAsync<string>("""
            () => new Promise(done => requestAnimationFrame(() => requestAnimationFrame(
                () => done(document.documentElement.dataset.shown))))
            """));
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task IntersectionObserver_ShouldFollowWhatItWatches_WhenItsPanelGoesToAWindowAndComesBack(
        string browser)
    {
        var page = await OpenAsync(browser, "/dock-popped-parts");
        await Expect(page.GetByRole(AriaRole.Group, new() { Name = "Bench" })).ToHaveAttributeAsync("data-ready", "");
        // An observer of the app's own, made on the page, of the buttons at the two ends of the panel's tall content.
        // It notes what it is told of each, whether it was handed itself, and each time the last one came into view.
        await page.GetByTestId("last").EvaluateAsync("""
            last => {
                const root = document.documentElement;
                window.watcher = new IntersectionObserver(function (entries, observer) {
                    for (const entry of entries) {
                        root.dataset[entry.target.dataset.testid] =
                            `${entry.isIntersecting} ${observer === watcher && this === watcher}`;
                        if (entry.target === last && entry.isIntersecting) {
                            root.dataset.shown = `${root.dataset.shown ?? ''}+`;
                        }

                        root.dataset.told = `${root.dataset.told ?? ''}${entry.target.dataset.testid[0]}`;
                    }
                });
                watcher.observe(document.querySelector('[data-testid=first]'));
                watcher.observe(last);
            }
            """);
        var seen = page.Locator("html");
        await Expect(seen).ToHaveAttributeAsync("data-first", "true true");
        await Expect(seen).ToHaveAttributeAsync("data-last", "false true");
        // Watching the first one again tells nothing new.
        Assert.Equal(2, await page.GetByTestId("first").EvaluateAsync<int>("""
            first => {
                watcher.observe(first);
                return new Promise(done => requestAnimationFrame(() => requestAnimationFrame(
                    () => done(document.documentElement.dataset.told.length))));
            }
            """));
        // It answers for its options and its records as the browser's does, has the newer members only where the
        // browser has them, and refuses what the browser refuses: no callback, options it can't read, and a target
        // that is no element, of which it then keeps nothing.
        Assert.Equal("null, 0px 0px 0px 0px, 0, true, true, true, true, true", await page.EvaluateAsync<string>("""
            () => {
                const refuses = action => {
                    try {
                        action();
                        return false;
                    } catch {
                        return true;
                    }
                };
                return [`${watcher.root}`, watcher.rootMargin, watcher.thresholds, Array.isArray(watcher.takeRecords()),
                    'trackVisibility' in watcher === 'isVisible' in IntersectionObserverEntry.prototype,
                    refuses(() => new IntersectionObserver(null)),
                    refuses(() => new IntersectionObserver(() => { }, { rootMargin: 'wide' })),
                    refuses(() => watcher.observe(document.createTextNode('text')))].join(', ');
            }
            """));
        // It lets the first one go, and still follows the other.
        await page.GetByTestId("first").EvaluateAsync("""
            first => {
                watcher.unobserve(first);
                delete document.documentElement.dataset.first;
            }
            """);

        var window = (await page.Tb().Dock("Bench").PopOutAsync("Tools")).Page;
        // The last one is told of again as it arrives in the panel's window, as by a new observer.
        await Expect(seen).ToHaveAttributeAsync("data-told", "fll");
        await window.GetByTestId("last").ScrollIntoViewIfNeededAsync();
        await Expect(seen).ToHaveAttributeAsync("data-last", "true true");
        await window.GetByTestId("first").ScrollIntoViewIfNeededAsync();
        await Expect(seen).ToHaveAttributeAsync("data-last", "false true");

        await page.Tb().Dock("Bench").ReturnAsync("Tools");
        await page.GetByTestId("last").ScrollIntoViewIfNeededAsync();
        await Expect(seen).ToHaveAttributeAsync("data-last", "true true");
        // The last one came into view once in the panel's window and once back on the page, each told once, and the
        // observer heard nothing more of the first one.
        Assert.Equal("++ none", await page.EvaluateAsync<string>("""
            () => new Promise(done => requestAnimationFrame(() => requestAnimationFrame(() => done(
                `${document.documentElement.dataset.shown} ${document.documentElement.dataset.first ?? 'none'}`))))
            """));
    }

    private static ILocator Toast(IPage page, string text = "Tools saved") =>
        page.Locator("[data-part=toast]").Filter(new() { HasText = text });

    // A press on the page beside the dock, on nothing that takes the focus.
    private static Task PressBesideTheDockAsync(IPage page) => page.Mouse.ClickAsync(1100, 600);

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Toast_ShouldShowInTheWindowTheUserIsWorkingIn_WhenAPanelIsInAWindowOfItsOwn(string browser)
    {
        var (page, window) = await OpenToolsInAWindowAsync(browser);
        var save = window.GetByRole(AriaRole.Button, new() { Name = "Show a toast" });

        // WebKit doesn't focus a button that is clicked, so the keyboard presses it.
        await save.FocusAsync();
        await window.Keyboard.PressAsync("Enter");

        await Expect(Toast(window)).ToBeVisibleAsync();
        await Expect(Toast(page)).ToHaveCountAsync(0);
        // F8 reaches the toast there, and Escape sends the focus back to the button.
        await window.Keyboard.PressAsync("F8");
        await FocusedAsync(Toast(window).GetByRole(AriaRole.Button));
        await window.Keyboard.PressAsync("Escape");
        await FocusedAsync(save);
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Toast_ShouldGoWhereTheUserWorks_WhenAPressAKeyOrTheFocusIsInAnotherWindow(string browser)
    {
        var (page, window) = await OpenToolsInAWindowAsync(browser);
        await window.GetByRole(AriaRole.Button, new() { Name = "Show a toast" }).FocusAsync();
        await window.Keyboard.PressAsync("Enter");
        await Expect(Toast(window)).ToBeVisibleAsync();

        await PressBesideTheDockAsync(page);
        await Expect(Toast(page)).ToBeVisibleAsync();
        await Expect(Toast(window)).ToHaveCountAsync(0);

        // The focus in the window has stayed where it was, so only the key says where the user is.
        await window.Keyboard.PressAsync("F8");
        await FocusedAsync(Toast(window).GetByRole(AriaRole.Button));
        await Expect(Toast(page)).ToHaveCountAsync(0);

        await page.GetByRole(AriaRole.Tab, new() { Name = "Other" }).FocusAsync();
        await Expect(Toast(page)).ToBeVisibleAsync();
        await Expect(Toast(window)).ToHaveCountAsync(0);
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Toast_ShouldComeBackToItsPlaceOnThePage_WhenTheWindowItShowsInCloses(string browser)
    {
        var page = await OpenAsync(browser, "/dock-popped-parts");
        await Expect(page.GetByRole(AriaRole.Group, new() { Name = "Bench" })).ToHaveAttributeAsync("data-ready", "");
        await page.EvaluateAsync("""
            () => {
                const toaster = document.querySelector('[data-part=toaster]');
                window.toasterPlace = [toaster.parentNode, toaster.nextSibling];
            }
            """);
        var window = (await page.Tb().Dock("Bench").PopOutAsync("Tools")).Page;
        await window.GetByRole(AriaRole.Button, new() { Name = "Show a toast" }).ClickAsync();
        await Expect(Toast(window)).ToBeVisibleAsync();

        await window.CloseAsync();

        await Expect(Toast(page)).ToBeVisibleAsync();
        Assert.True(await page.EvaluateAsync<bool>("""
            () => {
                const toaster = document.querySelector('[data-part=toaster]');
                return toaster.parentNode === window.toasterPlace[0] && toaster.nextSibling === window.toasterPlace[1];
            }
            """));
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Toast_ShouldRunItsTime_WhenThePageIsHiddenAndTheUserWorksInAPanelsWindow(string browser)
    {
        var (page, window) = await OpenToolsInAWindowAsync(browser);
        // The toaster is on the page when the page is hidden, which pauses it until it moves to the panel's window.
        await PressBesideTheDockAsync(page);
        await Expect(page.Locator("[data-part=toaster]")).ToHaveCountAsync(1);
        await page.EvaluateAsync("""
            () => {
                Object.defineProperty(document, 'hidden', { configurable: true, get: () => true });
                document.dispatchEvent(new Event('visibilitychange'));
            }
            """);

        await window.GetByRole(AriaRole.Button, new() { Name = "Show a brief toast" }).ClickAsync();

        await Expect(Toast(window, "Tools tidied")).ToBeVisibleAsync();
        await Expect(Toast(window, "Tools tidied")).ToHaveCountAsync(0, new() { Timeout = 10_000 });
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Toast_ShouldRunItsTime_WhenItLeavesTheWindowWhereThePointerAndTheFocusWereOnIt(string browser)
    {
        var (page, window) = await OpenToolsInAWindowAsync(browser);
        await window.GetByRole(AriaRole.Button, new() { Name = "Show a toast" }).ClickAsync();
        await window.GetByRole(AriaRole.Button, new() { Name = "Show a brief toast" }).FocusAsync();
        // The pointer on the toaster pauses every toast, so the brief one waits for the rest of the test.
        await Toast(window).HoverAsync();
        await window.Keyboard.PressAsync("Enter");
        await Expect(Toast(window, "Tools tidied")).ToBeVisibleAsync();
        await window.Keyboard.PressAsync("F8");
        await FocusedAsync(Toast(window, "Tools tidied").GetByRole(AriaRole.Button));

        await PressBesideTheDockAsync(page);

        await Expect(Toast(page, "Tools tidied")).ToBeVisibleAsync();
        await Expect(Toast(page, "Tools tidied")).ToHaveCountAsync(0, new() { Timeout = 10_000 });
        // The toast that is left doesn't take the focus the brief one had in the other window.
        await Expect(Toast(page)).ToBeVisibleAsync();
        Assert.True(await page.EvaluateAsync<bool>("() => document.activeElement === document.body"));
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Toaster_ShouldLeaveTheFocusWhereItIs_WhenEscapeIsPressedAfterItChangedWindows(string browser)
    {
        var (page, window) = await OpenToolsInAWindowAsync(browser);
        await window.GetByRole(AriaRole.Button, new() { Name = "Show a toast" }).ClickAsync();
        await page.GetByRole(AriaRole.Tab, new() { Name = "Other" }).FocusAsync();
        await Expect(Toast(page)).ToBeVisibleAsync();
        await page.Keyboard.PressAsync("F8");
        await FocusedAsync(Toast(page).GetByRole(AriaRole.Button));
        await window.GetByTestId("first").ClickAsync();
        var dismiss = Toast(window).GetByRole(AriaRole.Button);
        await dismiss.FocusAsync();

        await window.Keyboard.PressAsync("Escape");

        // F8 was pressed on the page, and the tab that had the focus there doesn't take it from this window.
        await FocusedAsync(dismiss);
        await Expect(Toast(page)).ToHaveCountAsync(0);
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Tooltip_ShouldGoOnDescribingItsTrigger_WhenItsPanelGoesToAWindowAndComesBack(string browser)
    {
        var (page, window) = await OpenToolsInAWindowAsync(browser);
        var save = window.GetByRole(AriaRole.Button, new() { Name = "Save" });

        await Expect(save).ToHaveAccessibleDescriptionAsync("Saves a copy");
        await save.HoverAsync();
        await Expect(window.GetByRole(AriaRole.Tooltip)).ToHaveTextAsync("Saves a copy");

        await page.Tb().Dock("Bench").ReturnAsync("Tools");

        await Expect(page.GetByRole(AriaRole.Button, new() { Name = "Save" }))
            .ToHaveAccessibleDescriptionAsync("Saves a copy");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Tabs_ShouldShowTheirScrollButtons_WhenThePanelsWindowGetsTooNarrowForThem(string browser)
    {
        var (_, window) = await OpenToolsInAWindowAsync(browser);
        var later = window.GetByRole(AriaRole.Button, new() { Name = "Scroll to later tabs" });
        await Expect(window.GetByRole(AriaRole.Tab, new() { Name = "Section 8" })).ToBeVisibleAsync();
        await Expect(later).ToBeHiddenAsync();

        await window.SetViewportSizeAsync(360, 600);

        await Expect(later).ToBeVisibleAsync();
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Toolbar_ShouldDoTheFitItOwed_WhenItsPanelComesBackBeforeItsWindowDrewTheFrame(string browser)
    {
        var (page, window) = await OpenToolsInAWindowAsync(browser);
        var toolbar = window.GetByRole(AriaRole.Toolbar, new() { Name = "Format" });
        await Expect(toolbar).ToHaveAttributeAsync("data-ready", "");
        await Expect(toolbar).Not.ToHaveAttributeAsync("data-fitting", "");
        // A window that closes, or is covered, never draws the frame it owes.
        await window.EvaluateAsync("() => window.requestAnimationFrame = () => 0");
        await window.SetViewportSizeAsync(500, 600);
        await Expect(toolbar).ToHaveAttributeAsync("data-fitting", "");

        await page.Tb().Dock("Bench").ReturnAsync("Tools");

        await Expect(page.GetByRole(AriaRole.Toolbar, new() { Name = "Format" }))
            .Not.ToHaveAttributeAsync("data-fitting", "");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task ContextMenu_ShouldStayInsideThePanelsWindow_WhenOpenedNearItsEdge(string browser)
    {
        var (_, window) = await OpenToolsInAWindowAsync(browser);
        var area = window.GetByText("Trails", new() { Exact = true }).Locator("xpath=..");
        await Expect(area).ToHaveAttributeAsync("data-ready", "");
        var edges = await EdgesAsync(area);

        // The page's window is wider than the panel's, so a menu placed by the page's width would run off this one.
        await window.Mouse.ClickAsync(edges.Right - 6, edges.Top + 6, new() { Button = MouseButton.Right });

        var menu = window.GetByRole(AriaRole.Menu, new() { Name = "Trail actions" });
        await Expect(menu).ToBeVisibleAsync();
        Assert.InRange((await EdgesAsync(menu)).Right, 0, 900 - 8);

        // The menu hears of its own window's size changing, and stays inside it.
        await window.SetViewportSizeAsync(500, 600);
        await window.WaitForFunctionAsync("menu => menu.getBoundingClientRect().right <= 500 - 8",
            await menu.ElementHandleAsync());
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Drag_ShouldBeCalledOff_WhenThePanelsWindowLosesTheFocus(string browser)
    {
        var (_, window) = await OpenToolsInAWindowAsync(browser);
        var item = window.Locator("[data-part=item]").Filter(new() { HasText = "Rule 1" });
        var from = (await window.GetByRole(AriaRole.Button, new() { Name = "Move Rule 1", Exact = true })
            .BoundingBoxAsync())!;
        var to = (await window.Locator("[data-part=item]").Filter(new() { HasText = "Rule 3" }).BoundingBoxAsync())!;
        await window.Mouse.MoveAsync(from.X + from.Width / 2, from.Y + from.Height / 2);
        await window.Mouse.DownAsync();
        await window.Mouse.MoveAsync(from.X + from.Width / 2, from.Y + from.Height / 2 + 12, new() { Steps = 3 });
        await Expect(item).ToHaveAttributeAsync("data-dragging", "");

        // The release may then happen in another window, where this one never hears of it.
        await window.EvaluateAsync("() => window.dispatchEvent(new Event('blur'))");

        await Expect(item).Not.ToHaveAttributeAsync("data-dragging", "");
        await window.Mouse.MoveAsync(to.X + to.Width / 2, to.Y + to.Height * 0.75f, new() { Steps = 5 });
        await window.Mouse.UpAsync();
        await Expect(window.GetByTestId("rules")).ToHaveTextAsync("Rule 1, Rule 2, Rule 3");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task NestedDock_ShouldOpenItsPanelInAWindow_WhenItsOwnPanelShowsInOne(string browser)
    {
        var (_, window) = await OpenToolsInAWindowAsync(browser);
        var inner = window.Tb().Dock("Inner");
        await Expect(window.GetByRole(AriaRole.Group, new() { Name = "Inner" })).ToHaveAttributeAsync("data-ready", "");

        // The window the click was in opens the new one: a browser lets no other window do it.
        var north = await inner.PopOutAsync("North");

        await Expect(north).ToContainTextAsync("North notes");
        Assert.Equal(["North"], await inner.PoppedOutAsync());
        Assert.Equal(["South"], await inner.PanelsAsync());

        await inner.ReturnAsync("North");

        Assert.Empty(await inner.FloatingAsync());
        Assert.Equal(["North", "South"], await inner.PanelsAsync());
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task ReturnBar_ShouldNotCoverTheControl_WhenTheControlIsScrolledToThePanelsTop(string browser)
    {
        var (_, window) = await OpenToolsInAWindowAsync(browser);
        await window.GetByTestId("last").ScrollIntoViewIfNeededAsync();
        var tab = window.GetByRole(AriaRole.Tab, new() { Name = "Section 1" });

        // Scrolled to the top, as the browser does when a control above what shows takes the focus.
        await tab.EvaluateAsync("tab => tab.scrollIntoView({ block: 'start' })");

        var bar = window.Locator("[data-part=return-bar]");
        Assert.True((await EdgesAsync(tab)).Top >= (await EdgesAsync(bar)).Bottom, "The tab is under the bar.");
    }
}
