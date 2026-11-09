using System.Text.RegularExpressions;

namespace Trailblazor.BrowserTests;

public class CardTests(BrowserFixture fixture) : BrowserTest(fixture)
{
    private static ILocator Card(IPage page, string title) =>
        page.GetByRole(AriaRole.Listitem).Filter(new() { Has = page.GetByRole(AriaRole.Link, new() { Name = title }) });

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Card_ShouldFollowItsLink_WhenClickedAwayFromTheTitle(string browser)
    {
        var page = await OpenAsync(browser, "/examples/card");

        // A corner, away from the title, where only the link's cover is.
        await Card(page, "Old mill").GetByRole(AriaRole.Article).ClickAsync(new() { Position = new() { X = 6, Y = 6 } });

        await Expect(page).ToHaveURLAsync(new Regex("/examples/card#old-mill$"));
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Card_ShouldPressItsButtonWithoutFollowingTheLink_WhenTheButtonIsClicked(string browser)
    {
        var page = await OpenAsync(browser, "/examples/card");
        var save = page.GetByRole(AriaRole.Button, new() { Name = "Save Pine hollow" });

        await save.ClickAsync();

        await Expect(save).ToHaveAttributeAsync("aria-pressed", "true");
        Assert.DoesNotContain("#", page.Url);
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Card_ShouldBeOneTabStopRingedAllAround_WhenItsLinkIsFocused(string browser)
    {
        var page = await OpenAsync(browser, "/examples/card");
        var card = Card(page, "Old mill").GetByRole(AriaRole.Article);

        await page.GetByRole(AriaRole.Link, new() { Name = "Old mill" }).FocusAsync();
        await Expect(card).ToHaveCSSAsync("outline-style", "solid");
        await page.Keyboard.PressAsync("Tab");

        await Expect(page.GetByRole(AriaRole.Button, new() { Name = "Save Old mill" })).ToBeFocusedAsync();
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Card_ShouldShowItsPictureAboveTheTitle_WhenItHasMedia(string browser)
    {
        var page = await OpenAsync(browser, "/examples/card");
        var card = page.GetByRole(AriaRole.Article, new() { Name = "Creek path" });

        var picture = await card.Locator("[data-part=media]").BoundingBoxAsync();
        var title = await card.Locator("[data-part=title]").BoundingBoxAsync();

        Assert.True(picture!.Y + picture.Height <= title!.Y, $"The picture ends at {picture.Y + picture.Height}, "
            + $"below the title's top at {title.Y}.");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Card_ShouldKeepABorder_WhenColorsAreForced(string browser)
    {
        var page = await OpenAsync(browser, "/examples/card", new() { ForcedColors = ForcedColors.Active });

        await Expect(page.GetByRole(AriaRole.Article, new() { Name = "Ridge loop" }))
            .ToHaveCSSAsync("border-top-style", "solid");
    }
}
