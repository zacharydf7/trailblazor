using Trailblazor.Testing.Playwright;

namespace Trailblazor.BrowserTests;

public class SortableBoardTests(BrowserFixture fixture) : BrowserTest(fixture)
{
    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Handle_ShouldMoveTheItemIntoAnotherListBeforeTheItemDroppedOn_WhenDraggedThere(string browser)
    {
        var page = await OpenReadyAsync(browser);

        await DragAsync(page, "Pack food", Item(page, "Book campsite"), 0.25f, async () =>
            await Expect(Item(page, "Book campsite")).ToHaveAttributeAsync("data-drop", "before"));

        await Expect(page.GetByTestId("done")).ToHaveTextAsync("Pack food, Book campsite");
        await Expect(page.GetByTestId("todo")).ToHaveTextAsync("Check weather, Fill water");
        await Expect(page.Locator("[data-drop]")).ToHaveCountAsync(0);
        await Expect(page.Locator("[data-part=announcer] [role=status] p").Last)
            .ToHaveTextAsync("Pack food moved to Done, position 1 of 2.");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Handle_ShouldMoveTheItemIntoAnEmptyList_WhenDraggedOntoIt(string browser)
    {
        var page = await OpenReadyAsync(browser);

        await DragAsync(page, "Check weather", List(page, "Doing"), 0.5f, async () =>
            await Expect(List(page, "Doing")).ToHaveAttributeAsync("data-drop", "end"));

        await Expect(page.GetByTestId("doing")).ToHaveTextAsync("Check weather");
        await Expect(page.GetByTestId("todo")).ToHaveTextAsync("Pack food, Fill water");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Handle_ShouldMoveTheItemIntoTheGap_WhenDroppedBetweenTwoItemsOfAnotherList(string browser)
    {
        var page = await OpenReadyAsync(browser);
        var above = (await Item(page, "Pack food").BoundingBoxAsync())!;
        var below = (await Item(page, "Check weather").BoundingBoxAsync())!;
        // The list itself, between the two items, is under the pointer here.
        var gap = (above.Y + above.Height + below.Y) / 2;
        var from = (await Handle(page, "Book campsite").BoundingBoxAsync())!;

        await page.Mouse.MoveAsync(from.X + from.Width / 2, from.Y + from.Height / 2);
        await page.Mouse.DownAsync();
        await page.Mouse.MoveAsync(from.X + from.Width / 2, from.Y + from.Height / 2 + 8, new() { Steps = 2 });
        await page.Mouse.MoveAsync(below.X + below.Width / 2, gap, new() { Steps = 10 });
        await Expect(Item(page, "Check weather")).ToHaveAttributeAsync("data-drop", "before");
        await page.Mouse.UpAsync();

        await Expect(page.GetByTestId("todo")).ToHaveTextAsync("Pack food, Book campsite, Check weather, Fill water");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Handle_ShouldShowWhyAndLeaveBothLists_WhenTheListItGoesToRefuses(string browser)
    {
        var page = await OpenReadyAsync(browser);

        await DragAsync(page, "Fill water", Item(page, "Book campsite"), 0.75f, async () =>
        {
            await Expect(Item(page, "Book campsite")).ToHaveAttributeAsync("data-drop-refused", "");
            await Expect(Root(page, "Done").Locator("[data-part=drop-reason]")).ToHaveTextAsync("Fill the water first.");
        });

        await Expect(page.GetByTestId("done")).ToHaveTextAsync("Book campsite");
        await Expect(page.GetByTestId("todo")).ToHaveTextAsync("Pack food, Check weather, Fill water");
        await Expect(Root(page, "Done").Locator("[data-part=drop-reason]")).ToBeHiddenAsync();
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Keys_ShouldMoveTheItemToTheListBesideAndKeepFocusOnItsHandle_WhenLeftOrRightIsPressed(
        string browser)
    {
        var page = await OpenReadyAsync(browser);
        await FocusReadyAsync(page, "Check weather");

        await page.Keyboard.PressAsync("ArrowRight");
        await Expect(page.GetByTestId("doing")).ToHaveTextAsync("Check weather");
        await Expect(Handle(page, "Check weather")).ToBeFocusedAsync();
        await page.Keyboard.PressAsync("ArrowRight");
        await Expect(page.GetByTestId("done")).ToHaveTextAsync("Check weather, Book campsite");
        await page.Keyboard.PressAsync("ArrowRight");
        await page.Keyboard.PressAsync("ArrowLeft");

        await Expect(page.GetByTestId("doing")).ToHaveTextAsync("Check weather");
        await Expect(page.GetByTestId("done")).ToHaveTextAsync("Book campsite");
        await Expect(Handle(page, "Check weather")).ToBeFocusedAsync();
        await Expect(page.Locator("[role=menu]:popover-open")).ToHaveCountAsync(0);
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Keys_ShouldMoveTheItemTheOtherWay_WhenThePageIsRightToLeft(string browser)
    {
        var page = await OpenReadyAsync(browser);
        await page.EvaluateAsync("() => document.documentElement.dir = 'rtl'");
        await FocusReadyAsync(page, "Pack food");

        await page.Keyboard.PressAsync("ArrowLeft");

        await Expect(page.GetByTestId("doing")).ToHaveTextAsync("Pack food");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Helpers_ShouldMoveThroughTheMenuAndDrag_WhenGivenTheListsLabel(string browser)
    {
        var page = await OpenReadyAsync(browser);

        await page.Tb().Sortable("To do").MoveToListAsync("Pack food", "Done");
        await page.Tb().Sortable("To do").DragToListAsync("Check weather", "Doing");
        await page.Tb().Sortable("Done").DragToListAsync("Pack food", "To do", before: "Fill water");

        await Expect(page.GetByTestId("todo")).ToHaveTextAsync("Pack food, Fill water");
        await Expect(page.GetByTestId("doing")).ToHaveTextAsync("Check weather");
        await Expect(page.GetByTestId("done")).ToHaveTextAsync("Book campsite");
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => page.Tb().Sortable("To do").MoveToListAsync("Fill water", "Archive"));
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => page.Tb().Sortable("To do").DragToListAsync("Fill water", "Archive"));
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => page.Tb().Sortable("To do").DragToListAsync("Fill water", "To do"));
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => page.Tb().Sortable("To do").DragToListAsync("Fill water", "Done", before: "Nothing"));
    }

    // Presses the item's handle, moves onto the target at this height, checks what shows, and lets go.
    private static async Task DragAsync(IPage page, string item, ILocator target, float at, Func<Task> whileOver)
    {
        var from = (await Handle(page, item).BoundingBoxAsync())!;
        await page.Mouse.MoveAsync(from.X + from.Width / 2, from.Y + from.Height / 2);
        await page.Mouse.DownAsync();
        await page.Mouse.MoveAsync(from.X + from.Width / 2, from.Y + from.Height / 2 + 8, new() { Steps = 2 });
        var to = (await target.BoundingBoxAsync())!;
        await page.Mouse.MoveAsync(to.X + to.Width / 2, to.Y + to.Height * at, new() { Steps = 10 });
        await whileOver();
        await page.Mouse.UpAsync();
    }

    // The handle's menu starts its script on first focus and draws its trigger again, so keys wait for it.
    private static async Task FocusReadyAsync(IPage page, string item)
    {
        await Handle(page, item).FocusAsync();
        await Expect(Handle(page, item)).ToHaveAttributeAsync("data-ready", "");
    }

    private async Task<IPage> OpenReadyAsync(string browser)
    {
        var page = await OpenAsync(browser, "/sortable-board");
        await Expect(page.Locator(".tb-sortable:not([data-ready])")).ToHaveCountAsync(0);
        return page;
    }

    private static ILocator List(IPage page, string name) =>
        page.GetByRole(AriaRole.List, new() { Name = name, Exact = true });

    private static ILocator Root(IPage page, string name) =>
        page.Locator(".tb-sortable", new() { Has = List(page, name) });

    private static ILocator Item(IPage page, string text) =>
        page.Locator("[data-part=item]").Filter(new() { HasText = text });

    private static ILocator Handle(IPage page, string text) =>
        page.GetByRole(AriaRole.Button, new() { Name = $"Move {text}", Exact = true });
}
