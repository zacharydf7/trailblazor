using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.Extensions.DependencyInjection;
using Trailblazor.Testing;

namespace Trailblazor.Tests.Components;

public class TbChatTests : BunitContext
{
    private static readonly ChatAuthor Robin = new("robin", "Robin");
    private static readonly ChatAuthor Sam = new("sam", "Sam");
    private static readonly DateTimeOffset Morning = new(2026, 11, 16, 9, 30, 0, TimeSpan.Zero);
    private readonly List<ChatSend> _sent = [];

    public TbChatTests() => this.AddTrailblazor();

    private FakeAnnouncer Announcer => Services.GetRequiredService<FakeAnnouncer>();

    private static ChatMessage From(ChatAuthor author, string id, string text) => new(id, author, text, Morning);

    [Fact]
    public void Render_ShouldShowEachMessageAsAnArticleNamedByItsAuthorAndTime_WhenGivenMessages()
    {
        var cut = RenderChat([From(Robin, "1", "Meet at the trailhead?"), From(Sam, "2", "Yes, at nine.")]);

        var feed = cut.Find("[role=feed]");
        var articles = cut.FindAll("article[data-part=message]");
        Assert.Equal("Trip planning", feed.GetAttribute("aria-label"));
        Assert.Equal([("1", "2", false), ("2", "2", true)], articles.Select(article => (article.GetAttribute(
            "aria-posinset"), article.GetAttribute("aria-setsize"), article.HasAttribute("data-own"))));
        var names = articles[0].GetAttribute("aria-labelledby")!.Split(' ')
            .Select(id => cut.Find($"#{id}").TextContent);
        Assert.Equal("Robin", names.First());
    }

    [Fact]
    public void Messages_ShouldReadOutOnlyNewOnesFromOthers_WhenTheyArrive()
    {
        var cut = RenderChat([From(Robin, "1", "Meet at the trailhead?")]);

        cut.Render(parameters => parameters.Add(chat => chat.Messages,
            [From(Robin, "1", "Meet at the trailhead?"), From(Sam, "2", "Yes, at nine."), From(Robin, "3", "Great.")]));

        Assert.Equal(["Robin: Great."], Announcer.Polite);
    }

    [Fact]
    public void Streaming_ShouldReadOutTheReplyOnceAndMarkItBusyMeanwhile_WhenItStopsStreaming()
    {
        var cut = RenderChat([]);
        var partial = From(Robin, "1", "The trail") with { IsStreaming = true };

        cut.Render(parameters => parameters.Add(chat => chat.Messages, [partial]));
        var busy = (cut.Find("[role=feed]").GetAttribute("aria-busy"), cut.Find("article").GetAttribute("aria-busy"));
        cut.Render(parameters => parameters.Add(chat => chat.Messages, [partial with { Text = "The trail is open." }]));
        cut.Render(parameters => parameters.Add(chat => chat.Messages,
            [partial with { Text = "The trail is open.", IsStreaming = false }]));

        Assert.Equal(("true", "true"), busy);
        Assert.Equal(["Robin: The trail is open."], Announcer.Polite);
        Assert.Equal("false", cut.Find("[role=feed]").GetAttribute("aria-busy"));
    }

    [Fact]
    public async Task Send_ShouldRaiseOnSendWithTheTrimmedTextAndClearTheBox_WhenSubmitted()
    {
        var cut = RenderChat([]);

        cut.Find("[data-part=input]").Input("  See you there  ");
        await cut.Find("form").SubmitAsync();

        Assert.Equal(("See you there", 0), (_sent.Single().Text, _sent.Single().Files.Count));
        Assert.Equal(("", "1"), (cut.Find("[data-part=input]").GetAttribute("value"),
            cut.Find("section").GetAttribute("data-commits")));
    }

    [Fact]
    public async Task Send_ShouldDoNothingAndStayOff_WhenTheBoxIsBlank()
    {
        var cut = RenderChat([]);

        cut.Find("[data-part=input]").Input("   ");
        await cut.Find("form").SubmitAsync();

        Assert.Empty(_sent);
        Assert.True(cut.Find("[data-part=send]").HasAttribute("disabled"));
    }

    [Fact]
    public async Task Attach_ShouldSendTheChosenFilesAndLetOneBeRemoved_WhenFilesAreChosen()
    {
        var cut = RenderChat([], parameters => parameters.Add(chat => chat.AllowAttachments, true));

        await cut.Tb().Chat().AttachAsync(InputFileContent.CreateFromText("Day one", "route.txt"),
            InputFileContent.CreateFromText("Day two", "camps.txt"));
        await cut.Find("[data-part=chosen] li:first-child [data-part=remove]").ClickAsync(new());
        await cut.Find("form").SubmitAsync();

        Assert.Equal(["camps.txt"], _sent.Single().Files.Select(file => file.Name));
        Assert.Empty(cut.FindAll("[data-part=chosen]"));
    }

    [Fact]
    public void Typing_ShouldNameWhoIsTyping_WhenGivenAuthors()
    {
        var cut = RenderChat([], parameters => parameters.Add(chat => chat.TypingAuthors, [Robin]));
        var one = cut.Find("[data-part=typing]").TextContent;

        cut.Render(parameters => parameters.Add(chat => chat.TypingAuthors, [Robin, Sam]));

        Assert.Equal(("Robin is typing…", "Robin and Sam are typing…"),
            (one, cut.Find("[data-part=typing]").TextContent));
    }

