namespace Trailblazor.BrowserTests;

public class MasonryTests(BrowserFixture fixture) : BrowserTest(fixture)
{
    // Wide enough for about three columns of the example's six cards, so some cards sit under others.
    private static readonly BrowserNewContextOptions ThreeColumns =
        new() { ViewportSize = new() { Width = 700, Height = 900 } };

    private static readonly string[] Stops =
        ["Pine Hollow", "Eagle Ridge", "Cedar Falls", "Fern Gully", "Lookout Point", "Granite Steps"];

    // Follows the cards in order, tracking each column's bottom, and lists any card not placed right under the shortest
    // one; -1 means the first column doesn't start at the masonry's content edge, and -2 that the masonry's content
    // doesn't end at its lowest card.
    private const string Check = """
        root => {
            const style = getComputedStyle(root);
            const box = root.getBoundingClientRect();
            const gap = parseFloat(style.rowGap) || 0;
            const top = box.top + parseFloat(style.borderTopWidth) + parseFloat(style.paddingTop);
            const left = box.left + parseFloat(style.borderLeftWidth) + parseFloat(style.paddingLeft);
            const cards = [...root.children].map(card => card.getBoundingClientRect());
            const lefts = [...new Set(cards.map(card => Math.round(card.left)))].sort((a, b) => a - b);
            const bottoms = lefts.map(() => null);
            const misplaced = Math.abs(lefts[0] - left) > 1 ? [-1] : [];
            const bottom = box.bottom - parseFloat(style.borderBottomWidth) - parseFloat(style.paddingBottom);
            if (Math.abs(bottom - Math.max(...cards.map(card => card.bottom))) > 1) {
                misplaced.push(-2);
            }

            cards.forEach((card, index) => {
                const heights = bottoms.map(bottom => bottom ?? top - gap);
                const shortest = heights.indexOf(Math.min(...heights));
                const column = lefts.indexOf(Math.round(card.left));
                if (column !== shortest || Math.abs(card.top - (heights[shortest] + gap)) > 1) {
                    misplaced.push(index);
                }

                bottoms[column] = card.bottom;
            });
            return { Columns: lefts.length, Misplaced: misplaced };
        }
        """;

    private sealed class Layout
    {
        public int Columns { get; set; }

        public int[] Misplaced { get; set; } = [];
    }

    private static async Task<Layout> LayoutAsync(IPage page)
    {
        var root = page.Locator(".tb-masonry");
        await Expect(root).ToHaveAttributeAsync("data-ready", "");
        return await root.EvaluateAsync<Layout>(Check);
    }

    // Waits until the cards are placed again, since the masonry only reacts to a change on the next animation frame.
    private static Task PlacedAsync(IPage page) => page.WaitForFunctionAsync(
        $"() => ({Check})(document.querySelector('.tb-masonry')).Misplaced.length === 0");

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Masonry_ShouldPlaceEachCardUnderTheShortestColumn_WhenPlaced(string browser)
    {
        var page = await OpenAsync(browser, "/examples/masonry", ThreeColumns);

        var layout = await LayoutAsync(page);

        Assert.InRange(layout.Columns, 2, 4);
        Assert.Empty(layout.Misplaced);
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Masonry_ShouldPlaceTheCardsInsideItsPadding_WhenPadded(string browser)
    {
        var page = await OpenAsync(browser, "/examples/masonry", ThreeColumns);
        await LayoutAsync(page);

        await page.Locator(".tb-masonry").EvaluateAsync("root => root.style.padding = '24px'");

        await PlacedAsync(page);
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Masonry_ShouldPlaceTheCardsAgain_WhenACardGrows(string browser)
    {
        var page = await OpenAsync(browser, "/examples/masonry", ThreeColumns);
        await LayoutAsync(page);

        await page.Locator(".tb-masonry > *").First.EvaluateAsync(
            "card => card.append(...Array.from({ length: 8 }, () => document.createElement('br')))");

        await PlacedAsync(page);
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Masonry_ShouldPlaceANewCard_WhenOneIsAdded(string browser)
    {
        var page = await OpenAsync(browser, "/examples/masonry", ThreeColumns);
        await LayoutAsync(page);

        await page.Locator(".tb-masonry").EvaluateAsync("root => root.append(root.children[1].cloneNode(true))");

        await PlacedAsync(page);
        Assert.Equal(7, await page.Locator(".tb-masonry > *").CountAsync());
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Masonry_ShouldShowItsCardsPlaced_WhenShownAgainAfterBeingHidden(string browser)
    {
        var page = await OpenAsync(browser, "/examples/masonry", ThreeColumns);
        await LayoutAsync(page);
        var root = page.Locator(".tb-masonry");

        await root.EvaluateAsync("root => root.style.display = 'none'");
        await page.EvaluateAsync("() => new Promise(done => requestAnimationFrame(() => requestAnimationFrame(done)))");
        // Shown and checked in one call, before the masonry could place its cards again.
        var shown = await root.EvaluateAsync<Layout>($"root => {{ root.style.display = ''; return ({Check})(root); }}");

        Assert.Empty(shown.Misplaced);
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Masonry_ShouldKeepTheTabOrderOfItsCards_WhenPlaced(string browser)
    {
        var page = await OpenAsync(browser, "/examples/masonry", ThreeColumns);
        await LayoutAsync(page);

        await page.GetByRole(AriaRole.Button, new() { Name = $"Save {Stops[0]}" }).FocusAsync();
        foreach (var stop in Stops.Skip(1))
        {
            await page.Keyboard.PressAsync("Tab");
            await Expect(page.GetByRole(AriaRole.Button, new() { Name = $"Save {stop}" })).ToBeFocusedAsync();
        }
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Masonry_ShouldPlaceTheCardsAgain_WhenItNarrows(string browser)
    {
        var page = await OpenAsync(browser, "/examples/masonry");
        var wide = await LayoutAsync(page);

        await page.SetViewportSizeAsync(420, 900);
        await page.WaitForFunctionAsync("""
            columns => new Set([...document.querySelector('.tb-masonry').children]
                .map(card => Math.round(card.getBoundingClientRect().left))).size < columns
            """, wide.Columns);

        Assert.Empty((await LayoutAsync(page)).Misplaced);
    }
}
