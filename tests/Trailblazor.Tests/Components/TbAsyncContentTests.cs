using System.Diagnostics.Metrics;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.Metrics.Testing;
using Microsoft.Extensions.Time.Testing;
using Trailblazor.Testing;

namespace Trailblazor.Tests.Components;

public class TbAsyncContentTests : BunitContext
{
    private readonly FakeTimeProvider _time = new();
    private readonly Queue<TaskCompletionSource<string[]>> _loads = new();
    private readonly List<CancellationToken> _tokens = [];
    private bool _ignoreTokens;

    public TbAsyncContentTests()
    {
        Services.AddSingleton<TimeProvider>(_time);
        this.AddTrailblazor();
    }

    // Each load waits for the next answer the test gives, and stops waiting when its token is canceled.
    private TaskCompletionSource<string[]> NextLoad()
    {
        var load = new TaskCompletionSource<string[]>();
        _loads.Enqueue(load);
        return load;
    }

    private async Task<string[]> FetchAsync(CancellationToken token)
    {
        _tokens.Add(token);
        var load = _loads.Dequeue().Task;
        return _ignoreTokens ? await load : await load.WaitAsync(token);
    }

    private IRenderedComponent<GroupsHost> RenderGroups(
        Action<ComponentParameterCollectionBuilder<GroupsHost>>? more = null) => Render<GroupsHost>(parameters =>
        {
            parameters.Add(host => host.Fetch, FetchAsync);
            more?.Invoke(parameters);
        });

    private static TbAsyncContentDriver Region(IRenderedComponent<GroupsHost> cut) => cut.Tb().AsyncContent("groups");

    private static string Status(IRenderedComponent<GroupsHost> cut) =>
        cut.Find("[data-part=status]").TextContent.Trim();

    [Fact]
    public void TbAsyncContent_ShouldShowNothing_WhenLoadFinishesWithinShowDelay()
    {
        var load = NextLoad();
        var cut = RenderGroups();
        Assert.Equal((AsyncContentState.Loading, true), (Region(cut).State, Region(cut).Busy));

        _time.Advance(TimeSpan.FromMilliseconds(199));
        load.SetResult(["Hikers", "Paddlers"]);

        cut.WaitForAssertion(() => Assert.Equal(AsyncContentState.Content, Region(cut).State));
        Assert.Equal(("", false, "Hikers, Paddlers"),
            (Status(cut), Region(cut).Busy, cut.Find("p.groups").TextContent));
    }

    [Fact]
    public async Task TbAsyncContent_ShouldKeepSpinnerForMinimum_WhenShown()
    {
        var load = NextLoad();
        var cut = RenderGroups();

        _time.Advance(TimeSpan.FromMilliseconds(200));
        cut.WaitForAssertion(() => Assert.Equal("Loading groups", Status(cut)));
        _time.Advance(TimeSpan.FromMilliseconds(100));
        // The answer resumes the load on the renderer, so the clock moves on only once the load has seen it.
        await cut.InvokeAsync(() => load.SetResult(["Hikers"]));
        await cut.InvokeAsync(() => { });
        _time.Advance(TimeSpan.FromMilliseconds(299));

        Assert.Equal((AsyncContentState.Loading, "Loading groups"), (Region(cut).State, Status(cut)));
        _time.Advance(TimeSpan.FromMilliseconds(1));
        cut.WaitForAssertion(() => Assert.Equal(AsyncContentState.Content, Region(cut).State));
        Assert.Equal("", Status(cut));
    }

