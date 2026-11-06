using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Time.Testing;
using Microsoft.JSInterop;

namespace Trailblazor.Tests.Components;

public class TbAutocompleteTests : BunitContext
{
    private static readonly string[] Trails = ["Pine Hollow", "Eagle Ridge", "Cedar Flats", "Pine Bluff"];

    private readonly FakeTimeProvider _time = new();

    public TbAutocompleteTests()
    {
        Services.AddSingleton<TimeProvider>(_time);
        this.AddTrailblazor();
    }

    [Fact]
    public void Render_ShouldBeAComboboxThatControlsAListbox_WhenInteractive()
    {
        var cut = RenderTrails();

        var input = cut.Find("input");
        Assert.Equal("combobox", input.GetAttribute("role"));
        Assert.Equal("list", input.GetAttribute("aria-autocomplete"));
        Assert.Equal(input.Id, cut.Find("label").GetAttribute("for"));
        Assert.Equal("listbox", cut.Find($"#{input.GetAttribute("aria-controls")}").GetAttribute("role"));
    }

    [Fact]
    public void Input_ShouldSuggestTheMatchingItems_WhenTheUserTypes()
    {
        var cut = RenderTrails(maxItems: 1);

        cut.Find("input").Input("pine");

        Assert.Equal(["Pine Hollow"], cut.FindAll("[role=option]").Select(option => option.TextContent.Trim()));
    }

    [Fact]
    public void Click_ShouldChooseTheSuggestion_WhenAnOptionIsClicked()
    {
        string? chosen = null;
        var cut = RenderTrails(onChange: value => chosen = value);
        cut.Find("input").Input("ridge");

        cut.Find("[role=option]").Click();

        Assert.Equal("Eagle Ridge", chosen);
        Assert.Equal("Eagle Ridge", cut.Find("input").GetAttribute("value"));
    }

    [Fact]
    public void Input_ShouldSearchOnceTypingHasSettledForTheDelay_WhenTheUserTypes()
    {
        var searched = new List<string>();
        // A minute the wall clock won't reach, so only the app's TimeProvider can end it.
        var cut = Render<TbAutocomplete<string>>(parameters => parameters
            .Add(field => field.DebounceDelay, TimeSpan.FromMinutes(1)).Add(field => field.Search, (text, _) =>
            {
                searched.Add(text);
                return Task.FromResult(Trails.Where(trail => trail.Contains(text, StringComparison.OrdinalIgnoreCase)));
            }));

        cut.Find("input").Input("p");
        _time.Advance(TimeSpan.FromSeconds(50));
        cut.Find("input").Input("pine b");
        _time.Advance(TimeSpan.FromSeconds(59));
        Assert.Empty(searched);
        _time.Advance(TimeSpan.FromSeconds(1));

        cut.WaitForAssertion(() => Assert.Equal(["Pine Bluff"],
            cut.FindAll("[role=option]").Select(option => option.TextContent.Trim())));
        Assert.Equal(["pine b"], searched);
    }

    [Fact]
    public async Task Input_ShouldCancelTheRunningSearch_WhenTheUserTypesAgain()
    {
        var tokens = new List<CancellationToken>();
        var cut = Render<TbAutocomplete<string>>(parameters => parameters
            .Add(field => field.DebounceDelay, TimeSpan.Zero)
            .Add(field => field.Search, async (text, token) =>
            {
                tokens.Add(token);
                if (tokens.Count == 1)
                {
                    await Task.Delay(Timeout.Infinite, token);
                }

                return Trails.Where(trail => trail.Contains(text, StringComparison.OrdinalIgnoreCase));
            }));

        var first = cut.InvokeAsync(() => cut.Find("input").Input("p"));
        await cut.InvokeAsync(() => cut.Find("input").Input("pine b"));
        await first;

        Assert.True(tokens[0].IsCancellationRequested);
        cut.WaitForAssertion(() => Assert.Equal(["Pine Bluff"],
            cut.FindAll("[role=option]").Select(option => option.TextContent.Trim())));
    }

