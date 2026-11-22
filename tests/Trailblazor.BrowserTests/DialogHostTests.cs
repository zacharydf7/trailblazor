using Microsoft.Playwright;

namespace Trailblazor.BrowserTests;

public class DialogHostTests(BrowserFixture fixture) : BrowserTest(fixture)
{
    // Whether the actions bar sits within its container, which then has nothing to scroll sideways.
    private const string Inside = "(bar, container) => { const [a, c] = [bar.getBoundingClientRect(), "
        + "container.getBoundingClientRect()]; return a.left >= c.left && a.right <= c.right "
        + "&& container.scrollWidth <= container.clientWidth; }";

    // Whether the actions bar spans its container from edge to edge, as it does a dialog's body.
    private const string Reaches = "(bar, container) => { const [a, c] = [bar.getBoundingClientRect(), "
        + "container.getBoundingClientRect()]; return Math.abs(a.left - c.left) < 1 "
        + "&& Math.abs(a.right - c.right) < 1; }";

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Actions_ShouldStayInsideThePanel_WhenDialogContentIsHostedInADockPanel(string browser)
    {
        var page = await OpenAsync(browser, "/examples/dialog");
        var dock = page.GetByRole(AriaRole.Group, new() { Name = "Reports", Exact = true });
        await Expect(dock).ToHaveAttributeAsync("data-ready", "");

        await dock.GetByRole(AriaRole.Button, new() { Name = "Rename in a panel" }).ClickAsync();

        var panel = dock.GetByRole(AriaRole.Tabpanel, new() { Name = "Rename report" });
        Assert.True(await panel.Locator("[data-part=actions]").EvaluateAsync<bool>(Inside,
            await panel.ElementHandleAsync()));

        // The content closes with its result there, as it does in a dialog.
        await panel.GetByLabel("Name").FillAsync("Annual report");
        await panel.GetByRole(AriaRole.Button, new() { Name = "Save" }).ClickAsync();

        await Expect(dock.GetByText("Renamed to Annual report")).ToBeVisibleAsync();
        await Expect(panel).ToHaveCountAsync(0);
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Actions_ShouldReachTheDialogsEdges_WhenTheSameContentShowsInADialog(string browser)
    {
        var page = await OpenAsync(browser, "/examples/dialog");

        await page.GetByRole(AriaRole.Button, new() { Name = "Rename report" }).ClickAsync();

        var body = page.GetByRole(AriaRole.Dialog, new() { Name = "Rename report" }).Locator("[data-part=body]");
        Assert.True(await body.Locator("[data-part=actions]").EvaluateAsync<bool>(Reaches,
            await body.ElementHandleAsync()));
    }
}
