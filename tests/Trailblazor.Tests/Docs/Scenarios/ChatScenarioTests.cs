using Bunit;
using Trailblazor.Docs.Examples;
using Trailblazor.Testing;

namespace Trailblazor.Tests.Docs.Scenarios;

// The page of this component shows the body of this test, so it has to read well without the class around it.
public class ChatScenarioTests : BunitContext
{
    public ChatScenarioTests() => this.AddTrailblazor();

    [Fact]
    public async Task TripChat_ShouldShowTheMessageAndTheReply_WhenTheUserSends()
    {
        var cut = Render<ChatExample>();
        var chat = cut.Tb().Chat("Trip planning");

        // SendAsync writes in the box and sends, as Enter does, and the app's OnSend adds the message.
        await chat.SendAsync("Shall we meet at eight?");
        Assert.Equal("You: Shall we meet at eight?", chat.Messages[^1]);
        Assert.Equal("Robin is typing…", chat.Typing);

        // The example's reply streams in after a pause, so the test waits for it.
        cut.WaitForAssertion(() => Assert.Equal(
            "Robin: Sounds good. I'll bring the map and meet you at the trailhead at eight.", chat.Messages[^1]),
            TimeSpan.FromSeconds(5));
        Assert.Null(chat.Typing);
    }
}