    [Fact]
    public async Task TbAsyncContent_ShouldKeepContent_WhenReloading()
    {
        NextLoad().SetResult(["Hikers"]);
        var cut = RenderGroups();
        cut.WaitForAssertion(() => Assert.Equal(AsyncContentState.Content, Region(cut).State));
        var reload = NextLoad();

        var reloading = cut.InvokeAsync(() => cut.FindComponent<TbAsyncContent>().Instance.ReloadAsync());
        _time.Advance(TimeSpan.FromMilliseconds(200));

        cut.WaitForAssertion(() => Assert.NotNull(cut.Find("[data-part=progress]")));
        Assert.Equal((AsyncContentState.Content, true, "Hikers", ""),
            (Region(cut).State, Region(cut).Busy, cut.Find("p.groups").TextContent, Status(cut)));
        reload.SetResult(["Hikers", "Climbers"]);
        _time.Advance(TimeSpan.FromMilliseconds(400));
        await reloading;
        cut.WaitForAssertion(() => Assert.Equal("Hikers, Climbers", cut.Find("p.groups").TextContent));
        Assert.Empty(cut.FindAll("[data-part=progress]"));
    }

    [Fact]
    public async Task TbAsyncContent_ShouldCancelLoad_WhenDisposed()
    {
        NextLoad();
        RenderGroups();

        await DisposeComponentsAsync();

        Assert.True(Assert.Single(_tokens).IsCancellationRequested);
    }

    [Fact]
    public async Task Reload_ShouldIgnoreTheReplacedLoad_WhenItFailsLateHavingIgnoredItsToken()
    {
        _ignoreTokens = true;
        var first = NextLoad();
        var cut = RenderGroups();
        NextLoad().SetResult(["Climbers"]);
        await cut.InvokeAsync(() => cut.FindComponent<TbAsyncContent>().Instance.ReloadAsync());

        await cut.InvokeAsync(() => first.SetException(new HttpRequestException("Offline")));

        Assert.Equal((AsyncContentState.Content, false), (Region(cut).State, Region(cut).Busy));
        Assert.Empty(Services.GetRequiredService<FakeAnnouncer>().Polite);
    }

    [Fact]
    public void Load_ShouldShowTheFailure_WhenTheLoadTimesOutOnItsOwn()
    {
        // An HttpClient's own timeout cancels the load without the region's token, so it's a failure, not a reload.
        NextLoad().SetCanceled(Xunit.TestContext.Current.CancellationToken);

        var cut = RenderGroups();

        cut.WaitForAssertion(() => Assert.Equal(AsyncContentState.Failed, Region(cut).State));
    }

    [Fact]
    public async Task Reload_ShouldDoNothing_WhenTheRegionIsGone()
    {
        NextLoad().SetResult(["Hikers"]);
        var cut = RenderGroups();
        var region = cut.FindComponent<TbAsyncContent>().Instance;
        await DisposeComponentsAsync();

        await region.ReloadAsync();

        Assert.Single(_tokens);
    }

    [Fact]
    public async Task TbAsyncContent_ShouldIgnoreResult_WhenReloadedDuringLoad()
    {
        var first = NextLoad();
        var cut = RenderGroups();
        var second = NextLoad();

        var reloading = cut.InvokeAsync(() => cut.FindComponent<TbAsyncContent>().Instance.ReloadAsync());
        second.SetResult(["Climbers"]);
        await reloading;
        first.TrySetResult(["Hikers"]);

        cut.WaitForAssertion(() => Assert.Equal("Climbers", cut.Find("p.groups").TextContent));
        Assert.True(_tokens[0].IsCancellationRequested);
        Assert.Equal(AsyncContentState.Content, Region(cut).State);
    }

    [Fact]
    public void TbAsyncContent_ShouldAnnounceFailure_WhenLoadThrows()
    {
        var failures = new List<Exception>();
        NextLoad().SetException(new HttpRequestException("Offline"));
        var cut = RenderGroups(parameters => parameters.Add(host => host.OnLoadFailed, failures.Add));

        cut.WaitForAssertion(() => Assert.Equal(AsyncContentState.Failed, Region(cut).State));
        Assert.Equal("Unable to load groups.", cut.Find("[data-part=failed] p").TextContent);
        Assert.Equal(["Unable to load groups."], Services.GetRequiredService<FakeAnnouncer>().Polite);
        Assert.Equal("Offline", Assert.Single(failures).Message);
        Assert.False(cut.Tb().Button("Retry").Busy);
    }

