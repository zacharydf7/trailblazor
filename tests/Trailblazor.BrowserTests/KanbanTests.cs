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
