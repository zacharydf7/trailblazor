using System.Text.RegularExpressions;
using Trailblazor.Testing.Playwright;

namespace Trailblazor.BrowserTests;

public class ButtonTests(BrowserFixture fixture) : BrowserTest(fixture)
{
    protected override bool HoldsTheMouse => true;

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task IconButton_ShouldTurnItsToggleOffAndOn_WhenSpaceThenEnterIsPressed(string browser)
    {
        var page = await OpenAsync(browser, "/examples/icon-button");
        var bell = page.GetByRole(AriaRole.Button, new() { Name = "Notifications" });
        await Expect(bell).ToHaveAttributeAsync("aria-pressed", "true");
        await page.Tb().InteractiveAsync();
        await bell.FocusAsync();

        await page.Keyboard.PressAsync("Space");
        await Expect(bell).ToHaveAttributeAsync("aria-pressed", "false");
        await page.Keyboard.PressAsync("Enter");

        await Expect(bell).ToHaveAttributeAsync("aria-pressed", "true");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Helper_ShouldReturnOnceTheAppHasHandledTheClick_WhenItsOnClickTakesAWhile(string browser)
    {
        var page = await OpenAsync(browser, "/button-busy");

        await page.Tb().Button("Save").ClickAsync();

        Assert.Equal("1", await page.GetByLabel("Saved").TextContentAsync());
        Assert.False(await page.Tb().Button("Save").BusyAsync());
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Helper_ShouldReturnOnceTheAppHasHandledTheClick_WhenTheButtonIsRenamedWhileItsBusy(string browser)
    {
        var page = await OpenAsync(browser, "/button-busy");

        await page.Tb().Button("Publish").ClickAsync();

        Assert.Equal("1", await page.GetByLabel("Published").TextContentAsync());
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Helper_ShouldSubmitTheForm_WhenTheButtonSubmitsAFormOnAStaticPage(string browser)
    {
        var page = await OpenStaticAsync(browser, "/static-attempted");

        await page.Tb().Button("Send").ClickAsync();

        await Expect(page.GetByTestId("posted")).ToHaveTextAsync("30");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Helper_ShouldClickOnceTheButtonIsNoLongerBusy_WhenItIsBusy(string browser)
    {
        var page = await OpenAsync(browser, "/button-busy");

        await page.Tb().Button("Refresh").ClickAsync();

        Assert.Equal("1", await page.GetByLabel("Refreshed").TextContentAsync());
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Button_ShouldCallOnClickOnceAndShowItsSpinner_WhenDoubleClickedWhileTheClickRuns(string browser)
    {
        var page = await OpenAsync(browser, "/button-busy");
        var send = page.GetByRole(AriaRole.Button, new() { Name = "Send code" });

        await send.DblClickAsync();

        await Expect(send.Locator("[data-part=spinner]")).ToBeVisibleAsync();
        await Expect(send).ToHaveAttributeAsync("aria-busy", "true");
        await Expect(send).Not.ToHaveAttributeAsync("aria-busy", "true");
        Assert.Equal("1", await page.GetByLabel("Sent").TextContentAsync());
        // The helper waits for the handler, as for any click.
        await page.Tb().Button("Send code").ClickAsync();
        Assert.Equal("2", await page.GetByLabel("Sent").TextContentAsync());
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Button_ShouldSayWhyItIsOff_WhenHoveredFocusedOrTapped(string browser)
    {
        var page = await OpenAsync(browser, "/button-reason", new() { HasTouch = true });
        var publish = page.GetByRole(AriaRole.Button, new() { Name = "Publish" });
        var reason = page.GetByRole(AriaRole.Tooltip, new() { IncludeHidden = true }).Filter(new()
        {
            HasText = "Name the trip first.",
        });

        await Expect(publish).ToHaveAccessibleDescriptionAsync("Name the trip first.");
        await Expect(publish).ToHaveAttributeAsync("aria-disabled", "true");
        // A tap shows it, since a press would do nothing else, and a tap anywhere else hides it. Playwright won't
        // tap what's aria-disabled unless forced, though a finger can.
        await publish.TapAsync(new() { Force = true });
        await Expect(reason).ToBeVisibleAsync();
        await page.GetByRole(AriaRole.Button, new() { Name = "Save draft" }).TapAsync();
        await Expect(reason).ToBeHiddenAsync();
        // Keyboard focus shows it too, and Escape hides it.
        await page.GetByRole(AriaRole.Button, new() { Name = "Save draft" }).FocusAsync();
        await page.Keyboard.PressAsync("Shift+Tab");
        await Expect(publish).ToBeFocusedAsync();
        await Expect(reason).ToBeVisibleAsync();
        await page.Keyboard.PressAsync("Escape");
        await Expect(reason).ToBeHiddenAsync();
        await publish.HoverAsync();
        await Expect(reason).ToBeVisibleAsync();
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Button_ShouldKeepFocus_WhenItTurnsItselfOffWithAReason(string browser)
    {
        var page = await OpenAsync(browser, "/button-reason");
        var send = page.GetByRole(AriaRole.Button, new() { Name = "Send invite" });
        var undo = page.GetByRole(AriaRole.Button, new() { Name = "Undo" });

        await send.FocusAsync();
        await page.Keyboard.PressAsync("Enter");
        await Expect(send).ToHaveAccessibleDescriptionAsync("Already sent.");
        await Expect(send).ToBeFocusedAsync();
        await undo.FocusAsync();
        await page.Keyboard.PressAsync("Enter");
        await Expect(undo).ToHaveAccessibleDescriptionAsync("Nothing to undo.");
        await Expect(undo).ToBeFocusedAsync();
        // A reason that arrives with Disabled, rather than staying set, keeps focus too.
        var save = page.GetByRole(AriaRole.Button, new() { Name = "Save trip" });
        await save.FocusAsync();
        await page.Keyboard.PressAsync("Enter");
        await Expect(save).ToHaveAccessibleDescriptionAsync("Already saved.");
        await Expect(save).ToBeFocusedAsync();
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Button_ShouldNotPostItsForm_WhenOffWithAReasonOnAStaticPage(string browser)
    {
        var page = await OpenStaticAsync(browser, "/static-button-reason");
        // The library's script stops the post, so the test waits for it to load.
        await Expect(page.Locator("html")).ToHaveAttributeAsync("data-tb-script", "");
        // Records whether each submit went ahead, and stops it either way so the page stays.
        await page.EvaluateAsync("() => document.querySelector('form').addEventListener('submit', event => { "
            + "window.posts = [...(window.posts ?? []), !event.defaultPrevented]; event.preventDefault(); })");

        await page.GetByRole(AriaRole.Button, new() { Name = "Send" }).ClickAsync(new() { Force = true });
        await page.GetByRole(AriaRole.Textbox, new() { Name = "Note" }).PressAsync("Enter");

        var posts = await page.EvaluateAsync<bool[]>("() => window.posts");
        Assert.Equal([false, false], posts);
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Button_ShouldKeepItsOwnPlaceAndTooltip_WhenOffWithAReason(string browser)
    {
        var page = await OpenAsync(browser, "/button-reason");
        var book = page.GetByRole(AriaRole.Button, new() { Name = "Book" });
        var fab = page.GetByRole(AriaRole.Button, new() { Name = "Add a stop" });
        var full = page.GetByRole(AriaRole.Tooltip, new() { IncludeHidden = true }).Filter(new()
        {
            HasText = "The trip is full.",
        });

        Assert.Equal(320, (await book.BoundingBoxAsync())!.Width, tolerance: 1);
        await fab.HoverAsync();
        await Expect(full).ToBeVisibleAsync();

        // The fixed button's tooltip sits just above it, not where the button sits in the markup.
        var button = (await fab.BoundingBoxAsync())!;
        var tooltip = (await full.BoundingBoxAsync())!;
        Assert.InRange(button.Y - (tooltip.Y + tooltip.Height), 0, 20);
        Assert.True(tooltip.X < button.X + button.Width && tooltip.X + tooltip.Width > button.X);
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Helper_ShouldRefuseADisabledButton_WhenAskedToClickIt(string browser)
    {
        var page = await OpenAsync(browser, "/button-busy");

        var disabled = await Assert.ThrowsAsync<InvalidOperationException>(
            () => page.Tb().Button("Delete").ClickAsync());

        Assert.Contains("disabled", disabled.Message);
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Helper_ShouldClickOnceInteractive_WhenThePageIsStillPrerendered(string browser)
    {
        var (page, held) = await OpenHeldAsync(browser, "/button-busy");

        var clicking = page.Tb().Button("Save").ClickAsync();
        await page.WaitForTimeoutAsync(300);
        held.SetResult();
        await clicking;

        await Expect(page.GetByLabel("Saved")).ToHaveTextAsync("1");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Helper_ShouldRefuseToClick_WhenThePageIsStatic(string browser)
    {
        var page = await OpenStaticAsync(browser, "/static-button");

        var still = await Assert.ThrowsAsync<InvalidOperationException>(() => page.Tb().Button("Save").ClickAsync());
        var disabled = await Assert.ThrowsAsync<InvalidOperationException>(
            () => page.Tb().Button("Send").ClickAsync());

        Assert.Contains("static page", still.Message);
        Assert.Contains("disabled", disabled.Message);
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Button_ShouldKeepItsTextClearOfItsBorders_WhenALongLabelWraps(string browser)
    {
        var page = await OpenAsync(browser, "/examples/button");
        var button = page.GetByRole(AriaRole.Button, new() { NameRegex = new Regex("^Add the Eagle Ridge") });

        var box = (await button.BoundingBoxAsync())!;
        var text = (await button.Locator("[data-part=label]").BoundingBoxAsync())!;

        Assert.True(text.Height > 30, "The label should wrap onto more than one line.");
        Assert.True(text.Y >= box.Y + 2 && text.Y + text.Height <= box.Y + box.Height - 2,
            "The text should stay clear of the button's top and bottom borders.");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Button_ShouldActivate_WhenEnterOrSpaceIsPressed(string browser)
    {
        var page = await OpenAsync(browser, "/examples/button");
        var counter = page.GetByRole(AriaRole.Button, new() { NameRegex = new Regex("^Clicks") });
        await counter.FocusAsync();

        await page.Keyboard.PressAsync("Enter");
        await Expect(counter).ToHaveTextAsync("Clicks: 1");
        await page.Keyboard.PressAsync("Space");

        await Expect(counter).ToHaveTextAsync("Clicks: 2");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Button_ShouldSubmitTheForm_WhenClickedOrEnterIsPressedInAField(string browser)
    {
        var page = await OpenAsync(browser, "/submit-button");

        await page.GetByTestId("submit").ClickAsync();
        await page.GetByTestId("field").PressAsync("Enter");

        await Expect(page.GetByTestId("submits")).ToHaveTextAsync("2");
    }

    [Theory, MemberData(nameof(InertButtons))]
    public async Task Button_ShouldNotSubmitTheForm_WhenLoadingOrDisabledButFocusable(string browser, string query)
    {
        var page = await OpenAsync(browser, $"/submit-button?{query}=true");

        // Playwright treats aria-disabled as disabled and would wait forever without Force.
        await page.GetByTestId("submit").ClickAsync(new() { Force = true });
        await page.GetByTestId("field").PressAsync("Enter");
        await page.GetByTestId("ping").ClickAsync();

        // The ping round-trips after both submit attempts, so a submit would have landed by now.
        await Expect(page.GetByTestId("pings")).ToHaveTextAsync("1");
        await Expect(page.GetByTestId("submits")).ToHaveTextAsync("0");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task IconButton_ShouldShowItsNameWithoutDescribingItself_WhenFocusedWithShowTooltip(string browser)
    {
        var page = await OpenAsync(browser, "/examples/icon-button");
        var bell = page.GetByRole(AriaRole.Button, new() { Name = "Notifications" });

        await bell.FocusAsync();

        await Expect(page.GetByRole(AriaRole.Tooltip, new() { Name = "Notifications" })).ToBeVisibleAsync();
        await Expect(bell).ToHaveAccessibleDescriptionAsync("");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task IconButton_ShouldDrawTheQuietBar_WhenPressedOnASelectedTreeRow(string browser)
    {
        var page = await OpenAsync(browser, "/examples/icon-button", new() { ReducedMotion = ReducedMotion.Reduce });
        var row = page.GetByRole(AriaRole.Treeitem, new() { Name = "Eagle Ridge" });
        var watch = row.GetByRole(AriaRole.Button, new() { Name = "Watch Eagle Ridge" });
        await Expect(row).ToHaveAttributeAsync("aria-selected", "true");
        await Expect(watch).ToHaveAttributeAsync("aria-pressed", "true");
        await page.Mouse.MoveAsync(0, 0);
        var pressed = await row.ScreenshotAsync();
        var bar = await watch.EvaluateAsync<string>("button => getComputedStyle(button).boxShadow");

        await watch.ClickAsync();
        await Expect(watch).ToHaveAttributeAsync("aria-pressed", "false");
        await page.Mouse.MoveAsync(0, 0);

        Assert.Matches(@"inset|0px -2px", bar);
        Assert.Equal("none", await watch.EvaluateAsync<string>("button => getComputedStyle(button).boxShadow"));
        Assert.NotEqual(pressed, await row.ScreenshotAsync());
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task IconButton_ShouldDrawAMixedToggleApartFromOnAndOff_WhenMixed(string browser)
    {
        var page = await OpenAsync(browser, "/examples/icon-button");
        var bold = page.GetByRole(AriaRole.Button, new() { Name = "Bold", Exact = true });
        var notifications = page.GetByRole(AriaRole.Button, new() { Name = "Notifications" });
        await Expect(bold).ToHaveAttributeAsync("aria-pressed", "mixed");
        await Expect(bold).ToHaveCSSAsync("border-top-style", "dashed");
        await Expect(notifications).ToHaveCSSAsync("border-top-style", "solid");
        await notifications.ClickAsync();
        await Expect(notifications).ToHaveAttributeAsync("aria-pressed", "false");
        await Expect(notifications).ToHaveCSSAsync("border-top-style", "solid");

        // A filled button's hover fill would leave a mixed button's text, colored for the light fill, unreadable.
        var filledHover = await bold.EvaluateAsync<string[]>("""
            async bold => {
                bold.classList.replace('tb-button--text', 'tb-button--filled');
                const probe = bold.appendChild(document.createElement('span'));
                probe.style.background = 'var(--_tb-button-fill-hover)';
                const fill = getComputedStyle(probe).backgroundColor;
                probe.remove();
                return [fill, getComputedStyle(bold).backgroundColor];
            }
            """);
        await bold.HoverAsync();
        await WaitForFadesAsync(page);
        var hovered = await bold.EvaluateAsync<string>("bold => getComputedStyle(bold).backgroundColor");
        Assert.NotEqual(filledHover[0], hovered);
        Assert.NotEqual(filledHover[1], hovered);

        await bold.ClickAsync();

        await Expect(bold).ToHaveAttributeAsync("aria-pressed", "true");
        await Expect(bold).ToHaveCSSAsync("border-top-style", "solid");
        await Expect(page.GetByRole(AriaRole.Checkbox, new() { Name = "Pack in bold" })).ToBeCheckedAsync();
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Button_ShouldShowAFocusRing_WhenFocusedByKeyboard(string browser)
    {
        var page = await OpenAsync(browser, "/examples/button");
        var counter = page.GetByRole(AriaRole.Button, new() { NameRegex = new Regex("^Clicks") });
        await counter.FocusAsync();
        await page.Keyboard.PressAsync("Shift+Tab");

        await page.Keyboard.PressAsync("Tab");

        await Expect(counter).ToBeFocusedAsync();
        await Expect(counter).ToHaveCSSAsync("outline-style", "solid");
        await Expect(counter).ToHaveCSSAsync("outline-width", "2px");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Button_ShouldStayInTheTabOrderAndShowItsTooltip_WhenDisabledButFocusable(string browser)
    {
        var page = await OpenAsync(browser, "/button-reason");
        var sync = page.GetByRole(AriaRole.Button, new() { Name = "Sync" });
        await page.GetByRole(AriaRole.Button, new() { Name = "Save draft" }).FocusAsync();

        await page.Keyboard.PressAsync("Tab");

        await Expect(sync).ToBeFocusedAsync();
        await Expect(sync).ToBeDisabledAsync();
        await Expect(page.GetByRole(AriaRole.Tooltip)).ToHaveTextAsync("Syncing is paused");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Button_ShouldTogglePressed_WhenSpaceIsPressedOnAToggle(string browser)
    {
        var page = await OpenAsync(browser, "/examples/button");
        var pin = page.GetByRole(AriaRole.Button, new() { Name = "Pin to top" });
        var unpressed = await pin.EvaluateAsync<string>("button => getComputedStyle(button).backgroundColor");

        await pin.FocusAsync();
        await page.Keyboard.PressAsync("Space");

        await Expect(pin).ToHaveAttributeAsync("aria-pressed", "true");
        await Expect(pin).Not.ToHaveCSSAsync("background-color", unpressed);
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Button_ShouldNavigateAndShowItsHover_WhenItIsALink(string browser)
    {
        var page = await OpenAsync(browser, "/examples/button");
        var link = page.GetByRole(AriaRole.Link, new() { Name = "Read the theming guide" });
        var resting = await link.EvaluateAsync<string>("link => getComputedStyle(link).backgroundColor");

        await link.HoverAsync();
        await Expect(link).Not.ToHaveCSSAsync("background-color", resting);
        await link.ClickAsync();

        await Expect(page).ToHaveURLAsync(new Regex("/guides/theming$"));
    }

    public static TheoryData<string, string> InertButtons => new(
        from browser in AllBrowsers.Names
        from query in new[] { "loading", "focusable-disabled" }
        select (browser, query));

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Button_ShouldDeepenItsSoftFill_WhenHoveredAndThenPressed(string browser)
    {
        var page = await OpenAsync(browser, "/examples/button");
        var soft = page.GetByRole(AriaRole.Button, new() { Name = "Soft Primary", Exact = true });
        var resting = await soft.EvaluateAsync<string>("button => getComputedStyle(button).backgroundColor");

        await soft.HoverAsync();
        await Expect(soft).Not.ToHaveCSSAsync("background-color", resting);
        var hovered = await soft.EvaluateAsync<string>("button => getComputedStyle(button).backgroundColor");
        await page.Mouse.DownAsync();

        await Expect(soft).Not.ToHaveCSSAsync("background-color", hovered);
        await page.Mouse.UpAsync();
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Button_ShouldFillItsContainersWidth_WhenFullWidth(string browser)
    {
        var page = await OpenAsync(browser, "/examples/button");
        var button = page.GetByRole(AriaRole.Button, new() { Name = "Check permit" });

        var width = (await button.BoundingBoxAsync())!.Width;
        var container = (await button.Locator("xpath=..").BoundingBoxAsync())!.Width;

        Assert.Equal(container, width, tolerance: 0.5);
        Assert.True(width > 300);
    }
}