    [Fact]
    public async Task TbAsyncContent_ShouldKeepRetryFocused_WhenRetryFails()
    {
        NextLoad().SetException(new HttpRequestException("Offline"));
        var cut = RenderGroups();
        await Region(cut).WaitUntilLoadedAsync();
        var retry = NextLoad();

        // The failure and its Retry button stay while the retry runs, so the focused button isn't replaced.
        var retrying = Region(cut).RetryAsync();
        Assert.Equal((AsyncContentState.Failed, true), (Region(cut).State, Region(cut).Busy));
        Assert.NotNull(cut.Find("[data-part=failed] [data-part=retry]"));
        retry.SetException(new HttpRequestException("Still offline"));
        await retrying;

        Assert.Equal((AsyncContentState.Failed, false), (Region(cut).State, Region(cut).Busy));
        Assert.NotNull(cut.Find("[data-part=failed] [data-part=retry]"));
        Assert.Equal(2, Services.GetRequiredService<FakeAnnouncer>().Polite.Count);
    }

    [Fact]
    public async Task Retry_ShouldShowTheContent_WhenTheLoadWorksTheSecondTime()
    {
        NextLoad().SetException(new HttpRequestException("Offline"));
        var cut = RenderGroups();
        await Region(cut).WaitUntilLoadedAsync();
        NextLoad().SetResult(["Hikers"]);

        await Region(cut).RetryAsync();

        Assert.Equal((AsyncContentState.Content, "Hikers"), (Region(cut).State, cut.Find("p.groups").TextContent));
        Assert.Empty(cut.FindAll("[data-part=failed]"));
    }

    [Fact]
    public async Task Reload_ShouldKeepTheContentUnderTheFailure_WhenAReloadFails()
    {
        NextLoad().SetResult(["Hikers"]);
        var cut = RenderGroups();
        await Region(cut).WaitUntilLoadedAsync();
        NextLoad().SetException(new HttpRequestException("Offline"));

        await cut.InvokeAsync(() => cut.FindComponent<TbAsyncContent>().Instance.ReloadAsync());

        Assert.Equal((AsyncContentState.Failed, "Hikers"), (Region(cut).State, cut.Find("p.groups").TextContent));
        Assert.NotNull(cut.Find("[data-part=failed] [data-part=retry]"));
    }

    [Fact]
    public void TbAsyncContent_ShouldShowEmptyContent_WhenLoadedWithNoItems()
    {
        NextLoad().SetResult([]);

        var cut = RenderGroups();

        // The host's IsEmpty reads the list its Load filled, and the region asks it once the load ends.
        cut.WaitForAssertion(() => Assert.Equal(AsyncContentState.Empty, Region(cut).State));
        Assert.Equal("No groups yet", cut.Find("[data-part=empty]").TextContent);
    }

    [Fact]
    public async Task IsEmpty_ShouldReadWhatTheLoadFilled_WhenLoadIsALambdaOverALocal()
    {
        string[] groups = [];
        var load = NextLoad();
        // A lambda over a local, as in a loop, belongs to no component, so nothing but the region renders again.
        var cut = Render<TbAsyncContent>(parameters => parameters.Add(region => region.Label, "groups")
            .Add(region => region.Load, async token => groups = await FetchAsync(token))
            .Add(region => region.IsEmpty, () => groups.Length == 0)
            .Add(region => region.EmptyContent, "No groups yet")
            .Add(region => region.ChildContent, builder => builder.AddContent(0, string.Join(", ", groups))));

        await cut.InvokeAsync(() => load.SetResult(["Hikers"]));

        cut.WaitForAssertion(() => Assert.Equal(AsyncContentState.Content, cut.Tb().AsyncContent().State));
        Assert.Contains("Hikers", cut.Markup);
    }

