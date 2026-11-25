using System.Diagnostics.Metrics;
using System.Linq.Expressions;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.Metrics.Testing;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;
using Microsoft.JSInterop;
using Trailblazor.Testing;

namespace Trailblazor.Tests.Components;

// An app that binds no failure callback still has to hear of a load or a search that failed.
public class LoadFailureTests : BunitContext
{
    private readonly FakeLogCollector _logs = new();
    private readonly MetricCollector<long> _failed;

    public LoadFailureTests()
    {
        Services.AddLogging(logging => logging.SetMinimumLevel(LogLevel.Debug)
            .AddProvider(new FakeLoggerProvider(_logs)));
        this.AddTrailblazor();
        JSInterop.SetupModule("./_content/Trailblazor/mention-input.js")
            .SetupModule("createMentionInput", _ => true).SetupVoid("dispose", _ => true).SetVoidResult();
        _failed = new(Services.GetRequiredService<IMeterFactory>(), "Trailblazor", "trailblazor.control.failed");
    }

    [Fact]
    public void Grid_ShouldLogAWarningAndCountTheFailure_WhenItsProviderThrows()
    {
        var cut = Render<TbDataGrid<string>>(parameters => parameters.Add(grid => grid.AriaLabel, "Trails")
            .Add(grid => grid.TelemetryName, "trail-grid").Add(grid => grid.ChildContent, NameColumn())
            .Add(grid => grid.DataProvider, (_, _) => Task.FromException<GridResult<string>>(new IOException("Down"))));

        cut.WaitForAssertion(() => Assert.NotNull(cut.Find("[data-part=failed]")));
        AssertFailed("Trailblazor.TbDataGrid", "TbDataGrid trail-grid failed to load.", "trail-grid", "load");
    }

    [Fact]
    public async Task Grid_ShouldCountAFailedExportAndRaiseOnLoadFailed_WhenItsProviderThrowsDuringAnExport()
    {
        var loads = 0;
        var failed = new List<Exception>();
        var host = Render<AnnouncerHost>();
        var cut = Render<TbDataGrid<string>>(parameters => parameters.Add(grid => grid.AriaLabel, "Trails")
            .Add(grid => grid.TelemetryName, "trail-grid").Add(grid => grid.ChildContent, NameColumn())
            .Add(grid => grid.Exportable, true).Add(grid => grid.OnLoadFailed, (Exception error) => failed.Add(error))
            .Add(grid => grid.DataProvider, (_, _) => ++loads == 1
                ? Task.FromResult(new GridResult<string>(["Pine Hollow"], 1))
                : Task.FromException<GridResult<string>>(new IOException("Down"))));

        await cut.Tb().Menu("Export").ChooseAsync("CSV");

        AssertFailed("Trailblazor.TbDataGrid", "TbDataGrid trail-grid failed to export.", "trail-grid", "export");
        Assert.Equal("Down", Assert.Single(failed).Message);
        host.WaitForAssertion(() => Assert.Contains("Couldn't export the rows.", Spoken(host)));
    }

    [Fact]
    public async Task Grid_ShouldCountAFailedExportButNoFailedLoad_WhenAColumnThrowsWhileTheFileIsBuilt()
    {
        var failed = new List<Exception>();
        var host = Render<AnnouncerHost>();
        var trip = new Trip();
        var cut = Render<TbDataGrid<Trip>>(parameters => parameters.Add(grid => grid.AriaLabel, "Trips")
            .Add(grid => grid.TelemetryName, "trip-grid")
            .Add(grid => grid.DataProvider, (_, _) => Task.FromResult(new GridResult<Trip>([trip], 1)))
            .Add(grid => grid.Exportable, true).Add(grid => grid.OnLoadFailed, (Exception error) => failed.Add(error))
            .Add(grid => grid.Labels, new DataGridLabels { ExportFailed = "The trips didn't export." })
            .AddChildContent<TbColumn<Trip>>(column => column.Add(name => name.Field, trip => trip.Name)));
        trip.Broken = true;

        await cut.Tb().Menu("Export").ChooseAsync("CSV");

        AssertFailed("Trailblazor.TbDataGrid", "TbDataGrid trip-grid failed to export.", "trip-grid", "export");
        Assert.Empty(failed);
        host.WaitForAssertion(() => Assert.Contains("The trips didn't export.", Spoken(host)));
        Assert.DoesNotContain("Couldn't load the rows.", Spoken(host));
    }

