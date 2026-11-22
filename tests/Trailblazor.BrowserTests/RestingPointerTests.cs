namespace Trailblazor.BrowserTests;

// A browser sends a pointer move when what is under a resting pointer changes, as WebKit does about a tenth of a
// second after a list scrolls. Each list that follows the pointer has to leave such a move alone.
public class RestingPointerTests(BrowserFixture fixture) : BrowserTest(fixture)
{
    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Autocomplete_ShouldLeaveTheActiveOptionToTheKeys_WhenItsListScrollsUnderARestingPointer(
        string browser)
    {
        var page = await OpenAsync(browser, "/resting-pointer");
        var camp = page.GetByRole(AriaRole.Combobox, new() { Name = "Camp" });
        await Expect(camp).ToHaveAttributeAsync("data-ready", "");
        await camp.FocusAsync();
        await camp.PressAsync("ArrowDown");

        await ScrollUnderTheRestingPointerAsync(page, "Camp");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Autocomplete_ShouldLeaveTheActiveOptionToTheKeys_WhenAnotherPointerMovedInAnotherWindowSince(
        string browser)
    {
        var (host, window) = await OpenPoppedAsync(browser, "/resting-pointer");
        var camp = window.GetByRole(AriaRole.Combobox, new() { Name = "Camp" });
        await Expect(camp).ToHaveAttributeAsync("data-ready", "");
        await camp.FocusAsync();
        await camp.PressAsync("ArrowDown");

        // Each window's pointer is its own here, as a mouse and a finger are, so a move in the page says nothing
        // of the pointer that rests in the panel's window.
        await ScrollUnderTheRestingPointerAsync(window, "Camp", () => host.Mouse.MoveAsync(40, 40));
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Cascader_ShouldLeaveTheActiveOptionToTheKeys_WhenItsListScrollsUnderARestingPointer(
        string browser)
    {
        var page = await OpenGatesAsync(browser);

        await ScrollUnderTheRestingPointerAsync(page, "Gate");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Cascader_ShouldFollowThePointer_WhenItMovesStraightAcrossToTheNextList(string browser)
    {
        var page = await OpenGatesAsync(browser);
        var active = page.Locator("[role=option][data-active]");
        var gate = (await Option(page, "Gate 2").BoundingBoxAsync())!;
        var level = gate.Y + gate.Height / 2;
        await page.Mouse.MoveAsync(gate.X + gate.Width / 2, level);
        await Expect(active).ToHaveTextAsync("Gate 2");
        await page.Keyboard.PressAsync("ArrowRight");
        await Expect(active).ToHaveTextAsync("Lot A");
        var lot = (await Option(page, "Lot B").BoundingBoxAsync())!;
        Assert.InRange(level, lot.Y, lot.Y + lot.Height);

        // Only the pointer's place across the window changes, but that is still a move of the pointer.
        await page.Mouse.MoveAsync(lot.X + lot.Width / 2, level);

        await Expect(active).ToHaveTextAsync("Lot B");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task CommandPalette_ShouldLeaveTheActiveOptionToTheKeys_WhenItsListScrollsUnderARestingPointer(
        string browser)
    {
        var page = await OpenAsync(browser, "/resting-pointer");
        await page.GetByRole(AriaRole.Button, new() { Name = "Open tasks" }).ClickAsync();
        var tasks = page.GetByRole(AriaRole.Combobox, new() { Name = "Tasks" });
        await Expect(tasks).ToHaveAttributeAsync("data-ready", "");

        await ScrollUnderTheRestingPointerAsync(page, "Task");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task MentionInput_ShouldLeaveTheActiveOptionToTheKeys_WhenItsListScrollsUnderARestingPointer(
        string browser)
    {
        var page = await OpenAsync(browser, "/resting-pointer");
        var note = page.GetByRole(AriaRole.Textbox, new() { Name = "Note" });
        await Expect(note).ToHaveAttributeAsync("data-ready", "");
        await note.PressSequentiallyAsync("@");

        await ScrollUnderTheRestingPointerAsync(page, "Guide");
    }

    // The first showing option with the text in it. The options come in order, so "Gate 2" is found before "Gate 20".
    private static ILocator Option(IPage page, string text) =>
        page.Locator("[role=option]:visible", new() { HasTextString = text }).First;

    private async Task<IPage> OpenGatesAsync(string browser)
    {
        var page = await OpenAsync(browser, "/resting-pointer");
        var gate = page.GetByRole(AriaRole.Combobox, new() { Name = "Gate" });
        await Expect(gate).ToHaveAttributeAsync("data-ready", "");
        await gate.FocusAsync();
        await gate.PressAsync("ArrowDown");
        return page;
    }

    // The pointer comes to rest on the second option, which becomes the active one, as the option under a moving
    // pointer does. Arrow Down then moves on from it far enough to scroll the list, and the option the keys reached
    // has to outlast the move the browser sends for that scroll.
    private static async Task ScrollUnderTheRestingPointerAsync(IPage page, string name, Func<Task>? onceResting = null)
    {
        var active = page.Locator("[role=option][data-active]");
        await Option(page, $"{name} 2").HoverAsync();
        await Expect(active).ToHaveTextAsync($"{name} 2");
        await (onceResting?.Invoke() ?? Task.CompletedTask);

        for (var press = 0; press < 20; press++)
        {
            await page.Keyboard.PressAsync("ArrowDown");
        }

        await Expect(active).ToHaveTextAsync($"{name} 22");
        await page.WaitForTimeoutAsync(400);
        await Expect(active).ToHaveTextAsync($"{name} 22");
    }
}
