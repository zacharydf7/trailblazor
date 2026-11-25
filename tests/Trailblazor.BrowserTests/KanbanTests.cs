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
        await Expect(page.Locator("[data-part=column][data-key=doing]")).ToHaveAttributeAsync("data-full", "");
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
