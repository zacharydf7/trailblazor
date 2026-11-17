using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;
using Trailblazor.Testing;

namespace Trailblazor.Tests.Components;

public class TbMentionInputTests : BunitContext
{
    private static readonly MentionItem Ana = new("Ana Lopez") { Id = "u1", Description = "Trail crew" };
    private static readonly MentionItem Dan = new("Dan Ng") { Id = "u2" };
    private static readonly MentionItem Priya = new("Priya Shah") { Id = "u3" };

    private string? _value;
    private IReadOnlyList<Mention>? _mentions;

    public TbMentionInputTests()
    {
        this.AddTrailblazor();
        JSInterop.SetupModule("./_content/Trailblazor/mention-input.js")
            .SetupModule("createMentionInput", _ => true).SetupVoid("dispose", _ => true).SetVoidResult();
    }

    [Fact]
    public void Render_ShouldBeALabeledFieldThatControlsAListOfSuggestions_WhenCalled()
    {
        var cut = RenderInput();

        var input = cut.Find("textarea[data-part=input]");
        var listbox = cut.Find("[role=listbox]");
        Assert.Equal(input.Id, cut.Find("label").GetAttribute("for"));
        Assert.Equal(("list", listbox.Id, "@"), (input.GetAttribute("aria-autocomplete"),
            input.GetAttribute("aria-controls"), input.GetAttribute("data-triggers")));
        Assert.Equal(cut.Find("label").Id, listbox.GetAttribute("aria-labelledby"));
        Assert.Equal("text", Render<TbMentionInput>(parameters => parameters.Add(field => field.Label, "Title")
            .Add(field => field.Lines, 1)).Find("input[data-part=input]").GetAttribute("type"));
    }

    [Fact]
    public async Task TypeAsync_ShouldSuggestTheItemsMatchingTheTextAfterTheTrigger_WhenATriggerStartsAWord()
    {
        var cut = RenderInput();
        var field = cut.Tb().MentionInput("Comment");

        await field.TypeAsync("Thanks @an");

        Assert.Equal(["Ana Lopez", "Dan Ng"], field.Suggestions);
        Assert.Equal("Trail crew", cut.Find($"#{cut.Find("[role=option]").GetAttribute("aria-describedby")}")
            .TextContent);
        await field.TypeAsync("Thanks@an");
        Assert.Empty(field.Suggestions);
    }

    [Fact]
    public async Task ChooseAsync_ShouldPutTheMentionInAndReportIt_WhenASuggestionIsChosen()
    {
        var cut = RenderInput();
        var field = cut.Tb().MentionInput("Comment");
        await field.TypeAsync("Thanks @an");

        await field.ChooseAsync("Ana Lopez");

        Assert.Equal("Thanks @Ana Lopez ", _value);
        Assert.Equal([new Mention("@", Ana)], _mentions);
        Assert.Empty(field.Suggestions);
    }

    [Fact]
    public async Task TypeAsync_ShouldDropAMention_WhenItsTextIsDeleted()
    {
        var cut = RenderInput();
        var field = cut.Tb().MentionInput("Comment");
        await field.TypeAsync("Thanks @an");
        await field.ChooseAsync("Ana Lopez");

        await field.TypeAsync("Thanks @Ana Lo");

        Assert.Empty(_mentions!);
    }

    [Fact]
    public async Task Search_ShouldBeAskedWithTheTriggerAndText_WhenGiven()
    {
        var queries = new List<MentionQuery>();
        var cut = Render<TbMentionInput>(parameters => parameters.Add(field => field.Label, "Comment")
            .Add(field => field.Triggers, "@#")
            .Add(field => field.Search, (query, _) =>
            {
                queries.Add(query);
                return Task.FromResult<IEnumerable<MentionItem>>(query.Trigger == "#" ? [new("pine-hollow")] : []);
            }));

        await cut.Tb().MentionInput("Comment").TypeAsync("See #pi");

        Assert.Equal([new MentionQuery("#", "pi")], queries);
        Assert.Equal(["pine-hollow"], cut.Tb().MentionInput("Comment").Suggestions);
    }

    [Fact]
    public async Task TypeAsync_ShouldCancelTheSearchBefore_WhenTheTextChangesAgain()
    {
        var first = new TaskCompletionSource<IEnumerable<MentionItem>>();
        var tokens = new List<CancellationToken>();
        var cut = Render<TbMentionInput>(parameters => parameters.Add(field => field.Label, "Comment")
            .Add(field => field.Search, (query, token) =>
            {
                tokens.Add(token);
                return query.Text == "a" ? first.Task : Task.FromResult<IEnumerable<MentionItem>>([Priya]);
            }));
        var field = cut.Tb().MentionInput("Comment");

        _ = field.TypeAsync("@a");
        await field.TypeAsync("@pr");
        first.SetResult([Ana]);

        Assert.True(tokens[0].IsCancellationRequested);
        Assert.Equal(["Priya Shah"], field.Suggestions);
    }

