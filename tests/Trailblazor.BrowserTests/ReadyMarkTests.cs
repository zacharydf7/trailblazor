using Trailblazor.Testing.Playwright;
using Trailblazor.Docs.Examples;

namespace Trailblazor.BrowserTests;

public class ReadyMarkTests(BrowserFixture fixture) : BrowserTest(fixture)
{
    public static TheoryData<string> MarkedExamples =>
        new(ExampleCatalog.All.Where(example => example.Ready is not null).Select(example => example.Name));

    // The marks come from the scripts and the server, not the engine, so chromium alone checks them.
    [Theory, MemberData(nameof(MarkedExamples))]
    public async Task Example_ShouldMarkItsReadyElement_WhenTheExampleIsInteractive(string name)
    {
        var ready = ExampleCatalog.Find(name)!.Ready!;
        var page = await OpenAsync("chromium", $"/examples/{name}");
        var marked = page.Locator($"{ready.Selector}[data-ready]");
        if (ready.OpenedBy is { } opener)
        {
            // The mark is looked for on what the opener brings, so what is there already is set aside: a page can
            // hold another such element that is marked from the start.
            await page.Locator(ready.Selector).EvaluateAllAsync<int>(
                "elements => elements.map(element => element.dataset.thereBefore = '').length");
            await ExampleTargetTests.Find(page, opener).ClickAsync();
            marked = page.Locator($"{ready.Selector}[data-ready]:not([data-there-before])");
        }

        await Expect(marked.First).ToBeAttachedAsync();
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Interactive_ShouldReturnOnceThePageHandlesEvents_WhenTheHostsStartInteractively(string browser)
    {
        // Opened without waiting for the layout's own mark, so only the helper's wait stands between load and click.
        var page = await OpenStaticAsync(browser, "/examples/dialog");

        await page.Tb().InteractiveAsync();

        Assert.Equal(1, await page.Locator("[data-tb-interactive]").CountAsync());
        await page.GetByRole(AriaRole.Button, new() { Name = "Delete report" }).ClickAsync();
        await Expect(page.GetByRole(AriaRole.Alertdialog, new() { Name = "Delete report?" })).ToBeVisibleAsync();
    }
}