    [Fact]
    public void Tree_ShouldLogAWarningAndCountTheFailure_WhenLoadingChildrenThrows()
    {
        var cut = Render<TbTreeView<string, string>>(parameters => parameters.Add(tree => tree.AriaLabel, "Places")
            .Add(tree => tree.TelemetryName, "place-tree").Add(tree => tree.Items, ["North"])
            .Add(tree => tree.HasChildren, _ => true)
            .Add(tree => tree.LoadChildren, (_, _) => Task.FromException<IEnumerable<string>>(new IOException("Down"))));

        cut.Find("[role=treeitem] [data-part=toggle]").Click();

        cut.WaitForAssertion(() => Assert.NotNull(cut.Find("[data-part=failed]")));
        AssertFailed("Trailblazor.TbTreeView", "TbTreeView place-tree failed to load.", "place-tree", "load");
    }

    [Fact]
    public async Task Autocomplete_ShouldLogAWarningAndCountTheFailure_WhenItsSearchThrows()
    {
        var cut = Render<TbAutocomplete<string>>(parameters => parameters.Add(field => field.Label, "Trail")
            .Add(field => field.TelemetryName, "trail-search").Add(field => field.DebounceDelay, TimeSpan.Zero)
            .Add(field => field.Search, (_, _) => Task.FromException<IEnumerable<string>>(new IOException("Down"))));

        await cut.InvokeAsync(() => cut.Find("input").Input("pi"));

        cut.WaitForAssertion(() => Assert.Single(_logs.GetSnapshot(), log => log.Level == LogLevel.Warning));
        AssertFailed("Trailblazor.TbAutocomplete", "TbAutocomplete trail-search failed to search.", "trail-search",
            "search");
    }

    [Fact]
    public async Task MentionInput_ShouldLogAWarningAndCountTheFailure_WhenItsSearchThrows()
    {
        var cut = Render<TbMentionInput>(parameters => parameters.Add(field => field.Label, "Comment")
            .Add(field => field.TelemetryName, "comment-box")
            .Add(field => field.Search, (_, _) => throw new IOException("Down")));

        await cut.Tb().MentionInput("Comment").TypeAsync("@a");

        AssertFailed("Trailblazor.TbMentionInput", "TbMentionInput comment-box failed to search.", "comment-box",
            "search");
    }

    [Fact]
    public async Task RichTextEditor_ShouldLogAWarningAndCountTheFailure_WhenItsMentionSearchThrows()
    {
        var handle = JSInterop.SetupModule("./_content/Trailblazor/rich-text-editor.js")
            .SetupModule("attachRichTextEditor", _ => true);
        foreach (var call in new[] { "setValue", "setOptions", "setMentions", "dispose" })
        {
            handle.SetupVoid(call, _ => true).SetVoidResult();
        }

        var cut = Render<TbRichTextEditor>(parameters => parameters.Add(editor => editor.Label, "Report")
            .Add(editor => editor.TelemetryName, "trail-report").Add(editor => editor.MentionTriggers, "@")
            .Add(editor => editor.MentionSearch, (_, _) => throw new IOException("Down")));
        var handed = handle.Invocations["setMentions"].Single().Arguments[1]!;

        await cut.InvokeAsync(() => ((DotNetObjectReference<MentionInputEvents>)handed).Value.OnQuery("@", "a", 1));

        AssertFailed("Trailblazor.TbRichTextEditor", "TbRichTextEditor trail-report failed to search.", "trail-report",
            "search");
    }

    [Fact]
    public void Grid_ShouldLogUnderItsLabelAndCountNothing_WhenItHasNoTelemetryName()
    {
        var cut = Render<TbDataGrid<string>>(parameters => parameters.Add(grid => grid.AriaLabel, "Trails")
            .Add(grid => grid.ChildContent, NameColumn())
            .Add(grid => grid.DataProvider, (_, _) => Task.FromException<GridResult<string>>(new IOException("Down"))));

        cut.WaitForAssertion(() => Assert.NotNull(cut.Find("[data-part=failed]")));
        Assert.Equal("TbDataGrid Trails failed to load.", Assert.Single(Warnings()).Message);
        Assert.Empty(_failed.GetMeasurementSnapshot());
    }