    [Fact]
    public async Task Render_ShouldDescribeTheClearButtonByTheFieldsLabel_WhenThereIsTextToClear()
    {
        var cut = Render<TbAutocomplete<string>>(parameters => parameters.Add(field => field.Label, "Trail")
            .Add(field => field.DebounceDelay, TimeSpan.Zero).Add(field => field.Clearable, true)
            .Add(field => field.Search, (_, _) => Task.FromResult<IEnumerable<string>>([])));

        await cut.InvokeAsync(() => cut.Find("input").Input("lak"));

        var clear = cut.Find("[data-part=clear]");
        Assert.Equal("Trail", cut.Find($"[id='{clear.GetAttribute("aria-describedby")}']").TextContent.Trim());
    }

    [Fact]
    public async Task Clear_ShouldStopTheSearch_WhenOneIsRunning()
    {
        var cut = Render<TbAutocomplete<string>>(parameters => parameters
            .Add(field => field.DebounceDelay, TimeSpan.Zero)
            .Add(field => field.Clearable, true)
            .Add(field => field.Search, async (_, token) =>
            {
                await Task.Delay(Timeout.Infinite, token);
                return [];
            }));
        var typing = cut.InvokeAsync(() => cut.Find("input").Input("lak"));

        await cut.InvokeAsync(() => cut.Find("[data-part=clear]").Click());
        await typing;

        Assert.False(cut.Find("[role=listbox]").HasAttribute("aria-busy"));
    }

    [Fact]
    public async Task Render_ShouldShowAndSayTheLabelsText_WhenGivenLabels()
    {
        var announcer = Render<AnnouncerHost>();
        var cut = Render<TbAutocomplete<string>>(parameters => parameters
            .Add(field => field.DebounceDelay, TimeSpan.Zero).Add(field => field.Clearable, true)
            .Add(field => field.Labels,
                new AutocompleteLabels { Clear = "Empty", SearchFailed = "Trails are offline." })
            .Add(field => field.Search, (_, _) => Task.FromException<IEnumerable<string>>(new IOException("Down"))));

        await cut.InvokeAsync(() => cut.Find("input").Input("pi"));

        cut.WaitForAssertion(() =>
            Assert.Equal("Trails are offline.", cut.Find("[data-part=status]").TextContent.Trim()));
        announcer.WaitForAssertion(() =>
            Assert.Equal("Trails are offline.", announcer.Find("[role=status] p").TextContent));
        Assert.Equal("Empty", cut.Find("[data-part=clear]").GetAttribute("aria-label"));
    }

    [Fact]
    public async Task Search_ShouldSayTheSuggestionsFailedAndRaiseTheError_WhenTheSearchThrows()
    {
        var announcer = Render<AnnouncerHost>();
        Exception? failure = null;
        var calls = 0;
        var cut = Render<TbAutocomplete<string>>(parameters => parameters
            .Add(field => field.DebounceDelay, TimeSpan.Zero)
            .Add(field => field.OnSearchFailed, (Exception error) => failure = error)
            .Add(field => field.Search, (_, _) => ++calls == 1
                ? Task.FromException<IEnumerable<string>>(new HttpRequestException("Down"))
                : Task.FromResult<IEnumerable<string>>(Trails)));

        await cut.InvokeAsync(() => cut.Find("input").Input("pi"));
        cut.WaitForAssertion(() =>
            Assert.Equal("Couldn't load suggestions.", cut.Find("[data-part=status]").TextContent.Trim()));
        Assert.Equal("Down", failure?.Message);
        Assert.False(cut.Find("[role=listbox]").HasAttribute("aria-busy"));
        announcer.WaitForAssertion(() =>
            Assert.Equal("Couldn't load suggestions.", announcer.Find("[role=status] p").TextContent));
        await cut.InvokeAsync(() => cut.Find("input").Input("pin"));

        cut.WaitForAssertion(() => Assert.Equal(4, cut.FindAll("[role=option]").Count));
    }

