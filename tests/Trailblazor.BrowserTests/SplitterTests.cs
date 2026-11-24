using Trailblazor.Testing.Playwright;

namespace Trailblazor.BrowserTests;

public class SplitterTests(BrowserFixture fixture) : SplitterKeysTests(fixture)
{

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Splitter_ShouldStackItsPanesWithNothingToDrag_WhenItIsNarrowerThanStackBelow(string browser)
    {
        var page = await OpenSplitterAsync(browser);
        var notes = page.GetByText("The saddle loop climbs through pines");
        var splitter = page.Locator("[data-part=splitter]", new() { Has = notes });
        var (list, text) = (splitter.Locator("[data-part=pane]").First, splitter.Locator("[data-part=pane]").Last);
        var separator = Separator(page, "Resize Trail list");
        await Expect(separator).ToBeVisibleAsync();
        var beside = ((await list.BoundingBoxAsync())!, (await text.BoundingBoxAsync())!);
        Assert.Equal(beside.Item1.Y, beside.Item2.Y, 0.5);

        // The example's own frame, set to 30rem (480px), narrower than its 640px StackBelow.
        await splitter.Locator("xpath=..").EvaluateAsync("frame => frame.style.inlineSize = '30rem'");

        await Expect(splitter).ToHaveAttributeAsync("data-stacked", "");
        await Expect(separator).ToBeHiddenAsync();
        var (above, below, whole) = ((await list.BoundingBoxAsync())!, (await text.BoundingBoxAsync())!,
            (await splitter.BoundingBoxAsync())!);
        // Each pane is as wide as the splitter, the second under the first, in the height the example sets.
        Assert.Equal(whole.Width, above.Width, 0.5);
        Assert.Equal(above.Y + above.Height, below.Y, 0.5);
        Assert.Equal(160, whole.Height, 0.5);

        // The helper says why it can't resize a stacked splitter, where it would otherwise wait for a hidden separator.
        var stacked = await Assert.ThrowsAsync<InvalidOperationException>(
            () => page.Tb().Splitter("Resize Trail list").ResizeAsync(40));
        Assert.Contains("StackBelow", stacked.Message);

        // With less height than the panes need, they keep the height of what they hold, and the splitter scrolls.
        await splitter.EvaluateAsync("splitter => splitter.style.blockSize = '4rem'");
        var held = (await list.Locator(":scope > *").BoundingBoxAsync())!.Height;
        Assert.True((await list.BoundingBoxAsync())!.Height >= held - 0.5, "The first pane is shorter than its text.");
        await Expect(splitter).ToHaveCSSAsync("overflow-y", "auto");
        Assert.True(await splitter.EvaluateAsync<bool>("splitter => splitter.scrollHeight > splitter.clientHeight"));
        await splitter.EvaluateAsync("splitter => splitter.style.blockSize = '10rem'");

        // A splitter whose StackBelow goes away, as when the app clears it, is side by side again at once.
        await splitter.EvaluateAsync("splitter => splitter.removeAttribute('data-stack-below')");
        await Expect(splitter).Not.ToHaveAttributeAsync("data-stacked", "");
        await splitter.EvaluateAsync("splitter => splitter.setAttribute('data-stack-below', '640')");
        await Expect(splitter).ToHaveAttributeAsync("data-stacked", "");

        await splitter.Locator("xpath=..").EvaluateAsync("frame => frame.style.inlineSize = ''");
        await Expect(splitter).Not.ToHaveAttributeAsync("data-stacked", "");
        await Expect(separator).ToBeVisibleAsync();
        await Expect(separator).ToHaveAttributeAsync("aria-valuenow", "35");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Splitter_ShouldStackAVerticalSplitterInsideAndHideACollapsedPane_WhenItsOuterSplitterStacks(
        string browser)
    {
        var page = await OpenSplitterAsync(browser);
        var profile = page.GetByText("The elevation profile.");
        var outer = page.Locator("[data-part=splitter]", new() { Has = profile }).First;

        await page.SetViewportSizeAsync(390, 900);
        await Expect(outer).ToHaveAttributeAsync("data-stacked", "");
        // With no height from its frame, the vertical splitter in the Map pane has none to share, and stacks too.
        await outer.Locator("xpath=..").EvaluateAsync("frame => frame.style.blockSize = 'auto'");
        var elevation = page.Locator("[data-part=pane]", new() { Has = profile }).Last;
        Assert.True((await elevation.BoundingBoxAsync())!.Height >= (await profile.BoundingBoxAsync())!.Height - 0.5,
            "The Elevation pane is shorter than its text.");

        // A collapsed pane takes no room while stacked.
        var trails = page.GetByText("Ridge loop, creek path and summit spur.");
        var before = (await outer.BoundingBoxAsync())!.Height;
        await page.GetByRole(AriaRole.Button, new() { Name = "Hide trails" }).ClickAsync();
        await Expect(trails).ToBeHiddenAsync();
        Assert.True((await outer.BoundingBoxAsync())!.Height < before, "The collapsed pane still takes room.");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Splitter_ShouldResizeAndCollapseThePane_WhenTheGripsMenuIsClicked(string browser)
    {
        var page = await OpenSplitterAsync(browser);
        var trails = Separator(page, "Resize Trails");
        var grip = page.GetByRole(AriaRole.Button, new() { Name = "Size of Trails", Exact = true });

        await grip.ClickAsync();
        await page.GetByRole(AriaRole.Menuitem, new() { Name = "Make Trails larger" }).ClickAsync();
        await Expect(trails).ToHaveAttributeAsync("aria-valuenow", "35");
        await Expect(page.GetByText("Trails 35%")).ToBeVisibleAsync();
        await grip.ClickAsync();
        await page.GetByRole(AriaRole.Menuitem, new() { Name = "Collapse Trails" }).ClickAsync();
        await Expect(page.GetByText("Ridge loop, creek path")).ToBeHiddenAsync();
        await grip.ClickAsync();
        await page.GetByRole(AriaRole.Menuitem, new() { Name = "Restore Trails" }).ClickAsync();

        await Expect(page.GetByText("Ridge loop, creek path")).ToBeVisibleAsync();
        await Expect(trails).ToHaveAttributeAsync("aria-valuenow", "35");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Splitter_ShouldHideACollapsedPanesContentFromTheTabOrder_WhenCollapsedFromAButton(string browser)
    {
        var page = await OpenSplitterAsync(browser);

        await page.GetByRole(AriaRole.Button, new() { Name = "Hide trails" }).ClickAsync();

        await Expect(page.GetByText("Ridge loop, creek path")).ToBeHiddenAsync();
        await Expect(page.Locator(".tb-splitter__pane").First).ToHaveCSSAsync("visibility", "hidden");
        await Expect(page.GetByText("Trails hidden")).ToBeVisibleAsync();
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Splitter_ShouldKeepAPaneCollapsedFromAButton_WhenThePageIsReloaded(string browser)
    {
        var page = await OpenSplitterAsync(browser, "/splitter");

        await page.GetByRole(AriaRole.Button, new() { Name = "Toggle left" }).ClickAsync();
        await Expect(page.GetByText("Left pane")).ToBeHiddenAsync();
        await page.WaitForFunctionAsync("() => localStorage.getItem('tb-splitter:splitter-fixture')?.includes('true')");
        await page.ReloadAsync();
        await Expect(page.Locator(".tb-splitter")).ToHaveAttributeAsync("data-ready", "");

        await Expect(page.GetByText("Left pane")).ToBeHiddenAsync();
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Splitter_ShouldIgnoreASavedLayoutThatDoesntFit_WhenTheStorageHoldsSomethingElse(string browser)
    {
        var page = await OpenSplitterAsync(browser, "/splitter");
        await page.EvaluateAsync("""
            localStorage.setItem('tb-splitter:splitter-fixture', JSON.stringify({ sizes: [50, 50], collapsed: [true] }))
            """);
        await page.ReloadAsync();
        await Expect(page.Locator(".tb-splitter")).ToHaveAttributeAsync("data-ready", "");

        await Expect(Separator(page, "Resize Left")).ToHaveAttributeAsync("aria-valuenow", "30");
        await Expect(page.GetByText("Left pane")).ToBeVisibleAsync();
    }
}

// Dragging uses pointer capture, which Firefox can leak into parallel pages, so it runs with the other drag tests.
[Collection(nameof(PointerDrags))]
public class SplitterDragTests(BrowserFixture fixture) : BrowserTest(fixture)
{
    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Splitter_ShouldResizeAndStopAtTheLimit_WhenTheSeparatorIsDragged(string browser)
    {
        var page = await OpenAsync(browser, "/examples/splitter");
        await Expect(page.Locator(".tb-splitter").First).ToHaveAttributeAsync("data-ready", "");
        var trails = page.GetByRole(AriaRole.Separator, new() { Name = "Resize Trails", Exact = true });
        var root = (await page.Locator(".tb-splitter").First.BoundingBoxAsync())!;
        var box = (await trails.BoundingBoxAsync())!;
        // Away from the grip in the line's middle.
        var y = box.Y + box.Height / 4;

        await page.Mouse.MoveAsync(box.X + box.Width / 2, y);
        await page.Mouse.DownAsync();
        await page.Mouse.MoveAsync(box.X + box.Width / 2 + root.Width * 0.1f, y, new() { Steps = 5 });
        await page.Mouse.UpAsync();
        await Expect(trails).ToHaveAttributeAsync("aria-valuenow", "35");
        await Expect(page.GetByText("Trails 35%")).ToBeVisibleAsync();

        await page.Mouse.MoveAsync(box.X + box.Width / 2 + root.Width * 0.1f, y);
        await page.Mouse.DownAsync();
        await page.Mouse.MoveAsync(root.X + root.Width, y, new() { Steps = 5 });
        await page.Mouse.UpAsync();

        await Expect(trails).ToHaveAttributeAsync("aria-valuenow", "40");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Splitter_ShouldResizeWithoutOpeningTheMenu_WhenTheGripIsDragged(string browser)
    {
        var page = await OpenAsync(browser, "/examples/splitter");
        await Expect(page.Locator(".tb-splitter").First).ToHaveAttributeAsync("data-ready", "");
        var trails = page.GetByRole(AriaRole.Separator, new() { Name = "Resize Trails", Exact = true });
        var root = (await page.Locator(".tb-splitter").First.BoundingBoxAsync())!;
        var box = (await page.GetByRole(AriaRole.Button, new() { Name = "Size of Trails" }).BoundingBoxAsync())!;
        var (x, y) = (box.X + box.Width / 2, box.Y + box.Height / 2);

        await page.Mouse.MoveAsync(x, y);
        await page.Mouse.DownAsync();
        await page.Mouse.MoveAsync(x + root.Width * 0.1f, y, new() { Steps = 5 });
        await page.Mouse.UpAsync();

        await Expect(trails).ToHaveAttributeAsync("aria-valuenow", "35");
        await Expect(page.GetByRole(AriaRole.Menu)).ToHaveCountAsync(0);
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Splitter_ShouldOpenTheMenuWithoutResizing_WhenAClickOnTheGripWandersAFewPixels(string browser)
    {
        var page = await OpenAsync(browser, "/examples/splitter");
        await Expect(page.Locator(".tb-splitter").First).ToHaveAttributeAsync("data-ready", "");
        var box = (await page.GetByRole(AriaRole.Button, new() { Name = "Size of Trails" }).BoundingBoxAsync())!;
        var (x, y) = (box.X + box.Width / 2, box.Y + box.Height / 2);

        await page.Mouse.MoveAsync(x, y);
        await page.Mouse.DownAsync();
        await page.Mouse.MoveAsync(x + 3, y, new() { Steps = 3 });
        await page.Mouse.UpAsync();

        await Expect(page.GetByRole(AriaRole.Menuitem, new() { Name = "Make Trails larger" })).ToBeVisibleAsync();
        await Expect(page.Locator("[data-part=pane]").First)
            .ToHaveAttributeAsync("style", new System.Text.RegularExpressions.Regex("flex-grow: 25;"));
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Splitter_ShouldSelectNoText_WhenTheSeparatorIsDraggedOverThePanes(string browser)
    {
        var page = await OpenAsync(browser, "/examples/splitter");
        await Expect(page.Locator(".tb-splitter").First).ToHaveAttributeAsync("data-ready", "");
        var trails = page.GetByRole(AriaRole.Separator, new() { Name = "Resize Trails", Exact = true });
        var root = (await page.Locator(".tb-splitter").First.BoundingBoxAsync())!;
        var box = (await trails.BoundingBoxAsync())!;
        var y = box.Y + box.Height / 4;

        await page.Mouse.MoveAsync(box.X + box.Width / 2, y);
        await page.Mouse.DownAsync();
        await page.Mouse.MoveAsync(root.X, y, new() { Steps = 5 });
        await page.Mouse.UpAsync();

        Assert.Equal("", await page.EvaluateAsync<string>("() => getSelection().toString()"));
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Separator_ShouldShowAHighlightOnlyForKeyboardFocus_WhenPressedThenGivenAKeyOrTabbedBackTo(
        string browser)
    {
        var page = await OpenAsync(browser, "/examples/splitter");
        await Expect(page.Locator(".tb-splitter").First).ToHaveAttributeAsync("data-ready", "");
        var trails = page.GetByRole(AriaRole.Separator, new() { Name = "Resize Trails", Exact = true });
        var box = (await trails.BoundingBoxAsync())!;
        // The line's color eases in and out, so it's read once its transitions have finished.
        const string line = """
            async separator => {
                await Promise.all(separator.getAnimations({ subtree: true }).map(animation => animation.finished));
                return getComputedStyle(separator, '::before').backgroundColor;
            }
            """;
        var resting = await trails.EvaluateAsync<string>(line);

        // Away from the grip, whose press opens its menu, then off the line, so hover doesn't highlight it.
        await page.Mouse.ClickAsync(box.X + box.Width / 2, box.Y + box.Height / 4);
        await page.Mouse.MoveAsync(0, 0);
        await Expect(trails).ToBeFocusedAsync();
        var pressed = await trails.EvaluateAsync<string>(line);
        await page.Keyboard.PressAsync("Shift");
        var keyed = await trails.EvaluateAsync<string>(line);
        await page.Mouse.ClickAsync(box.X + box.Width / 2, box.Y + box.Height / 4);
        await page.Mouse.MoveAsync(0, 0);
        await page.Keyboard.PressAsync("Tab");
        await page.Keyboard.PressAsync("Shift+Tab");
        await Expect(trails).ToBeFocusedAsync();

        Assert.Equal(resting, pressed);
        Assert.NotEqual(resting, keyed);
        Assert.NotEqual(resting, await trails.EvaluateAsync<string>(line));
    }
}
