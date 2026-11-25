using System.Collections.Concurrent;
using System.Diagnostics;
using System.Linq.Expressions;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;
using Trailblazor.Testing;

namespace Trailblazor.Tests.Components;

// The Trailblazor source is heard across the whole test run, so each test here uses a telemetry name no other test uses,
// and finds its spans by it.
public sealed class TelemetrySpanTests : BunitContext
{
    private readonly ConcurrentQueue<Activity> _ended = new();
    private readonly ActivityListener _listener;

    public TelemetrySpanTests()
    {
        _listener = new()
        {
            ShouldListenTo = source => source.Name == "Trailblazor",
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded,
            ActivityStopped = _ended.Enqueue,
        };
        ActivitySource.AddActivityListener(_listener);
        JSInterop.SetupModule("./_content/Trailblazor/mention-input.js")
            .SetupModule("createMentionInput", _ => true).SetupVoid("dispose", _ => true).SetVoidResult();
    }

    private sealed class CustomerTags : ITelemetryTags
    {
        public IEnumerable<KeyValuePair<string, object?>> Tags => [new("customer", "acme")];
    }

    [Fact]
    public void Grid_ShouldTraceItsLoadWithTheRowsItGot_WhenSomeoneListens()
    {
        this.AddTrailblazor();
        Services.AddScoped<ITelemetryTags, CustomerTags>();
        string? inside = null;
        var cut = RenderGrid("span-grid", (_, _) =>
        {
            inside = Activity.Current?.OperationName;
            return Task.FromResult(new GridResult<string>(["Ridge", "Lake"], 2));
        });

        cut.WaitForAssertion(() => Assert.Equal(2, cut.FindAll("tbody tr").Count));

        var span = Single("span-grid");
        Assert.Equal(("TbDataGrid load", "TbDataGrid load"), (span.OperationName, inside));
        Assert.Equal(("load", "succeeded", 2, "acme"), ((string?)span.GetTagItem("trailblazor.action"),
            (string?)span.GetTagItem("trailblazor.outcome"), span.GetTagItem("trailblazor.items"),
            (string?)span.GetTagItem("customer")));
        Assert.Equal(ActivityStatusCode.Unset, span.Status);
    }

    [Fact]
    public void Grid_ShouldMarkItsSpanFailedWithTheExceptionsTypeOnly_WhenItsProviderThrows()
    {
        this.AddTrailblazor();
        var cut = RenderGrid("failing-grid",
            (_, _) => Task.FromException<GridResult<string>>(new IOException("Disk at /srv/secret is full")));

        cut.WaitForAssertion(() => Assert.NotNull(cut.Find("[data-part=failed]")));

        var span = Single("failing-grid");
        Assert.Equal((ActivityStatusCode.Error, "failed", "System.IO.IOException"), (span.Status,
            (string?)span.GetTagItem("trailblazor.outcome"), (string?)span.GetTagItem("error.type")));
        Assert.DoesNotContain(span.TagObjects, tag => tag.Value?.ToString()?.Contains("secret") == true);
        Assert.Empty(span.Events);
    }

    [Fact]
    public void Tree_ShouldTraceLoadingAnItemsChildren_WhenSomeoneListens()
    {
        this.AddTrailblazor();
        var cut = Render<TbTreeView<string, string>>(parameters => parameters.Add(tree => tree.AriaLabel, "Places")
            .Add(tree => tree.TelemetryName, "span-tree").Add(tree => tree.Items, ["North"])
            .Add(tree => tree.HasChildren, item => item == "North")
            .Add(tree => tree.LoadChildren, (_, _) => Task.FromResult<IEnumerable<string>>(["Ridge", "Lake", "Pass"])));

        cut.Find("[role=treeitem] [data-part=toggle]").Click();

        cut.WaitForAssertion(() => Assert.Equal(4, cut.FindAll("[role=treeitem]").Count));
        var span = Single("span-tree");
        Assert.Equal(("TbTreeView load", 3), (span.OperationName, span.GetTagItem("trailblazor.items")));
    }

    [Fact]
    public async Task Autocomplete_ShouldTraceItsSearch_WhenSomeoneListens()
    {
        this.AddTrailblazor();
        var cut = Render<TbAutocomplete<string>>(parameters => parameters.Add(field => field.Label, "Trail")
            .Add(field => field.TelemetryName, "span-search").Add(field => field.DebounceDelay, TimeSpan.Zero)
            .Add(field => field.Search, (_, _) => Task.FromResult<IEnumerable<string>>(["Pine Ridge"])));

        await cut.InvokeAsync(() => cut.Find("input").Input("pi"));

        cut.WaitForAssertion(() => Assert.Single(Spans("span-search")));
        var span = Single("span-search");
        Assert.Equal(("TbAutocomplete search", "succeeded", 1), (span.OperationName,
            (string?)span.GetTagItem("trailblazor.outcome"), span.GetTagItem("trailblazor.items")));
    }

    [Fact]
    public async Task MentionInput_ShouldTraceItsSearch_WhenSomeoneListens()
    {
        this.AddTrailblazor();
        var cut = Render<TbMentionInput>(parameters => parameters.Add(field => field.Label, "Comment")
            .Add(field => field.TelemetryName, "span-mentions")
            .Add(field => field.Search, (_, _) => Task.FromResult<IEnumerable<MentionItem>>([new("Ana"), new("Abe")])));

        await cut.Tb().MentionInput("Comment").TypeAsync("@a");

        var span = Single("span-mentions");
        Assert.Equal(("TbMentionInput search", 2), (span.OperationName, span.GetTagItem("trailblazor.items")));
    }