    [Fact]
    public async Task Search_ShouldIgnoreAnEarlierSearchsFailure_WhenItEndsAfterTheUserTypedAgain()
    {
        var first = new TaskCompletionSource<IEnumerable<string>>();
        Exception? failure = null;
        var calls = 0;
        var cut = Render<TbAutocomplete<string>>(parameters => parameters
            .Add(field => field.DebounceDelay, TimeSpan.Zero)
            .Add(field => field.OnSearchFailed, (Exception error) => failure = error)
            .Add(field => field.Search, (_, _) => ++calls == 1 ? first.Task
                : Task.FromResult<IEnumerable<string>>(Trails)));
        var typed = cut.InvokeAsync(() => cut.Find("input").Input("pi"));
        await cut.InvokeAsync(() => cut.Find("input").Input("pin"));

        await cut.InvokeAsync(() => first.SetException(new InvalidOperationException("Cancelled upstream")));
        await typed;

        Assert.Null(failure);
        cut.WaitForAssertion(() => Assert.Equal(4, cut.FindAll("[role=option]").Count));
    }

    [Fact]
    public async Task Search_ShouldAskForMoreTyping_WhenAFailedSearchsTextBecomesTooShort()
    {
        var cut = Render<TbAutocomplete<string>>(parameters => parameters
            .Add(field => field.DebounceDelay, TimeSpan.Zero)
            .Add(field => field.MinCharacters, 2)
            .Add(field => field.Search, (_, _) => Task.FromException<IEnumerable<string>>(new IOException())));
        await cut.InvokeAsync(() => cut.Find("input").Input("pi"));

        await cut.InvokeAsync(() => cut.Find("input").Input("p"));

        Assert.Equal("Type to search", cut.Find("[data-part=status]").TextContent.Trim());
    }

    [Fact]
    public async Task Browse_ShouldShowThatItIsSearchingThenTheSuggestions_WhenTheListOpensWithoutTyping()
    {
        var module = JSInterop.SetupModule("./_content/Trailblazor/autocomplete.js");
        module.SetupModule("createAutocomplete", _ => true).Mode = JSRuntimeMode.Loose;
        var found = new TaskCompletionSource<IEnumerable<string>>();
        var cut = Render<TbAutocomplete<string>>(parameters => parameters
            .Add(field => field.DebounceDelay, TimeSpan.Zero)
            .Add(field => field.Search, (_, _) => found.Task));
        var events = (DotNetObjectReference<AutocompleteEvents>)module.Invocations["createAutocomplete"].Single()
            .Arguments[2]!;

        var browse = cut.InvokeAsync(() => events.Value.OnBrowse());
        cut.WaitForAssertion(() => Assert.Equal("true", cut.Find("[role=listbox]").GetAttribute("aria-busy")));
        Assert.Equal("Searching…", cut.Find("[data-part=status]").TextContent.Trim());
        found.SetResult(Trails);
        await browse;

        cut.WaitForAssertion(() => Assert.Equal(4, cut.FindAll("[role=option]").Count));
    }

    [Fact]
    public void Open_ShouldShowTheSuggestions_WhenTheParentOpensTheList()
    {
        var module = JSInterop.SetupModule("./_content/Trailblazor/autocomplete.js");
        var handle = module.SetupModule("createAutocomplete", _ => true);
        handle.Setup<bool>("setOpen", call => call.Arguments[0] is true).SetResult(true);

        var cut = Render<TbAutocomplete<string>>(parameters => parameters.Add(field => field.Items, Trails)
            .Add(field => field.Open, true));

        cut.WaitForAssertion(() => Assert.Equal(4, cut.FindAll("[role=option]").Count));
    }

    [Fact]
    public void Input_ShouldAnnounceTheNumberOfSuggestionsOnlyWhenItChanges_WhenTheUserTypes()
    {
        var announcer = Render<AnnouncerHost>();
        var cut = RenderTrails();

        cut.Find("input").Input("p");
        cut.Find("input").Input("pi");
        cut.Find("input").Input("pine b");

        Assert.Equal(["Suggestions: 2", "Suggestions: 1"],
            announcer.FindAll("[role=status] p").Select(spoken => spoken.TextContent));
    }

    [Fact]
    public void Change_ShouldPutBackTheChosenText_WhenTypedTextIsntASuggestion()
    {
        var cut = RenderTrails("Cedar Flats");

        cut.Find("input").Input("mountain");
        cut.Find("input").Change("mountain");

        Assert.Equal("Cedar Flats", cut.Find("input").GetAttribute("value"));
    }

    [Fact]
    public void Change_ShouldChooseASuggestion_WhenTheTypedTextMatchesOne()
    {
        string? chosen = null;
        var cut = RenderTrails(onChange: value => chosen = value);

        cut.Find("input").Input("eagle ridge");
        cut.Find("input").Change("eagle ridge");

        Assert.Equal("Eagle Ridge", chosen);
    }

