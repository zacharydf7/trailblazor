using Trailblazor.Testing.Playwright;

namespace Trailblazor.BrowserTests;

public class ScopedHelpersTests(BrowserFixture fixture) : BrowserTest(fixture)
{
    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Tb_ShouldFindTheFieldInTheDialog_WhenTheDialogIsTheScope(string browser)
    {
        var page = await OpenAsync(browser, "/scoped-helpers");
        var dialog = page.GetByRole(AriaRole.Dialog, new() { Name = "Edit" });

        await dialog.Tb().Select("Trail").ChooseAsync("Ridge");

        await Expect(page.GetByTestId("dialog-trail")).ToHaveTextAsync("Ridge");
        await Expect(page.GetByTestId("page-trail")).ToHaveTextAsync("none");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Tb_ShouldDriveTheFieldTheLocatorFinds_WhenGivenALocatorInsteadOfALabel(string browser)
    {
        var page = await OpenAsync(browser, "/scoped-helpers");
        var texts = await OpenAsync(browser, "/examples/text-field");
        var towns = await OpenAsync(browser, "/examples/autocomplete");
        var stove = page.GetByRole(AriaRole.Row).Filter(new() { HasText = "Stove" });

        await page.Tb().NumberField(stove.GetByLabel("Count")).EnterAsync("3");
        await texts.Tb().TextField(texts.GetByLabel("Campsite")).EnterAsync("Pine");
        await towns.Tb().Autocomplete(towns.GetByLabel("Trailhead")).ChooseAsync("cedar", "Cedar Flats River valley");

        await Expect(page.GetByTestId("counts")).ToHaveTextAsync("0,3");
        await Expect(texts.GetByLabel("Campsite")).ToHaveValueAsync("Pine");
        await Expect(towns.GetByText("Starting at:")).ToHaveTextAsync("Starting at: Cedar Flats");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Tb_ShouldFindTheFieldInTheRow_WhenAGridRowIsTheScope(string browser)
    {
        var page = await OpenAsync(browser, "/scoped-helpers");
        var stove = page.GetByRole(AriaRole.Row).Filter(new() { HasText = "Stove" });

        await stove.Tb().NumberField("Count").EnterAsync("3");

        await Expect(page.GetByTestId("counts")).ToHaveTextAsync("0,3");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Tb_ShouldFindTheFieldsInTheFrame_WhenAFrameIsTheScope(string browser)
    {
        var page = await OpenAsync(browser, "/frame-helpers");
        var frame = page.FrameLocator("iframe[title=Helpers]");

        await frame.GetByRole(AriaRole.Dialog, new() { Name = "Edit" }).Tb().Select("Trail").ChooseAsync("Pine");
        await frame.Tb().Grid("Orders").Row("Tent").Tb().NumberField("Count").EnterAsync("2");
        await frame.Tb().Grid("Orders").SortAsync("Name");

        await Expect(frame.GetByTestId("dialog-trail")).ToHaveTextAsync("Pine");
        await Expect(frame.GetByTestId("counts")).ToHaveTextAsync("2,0");
        await Expect(frame.GetByRole(AriaRole.Row).Nth(1)).ToContainTextAsync("Stove");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Tb_ShouldDragItemsAndRowsInTheFrame_WhenTheFrameIsSetInFromThePage(string browser)
    {
        var page = await OpenAsync(browser, "/frame-helpers");
        var frame = page.FrameLocator("iframe[title=Sortable]");

        await frame.Tb().Sortable("Rules").DragAfterAsync("Block spam", "Star the team");
        await frame.Tb().Grid("Route stops").DragRowBeforeAsync("Summit Gate", "Trailhead");

        await Expect(frame.GetByTestId("order"))
            .ToHaveTextAsync("Flag invoices, Star the team, Block spam, Catch-all");
        var stops = frame.GetByRole(AriaRole.Table, new() { Name = "Route stops" })
            .Locator("tbody td[data-column=stop]");
        await Expect(stops).ToHaveTextAsync(["Summit Gate", "Trailhead", "Lake camp"]);
    }
}
