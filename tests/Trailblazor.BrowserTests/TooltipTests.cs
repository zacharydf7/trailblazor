using Trailblazor.Testing.Playwright;

namespace Trailblazor.BrowserTests;

public class TooltipTests(BrowserFixture fixture) : BrowserTest(fixture)
{
    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Tooltip_ShouldShowAndDescribeItsTrigger_WhenThePageIsStatic(string browser)
    {
        var page = await OpenStaticAsync(browser, "/static-tooltip");
        var download = page.GetByRole(AriaRole.Button, new() { Name = "Download" });

        Assert.Equal("Saves a copy to your device", await page.Tb().Tooltip(download).TextAsync());
        await Expect(download).ToHaveAccessibleDescriptionAsync("Saves a copy to your device");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Tooltip_ShouldDropItsPendingShow_WhenItsTriggerIsRemovedDuringTheDelay(string browser)
    {
        var page = await OpenAsync(browser, "/tooltip-delay");
        var errors = new List<string>();
        page.PageError += (_, error) => errors.Add(error);
        var delete = page.GetByRole(AriaRole.Button, new() { Name = "Delete" });
        await Expect(delete.Locator("..")).ToHaveAttributeAsync("data-ready", "");

        await delete.HoverAsync();
        await page.GetByRole(AriaRole.Switch, new() { Name = "Show the button" }).FocusAsync();
        await page.Keyboard.PressAsync("Space");
        await Expect(delete).ToHaveCountAsync(0);
        await page.WaitForTimeoutAsync(1500);

        Assert.Empty(errors);
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Tooltip_ShouldStayHidden_WhenEscapeIsPressedDuringItsShowDelay(string browser)
    {
        var page = await OpenAsync(browser, "/tooltip-delay");
        var delete = page.GetByRole(AriaRole.Button, new() { Name = "Delete" });
        await Expect(delete.Locator("..")).ToHaveAttributeAsync("data-ready", "");

        await delete.HoverAsync();
        await page.Keyboard.PressAsync("Escape");
        await page.WaitForTimeoutAsync(1500);

        await Expect(page.GetByRole(AriaRole.Tooltip, new() { Name = "Delete the selection" })).ToBeHiddenAsync();
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Tooltip_ShouldAppearAboveTheTrigger_WhenThePointerRestsOnIt(string browser)
    {
        var page = await OpenAsync(browser, "/examples/tooltip");
        var add = page.GetByRole(AriaRole.Button, new() { Name = "Add" });
        var tooltip = page.GetByRole(AriaRole.Tooltip, new() { Name = "Add an item" });

        await add.HoverAsync();

        await Expect(tooltip).ToBeVisibleAsync();
        var trigger = await add.BoundingBoxAsync();
        var box = await tooltip.BoundingBoxAsync();
        Assert.True(box!.Y + box.Height <= trigger!.Y + 1, "The tooltip should sit above its trigger.");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Tooltip_ShouldAppearAboveTheTriggerAgain_WhenShownASecondTime(string browser)
    {
        var page = await OpenAsync(browser, "/examples/tooltip");
        var errors = new List<string>();
        page.PageError += (_, error) => errors.Add(error);
        var add = page.GetByRole(AriaRole.Button, new() { Name = "Add" });
        var tooltip = page.GetByRole(AriaRole.Tooltip, new() { Name = "Add an item" });
        await add.HoverAsync();
        await Expect(tooltip).ToBeVisibleAsync();
        await page.Mouse.MoveAsync(0, 0);
        await Expect(tooltip).ToBeHiddenAsync();

        await add.HoverAsync();

        await Expect(tooltip).ToBeVisibleAsync();
        var trigger = await add.BoundingBoxAsync();
        var box = await tooltip.BoundingBoxAsync();
        Assert.True(box!.Y + box.Height <= trigger!.Y + 1, "The tooltip should sit above its trigger.");
        Assert.Empty(errors);
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Tooltip_ShouldGoBackAboveTheTrigger_WhenThereIsRoomAgainAfterFlippingBelow(string browser)
    {
        var page = await OpenAsync(browser, "/tooltip");
        var edge = page.GetByRole(AriaRole.Button, new() { Name = "Flipping" });
        var tooltip = page.GetByRole(AriaRole.Tooltip, new() { Name = "Flips when it must" });
        await edge.HoverAsync();
        await Expect(tooltip).ToHaveAttributeAsync("data-side", "bottom");
        await page.Mouse.MoveAsync(0, 0);
        await Expect(tooltip).ToBeHiddenAsync();
        await page.Locator("#top-edge").EvaluateAsync("element => element.style.top = '12rem'");

        await edge.HoverAsync();

        await Expect(tooltip).ToBeVisibleAsync();
        await Expect(tooltip).ToHaveAttributeAsync("data-side", "top");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Tooltip_ShouldDescribeTheTriggerAndCloseOnEscape_WhenTheTriggerHasFocus(string browser)
    {
        var page = await OpenAsync(browser, "/examples/tooltip");
        var add = page.GetByRole(AriaRole.Button, new() { Name = "Add" });
        var tooltip = page.GetByRole(AriaRole.Tooltip, new() { Name = "Add an item" });

        await add.FocusAsync();
        await Expect(tooltip).ToBeVisibleAsync();
        await Expect(add).ToHaveAccessibleDescriptionAsync("Add an item");
        await page.Keyboard.PressAsync("Escape");

        await Expect(tooltip).ToBeHiddenAsync();
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Tooltip_ShouldStayOpen_WhenThePointerMovesOntoIt(string browser)
    {
        var page = await OpenAsync(browser, "/examples/tooltip");
        var below = page.GetByRole(AriaRole.Button, new() { Name = "Below" });
        var tooltip = page.GetByRole(AriaRole.Tooltip, new() { Name = "Shown below the button" });
        await below.HoverAsync();
        await Expect(tooltip).ToBeVisibleAsync();

        // Pause in the gap between them for longer than the hide delay, then go on onto the tooltip.
        var trigger = await below.BoundingBoxAsync();
        var box = await tooltip.BoundingBoxAsync();
        var middle = box!.X + box.Width / 2;
        await page.Mouse.MoveAsync(middle, (trigger!.Y + trigger.Height + box.Y) / 2, new() { Steps = 5 });
        await page.WaitForTimeoutAsync(500);
        await page.Mouse.MoveAsync(middle, box.Y + box.Height / 2, new() { Steps = 5 });
        await page.WaitForTimeoutAsync(500);

        await Expect(tooltip).ToBeVisibleAsync();
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Tooltip_ShouldWaitBeforeAppearing_WhenThePointerArrives(string browser)
    {
        var page = await OpenAsync(browser, "/examples/tooltip");
        var add = page.GetByRole(AriaRole.Button, new() { Name = "Add" });
        var tooltip = page.GetByRole(AriaRole.Tooltip, new() { Name = "Add an item" });
        // Times the gap in the page itself, since Playwright's hover can take longer than the delay.
        await add.EvaluateAsync("""
            button => {
                const anchor = button.parentElement;
                anchor.addEventListener('pointerenter', () => window.entered = performance.now());
                // Fires inside showPopover, while WebKit can deliver toggle after the tooltip is already visible.
                anchor.querySelector('[role=tooltip]')
                    .addEventListener('beforetoggle', () => window.opened = performance.now());
            }
            """);

        await add.HoverAsync();
        await Expect(tooltip).ToBeVisibleAsync();

        Assert.True(await page.EvaluateAsync<double>("() => window.opened - window.entered") >= 250);
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Tooltip_ShouldClose_WhenThePointerLeaves(string browser)
    {
        var page = await OpenAsync(browser, "/examples/tooltip");
        var tooltip = page.GetByRole(AriaRole.Tooltip, new() { Name = "Add an item" });
        await page.GetByRole(AriaRole.Button, new() { Name = "Add" }).HoverAsync();
        await Expect(tooltip).ToBeVisibleAsync();

        await page.Mouse.MoveAsync(0, 0);

        await Expect(tooltip).ToBeHiddenAsync();
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Tooltip_ShouldClose_WhenFocusMovesAway(string browser)
    {
        var page = await OpenAsync(browser, "/examples/tooltip");
        var tooltip = page.GetByRole(AriaRole.Tooltip, new() { Name = "Add an item" });
        await page.GetByRole(AriaRole.Button, new() { Name = "Add" }).FocusAsync();
        await Expect(tooltip).ToBeVisibleAsync();

        await page.Keyboard.PressAsync("Tab");

        await Expect(tooltip).ToBeHiddenAsync();
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Tooltip_ShouldStayClosed_WhenTheTriggerIsClicked(string browser)
    {
        var page = await OpenAsync(browser, "/examples/tooltip");
        var tooltip = page.GetByRole(AriaRole.Tooltip, new() { Name = "Add an item" });

        await page.GetByRole(AriaRole.Button, new() { Name = "Add" }).ClickAsync();
        await page.WaitForTimeoutAsync(500);

        await Expect(tooltip).ToBeHiddenAsync();
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Tooltip_ShouldStayClosed_WhenFocusReturnsToTheTriggerAfterAClickElsewhere(string browser)
    {
        var page = await OpenAsync(browser, "/examples/tooltip");
        var tooltip = page.GetByRole(AriaRole.Tooltip, new() { Name = "Add an item" });
        await page.GetByRole(AriaRole.Switch, new() { Name = "Show hints" }).ClickAsync();
        await page.GetByRole(AriaRole.Switch, new() { Name = "Show hints" }).ClickAsync();

        // As a dialog or menu closed with the mouse hands focus back to the button that opened it.
        await page.GetByRole(AriaRole.Button, new() { Name = "Add" }).EvaluateAsync("button => button.focus()");
        await page.WaitForTimeoutAsync(500);

        await Expect(tooltip).ToBeHiddenAsync();
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Tooltip_ShouldClose_WhenTheTriggerIsPressedWhileItShows(string browser)
    {
        var page = await OpenAsync(browser, "/examples/tooltip");
        var tooltip = page.GetByRole(AriaRole.Tooltip, new() { Name = "Add an item" });
        await page.GetByRole(AriaRole.Button, new() { Name = "Add" }).HoverAsync();
        await Expect(tooltip).ToBeVisibleAsync();

        await page.Mouse.DownAsync();

        await Expect(tooltip).ToBeHiddenAsync();
        await page.Mouse.UpAsync();
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Tooltip_ShouldShow_WhenTheKeyboardBringsFocusBackAfterAClick(string browser)
    {
        var page = await OpenAsync(browser, "/examples/tooltip");
        var add = page.GetByRole(AriaRole.Button, new() { Name = "Add" });
        var tooltip = page.GetByRole(AriaRole.Tooltip, new() { Name = "Add an item" });
        await add.ClickAsync();
        await add.FocusAsync();
        await page.Mouse.MoveAsync(0, 0);

        await page.Keyboard.PressAsync("Tab");
        await page.Keyboard.PressAsync("Shift+Tab");

        await Expect(add).ToBeFocusedAsync();
        await Expect(tooltip).ToBeVisibleAsync();
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Tooltip_ShouldHideTheFocusedTriggersTooltip_WhenAnotherTriggerIsHovered(string browser)
    {
        var page = await OpenAsync(browser, "/tooltip");
        var saves = page.GetByRole(AriaRole.Tooltip, new() { Name = "Saves a copy" });
        var goesOn = page.GetByRole(AriaRole.Tooltip, new() { Name = "Goes on", Exact = true });
        await page.GetByRole(AriaRole.Button, new() { Name = "Save" }).FocusAsync();
        await Expect(saves).ToBeVisibleAsync();

        await page.GetByRole(AriaRole.Button, new() { Name = "Forward" }).HoverAsync();

        await Expect(goesOn).ToBeVisibleAsync();
        await Expect(saves).ToBeHiddenAsync();
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Tooltip_ShouldHideOneWaitingToHide_WhenAnotherShows(string browser)
    {
        var page = await OpenAsync(browser, "/tooltip");
        var lingers = page.GetByRole(AriaRole.Tooltip, new() { Name = "Lingers" });
        var goesOn = page.GetByRole(AriaRole.Tooltip, new() { Name = "Goes on", Exact = true });
        await page.GetByRole(AriaRole.Button, new() { Name = "Lingering" }).HoverAsync();
        await Expect(lingers).ToBeVisibleAsync();

        // Its hide delay is a second, longer than the next one takes to show.
        await page.GetByRole(AriaRole.Button, new() { Name = "Forward" }).HoverAsync();

        await Expect(goesOn).ToBeVisibleAsync();
        Assert.False(await lingers.IsVisibleAsync());
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Tooltip_ShouldStillDescribeTheTrigger_WhenTheTriggerRewritesItsDescription(string browser)
    {
        var page = await OpenAsync(browser, "/tooltip");
        var save = page.GetByRole(AriaRole.Button, new() { Name = "Save" });

        await save.ClickAsync();

        await Expect(save).ToHaveAccessibleDescriptionAsync("Saved. Saves a copy");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Tooltip_ShouldDescribeTheTriggerWithItsContent_WhenGivenRichContent(string browser)
    {
        var page = await OpenAsync(browser, "/examples/tooltip");
        var save = page.GetByRole(AriaRole.Button, new() { Name = "Save" });

        await save.FocusAsync();

        await Expect(page.GetByRole(AriaRole.Tooltip).Locator("kbd")).ToHaveTextAsync("Ctrl+S");
        await Expect(save).ToHaveAccessibleDescriptionAsync("Saves a copy of the trail Ctrl+S");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Tooltip_ShouldLineUpWithTheTriggersStartAndPointAtIt_WhenAlignedToTheStartWithAnArrow(
        string browser)
    {
        var page = await OpenAsync(browser, "/examples/tooltip");
        var save = page.GetByRole(AriaRole.Button, new() { Name = "Save" });
        var tooltip = page.GetByRole(AriaRole.Tooltip, new() { Name = "Saves a copy of the trail Ctrl+S" });

        await save.FocusAsync();
        await Expect(tooltip).ToBeVisibleAsync();

        var trigger = await save.BoundingBoxAsync();
        var box = await tooltip.BoundingBoxAsync();
        var arrow = await tooltip.Locator("[data-part=arrow]").BoundingBoxAsync();
        Assert.True(Math.Abs(box!.X - trigger!.X) <= 1, $"The tooltip starts at {box.X}, the trigger at {trigger.X}.");
        Assert.True(box.Width > trigger.Width, "The tooltip should be wider, so its arrow isn't simply centered.");
        AssertPointsAt(arrow!, trigger);
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Tooltip_ShouldKeepItsArrowOnTheTrigger_WhenTheTooltipShiftsToFitThePage(string browser)
    {
        var page = await OpenAsync(browser, "/tooltip");
        var edge = page.GetByRole(AriaRole.Button, new() { Name = "Edge" });
        var tooltip = page.GetByRole(AriaRole.Tooltip, new() { Name = "A description that is much wider" });

        await edge.FocusAsync();
        await Expect(tooltip).ToBeVisibleAsync();

        var trigger = await edge.BoundingBoxAsync();
        var box = await tooltip.BoundingBoxAsync();
        var arrow = await tooltip.Locator("[data-part=arrow]").BoundingBoxAsync();
        Assert.True(box!.X > trigger!.X + trigger.Width / 2 - box.Width / 2, "The tooltip should have shifted to fit.");
        AssertPointsAt(arrow!, trigger);
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Tooltip_ShouldWaitItsOwnDelay_WhenShowDelayIsSet(string browser)
    {
        var page = await OpenAsync(browser, "/tooltip");
        var slow = page.GetByRole(AriaRole.Button, new() { Name = "Slow" });
        var tooltip = page.GetByRole(AriaRole.Tooltip, new() { Name = "Takes its time" });
        await slow.EvaluateAsync("""
            button => {
                const anchor = button.parentElement;
                anchor.addEventListener('pointerenter', () => window.entered = performance.now());
                anchor.querySelector('[role=tooltip]')
                    .addEventListener('beforetoggle', () => window.opened = performance.now());
            }
            """);

        await slow.HoverAsync();
        await Expect(tooltip).ToBeVisibleAsync();

        Assert.True(await page.EvaluateAsync<double>("() => window.opened - window.entered") >= 950);
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Tooltip_ShouldStayItsOwnDelay_WhenHideDelayIsSetAndThePointerLeaves(string browser)
    {
        var page = await OpenAsync(browser, "/tooltip");
        var lingering = page.GetByRole(AriaRole.Button, new() { Name = "Lingering" });
        var tooltip = page.GetByRole(AriaRole.Tooltip, new() { Name = "Lingers" });
        await lingering.EvaluateAsync("""
            button => {
                const anchor = button.parentElement;
                anchor.addEventListener('pointerleave', () => window.leftAt = performance.now());
                anchor.querySelector('[role=tooltip]').addEventListener('beforetoggle', event => {
                    if (event.newState === 'closed') {
                        window.hiddenAt = performance.now();
                    }
                });
            }
            """);
        await lingering.HoverAsync();
        await Expect(tooltip).ToBeVisibleAsync();

        await page.Mouse.MoveAsync(0, 0);
        await Expect(tooltip).ToBeHiddenAsync();

        Assert.True(await page.EvaluateAsync<double>("() => window.hiddenAt - window.leftAt") >= 950);
    }

    [Theory]
    [MemberData(nameof(BrowsersAndDirections))]
    public async Task Tooltip_ShouldPointItsArrowAcrossAtTheTrigger_WhenPlacedAtTheEnd(string browser, string name,
        bool rightToLeft)
    {
        var page = await OpenAsync(browser, "/tooltip");
        var trigger = page.GetByRole(AriaRole.Button, new() { Name = name });
        var tooltip = page.GetByRole(AriaRole.Tooltip).Filter(new() { Visible = true });

        await trigger.FocusAsync();
        await Expect(tooltip).ToBeVisibleAsync();

        var anchor = (await trigger.BoundingBoxAsync())!;
        var box = (await tooltip.BoundingBoxAsync())!;
        var arrow = (await tooltip.Locator("[data-part=arrow]").BoundingBoxAsync())!;
        Assert.True(rightToLeft ? box.X + box.Width <= anchor.X : box.X >= anchor.X + anchor.Width,
            "The tooltip should sit after the trigger in the reading direction.");
        var arrowMiddle = (X: arrow.X + arrow.Width / 2, Y: arrow.Y + arrow.Height / 2);
        Assert.True(Math.Abs(arrowMiddle.Y - (anchor.Y + anchor.Height / 2)) <= 1.5,
            $"The arrow's middle is at {arrowMiddle.Y}, the trigger's at {anchor.Y + anchor.Height / 2}.");
        Assert.True(rightToLeft
            ? arrowMiddle.X <= anchor.X && arrowMiddle.X >= box.X + box.Width / 2
            : arrowMiddle.X >= anchor.X + anchor.Width && arrowMiddle.X <= box.X + box.Width / 2,
            "The arrow should sit on the tooltip's edge that faces the trigger.");
    }

    public static TheoryData<string, string, bool> BrowsersAndDirections()
    {
        var data = new TheoryData<string, string, bool>();
        foreach (var browser in AllBrowsers.Names)
        {
            data.Add(browser, "Forward", false);
            data.Add(browser, "Onward", true);
        }

        return data;
    }

    // The arrow's middle lines up with the trigger's, and it sits between the two.
    private static void AssertPointsAt(LocatorBoundingBoxResult arrow, LocatorBoundingBoxResult trigger)
    {
        var arrowMiddle = arrow.X + arrow.Width / 2;
        var triggerMiddle = trigger.X + trigger.Width / 2;
        Assert.True(Math.Abs(arrowMiddle - triggerMiddle) <= 1.5,
            $"The arrow's middle is at {arrowMiddle}, the trigger's at {triggerMiddle}.");
        Assert.True(arrow.Y + arrow.Height / 2 >= trigger.Y + trigger.Height || arrow.Y + arrow.Height / 2 <= trigger.Y,
            "The arrow should sit outside the trigger, between it and the tooltip.");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Tooltip_ShouldNeitherShowNorDescribe_WhileItIsTurnedOff(string browser)
    {
        var page = await OpenAsync(browser, "/examples/tooltip");
        var delete = page.GetByRole(AriaRole.Button, new() { Name = "Delete" });
        var hints = page.GetByRole(AriaRole.Switch, new() { Name = "Show hints" });
        var tooltip = page.GetByRole(AriaRole.Tooltip, new() { Name = "Delete the selection" });

        await hints.ClickAsync();
        await delete.FocusAsync();
        await page.WaitForTimeoutAsync(300);
        await Expect(page.GetByRole(AriaRole.Tooltip)).ToHaveCountAsync(0);
        await Expect(delete).ToHaveAccessibleDescriptionAsync("");
        await hints.ClickAsync();
        await hints.FocusAsync();
        await page.Keyboard.PressAsync("Shift+Tab");

        await Expect(tooltip).ToBeVisibleAsync();
        await Expect(delete).ToHaveAccessibleDescriptionAsync("Delete the selection");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Tooltip_ShouldLimitItsWidth_WhenGivenAMaxWidth(string browser)
    {
        var page = await OpenAsync(browser, "/examples/tooltip");

        await page.GetByRole(AriaRole.Button, new() { Name = "Long description" }).HoverAsync();

        var tooltip = page.GetByRole(AriaRole.Tooltip, new() { Name = "This description is long" });
        await Expect(tooltip).ToBeVisibleAsync();
        Assert.Equal("192px", await tooltip.EvaluateAsync<string>("tooltip => getComputedStyle(tooltip).maxWidth"));
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task TextAsync_ShouldShowTheTooltipAndReturnItsText_WhenCalled(string browser)
    {
        var page = await OpenAsync(browser, "/examples/tooltip");
        var tooltip = page.Tb().Tooltip(page.GetByRole(AriaRole.Button, new() { Name = "Add" }));

        Assert.Equal("Add an item", await tooltip.TextAsync());
        await Expect(tooltip.Bubble).ToBeVisibleAsync();
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task TextAsync_ShouldReturnTheText_WhenTheTooltipIsShowingOrItsTriggerWasClicked(string browser)
    {
        var page = await OpenAsync(browser, "/examples/tooltip");
        var add = page.GetByRole(AriaRole.Button, new() { Name = "Add" });
        var tooltip = page.Tb().Tooltip(add);

        Assert.Equal("Add an item", await tooltip.TextAsync());
        Assert.Equal("Add an item", await tooltip.TextAsync());
        await add.ClickAsync();

        Assert.Equal("Add an item", await tooltip.TextAsync());
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task TextAsync_ShouldReadTheTriggersOwnTooltip_WhenTriggersShareAName(string browser)
    {
        var page = await OpenAsync(browser, "/tooltip");
        var remove = page.GetByRole(AriaRole.Button, new() { Name = "Remove", Exact = true });

        Assert.Equal("Remove Cedar", await page.Tb().Tooltip(remove.Nth(1)).TextAsync());
        Assert.Equal("Remove Pine",
            await page.Tb().Tooltip(page.GetByRole(AriaRole.List, new() { Name = "Trails" })
                .GetByRole(AriaRole.Listitem).First.Locator(remove)).TextAsync());
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Tooltip_ShouldShowAtOnce_WhenThePointerMovesOnFromAnotherThatShowed(string browser)
    {
        var page = await OpenAsync(browser, "/tooltip-delay");
        var cut = page.GetByRole(AriaRole.Button, new() { Name = "Cut" });
        var copy = page.GetByRole(AriaRole.Button, new() { Name = "Copy" });
        await Expect(copy.Locator("xpath=..")).ToHaveAttributeAsync("data-ready", "");

        await cut.HoverAsync();
        await Expect(page.GetByRole(AriaRole.Tooltip, new() { Name = "Cut the selection" })).ToBeVisibleAsync();
        await copy.HoverAsync();

        // Well within Copy's own one-second wait.
        await Expect(page.GetByRole(AriaRole.Tooltip, new() { Name = "Copy the selection" }))
            .ToBeVisibleAsync(new() { Timeout = 500 });
        // Long after the pointer left, the wait comes back.
        await page.GetByText("Away from the toolbar").HoverAsync();
        await page.WaitForTimeoutAsync(800);
        await cut.HoverAsync();
        await page.WaitForTimeoutAsync(400);
        await Expect(page.GetByRole(AriaRole.Tooltip, new() { Name = "Cut the selection" })).ToBeHiddenAsync();
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Tooltip_ShouldNotStartItsWait_WhenATouchArrives(string browser)
    {
        var page = await OpenAsync(browser, "/examples/tooltip");
        var add = page.GetByRole(AriaRole.Button, new() { Name = "Add" });
        await Expect(add.Locator("xpath=..")).ToHaveAttributeAsync("data-ready", "");

        // A touch that rests without pressing, as when it moves onto the trigger from elsewhere.
        await add.DispatchEventAsync("pointerover", new Dictionary<string, object> { ["pointerType"] = "touch" });
        await page.WaitForTimeoutAsync(800);
        await Expect(page.GetByRole(AriaRole.Tooltip, new() { Name = "Add an item" })).ToBeHiddenAsync();
        await add.DispatchEventAsync("pointerover", new Dictionary<string, object> { ["pointerType"] = "mouse" });

        await Expect(page.GetByRole(AriaRole.Tooltip, new() { Name = "Add an item" })).ToBeVisibleAsync();
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Tooltip_ShouldShowAtOnce_WhenThePointerReachesItSoonAfterAnotherHid(string browser)
    {
        var page = await OpenAsync(browser, "/tooltip-delay");
        var cut = page.GetByRole(AriaRole.Button, new() { Name = "Cut" });
        var copy = page.GetByRole(AriaRole.Button, new() { Name = "Copy" });
        await Expect(copy.Locator("xpath=..")).ToHaveAttributeAsync("data-ready", "");
        await cut.HoverAsync();
        var cutTip = page.GetByRole(AriaRole.Tooltip, new() { Name = "Cut the selection" });
        await Expect(cutTip).ToBeVisibleAsync();

        // Off the toolbar until Cut's tooltip hides, then straight onto Copy with a raw mouse move: HoverAsync's
        // wait for the button to settle could use up the half-second window.
        var target = (await copy.BoundingBoxAsync())!;
        await page.GetByText("Away from the toolbar").HoverAsync();
        await Expect(cutTip).ToBeHiddenAsync();
        await page.Mouse.MoveAsync(target.X + target.Width / 2, target.Y + target.Height / 2);

        await Expect(page.GetByRole(AriaRole.Tooltip, new() { Name = "Copy the selection" }))
            .ToBeVisibleAsync(new() { Timeout = 500 });
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Tooltip_ShouldKeepAFocusedTriggersTooltip_WhenThePointerPassesAnotherTrigger(string browser)
    {
        var page = await OpenAsync(browser, "/tooltip-delay");
        var cut = page.GetByRole(AriaRole.Button, new() { Name = "Cut" });
        var copy = page.GetByRole(AriaRole.Button, new() { Name = "Copy" });
        await Expect(copy.Locator("xpath=..")).ToHaveAttributeAsync("data-ready", "");
        await page.GetByRole(AriaRole.Switch, new() { Name = "Show the button" }).FocusAsync();
        await page.Keyboard.PressAsync("Tab");
        await Expect(cut).ToBeFocusedAsync();
        var cutTip = page.GetByRole(AriaRole.Tooltip, new() { Name = "Cut the selection" });
        await Expect(cutTip).ToBeVisibleAsync();

        // Passing over Copy for less than its one-second wait.
        await copy.HoverAsync();
        await page.WaitForTimeoutAsync(300);
        await page.GetByText("Away from the toolbar").HoverAsync();

        await Expect(cutTip).ToBeVisibleAsync();
        await Expect(page.GetByRole(AriaRole.Tooltip, new() { Name = "Copy the selection" })).ToBeHiddenAsync();
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Tooltip_ShouldStay_WhenATouchLeavesItsTrigger(string browser)
    {
        var page = await OpenAsync(browser, "/examples/tooltip");
        var add = page.GetByRole(AriaRole.Button, new() { Name = "Add" });
        var tooltip = page.GetByRole(AriaRole.Tooltip, new() { Name = "Add an item" });
        await add.HoverAsync();
        await Expect(tooltip).ToBeVisibleAsync();

        // A finger lifting off the trigger, past the example's hide delay.
        await add.DispatchEventAsync("pointerout", new Dictionary<string, object> { ["pointerType"] = "touch" });
        await page.WaitForTimeoutAsync(800);

        await Expect(tooltip).ToBeVisibleAsync();
    }
}
