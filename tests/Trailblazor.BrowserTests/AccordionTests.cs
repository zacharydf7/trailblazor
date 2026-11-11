using Trailblazor.Testing.Playwright;

namespace Trailblazor.BrowserTests;

public class AccordionTests(BrowserFixture fixture) : BrowserTest(fixture)
{
    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task AccordionItem_ShouldMoveFocusToItsHeader_WhenCodeClosesItWithFocusInItsPanel(string browser)
    {
        var page = await OpenAsync(browser, "/accordion");
        var header = page.GetByRole(AriaRole.Button, new() { Name = "Editing", Exact = true });
        await Expect(header).ToHaveAttributeAsync("aria-expanded", "true");
        await page.GetByRole(AriaRole.Button, new() { Name = "Save" }).FocusAsync();

        await page.Keyboard.PressAsync("Enter");

        await Expect(header).ToHaveAttributeAsync("aria-expanded", "false");
        await Expect(header).ToBeFocusedAsync();
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task AccordionItem_ShouldMoveFocusToItsHeader_WhenCodeClosesAnItemOnItsOwn(string browser)
    {
        var page = await OpenAsync(browser, "/accordion-item");
        var header = page.GetByRole(AriaRole.Button, new() { Name = "On its own" });
        await page.GetByRole(AriaRole.Button, new() { Name = "Done" }).FocusAsync();

        await page.Keyboard.PressAsync("Enter");

        await Expect(header).ToHaveAttributeAsync("aria-expanded", "false");
        await Expect(header).ToBeFocusedAsync();
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task AccordionItem_ShouldLeaveFocusAlone_WhenCodeClosesItWithFocusOutsideItsPanel(string browser)
    {
        var page = await OpenAsync(browser, "/accordion");
        var header = page.GetByRole(AriaRole.Button, new() { Name = "Editing", Exact = true });
        var toggle = page.GetByRole(AriaRole.Button, new() { Name = "Toggle editing" });
        await Expect(header).ToHaveAttributeAsync("aria-expanded", "true");
        await toggle.FocusAsync();

        await page.Keyboard.PressAsync("Enter");

        await Expect(header).ToHaveAttributeAsync("aria-expanded", "false");
        await Expect(toggle).ToBeFocusedAsync();
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Accordion_ShouldOpenOneItemAtATimeFromTheKeyboard_WhenEnterOrSpaceIsPressed(string browser)
    {
        var page = await OpenAsync(browser, "/accordion");
        var shipping = page.GetByRole(AriaRole.Button, new() { Name = "Shipping options" });
        var returns = page.GetByRole(AriaRole.Button, new() { Name = "Return policy" });
        await shipping.FocusAsync();

        await page.Keyboard.PressAsync("Enter");
        await Expect(shipping).ToHaveAttributeAsync("aria-expanded", "true");
        await Expect(page.GetByRole(AriaRole.Region, new() { Name = "Shipping options" })).ToBeVisibleAsync();

        await returns.FocusAsync();
        await page.Keyboard.PressAsync(" ");
        await Expect(returns).ToHaveAttributeAsync("aria-expanded", "true");
        await Expect(page.GetByRole(AriaRole.Region, new() { Name = "Return policy" })).ToHaveTextAsync("Returns content");
        await Expect(shipping).ToHaveAttributeAsync("aria-expanded", "false");
        await Expect(page.GetByLabel("Notes")).ToBeHiddenAsync();
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Accordion_ShouldKeepWhatWasTyped_WhenAnItemIsClosedAndOpenedAgain(string browser)
    {
        var page = await OpenAsync(browser, "/accordion");
        var trigger = page.GetByRole(AriaRole.Button, new() { Name = "Shipping options" });
        await trigger.ClickAsync();
        await page.GetByLabel("Notes").FillAsync("leave at the door");

        await trigger.ClickAsync();
        await Expect(page.GetByLabel("Notes")).ToBeHiddenAsync();
        await trigger.ClickAsync();

        await Expect(page.GetByLabel("Notes")).ToHaveValueAsync("leave at the door");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Accordion_ShouldMoveBetweenItsHeadersWithTheArrowsHomeAndEnd_WhenOneHasFocus(string browser)
    {
        var page = await OpenAsync(browser, "/accordion");
        await Expect(page.Locator(".tb-accordion").First).ToHaveAttributeAsync("data-ready", "");
        ILocator Header(string name) => page.GetByRole(AriaRole.Button, new() { Name = name, Exact = true });
        await Header("Shipping options").FocusAsync();

        await page.Keyboard.PressAsync("ArrowDown");
        await Expect(Header("Return policy")).ToBeFocusedAsync();
        await page.Keyboard.PressAsync("ArrowDown");
        await Expect(Header("Care")).ToBeFocusedAsync();
        // Care's open panel holds a menu and a nested accordion, whose buttons aren't this accordion's headers.
        await page.Keyboard.PressAsync("ArrowDown");
        await Expect(Header("Shipping options")).ToBeFocusedAsync();
        await page.Keyboard.PressAsync("ArrowUp");
        await Expect(Header("Care")).ToBeFocusedAsync();
        await page.Keyboard.PressAsync("Home");
        await Expect(Header("Shipping options")).ToBeFocusedAsync();
        await page.Keyboard.PressAsync("End");

        await Expect(Header("Care")).ToBeFocusedAsync();
        Assert.Equal(0, await page.EvaluateAsync<double>("() => scrollY"));
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Accordion_ShouldLeaveTheKeysToAMenuAndANestedAccordion_WhenTheyAreInAPanel(string browser)
    {
        var page = await OpenAsync(browser, "/accordion");
        await Expect(page.Locator(".tb-accordion").First).ToHaveAttributeAsync("data-ready", "");
        await Expect(page.Locator(".tb-accordion .tb-accordion")).ToHaveAttributeAsync("data-ready", "");

        await page.GetByRole(AriaRole.Button, new() { Name = "Washing" }).FocusAsync();
        await page.Keyboard.PressAsync("ArrowDown");
        await Expect(page.GetByRole(AriaRole.Button, new() { Name = "Drying" })).ToBeFocusedAsync();
        // The menu leaves Home alone, so focus stays only if the accordion does too.
        var menu = page.GetByRole(AriaRole.Button, new() { Name = "Care options" });
        await menu.FocusAsync();
        await page.Keyboard.PressAsync("Home");

        await Expect(menu).ToBeFocusedAsync();
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task AccordionPanel_ShouldSlideOnlyWhenMotionIsAllowed_WhenAnItemOpens(string browser)
    {
        var moving = await OpenAsync(browser, "/accordion");
        var still = await OpenAsync(browser, "/accordion", new() { ReducedMotion = ReducedMotion.Reduce });
        // The open panel's transitioned properties, their durations and their delays.
        const string Transition = """
            panel => ['transitionProperty', 'transitionDuration', 'transitionDelay']
                .map(property => getComputedStyle(panel)[property])
            """;

        var opened = new List<string[]>();
        foreach (var page in new[] { moving, still })
        {
            await page.GetByRole(AriaRole.Button, new() { Name = "Return policy" }).ClickAsync();
            var panel = page.GetByRole(AriaRole.Region, new() { Name = "Return policy" });
            await Expect(panel).ToBeVisibleAsync();
            opened.Add(await panel.EvaluateAsync<string[]>(Transition));
        }

        Assert.StartsWith("grid-template-rows", opened[0][0]);
        Assert.NotEqual("0s", opened[0][1].Split(", ")[0]);
        // Opening shows the content at once, so it slides into view rather than appearing at the end.
        Assert.All(opened[0][2].Split(", "), delay => Assert.Equal("0s", delay));
        Assert.All(opened[1][1].Split(", "), duration => Assert.Equal("0s", duration));
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task AccordionItem_ShouldReachItsActionsAfterItsHeaderButLeaveThemToTab_WhenItHasHeaderActions(
        string browser)
    {
        var page = await OpenAsync(browser, "/accordion");
        await Expect(page.GetByTestId("trips")).ToHaveAttributeAsync("data-ready", "");
        var dayOne = page.GetByRole(AriaRole.Button, new() { Name = "Day one", Exact = true });
        var delete = page.GetByRole(AriaRole.Button, new() { Name = "Delete day one" });

        await dayOne.FocusAsync();
        await page.Keyboard.PressAsync("Tab");
        await Expect(delete).ToBeFocusedAsync();
        await page.Keyboard.PressAsync("Enter");
        await Expect(page.GetByTestId("deleted")).ToHaveTextAsync("1");
        await Expect(dayOne).ToHaveAttributeAsync("aria-expanded", "false");
        await dayOne.FocusAsync();
        await page.Keyboard.PressAsync("ArrowDown");

        await Expect(page.GetByRole(AriaRole.Button, new() { Name = "Day two" })).ToBeFocusedAsync();
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task AccordionItem_ShouldOpenOnItsOwn_WhenNestedInAnotherItemsPanelOrOutsideAnAccordion(
        string browser)
    {
        // Reduced motion turns a chevron at once, so a wrongly turned one shows as soon as Day one opens.
        var page = await OpenAsync(browser, "/accordion", new() { ReducedMotion = ReducedMotion.Reduce });
        var dayOne = page.GetByRole(AriaRole.Button, new() { Name = "Day one", Exact = true });
        var gear = page.GetByRole(AriaRole.Button, new() { Name = "Gear list" });
        var packingTips = page.GetByRole(AriaRole.Button, new() { Name = "Packing tips" });

        await dayOne.ClickAsync();
        await Expect(dayOne).ToHaveAttributeAsync("aria-expanded", "true");
        await Expect(gear.Locator("[data-part=chevron]")).ToHaveCSSAsync("transform", "none");
        await gear.ClickAsync();
        await Expect(gear).ToHaveAttributeAsync("aria-expanded", "true");
        await Expect(dayOne).ToHaveAttributeAsync("aria-expanded", "true");
        await packingTips.ClickAsync();

        await Expect(packingTips).ToHaveAttributeAsync("aria-expanded", "true");
        await Expect(page.GetByRole(AriaRole.Region, new() { Name = "Packing tips" })).ToHaveTextAsync(
            "Packing content");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Helper_ShouldOpenTheItemHandBackItsPanelAndCloseIt_WhenGivenItsHeader(string browser)
    {
        var page = await OpenAsync(browser, "/examples/accordion");
        var sizing = page.Tb().AccordionItem("Sizing");

        var panel = await sizing.ExpandAsync();

        await Expect(panel).ToContainTextAsync("Sizes run true");
        await Expect(sizing.Trigger).ToHaveAttributeAsync("aria-expanded", "true");
        await sizing.CollapseAsync();
        await Expect(sizing.Trigger).ToHaveAttributeAsync("aria-expanded", "false");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Helper_ShouldOpenTheItem_WhenItIsOnItsOwnOrAlreadyOpen(string browser)
    {
        var page = await OpenAsync(browser, "/examples/accordion");

        await page.Tb().AccordionItem("Care instructions").ExpandAsync();
        var shipping = await page.Tb().AccordionItem("Shipping").ExpandAsync();

        await Expect(page.Tb().AccordionItem("Care instructions").Trigger)
            .ToHaveAttributeAsync("aria-expanded", "true");
        await Expect(shipping).ToContainTextAsync("two business days");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Helper_ShouldThrow_WhenTheItemIsDisabled(string browser)
    {
        var page = await OpenAsync(browser, "/examples/accordion");

        var failure = await Assert.ThrowsAsync<InvalidOperationException>(
            () => page.Tb().AccordionItem("Warranty").ExpandAsync());

        Assert.Contains("Warranty", failure.Message);
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Accordion_ShouldOpenAnItemAndCloseTheOthers_WhenThePageIsStatic(string browser)
    {
        var page = await OpenStaticAsync(browser, "/static-disclosure");
        // The startup script, which handles the page until it's interactive, has loaded once the network is idle.
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        var sizing = page.GetByRole(AriaRole.Button, new() { Name = "Sizing" });
        var care = page.GetByRole(AriaRole.Button, new() { Name = "Care" });
        // A disabled item that starts closed stays closed as the page loads.
        await Expect(page.GetByRole(AriaRole.Button, new() { Name = "Allergens" }))
            .ToHaveAttributeAsync("aria-expanded", "false");

        await care.ClickAsync();

        await Expect(care).ToHaveAttributeAsync("aria-expanded", "true");
        await Expect(page.GetByText("Wash cold and hang to dry.")).ToBeVisibleAsync();
        await Expect(sizing).ToHaveAttributeAsync("aria-expanded", "false");
        await Expect(page.GetByText("Sizes run small.")).ToBeHiddenAsync();
        var recalls = page.Locator("[data-part=panel]", new() { HasText = "None this year." });
        await Expect(recalls).ToHaveAttributeAsync("inert", "");
        Assert.Null(await recalls.GetAttributeAsync("hidden"));
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Accordion_ShouldLeaveTheOthersOpen_WhenThePageIsStaticAndSeveralMayBeOpen(string browser)
    {
        var page = await OpenStaticAsync(browser, "/static-disclosure");
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        var returns = page.GetByRole(AriaRole.Button, new() { Name = "Returns" });

        await returns.FocusAsync();
        await page.Keyboard.PressAsync("Enter");

        await Expect(returns).ToHaveAttributeAsync("aria-expanded", "true");
        await Expect(page.GetByRole(AriaRole.Button, new() { Name = "Shipping" }))
            .ToHaveAttributeAsync("aria-expanded", "true");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Accordion_ShouldOpenTheItemHoldingTheText_WhenTheBrowserFindsItOnAStaticPage(string browser)
    {
        var page = await OpenStaticAsync(browser, "/static-disclosure#:~:text=hang%20to%20dry");

        await Expect(page.GetByRole(AriaRole.Button, new() { Name = "Care" }))
            .ToHaveAttributeAsync("aria-expanded", "true");
        await Expect(page.GetByText("Wash cold and hang to dry.")).ToBeVisibleAsync();
    }
}