    [Fact]
    public async Task Autocomplete_ShouldEndASearchTheUserMovedOnFromAsCanceled_WhenTheyTypeAgain()
    {
        this.AddTrailblazor();
        var first = new TaskCompletionSource<IEnumerable<string>>();
        var calls = 0;
        var cut = Render<TbAutocomplete<string>>(parameters => parameters.Add(field => field.Label, "Trail")
            .Add(field => field.TelemetryName, "span-canceled").Add(field => field.DebounceDelay, TimeSpan.Zero)
            .Add(field => field.Search, (_, _) => ++calls == 1 ? first.Task
                : Task.FromResult<IEnumerable<string>>(["Pine Hollow"])));
        var typed = cut.InvokeAsync(() => cut.Find("input").Input("pi"));
        await cut.InvokeAsync(() => cut.Find("input").Input("pin"));

        await cut.InvokeAsync(() => first.SetResult(["Pine Ridge"]));
        await typed;

        Assert.Equal(["canceled", "succeeded"], Spans("span-canceled").Select(span =>
            (string?)span.GetTagItem("trailblazor.outcome")).Order());
    }

    [Fact]
    public async Task RichTextEditor_ShouldTraceItsMentionSearchButNotItsSlashCommands_WhenSomeoneListens()
    {
        this.AddTrailblazor();
        var handle = JSInterop.SetupModule("./_content/Trailblazor/rich-text-editor.js")
            .SetupModule("attachRichTextEditor", _ => true);
        foreach (var call in new[] { "setValue", "setOptions", "setMentions", "dispose" })
        {
            handle.SetupVoid(call, _ => true).SetVoidResult();
        }

        var cut = Render<TbRichTextEditor>(parameters => parameters.Add(editor => editor.Label, "Report")
            .Add(editor => editor.TelemetryName, "span-editor").Add(editor => editor.MentionTriggers, "@")
            .Add(editor => editor.SlashCommands, true)
            .Add(editor => editor.MentionSearch, (_, _) => Task.FromResult<IEnumerable<MentionItem>>([new("Ana")])));
        var events = ((DotNetObjectReference<MentionInputEvents>)handle.Invocations["setMentions"].Single()
            .Arguments[1]!).Value;

        await cut.InvokeAsync(() => events.OnQuery("/", "", 1, "paragraph"));
        await cut.InvokeAsync(() => events.OnQuery("@", "a", 2));

        var span = Single("span-editor");
        Assert.Equal(("TbRichTextEditor search", 1), (span.OperationName, span.GetTagItem("trailblazor.items")));
    }

    [Fact]
    public void AsyncContent_ShouldTraceItsLoad_WhenSomeoneListens()
    {
        this.AddTrailblazor();
        var cut = Render<TbAsyncContent>(parameters => parameters.Add(region => region.Label, "Groups")
            .Add(region => region.TelemetryName, "group-list").Add(region => region.Load, _ => Task.CompletedTask));

        cut.WaitForAssertion(() => Assert.Single(Spans("group-list")));
        var span = Single("group-list");
        Assert.Equal(("TbAsyncContent load", "succeeded"), (span.OperationName,
            (string?)span.GetTagItem("trailblazor.outcome")));
    }

    [Fact]
    public void Grid_ShouldStartNoSpan_WhenTelemetryIsOff()
    {
        this.AddTrailblazor(options => options.Telemetry.Enabled = false);
        var cut = RenderGrid("quiet-grid", (_, _) => Task.FromResult(new GridResult<string>(["Ridge"], 1)));

        cut.WaitForAssertion(() => Assert.Single(cut.FindAll("tbody tr")));

        Assert.Empty(Spans("quiet-grid"));
    }

    [Fact]
    public void Grid_ShouldStartNoSpan_WhenItHasNoTelemetryName()
    {
        this.AddTrailblazor();
        string? inside = "unset";
        var cut = RenderGrid(null, (_, _) =>
        {
            inside = Activity.Current?.Source.Name;
            return Task.FromResult(new GridResult<string>(["Ridge"], 1));
        });

        cut.WaitForAssertion(() => Assert.Single(cut.FindAll("tbody tr")));

        Assert.NotEqual("Trailblazor", inside);
    }

    private IRenderedComponent<TbDataGrid<string>> RenderGrid(string? telemetryName,
        Func<GridRequest, CancellationToken, Task<GridResult<string>>> provider) =>
        Render<TbDataGrid<string>>(parameters => parameters.Add(grid => grid.AriaLabel, "Trails")
            .Add(grid => grid.TelemetryName, telemetryName).Add(grid => grid.ChildContent, NameColumn())
            .Add(grid => grid.DataProvider, provider));

    private List<Activity> Spans(string telemetryName) =>
        [.. _ended.Where(span => (string?)span.GetTagItem("trailblazor.telemetry_name") == telemetryName)];

    private Activity Single(string telemetryName) => Assert.Single(Spans(telemetryName));

    private static RenderFragment NameColumn() => builder =>
    {
        builder.OpenComponent<TbColumn<string>>(0);
        builder.AddAttribute(1, nameof(TbColumn<string>.Field), (Expression<Func<string, object?>>)(name => name));
        builder.AddAttribute(2, nameof(TbColumn<string>.Title), "Name");
        builder.CloseComponent();
    };

    protected override void Dispose(bool disposing)
    {
        _listener.Dispose();
        base.Dispose(disposing);
    }
}
