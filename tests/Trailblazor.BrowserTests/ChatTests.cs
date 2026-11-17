using Microsoft.Playwright;
using Trailblazor.Testing.Playwright;

namespace Trailblazor.BrowserTests;

public class ChatTests(BrowserFixture fixture) : BrowserTest(fixture)
{
    private static ILocator Message(IPage page, int place) =>
        page.Locator($"[data-part=messages] > [data-part=message][aria-posinset='{place}']");

    private async Task<IPage> OpenChatAsync(string browser)
    {
        var page = await OpenAsync(browser, "/chat");
        await Expect(page.GetByRole(AriaRole.Region, new() { Name = "Trip planning" }))
            .ToHaveAttributeAsync("data-ready", "");
        return page;
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Chat_ShouldMoveBetweenMessagesAndLeaveTheFeed_WhenPageKeysAndCtrlEndOrHomeArePressed(
        string browser)
    {
        var page = await OpenChatAsync(browser);
        await page.GetByRole(AriaRole.Button, new() { Name = "Before" }).FocusAsync();

        await page.Keyboard.PressAsync("Tab");
        await Expect(Message(page, 12)).ToBeFocusedAsync();
        await page.Keyboard.PressAsync("PageUp");
        await page.Keyboard.PressAsync("PageUp");
        await Expect(Message(page, 10)).ToBeFocusedAsync();
        await page.Keyboard.PressAsync("PageDown");
        await Expect(Message(page, 11)).ToBeFocusedAsync();
        await page.Keyboard.PressAsync("Control+Home");
        await Expect(page.GetByRole(AriaRole.Button, new() { Name = "Before" })).ToBeFocusedAsync();
        // The message focus last rested on holds the feed's tab stop.
        await page.Keyboard.PressAsync("Tab");
        await Expect(Message(page, 11)).ToBeFocusedAsync();
        // At the newest message the "Newest messages" button hides, so the box is the first control after the feed.
        await page.Keyboard.PressAsync("PageDown");
        await Expect(Message(page, 12)).ToBeFocusedAsync();
        await page.Keyboard.PressAsync("Control+End");

        await Expect(page.GetByRole(AriaRole.Textbox, new() { Name = "Message" })).ToBeFocusedAsync();
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Composer_ShouldSendOnEnterAndStartALineOnShiftEnter_WhenTyping(string browser)
    {
        var page = await OpenChatAsync(browser);
        var box = page.GetByRole(AriaRole.Textbox, new() { Name = "Message" });

        await box.FillAsync("Bring water");
        await box.PressAsync("Shift+Enter");
        await box.PressSequentiallyAsync("and maps");
        await box.PressAsync("Enter");

        await Expect(page.GetByTestId("sent")).ToHaveTextAsync("Bring water\nand maps");
        await Expect(box).ToHaveValueAsync("");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Composer_ShouldNotSend_WhenEnterEndsAnInputMethodsComposition(string browser)
    {
        var page = await OpenChatAsync(browser);
        var box = page.GetByRole(AriaRole.Textbox, new() { Name = "Message" });
        await box.FillAsync("Kyoto");

        await box.EvaluateAsync("""
            box => box.dispatchEvent(new KeyboardEvent('keydown', { key: 'Enter', isComposing: true, bubbles: true,
                cancelable: true }))
            """);
        await page.GetByRole(AriaRole.Button, new() { Name = "Robin replies" }).ClickAsync();
        await Expect(Message(page, 13)).ToHaveCountAsync(1);

        await Expect(page.GetByTestId("sent")).ToBeEmptyAsync();
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Feed_ShouldFollowNewMessagesOnlyWhileAtTheNewest_WhenTheyArrive(string browser)
    {
        var page = await OpenChatAsync(browser);
        var feed = page.Locator("[data-part=messages]");
        var newest = page.GetByRole(AriaRole.Button, new() { Name = "Newest messages" });
        const string bottom = "feed => feed.scrollHeight - feed.scrollTop - feed.clientHeight < 4";
        Assert.True(await feed.EvaluateAsync<bool>(bottom));

        await page.GetByRole(AriaRole.Button, new() { Name = "Robin replies" }).ClickAsync();
        await Expect(Message(page, 13)).ToHaveCountAsync(1);
        var followed = await feed.EvaluateAsync<bool>(bottom);
        await feed.EvaluateAsync("feed => feed.scrollTop = 0");
        await Expect(newest).ToBeVisibleAsync();
        await page.GetByRole(AriaRole.Button, new() { Name = "Robin replies" }).ClickAsync();
        await Expect(Message(page, 14)).ToHaveCountAsync(1);
        var stayed = await feed.EvaluateAsync<double>("feed => feed.scrollTop");
        await newest.ClickAsync();

        Assert.Equal((true, 0d), (followed, stayed));
        Assert.True(await feed.EvaluateAsync<bool>(bottom));
        await Expect(newest).ToBeHiddenAsync();
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Attach_ShouldSendOnlyTheLastChoiceReadably_WhenFilesAreChosenTwice(string browser)
    {
        var page = await OpenChatAsync(browser);
        var chat = page.Tb().Chat("Trip planning");

        await chat.AttachAsync(new FilePayload { Name = "route.txt", MimeType = "text/plain", Buffer = "Day one"u8.ToArray() });
        await chat.AttachAsync(new FilePayload { Name = "camps.txt", MimeType = "text/plain", Buffer = "Day two"u8.ToArray() });
        await chat.SendAsync("Plans");

        await Expect(page.GetByTestId("sent")).ToHaveTextAsync("Plans, camps.txt (Day two)");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Feed_ShouldKeepItsTabStopOnTheNewest_WhenAMessageArrivesBeforeAnyIsFocused(string browser)
    {
        var page = await OpenChatAsync(browser);

        await page.GetByRole(AriaRole.Button, new() { Name = "Robin replies" }).ClickAsync();
        await Expect(Message(page, 13)).ToHaveCountAsync(1);
        await page.GetByRole(AriaRole.Button, new() { Name = "Before" }).FocusAsync();
        await page.Keyboard.PressAsync("Tab");

        await Expect(Message(page, 13)).ToBeFocusedAsync();
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Helper_ShouldSendWithAFileAndReadTheMessages_WhenUsed(string browser)
    {
        var page = await OpenChatAsync(browser);
        var chat = page.Tb().Chat("Trip planning");

        await chat.AttachAsync(new FilePayload
        {
            Name = "route.txt", MimeType = "text/plain", Buffer = "Day one"u8.ToArray(),
        });
        await chat.SendAsync("Here's the route");

        await Expect(page.GetByTestId("sent")).ToHaveTextAsync("Here's the route, route.txt (Day one)");
        Assert.Equal("You: Here's the route", (await chat.MessagesAsync())[^1]);
        Assert.Null(await chat.TypingAsync());
    }
}