    [Fact]
    public async Task Autocomplete_ShouldLogAndCountNothing_WhenAnEarlierSearchFailsAfterTheUserTypedAgain()
    {
        var first = new TaskCompletionSource<IEnumerable<string>>();
        var calls = 0;
        var cut = Render<TbAutocomplete<string>>(parameters => parameters.Add(field => field.Label, "Trail")
            .Add(field => field.TelemetryName, "trail-search").Add(field => field.DebounceDelay, TimeSpan.Zero)
            .Add(field => field.Search, (_, _) => ++calls == 1 ? first.Task
                : Task.FromResult<IEnumerable<string>>(["Pine Hollow"])));
        var typed = cut.InvokeAsync(() => cut.Find("input").Input("pi"));
        await cut.InvokeAsync(() => cut.Find("input").Input("pin"));

        await cut.InvokeAsync(() => first.SetException(new IOException("Down")));
        await typed;

        Assert.Empty(Warnings());
        Assert.Empty(_failed.GetMeasurementSnapshot());
    }

    [Fact]
    public async Task ExportCsvAsync_ShouldThrowTheProvidersOwnException_WhenTheProviderFails()
    {
        var loads = 0;
        var cut = Render<TbDataGrid<string>>(parameters => parameters.Add(grid => grid.AriaLabel, "Trails")
            .Add(grid => grid.ChildContent, NameColumn())
            .Add(grid => grid.DataProvider, (_, _) => ++loads == 1
                ? Task.FromResult(new GridResult<string>(["Pine Hollow"], 1))
                : Task.FromException<GridResult<string>>(new IOException("Down"))));

        var thrown = await Assert.ThrowsAsync<IOException>(() => cut.InvokeAsync(() => cut.Instance.ExportCsvAsync()));

        Assert.Equal("Down", thrown.Message);
    }

    [Fact]
    public void Tree_ShouldLogAKeyByItsType_WhenTheItemIsItsOwnKey()
    {
        var place = new Place { Name = "North" };
        var cut = Render<TbTreeView<Place, Place>>(parameters => parameters.Add(tree => tree.AriaLabel, "Places")
            .Add(tree => tree.Items, [place]));
        // A record's equality and hash code follow its values, so the row no longer finds it.
        place.Name = "South";

        cut.Find("[role=treeitem]").Click();

        var log = Assert.Single(_logs.GetSnapshot(), log => log.Category == "Trailblazor.TbTreeView");
        Assert.Equal("The tree Places dropped an event for the key of type Place, which no row has anymore.",
            log.Message);
    }

    [Fact]
    public void Tree_ShouldLogAtDebug_WhenAClickedRowsKeyFindsNoRowNow()
    {
        var key = new Shifty();
        var cut = Render<TbTreeView<string, Shifty>>(parameters => parameters.Add(tree => tree.AriaLabel, "Places")
            .Add(tree => tree.Items, ["North"]).Add(tree => tree.KeySelector, _ => key));
        key.Hash = 1;

        cut.Find("[role=treeitem]").Click();

        var log = Assert.Single(_logs.GetSnapshot(), log => log.Category == "Trailblazor.TbTreeView");
        Assert.Equal(LogLevel.Debug, log.Level);
        Assert.Equal("The tree Places dropped an event for the key Shifty, which no row has anymore.", log.Message);
    }

    // A key whose hash code changes no longer finds its row.
    private sealed class Shifty
    {
        public int Hash { get; set; }

        public override int GetHashCode() => Hash;

        public override string ToString() => nameof(Shifty);
    }

    private static IReadOnlyList<string> Spoken(IRenderedComponent<AnnouncerHost> host) =>
        [.. host.FindAll("[role=status] p").Select(p => p.TextContent)];

    private sealed record Place
    {
        public string Name { get; set; } = "";
    }

    // A trip whose name throws once it breaks, as a column's field can while a file is built.
    private sealed class Trip
    {
        public bool Broken { get; set; }

        public string Name => Broken ? throw new IOException("Down") : "Pine Hollow";
    }

    private IEnumerable<FakeLogRecord> Warnings() => _logs.GetSnapshot().Where(log => log.Level >= LogLevel.Warning);

    private void AssertFailed(string category, string message, string telemetryName, string action)
    {
        var log = Assert.Single(Warnings());
        Assert.Equal((category, LogLevel.Warning, message, "Down"),
            (log.Category, log.Level, log.Message, log.Exception?.Message));
        var counted = Assert.Single(_failed.GetMeasurementSnapshot());
        Assert.Equal((1L, telemetryName, action), (counted.Value, counted.Tags["trailblazor.telemetry_name"],
            counted.Tags["trailblazor.action"]));
    }

    private static RenderFragment NameColumn() => builder =>
    {
        builder.OpenComponent<TbColumn<string>>(0);
        builder.AddAttribute(1, nameof(TbColumn<string>.Field), (Expression<Func<string, object?>>)(name => name));
        builder.AddAttribute(2, nameof(TbColumn<string>.Title), "Name");
        builder.CloseComponent();
    };
}
