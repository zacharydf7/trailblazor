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
}
