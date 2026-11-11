using System.Text.RegularExpressions;

namespace Trailblazor.BrowserTests;

// A field whose message clears as a press blurs it keeps its height until the press ends.
public class MessageHoldTests(BrowserFixture fixture) : BrowserTest(fixture)
{
    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Press_ShouldSubmitTheFormAndThenGiveTheSpaceBack_WhenTheBlurItCausesClearsAMessage(string browser)
    {
        var page = await OpenAsync(browser, "/message-hold");
        var save = page.GetByRole(AriaRole.Button, new() { Name = "Save" });
        await save.ClickAsync();
        var message = page.Locator("[data-part=error]");
        await Expect(message).ToBeVisibleAsync();
        await page.GetByLabel("Name").FillAsync("Ada");

        var box = (await save.BoundingBoxAsync())!;
        await page.Mouse.MoveAsync(box.X + box.Width / 2, box.Y + box.Height / 2);
        await page.Mouse.DownAsync();
        await Expect(message).ToHaveCountAsync(0);
        await page.Mouse.UpAsync();

        await Expect(page.GetByLabel("Saves")).ToHaveTextAsync("1");
        var field = page.Locator(".tb-field").Filter(new() { Has = page.GetByLabel("Name") });
        await Expect(field).Not.ToHaveAttributeAsync("style", new Regex("min-block-size"));
        Assert.True((await save.BoundingBoxAsync())!.Y < box.Y, "The button didn't move back up once the press ended.");
    }

    // WebAssembly renders as the tap's blur is handled, before the tap's click target is chosen.
    [Fact]
    public async Task Tap_ShouldSubmitTheForm_WhenTheBlurItCausesClearsAMessageInWebAssembly()
    {
        var page = await OpenAsync("chromium", "/message-hold?mode=webassembly", new() { HasTouch = true });
        await Expect(page.Locator("main")).ToHaveAttributeAsync("data-renderer", "WebAssembly");
        var save = page.GetByRole(AriaRole.Button, new() { Name = "Save" });
        await save.TapAsync();
        await Expect(page.Locator("[data-part=error]")).ToBeVisibleAsync();
        await page.GetByLabel("Name").FillAsync("Ada");

        await save.TapAsync();

        await Expect(page.GetByLabel("Saves")).ToHaveTextAsync("1");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Press_ShouldGiveBackTheSpaceOfAFieldWithTwoMessages_WhenThePressEnds(string browser)
    {
        var page = await OpenAsync(browser, "/message-hold");
        await page.GetByRole(AriaRole.Button, new() { Name = "Upload" }).ClickAsync();
        await page.Locator("input[type=file]").SetInputFilesAsync(
            new FilePayload { Name = "notes.txt", MimeType = "text/plain", Buffer = "Trail notes"u8.ToArray() });
        var upload = page.Locator(".tb-field").Filter(new() { Has = page.Locator("input[type=file]") });
        await Expect(upload.Locator(".tb-field__error")).ToHaveCountAsync(2);

        await page.GetByLabel("Saves").ClickAsync();
        await page.GetByLabel("Saves").ClickAsync();

        await Expect(upload).Not.ToHaveAttributeAsync("style", new Regex("min-block-size"));
    }
}
