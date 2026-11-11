using Trailblazor.Testing.Playwright;

namespace Trailblazor.BrowserTests;

// The circuit tests need the server, so the suite runs there unless TRAILBLAZOR_RENDER_MODE is set. These check a
// few pages in WebAssembly on every run.
public class RenderModeTests(BrowserFixture fixture) : BrowserTest(fixture)
{
    [Fact]
    public async Task Select_ShouldChooseAnOption_WhenThePageRunsInWebAssembly()
    {
        var page = await OpenInWebAssemblyAsync("/select");

        await page.Tb().Select("Size").ChooseAsync("Medium");

        await Expect(page.GetByTestId("size")).ToHaveTextAsync("Medium");
    }

    [Fact]
    public async Task Tree_ShouldOpenARowWithTheKeys_WhenThePageRunsInWebAssembly()
    {
        var page = await OpenInWebAssemblyAsync("/tree-view");
        var tree = page.GetByRole(AriaRole.Tree, new() { Name = "Places" });
        await Expect(tree).ToHaveAttributeAsync("data-ready", "");
        var row = tree.Locator("[role=treeitem][data-text='North']");

        await row.FocusAsync();
        await page.Keyboard.PressAsync("ArrowRight");

        await Expect(row).ToHaveAttributeAsync("aria-expanded", "true");
    }

    [Fact]
    public async Task DatePicker_ShouldTakeATypedDate_WhenThePageRunsInWebAssembly()
    {
        var page = await OpenInWebAssemblyAsync("/date-picker");

        await page.Tb().DatePicker("Start date").ChooseAsync(new DateOnly(2026, 9, 14));

        await Expect(page.GetByTestId("start")).ToHaveTextAsync("2026-09-14");
    }

    private async Task<IPage> OpenInWebAssemblyAsync(string path)
    {
        var page = await OpenAsync("chromium", $"{path}?mode=webassembly");
        await Expect(page.Locator("main")).ToHaveAttributeAsync("data-renderer", "WebAssembly");
        return page;
    }
}
