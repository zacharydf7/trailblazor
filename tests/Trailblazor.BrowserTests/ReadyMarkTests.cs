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
        if (ready.OpenedBy is { } opener)
        {
            await ExampleTargetTests.Find(page, opener).ClickAsync();
        }

        await Expect(page.Locator($"{ready.Selector}[data-ready]").First).ToBeAttachedAsync();
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