    [Fact]
    public void TbAsyncContent_ShouldShowNoMatchesContent_WhenGivenHasNoMatches()
    {
        NextLoad().SetResult(["Hikers"]);

        var cut = RenderGroups(parameters => parameters.Add(host => host.Search, "Kayak"));

        cut.WaitForAssertion(() => Assert.Equal(AsyncContentState.NoMatches, Region(cut).State));
        Assert.Equal("No groups match Kayak", cut.Find("[data-part=no-matches]").TextContent);
    }

    [Fact]
    public void LoadKey_ShouldLoadAgain_WhenItChanges()
    {
        NextLoad().SetResult(["Hikers"]);
        var cut = RenderGroups(parameters => parameters.Add(host => host.Key, 1));
        cut.WaitForAssertion(() => Assert.Equal("Hikers", cut.Find("p.groups").TextContent));
        NextLoad().SetResult(["Climbers"]);

        cut.Render(parameters => parameters.Add(host => host.Key, 2));

        cut.WaitForAssertion(() => Assert.Equal("Climbers", cut.Find("p.groups").TextContent));
        Assert.Equal(2, _tokens.Count);
    }

    [Fact]
    public void Labels_ShouldReplaceTheSentences_WhenGiven()
    {
        NextLoad().SetException(new HttpRequestException("Offline"));

        var cut = RenderGroups(parameters => parameters.Add(host => host.Labels,
            new AsyncContentLabels { LoadFailed = "Your groups didn't load.", Retry = "Try again" }));

        cut.WaitForAssertion(() =>
            Assert.Equal("Your groups didn't load.", cut.Find("[data-part=failed] p").TextContent));
        Assert.Equal("Try again", cut.Find("[data-part=retry]").TextContent.Trim());
    }

    [Fact]
    public void FailedContent_ShouldTakeTheMessagesPlace_WhenGiven()
    {
        NextLoad().SetException(new HttpRequestException("Offline"));

        var cut = RenderGroups(parameters => parameters.Add(host => host.FailedContent,
            failure => builder => builder.AddContent(0, $"Sorry: {failure.Exception.Message}")));

        cut.WaitForAssertion(() => Assert.Equal("Sorry: Offline", cut.Find("[data-part=failed]").TextContent));
        Assert.Empty(cut.FindAll("[data-part=retry]"));
    }

    [Fact]
    public void Render_ShouldLoadAndOfferNoRetry_WhenThePageIsStatic()
    {
        SetRendererInfo(new RendererInfo("Static", isInteractive: false));
        NextLoad().SetException(new HttpRequestException("Offline"));

        var cut = RenderGroups();

        cut.WaitForAssertion(() => Assert.Equal(AsyncContentState.Failed, Region(cut).State));
        Assert.Empty(cut.FindAll("[data-part=retry]"));
    }

    [Fact]
    public void Render_ShouldNeedALabel_WhenGivenNone()
    {
        var exception = Assert.Throws<InvalidOperationException>(() => Render<TbAsyncContent>(parameters =>
            parameters.Add(region => region.Load, _ => Task.CompletedTask)));

        Assert.Contains("needs a Label", exception.Message);
    }