    [Fact]
    public async Task ChooseAsync_ShouldStopTheSearchStillRunning_WhenASuggestionIsChosen()
    {
        var later = new TaskCompletionSource<IEnumerable<MentionItem>>();
        CancellationToken stopped = default;
        var cut = Render<TbMentionInput>(parameters => parameters.Add(field => field.Label, "Comment")
            .Add(field => field.Search, (query, token) =>
            {
                if (query.Text == "a")
                {
                    stopped = token;
                    return later.Task;
                }

                return Task.FromResult<IEnumerable<MentionItem>>([Ana, Dan]);
            }));
        var field = cut.Tb().MentionInput("Comment");
        await field.TypeAsync("@");
        _ = field.TypeAsync("@a");

        await field.ChooseAsync("Ana Lopez");
        later.SetResult([Ana]);

        Assert.True(stopped.IsCancellationRequested);
        Assert.Empty(field.Suggestions);
    }

    [Fact]
    public async Task OnChosen_ShouldNotCountTheChoice_WhenItComesFromSuggestionsALaterQueryReplaced()
    {
        var cut = RenderInput();
        var field = cut.Tb().MentionInput("Comment");
        await field.TypeAsync("@a");
        var stale = int.Parse(cut.Find("[role=listbox]").GetAttribute("data-results-for")!);
        await field.TypeAsync("@an");
        var events = (DotNetObjectReference<MentionInputEvents>)JSInterop.Invocations["createMentionInput"][0]
            .Arguments[2]!;

        await cut.InvokeAsync(() => events.Value.OnChosen(0, stale));
        await cut.Find("textarea").InputAsync(new ChangeEventArgs { Value = "@Ana Lopez " });

        Assert.Null(_mentions);
    }

    [Fact]
    public async Task Render_ShouldAttachTheScriptAgain_WhenLinesSwapsTheInputForATextArea()
    {
        var cut = RenderInput();

        cut.Render(parameters => parameters.Add(field => field.Lines, 1));

        Assert.Equal(2, JSInterop.Invocations["createMentionInput"].Count);
        await cut.Tb().MentionInput("Comment").TypeAsync("@an");
        Assert.Equal(["Ana Lopez", "Dan Ng"], cut.Tb().MentionInput("Comment").Suggestions);
    }

    [Fact]
    public async Task TypeAsync_ShouldCloseTheListAndReportTheFailure_WhenTheSearchFails()
    {
        var failures = new List<Exception>();
        var cut = Render<TbMentionInput>(parameters => parameters.Add(field => field.Label, "Comment")
            .Add(field => field.Search, (query, _) => query.Text == "x"
                ? throw new TimeoutException("The people search timed out.")
                : Task.FromResult<IEnumerable<MentionItem>>([Ana]))
            .Add(field => field.OnSearchFailed, failure => failures.Add(failure)));
        var field = cut.Tb().MentionInput("Comment");
        await field.TypeAsync("@");

        await field.TypeAsync("@x");

        Assert.Empty(field.Suggestions);
        Assert.IsType<TimeoutException>(Assert.Single(failures));
        Assert.Contains("Couldn't load suggestions.", Services.GetRequiredService<FakeAnnouncer>().Polite);
    }

    [Fact]
    public void Render_ShouldMakeAOneLineFieldACombobox_WhenLinesIsOne()
    {
        var cut = Render<TbMentionInput>(parameters => parameters.Add(field => field.Label, "Comment")
            .Add(field => field.Lines, 1));

        var input = cut.Find("input[data-part=input]");
        Assert.Equal(("combobox", "false"), (input.GetAttribute("role"), input.GetAttribute("aria-expanded")));
        Assert.Null(RenderInput().Find("textarea").GetAttribute("role"));
    }

    [Fact]
    public async Task Mentions_ShouldKeepTheMentionsGiven_WhenTheirTextStays()
    {
        var cut = RenderInput("Hi @Dan Ng", [new Mention("@", Dan)]);
        var field = cut.Tb().MentionInput("Comment");

        await field.TypeAsync("Hi @Dan Ng and @pr");
        await field.ChooseAsync("Priya Shah");

        Assert.Equal([new Mention("@", Dan), new Mention("@", Priya)], _mentions);
    }

    [Fact]
    public async Task TypeAsync_ShouldThrow_WhenTheFieldIsReadOnly()
    {
        var cut = Render<TbMentionInput>(parameters => parameters.Add(field => field.Label, "Comment")
            .Add(field => field.ReadOnly, true));

        await Assert.ThrowsAsync<InvalidOperationException>(() => cut.Tb().MentionInput("Comment").TypeAsync("@a"));
    }

    [Fact]
    public void Render_ShouldThrow_WhenATriggerIsASpace()
    {
        var thrown = Assert.Throws<InvalidOperationException>(() => Render<TbMentionInput>(parameters => parameters
            .Add(field => field.Label, "Comment").Add(field => field.Triggers, "@ ")));

        Assert.Equal("TbMentionInput needs at least one character in Triggers, and no whitespace.", thrown.Message);
    }

    private IRenderedComponent<TbMentionInput> RenderInput(string? value = null,
        IReadOnlyList<Mention>? mentions = null)
    {
        (_value, _mentions) = (value, mentions);
        return Render<TbMentionInput>(parameters => parameters.Add(field => field.Label, "Comment")
            .Add(field => field.Value, value).Add(field => field.ValueChanged, text => _value = text)
            .Add(field => field.Mentions, mentions)
            .Add(field => field.MentionsChanged, found => _mentions = found)
            .Add(field => field.Items, [Ana, Dan, Priya]));
    }
}
