using Trailblazor.Testing.Playwright;

namespace Trailblazor.BrowserTests;

public class LabelTests(BrowserFixture fixture) : BrowserTest(fixture)
{
    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Label_ShouldNameItsControlAndExplainItInLines_WhenItsInfoIsOpened(string browser)
    {
        var page = await OpenAsync(browser, "/examples/label");
        await Expect(page.GetByRole(AriaRole.Textbox, new() { Name = "Project code", Exact = true }))
            .ToHaveAttributeAsync("id", "project-code");

        var info = await page.Tb().Popover("Information about Project code").OpenAsync();

        await Expect(info).ToContainTextAsync("The code on the order form, such as PRJ-2041.");
        await Expect(info).ToContainTextAsync("Codes aren't case sensitive.");
        // The break in Info is kept, so the text takes two lines however wide the panel is.
        Assert.True(await info.Locator(".tb-label__info-text").EvaluateAsync<bool>(
            "text => text.innerText.includes('\\n')"), "The info should keep its line break.");
        await page.Tb().Popover("Information about Project code").CloseAsync();
        await Expect(info).ToBeHiddenAsync();
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Label_ShouldNameAGroupWithItsTextAlone_WhenTheGroupPointsAtTextId(string browser)
    {
        var page = await OpenAsync(browser, "/examples/label");

        await Expect(page.GetByRole(AriaRole.Radiogroup, new() { Name = "Delivery speed", Exact = true }))
            .ToBeVisibleAsync();
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Label_ShouldWrapItsTextBesideItsInfoButton_WhenTheTextIsLongerThanTheLabelIsWide(string browser)
    {
        var page = await OpenAsync(browser, "/examples/label");

        var longText = (await page.GetByText("The person we should call").BoundingBoxAsync())!;
        var shortText = (await page.GetByText("Permit number").BoundingBoxAsync())!;
        var button = (await page.GetByRole(AriaRole.Button, new() { Name = "Information about The person" })
            .BoundingBoxAsync())!;

        Assert.True(longText.Height > shortText.Height * 1.5, $"The text is {longText.Height}px tall.");
        Assert.True(button.X >= longText.X + longText.Width - 1, "The button should sit beside the text.");
        Assert.True(button.Y < longText.Y + longText.Height, "The button should sit beside the text, not under it.");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Label_ShouldShowRichInfoFlowingAsMarkup_WhenThePointerRestsOnTheButton(string browser)
    {
        var page = await OpenAsync(browser, "/examples/label");
        var button = page.GetByRole(AriaRole.Button, new() { Name = "Information about Pickup time" });
        await Expect(button).ToHaveAttributeAsync("data-ready", "");

        await button.HoverAsync();

        var info = page.GetByRole(AriaRole.Dialog, new() { Name = "Information about Pickup time" });
        await Expect(info.Locator("strong")).ToHaveTextAsync("5 p.m.");
        // Line breaks in the Razor source are only spacing, so the sentence flows on.
        Assert.False(await info.Locator(".tb-label__info-text").EvaluateAsync<bool>(
            "text => text.innerText.includes('\\n')"), "Rich info shouldn't break where its source does.");
    }
}
