using Trailblazor.Testing.Playwright;

namespace Trailblazor.BrowserTests;

public class AutocompleteColumnsTests(BrowserFixture fixture) : BrowserTest(fixture)
{
    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Columns_ShouldLineUpTheCellsUnderTheirTitles_WhenTheListIsOpen(string browser)
    {
        var page = await OpenAsync(browser, "/examples/autocomplete");
        var field = page.GetByRole(AriaRole.Combobox, new() { Name = "Trail", Exact = true });

        await page.Tb().Autocomplete("Trail").TypeAsync("r");
        var listbox = page.Locator($"#{await field.GetAttributeAsync("aria-controls")}");
        var starts = await listbox.EvaluateAsync<double[]>("""
            box => [box.querySelectorAll('[data-part=column-title]')[0],
                box.querySelectorAll('[data-part=column-title]')[1],
                box.querySelector('[data-part=option] [data-part=cell]:nth-child(2)')]
                .map(part => Math.round(part.getBoundingClientRect().left))
            """);

        Assert.True(starts[1] > starts[0], "The second column starts right of the first.");
        Assert.Equal(starts[1], starts[2]);
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Columns_ShouldKeepTheListInsideTheViewport_WhenItsTextIsWiderThanAPhone(string browser)
    {
        var page = await OpenAsync(browser, "/autocomplete-columns", new() { ViewportSize = new() { Width = 360,
            Height = 640 } });
        var field = page.GetByRole(AriaRole.Combobox, new() { Name = "Campsite", Exact = true });

        await page.Tb().Autocomplete("Campsite").TypeAsync("site");
        var box = (await page.Locator($"#{await field.GetAttributeAsync("aria-controls")}").BoundingBoxAsync())!;

        Assert.True(box.X + box.Width <= 360 - 7.5, $"The list ends at {box.X + box.Width}px of 360px.");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Columns_ShouldKeepTheActiveSuggestionClearOfTheTitles_WhenTheListWrapsToTheTop(string browser)
    {
        var page = await OpenAsync(browser, "/autocomplete-columns");
        var field = page.GetByRole(AriaRole.Combobox, new() { Name = "Campsite", Exact = true });

        await page.Tb().Autocomplete("Campsite").TypeAsync("site");
        await field.PressAsync("ArrowUp");
        await Expect(page.GetByRole(AriaRole.Option).Last).ToHaveAttributeAsync("data-active", "");
        await field.PressAsync("ArrowDown");
        await Expect(page.GetByRole(AriaRole.Option).First).ToHaveAttributeAsync("data-active", "");

        var gap = await page.EvaluateAsync<double>("""
            () => document.querySelector('[data-part=option][data-active]').getBoundingClientRect().top
                - document.querySelector('[data-part=columns]').getBoundingClientRect().bottom
            """);
        Assert.True(gap >= -0.5, $"The active suggestion starts {-gap}px under the titles.");
    }
}