    [Fact]
    public async Task Telemetry_ShouldCountTheLoadsFailuresAndRetries_WhenNamed()
    {
        var meters = Services.GetRequiredService<IMeterFactory>();
        using var duration = new MetricCollector<double>(meters, "Trailblazor", "trailblazor.load.duration");
        using var failed = new MetricCollector<long>(meters, "Trailblazor", "trailblazor.control.failed");
        using var used = new MetricCollector<long>(meters, "Trailblazor", "trailblazor.control.used");
        var load = NextLoad();
        var cut = RenderGroups(parameters => parameters.Add(host => host.TelemetryName, "trip-groups"));
        // The load takes 0.3 seconds; the spinner it showed is then held to its minimum, which isn't counted.
        _time.Advance(TimeSpan.FromMilliseconds(200));
        cut.WaitForAssertion(() => Assert.Equal("Loading groups", Status(cut)));
        _time.Advance(TimeSpan.FromMilliseconds(100));
        await cut.InvokeAsync(() => load.SetException(new HttpRequestException("Offline")));
        await cut.InvokeAsync(() => { });
        _time.Advance(TimeSpan.FromMilliseconds(300));
        await Region(cut).WaitUntilLoadedAsync();
        NextLoad().SetResult(["Hikers"]);

        await Region(cut).RetryAsync();

        Assert.Equal([("failed", 0.3), ("loaded", 0.0)], duration.GetMeasurementSnapshot()
            .Select(measurement => ((string)measurement.Tags["trailblazor.outcome"]!, measurement.Value)));
        Assert.Equal("load", Assert.Single(failed.GetMeasurementSnapshot()).Tags["trailblazor.action"]);
        Assert.Contains(used.GetMeasurementSnapshot(), use => (string?)use.Tags["trailblazor.action"] == "retried");
    }

    // A page that loads its groups into a field of its own, as an app does.
    private sealed class GroupsHost : ComponentBase
    {
        private string[] _groups = [];

        [Parameter]
        public Func<CancellationToken, Task<string[]>> Fetch { get; set; } = default!;

        [Parameter]
        public string? Search { get; set; }

        [Parameter]
        public object? Key { get; set; }

        [Parameter]
        public AsyncContentLabels Labels { get; set; } = new();

        [Parameter]
        public RenderFragment<AsyncContentFailure>? FailedContent { get; set; }

        [Parameter]
        public EventCallback<Exception> OnLoadFailed { get; set; }

        [Parameter]
        public string? TelemetryName { get; set; }

        private string[] Shown => [.. _groups.Where(group => Search is null || group.Contains(Search))];

        private async Task LoadAsync(CancellationToken token) => _groups = await Fetch(token);

        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            builder.OpenComponent<TbAsyncContent>(0);
            builder.AddComponentParameter(1, nameof(TbAsyncContent.Label), "groups");
            builder.AddComponentParameter(2, nameof(TbAsyncContent.Load), (Func<CancellationToken, Task>)LoadAsync);
            builder.AddComponentParameter(3, nameof(TbAsyncContent.IsEmpty), () => _groups.Length == 0);
            builder.AddComponentParameter(4, nameof(TbAsyncContent.HasNoMatches), () => Shown.Length == 0);
            builder.AddComponentParameter(5, nameof(TbAsyncContent.LoadKey), Key);
            builder.AddComponentParameter(6, nameof(TbAsyncContent.Labels), Labels);
            builder.AddComponentParameter(7, nameof(TbAsyncContent.FailedContent), FailedContent);
            builder.AddComponentParameter(8, nameof(TbAsyncContent.OnLoadFailed), OnLoadFailed);
            builder.AddComponentParameter(9, nameof(TbAsyncContent.TelemetryName), TelemetryName);
            builder.AddComponentParameter(10, nameof(TbAsyncContent.EmptyContent), (RenderFragment)(empty =>
            {
                empty.OpenElement(0, "p");
                empty.AddAttribute(1, "data-part", "empty");
                empty.AddContent(2, "No groups yet");
                empty.CloseElement();
            }));
            builder.AddComponentParameter(11, nameof(TbAsyncContent.NoMatchesContent), (RenderFragment)(none =>
            {
                none.OpenElement(0, "p");
                none.AddAttribute(1, "data-part", "no-matches");
                none.AddContent(2, $"No groups match {Search}");
                none.CloseElement();
            }));
            builder.AddComponentParameter(12, nameof(TbAsyncContent.ChildContent), (RenderFragment)(content =>
            {
                content.OpenElement(0, "p");
                content.AddAttribute(1, "class", "groups");
                content.AddContent(2, string.Join(", ", Shown));
                content.CloseElement();
            }));
            builder.CloseComponent();
        }
    }
}
