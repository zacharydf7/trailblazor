using Trailblazor.Testing.Playwright;

namespace Trailblazor.BrowserTests;

public class KanbanTests(BrowserFixture fixture) : BrowserTest(fixture)
{
    protected override bool HoldsTheMouse => true;

    private static ILocator Column(IPage page, string title) =>
        page.GetByRole(AriaRole.List, new() { Name = title, Exact = true });

    private static ILocator Handle(IPage page, string card) =>
        page.GetByRole(AriaRole.Button, new() { Name = $"Move {card}", Exact = true });

    // Each column's list hears drags and keys once its script has attached.
    private async Task<IPage> OpenReadyAsync(string browser, BrowserNewContextOptions? options = null)
    {
        var page = await OpenAsync(browser, "/examples/kanban", options);
        await Expect(page.Locator(".tb-kanban .tb-sortable:not([data-ready])")).ToHaveCountAsync(0);
        return page;
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Lanes_ShouldKeepACardInItsLane_WhenLeftOrRightIsPressed(string browser)
    {
        var page = await OpenAsync(browser, "/examples/kanban/lanes");
        await Expect(page.Locator(".tb-kanban .tb-sortable:not([data-ready])")).ToHaveCountAsync(0);
        var board = page.Tb().Kanban("Crew board");

        await Handle(page, "Fix the footbridge").FocusAsync();
        await page.Keyboard.PressAsync("ArrowLeft");

        await Expect(page.GetByRole(AriaRole.List, new() { Name = "To do, North crew", Exact = true })
            .GetByText("Fix the footbridge")).ToBeVisibleAsync();
        Assert.Equal(["Mark the river ford", "Cut back the hedge"], await board.CardsAsync("To do", "South crew"));
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Lanes_ShouldLineUpALanesCellsAcrossTheColumns_WhenShown(string browser)
    {
        var page = await OpenAsync(browser, "/examples/kanban/lanes");
        var cells = page.Locator("[data-part=cell][data-lane=south]");

        var tops = await cells.EvaluateAllAsync<double[]>(
            "cells => cells.map(cell => cell.getBoundingClientRect().top)");
        var lane = await page.Locator("[data-part=lane][data-key=south]").BoundingBoxAsync();

        Assert.Equal(3, tops.Length);
        Assert.All(tops, top => Assert.Equal(tops[0], top, 0.5));
        Assert.Equal(tops[0], lane!.Y, 0.5);
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Lanes_ShouldNarrowACollapsedColumnAndKeepTheRows_WhenOneCollapses(string browser)
    {
        var page = await OpenAsync(browser, "/examples/kanban/lanes");
        var board = page.Tb().Kanban("Crew board");
        var southTop = await page.Locator("[data-part=cell][data-lane=south]").First.BoundingBoxAsync();

        await board.CollapseAsync("Done");

        var done = await board.Kanban.Locator("[data-part=column][data-key=done]").BoundingBoxAsync();
        var lanes = await board.Kanban.Locator("[data-part=lanes]").BoundingBoxAsync();
        Assert.True(done!.Width < 100, $"The collapsed column is {done.Width}px wide.");
        Assert.True(lanes!.Width < 200, $"The lane titles take {lanes.Width}px.");
        Assert.Equal(southTop!.Y,
            (await page.Locator("[data-part=cell][data-lane=south]").First.BoundingBoxAsync())!.Y, 0.5);
        var button = await board.Kanban.Locator("[data-key=done] [data-part=collapse]").BoundingBoxAsync();
        Assert.True(button!.Y - done.Y < 40, $"The collapsed column's button is {button.Y - done.Y}px down it.");
        Assert.Equal(["Paint the signs"], await board.CardsAsync("Doing", "South crew"));
        await Assert.ThrowsAsync<InvalidOperationException>(() => board.CardsAsync("Doing"));
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Lanes_ShouldKeepTheLaneTitlesInView_WhenTheBoardScrollsSideways(string browser)
    {
        var page = await OpenAsync(browser, "/examples/kanban/lanes");
        await page.SetViewportSizeAsync(390, 800);
        var board = page.Tb().Kanban("Crew board").Kanban;

        await board.EvaluateAsync("board => board.scrollLeft = 300");

        await Expect(board).ToHaveAttributeAsync("data-more-left", "");
        var lanes = await board.Locator("[data-part=lanes]").BoundingBoxAsync();
        var edge = await board.BoundingBoxAsync();
        Assert.Equal(edge!.X, lanes!.X, 1);
        // Only a side with more columns past it fades, and the lane titles' side doesn't.
        Assert.DoesNotContain("to right",
            await board.EvaluateAsync<string>("board => getComputedStyle(board).maskImage"));
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Lanes_ShouldMoveACardToAnotherLane_WhenMovedThroughItsMenu(string browser)
    {
        var page = await OpenAsync(browser, "/examples/kanban/lanes");
        await Expect(page.Locator(".tb-kanban .tb-sortable:not([data-ready])")).ToHaveCountAsync(0);
        var board = page.Tb().Kanban("Crew board");

        await board.MoveToColumnAsync("Fix the footbridge", "Doing", "South crew");

        Assert.Equal(["North crew", "South crew"], await board.LanesAsync());
        Assert.Equal(["Paint the signs", "Fix the footbridge"], await board.CardsAsync("Doing", "South crew"));
        Assert.Empty(await board.CardsAsync("Doing", "North crew"));
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Collapse_ShouldKeepFocusOnTheButtonAndOpenAgain_WhenPressed(string browser)
    {
        var page = await OpenAsync(browser, "/examples/kanban/collapsing");
        var board = page.Tb().Kanban("Season plan");
        var collapse = page.GetByRole(AriaRole.Button, new() { Name = "Cards in Planned", Exact = true });
        await collapse.FocusAsync();

        await page.Keyboard.PressAsync("Enter");
        await Expect(collapse).ToHaveAttributeAsync("aria-expanded", "false");
        await Expect(collapse).ToBeFocusedAsync();
        await page.Keyboard.PressAsync("Enter");
        await Expect(collapse).ToHaveAttributeAsync("aria-expanded", "true");
        await Expect(collapse).ToBeFocusedAsync();
        await page.Keyboard.PressAsync("Enter");
        await Expect(collapse).ToHaveAttributeAsync("aria-expanded", "false");
        var collapsed = await board.IsCollapsedAsync("Planned");
        await board.ExpandAsync("Past");

        Assert.True(collapsed);
        Assert.Equal(["Winter lantern walk", "Autumn bird count"], await board.CardsAsync("Past"));
        await Expect(page.Locator("output")).ToHaveTextAsync("Collapsed: planned");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Kanban_ShouldFadeTheSideWithMoreColumns_WhenItScrollsWithNoScrollbarOfItsOwn(string browser)
    {
        var page = await OpenReadyAsync(browser);
        await page.SetViewportSizeAsync(390, 800);
        var board = page.GetByRole(AriaRole.Region, new() { Name = "Trail work", Exact = true });
        // A scrollbar that takes room shows that the board scrolls, so the board is given none, as on a phone.
        // The test browsers hide theirs anyway, so this matters only where a test runs in a window.
        await board.EvaluateAsync("board => board.style.scrollbarWidth = 'none'");

        await Expect(board).ToHaveAttributeAsync("data-more-right", "");
        await Expect(board).Not.ToHaveAttributeAsync("data-more-left", "");
        Assert.StartsWith("linear-gradient(to left", await MaskAsync());

        await board.EvaluateAsync("board => board.scrollLeft = board.scrollWidth");
        await Expect(board).ToHaveAttributeAsync("data-more-left", "");
        await Expect(board).Not.ToHaveAttributeAsync("data-more-right", "");
        Assert.StartsWith("linear-gradient(to right", await MaskAsync());
        // A scroll that ends near a column's start comes to rest on that column.
        // Browsers leave "proximity", the default, out of the value they report.
        await Expect(board).ToHaveCSSAsync("scroll-snap-type", "inline");
        await Expect(board.Locator("[data-part=column]").First).ToHaveCSSAsync("scroll-snap-align", "start");

        // In right-to-left text the board starts at its right, so scrolled to its end it has more on the right, and
        // back at its start, on the left.
        await board.EvaluateAsync(
            "board => { board.style.direction = 'rtl'; board.scrollLeft = -board.scrollWidth; }");
        await Expect(board).ToHaveAttributeAsync("data-more-right", "");
        await Expect(board).Not.ToHaveAttributeAsync("data-more-left", "");
        await board.EvaluateAsync("board => board.scrollLeft = 0");
        await Expect(board).ToHaveAttributeAsync("data-more-left", "");
        await Expect(board).Not.ToHaveAttributeAsync("data-more-right", "");

        Task<string> MaskAsync() => board.EvaluateAsync<string>("board => getComputedStyle(board).maskImage");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Kanban_ShouldMoveACardAcrossColumns_WhenLeftAndRightArePressedOnItsHandle(string browser)
    {
        var page = await OpenReadyAsync(browser);

        await Handle(page, "Repaint blazes").FocusAsync();
        await page.Keyboard.PressAsync("ArrowRight");
        await Expect(Column(page, "Doing").GetByText("Repaint blazes")).ToHaveCountAsync(1);
        await Expect(Handle(page, "Repaint blazes")).ToBeFocusedAsync();
        await page.Keyboard.PressAsync("ArrowLeft");

        await Expect(Column(page, "To do").GetByText("Repaint blazes")).ToHaveCountAsync(1);
        await Expect(Handle(page, "Repaint blazes")).ToBeFocusedAsync();
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Kanban_ShouldMoveACardToAnotherColumn_WhenDraggedThere(string browser)
    {
        var page = await OpenReadyAsync(browser);
        var board = page.Tb().Kanban("Trail work");

        await board.Column("To do").DragToListAsync("Clear fallen trees", "Done");

        Assert.Equal(["Trim the switchbacks", "Clear fallen trees"], await board.CardsAsync("Done"));
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Kanban_ShouldRefuseAMoveIntoAFullColumn_WhenItHoldsItsLimit(string browser)
    {
        var page = await OpenReadyAsync(browser);
        var board = page.Tb().Kanban("Trail work");

        await board.MoveToColumnAsync("Clear fallen trees", "Doing");
        await board.MoveToColumnAsync("Repaint blazes", "Doing");

        Assert.Equal(["Fix the footbridge", "Clear fallen trees"], await board.CardsAsync("Doing"));
        Assert.Equal(["Repaint blazes"], await board.CardsAsync("To do"));
        await Expect(board.Kanban.Locator("[data-part=column][data-key=doing]")).ToHaveAttributeAsync("data-full", "");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Kanban_ShouldKeepItsCardEdges_WhenColorsAreForced(string browser)
    {
        var page = await OpenReadyAsync(browser, new() { ForcedColors = ForcedColors.Active });

        var border = await page.Locator(".tb-kanban [data-part=item]").First
            .EvaluateAsync<string>("card => getComputedStyle(card).borderTopStyle");

        Assert.Equal("solid", border);
    }
}
