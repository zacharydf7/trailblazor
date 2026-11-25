using Trailblazor.Testing.Playwright;

namespace Trailblazor.BrowserTests;

public class DescriptionListTests(BrowserFixture fixture) : BrowserTest(fixture)
{
    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task TbDescriptionList_ShouldStackColumns_WhenContainerIsNarrow(string browser)
    {
        var page = await OpenAsync(browser, "/description-list");

        async Task<(float First, float Second)> TopsAsync(string box)
        {
            var items = page.Locator($"[data-box={box}] [data-part=item]");
            return ((await items.Nth(0).BoundingBoxAsync())!.Y, (await items.Nth(1).BoundingBoxAsync())!.Y);
        }

        var wide = await TopsAsync("wide");
        var narrow = await TopsAsync("narrow");

        Assert.Equal(wide.First, wide.Second, tolerance: 1);
        Assert.True(narrow.Second > narrow.First + 10, "A narrow list should put the second pair under the first.");
        // Inline, the term sits beside its value rather than above it.
        var term = (await page.Locator("[data-box=inline] dt").BoundingBoxAsync())!;
        var value = (await page.Locator("[data-box=inline] dd").BoundingBoxAsync())!;
        Assert.Equal(term.Y, value.Y, tolerance: 1);
        Assert.True(value.X > term.X + term.Width - 1, "An inline value should sit to the right of its term.");
        // Screen readers and Playwright find the pairs by the term and definition roles.
        await Expect(page.Locator("[data-box=wide]").GetByRole(AriaRole.Term)).ToHaveCountAsync(2);
        await Expect(page.Locator("[data-box=wide]").GetByRole(AriaRole.Definition)).ToHaveCountAsync(2);
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Helper_ShouldReadTheListsOwnPairs_WhenAValueHoldsAnotherList(string browser)
    {
        var page = await OpenAsync(browser, "/description-list");
        var nested = page.Locator("[data-box=nested]");

        var details = nested.Tb().DescriptionList();

        Assert.Equal(["Trail", "Owner"], await details.TermsAsync());
        Assert.Equal("Robin Hale", await details.ValueOfAsync("Owner"));
        Assert.Equal("Park service",
            await nested.Locator("[data-part=value]").First.Tb().DescriptionList().ValueOfAsync("Owner"));
    }
}