    [Fact]
    public void Messages_ShouldNotReadOutOlderOnes_WhenTheyAreLoadedBeforeTheNewest()
    {
        var cut = RenderChat([From(Robin, "5", "See you there.")]);

        cut.Render(parameters => parameters.Add(chat => chat.Messages,
            [From(Robin, "3", "Which trail?"), From(Robin, "4", "The ridge."), From(Robin, "5", "See you there.")]));

        Assert.Empty(Announcer.Polite);
    }

    [Fact]
    public void Messages_ShouldReadOutOnlyTheNewest_WhenAHistoryArrivesAfterTheChatShowedNone()
    {
        var cut = RenderChat([]);

        cut.Render(parameters => parameters.Add(chat => chat.Messages,
            [From(Robin, "1", "Which trail?"), From(Robin, "2", "The ridge.")]));

        Assert.Equal(["Robin: The ridge."], Announcer.Polite);
    }

    [Fact]
    public async Task Send_ShouldTakeOneSendAtATimeAndKeepWhatIsTypedMeanwhile_WhenOnSendIsSlow()
    {
        var answer = new TaskCompletionSource();
        var cut = Render<TbChat>(parameters => parameters.Add(chat => chat.AriaLabel, "Trip planning")
            .Add(chat => chat.OnSend, async (ChatSend send) =>
            {
                _sent.Add(send);
                await answer.Task;
            }));

        cut.Find("[data-part=input]").Input("First");
        var first = cut.Find("form").SubmitAsync();
        cut.Find("[data-part=input]").Input("Second");
        var second = cut.Find("form").SubmitAsync();
        var third = cut.Find("form").SubmitAsync();
        answer.SetResult();
        await Task.WhenAll(first, second, third);

        Assert.Equal(["First"], _sent.Select(send => send.Text));
        Assert.Equal("Second", cut.Find("[data-part=input]").GetAttribute("value"));
    }

    [Fact]
    public async Task Attach_ShouldBeOff_WhenOnSendIsStillRunning()
    {
        var answer = new TaskCompletionSource();
        var cut = Render<TbChat>(parameters => parameters.Add(chat => chat.AriaLabel, "Trip planning")
            .Add(chat => chat.AllowAttachments, true).Add(chat => chat.OnSend, async (ChatSend send) => await answer.Task));

        cut.Find("[data-part=input]").Input("Here's the route");
        var sending = cut.Find("form").SubmitAsync();
        var during = (cut.Find("input[type=file]").HasAttribute("disabled"),
            cut.Find("[data-part=attach]").GetAttribute("aria-disabled"));
        var helper = await Assert.ThrowsAsync<InvalidOperationException>(
            () => cut.Tb().Chat().AttachAsync(InputFileContent.CreateFromText("Day one", "route.txt")));
        answer.SetResult();
        await sending;

        Assert.Equal((true, "true", "This chat is still sending; attach once OnSend has run."),
            (during.Item1, during.Item2, helper.Message));
        Assert.False(cut.Find("input[type=file]").HasAttribute("disabled"));
    }

    [Fact]
    public void Time_ShouldShowInTheChatsTimeZone_WhenOneIsGiven()
    {
        var tokyo = TimeZoneInfo.CreateCustomTimeZone("Tokyo", TimeSpan.FromHours(9), "Tokyo", "Tokyo");

        var cut = RenderChat([From(Robin, "1", "Morning.")], parameters => parameters.Add(chat => chat.TimeZone, tokyo));

        Assert.Equal(Morning.ToOffset(TimeSpan.FromHours(9)).ToString("t", System.Globalization.CultureInfo.CurrentCulture),
            cut.Find("[data-part=time]").TextContent);
    }

    [Fact]
    public void Attachments_ShouldShowAsLinks_WhenAMessageHasFiles()
    {
        var cut = RenderChat([From(Robin, "1", "The map") with
        {
            Attachments = [new ChatAttachment("map.pdf", "https://example.com/map.pdf")],
        }]);

        var link = cut.Find("[data-part=attachment]");
        Assert.Equal(("map.pdf", "https://example.com/map.pdf"), (link.TextContent, link.GetAttribute("href")));
    }

    [Fact]
    public void Composer_ShouldBeLeftOut_WhenShowComposerIsOff()
    {
        var cut = RenderChat([], parameters => parameters.Add(chat => chat.ShowComposer, false));

        Assert.Empty(cut.FindAll("[data-part=composer]"));
    }

    [Fact]
    public void Render_ShouldThrow_WhenItHasNoAriaLabel()
    {
        var thrown = Assert.Throws<InvalidOperationException>(() => Render<TbChat>());

        Assert.Equal("TbChat needs an AriaLabel.", thrown.Message);
    }

    private IRenderedComponent<TbChat> RenderChat(IReadOnlyList<ChatMessage> messages,
        Action<ComponentParameterCollectionBuilder<TbChat>>? configure = null) => Render<TbChat>(parameters =>
    {
        parameters.Add(chat => chat.AriaLabel, "Trip planning").Add(chat => chat.Messages, messages)
            .Add(chat => chat.CurrentAuthorId, "sam").Add(chat => chat.OnSend, (ChatSend send) => _sent.Add(send));
        configure?.Invoke(parameters);
    });
}