    [Fact]
    public void Change_ShouldDropAParseError_WhenTheValuesOwnTextIsTypedBack()
    {
        var cut = Render<TbAutocomplete<double?>>(parameters => parameters.Add(field => field.FreeText, true)
            .Add(field => field.Value, 5).Add(field => field.Items, [5, 10]));

        cut.Find("input").Input("abc");
        cut.Find("input").Change("abc");
        Assert.NotEmpty(cut.FindAll("[data-part=error]"));
        cut.Find("input").Input("5");
        cut.Find("input").Change("5");

        Assert.Empty(cut.FindAll("[data-part=error]"));
    }

    [Fact]
    public void Change_ShouldClearTheValue_WhenTheTextIsEmptied()
    {
        string? chosen = "Cedar Flats";
        var cut = RenderTrails("Cedar Flats", onChange: value => chosen = value);

        cut.Find("input").Input("");
        cut.Find("input").Change("");

        Assert.Null(chosen);
    }

    [Fact]
    public void Change_ShouldReadTheTypedTextAsTheValue_WhenFreeText()
    {
        double? weight = null;
        var cut = Render<TbAutocomplete<double?>>(parameters => parameters.Add(field => field.FreeText, true)
            .Add(field => field.Items, [5, 10])
            .Add(field => field.ValueChanged, (double? value) => weight = value));

        cut.Find("input").Input("8.25");
        cut.Find("input").Change("8.25");

        Assert.Equal(8.25, weight);
    }

    [Fact]
    public void Render_ShouldSayWhyTheListIsEmpty_WhenNothingMatchesOrMoreTextIsNeeded()
    {
        var cut = RenderTrails(minCharacters: 2);

        cut.Find("input").Input("p");
        Assert.Equal("Type to search", cut.Find("[data-part=status]").TextContent.Trim());
        cut.Find("input").Input("zzz");

        Assert.Equal("No matches", cut.Find("[data-part=status]").TextContent.Trim());
    }

    [Fact]
    public void Render_ShouldKeepTheListValidWithAnOptionThatCannotBeChosen_WhenOnlyAMessageShows()
    {
        var cut = RenderTrails(minCharacters: 2);

        cut.Find("input").Input("p");

        var message = Assert.Single(cut.FindAll("[role=listbox] > [role=option]"));
        Assert.Equal("status", message.GetAttribute("data-part"));
        Assert.Equal("true", message.GetAttribute("aria-disabled"));
    }

    [Fact]
    public void Click_ShouldClearTheFieldAndFocusIt_WhenTheClearButtonIsClicked()
    {
        string? chosen = "Cedar Flats";
        var cut = RenderTrails("Cedar Flats", onChange: value => chosen = value, clearable: true);

        cut.Find("[data-part=clear]").Click();

        Assert.Null(chosen);
        Assert.Equal("", cut.Find("input").GetAttribute("value"));
        JSInterop.VerifyFocusAsyncInvoke();
    }

    [Fact]
    public void Render_ShouldBeAPlainTextInput_WhenRenderedStatically()
    {
        SetRendererInfo(new RendererInfo("Static", isInteractive: false));

        var cut = RenderTrails("Cedar Flats");

        var input = cut.Find("input");
        Assert.Null(input.GetAttribute("role"));
        Assert.True(input.HasAttribute("data-static"));
        Assert.Empty(cut.FindAll("[role=listbox]"));
        Assert.Equal("Cedar Flats", input.GetAttribute("value"));
    }

    private IRenderedComponent<TbAutocomplete<string?>> RenderTrails(string? value = null,
        Action<string?>? onChange = null, int? maxItems = null, int minCharacters = 0, bool clearable = false) =>
        Render<TbAutocomplete<string?>>(parameters => parameters.Add(field => field.Label, "Trail")
            .Add(field => field.Items, Trails).Add(field => field.Value, value)
            .Add(field => field.MaxItems, maxItems).Add(field => field.MinCharacters, minCharacters)
            .Add(field => field.Clearable, clearable)
            .Add(field => field.ValueChanged, (string? next) => onChange?.Invoke(next)));
}
