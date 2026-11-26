using System.Diagnostics.Metrics;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.Metrics.Testing;
using Trailblazor.Testing;

namespace Trailblazor.Tests.Components;

// Rows the user adds to a grid that edits in batches, kept as changes until they're saved.
public class TbDataGridBatchAddTests : BunitContext
{
    public TbDataGridBatchAddTests()
    {
        this.AddTrailblazor();
    }

    public sealed class Hike
    {
        public int Id { get; set; }

        public string Name { get; set; } = "";
    }

    private readonly List<Hike> _hikes =
        [new() { Id = 1, Name = "Pine Hollow" }, new() { Id = 2, Name = "Eagle Ridge" }];

    private readonly List<GridBatchEdit<Hike>> _saved = [];

    private string? _refusal;

    private int _nextId = 10;

    // When set, the save handler waits on it, as one sending the changes to a server would.
    private TaskCompletionSource? _server;

    private IRenderedComponent<TbDataGrid<Hike>> RenderHikes(bool handler = true, string? telemetryName = null) =>
        Render<TbDataGrid<Hike>>(parameters =>
        {
            parameters.Add(grid => grid.Items, _hikes).Add(grid => grid.AriaLabel, "Hikes")
                .Add(grid => grid.KeySelector, hike => hike.Id).Add(grid => grid.EditMode, GridEditMode.Batch)
                .Add(grid => grid.Deletable, true).Add(grid => grid.TelemetryName, telemetryName)
                .Add(grid => grid.NewItem, () => new Hike { Id = _nextId++ })
                .AddChildContent<TbColumn<Hike>>(column => column.Add(name => name.Field, hike => hike.Name)
                    .Add(name => name.Editable, true).Add(name => name.Sortable, true)
                    .Add(name => name.Validate, (Hike _, object? value) =>
                        string.IsNullOrWhiteSpace(value as string) ? "Give the hike a name." : null));
            if (handler)
            {
                parameters.Add(grid => grid.OnBatchSave, async (GridBatchEdit<Hike> batch) =>
                {
                    _saved.Add(batch);
                    if (_server is not null)
                    {
                        await _server.Task;
                    }

                    if (_refusal is not null)
                    {
                        batch.Error = _refusal;
                        return;
                    }

                    _hikes.AddRange(batch.Added);
                });
            }
        });

    private static string Count(IRenderedComponent<TbDataGrid<Hike>> cut) =>
        cut.Find("[data-part=change-count]").TextContent.Trim();

    private static async Task TypeNameAsync(IRenderedComponent<TbDataGrid<Hike>> cut, string name)
    {
        await cut.Find("tr[data-new] td[data-column=Name] input").InputAsync(new ChangeEventArgs { Value = name });
        await cut.Find("[data-part=editor]").TriggerEventAsync("ontbeditkey",
            new GridEditKeyEventArgs { Key = "Enter" });
    }

    [Fact]
    public async Task AddRow_ShouldAddARowAtTheTopWithItsFirstCellOpen_WhenPressedInABatchGrid()
    {
        var meters = Services.GetRequiredService<IMeterFactory>();
        using var used = new MetricCollector<long>(meters, "Trailblazor", "trailblazor.control.used");
        var cut = RenderHikes(telemetryName: "hikes");
        var grid = cut.Tb().Grid("Hikes");

        await grid.AddRowAsync();

        Assert.True(cut.Find("tbody > tr").HasAttribute("data-new"));
        Assert.NotNull(grid.NewRow.QuerySelector("td[data-column=Name] [data-part=editor]"));
        Assert.Null(grid.NewRow.QuerySelector("[data-part=delete]")!.GetAttribute("aria-pressed"));
        Assert.Equal("Unsaved changes: 1", Count(cut));
        Assert.True(cut.Instance.HasChanges);
        Assert.Equal(2, grid.RowCount);
        Assert.Contains("add", used.GetMeasurementSnapshot()
            .Where(measurement => Equals(measurement.Tags["trailblazor.telemetry_name"], "hikes"))
            .Select(measurement => measurement.Tags["trailblazor.action"]));
    }

    [Fact]
    public async Task AddRow_ShouldPutTheNewestRowFirstAndCountEachOnce_WhenPressedTwice()
    {
        var cut = RenderHikes();
        var grid = cut.Tb().Grid("Hikes");
        await grid.AddRowAsync();
        await TypeNameAsync(cut, "Fern Gully");

        await grid.AddRowAsync();
        await TypeNameAsync(cut, "Moss Bend");

        Assert.Equal(["Moss Bend", "Fern Gully"], cut.FindAll("tr[data-new] td[data-column=Name]")
            .Select(cell => cell.TextContent.Trim()));
        Assert.Equal("Unsaved changes: 2", Count(cut));
    }

    [Fact]
    public async Task SaveChanges_ShouldSendTheAddedItemsWithTheirValues_WhenRowsAreAdded()
    {
        var cut = RenderHikes();
        var grid = cut.Tb().Grid("Hikes");
        await grid.AddRowAsync();
        await TypeNameAsync(cut, "Fern Gully");

        await grid.SaveChangesAsync();

        var batch = Assert.Single(_saved);
        var added = Assert.Single(batch.Added);
        Assert.Equal("Fern Gully", added.Name);
        Assert.Same(added, Assert.Single(batch.Changes).Item);
        Assert.Equal([["Pine Hollow"], ["Eagle Ridge"], ["Fern Gully"]], grid.Rows);
        Assert.Empty(cut.FindAll("tr[data-new]"));
        Assert.False(cut.Instance.HasChanges);
    }

    [Fact]
    public async Task SaveChanges_ShouldKeepTheAddedRows_WhenTheAppTurnsTheBatchDown()
    {
        _refusal = "The server is down.";
        var cut = RenderHikes();
        var grid = cut.Tb().Grid("Hikes");
        await grid.AddRowAsync();
        await TypeNameAsync(cut, "Fern Gully");

        await grid.SaveChangesAsync();

        Assert.Equal("Fern Gully", grid.NewRow.QuerySelector("td[data-column=Name]")!.TextContent.Trim());
        Assert.Equal("Unsaved changes: 1", Count(cut));
    }

    [Fact]
    public async Task Delete_ShouldRemoveTheAddedRowAndWhatWasTyped_WhenPressedOnIt()
    {
        var host = Render<AnnouncerHost>();
        var cut = RenderHikes();
        var grid = cut.Tb().Grid("Hikes");
        await grid.AddRowAsync();
        await TypeNameAsync(cut, "Fern Gully");

        Assert.Equal("Delete Fern Gully", grid.NewRow.QuerySelector("[data-part=delete]")!.GetAttribute("aria-label"));

        await grid.DeleteRowAsync("Fern Gully");

        Assert.Empty(cut.FindAll("tr[data-new]"));
        Assert.Equal("Unsaved changes: 0", Count(cut));
        Assert.False(cut.Instance.HasChanges);
        host.WaitForAssertion(() => Assert.Contains("New row removed.", host.Find("[role=status]").TextContent));
    }

    [Fact]
    public async Task SaveChanges_ShouldAddEveryRow_WhenTwoRowsAreAdded()
    {
        var cut = RenderHikes();
        var grid = cut.Tb().Grid("Hikes");
        await grid.AddRowAsync();
        await TypeNameAsync(cut, "Fern Gully");
        await grid.AddRowAsync();
        await TypeNameAsync(cut, "Moss Bend");

        await grid.SaveChangesAsync();

        Assert.Equal([["Pine Hollow"], ["Eagle Ridge"], ["Fern Gully"], ["Moss Bend"]], grid.Rows);
    }

    [Fact]
    public async Task SaveChanges_ShouldTurnTheBatchDown_WhenAnAddedRowsUntouchedCellFailsItsCheck()
    {
        var cut = RenderHikes();
        var grid = cut.Tb().Grid("Hikes");
        await grid.AddRowAsync();
        await cut.Find("[data-part=editor]").TriggerEventAsync("ontbeditkey",
            new GridEditKeyEventArgs { Key = "Escape" });

        await grid.SaveChangesAsync();

        Assert.Empty(_saved);
        Assert.Equal("Give the hike a name.", cut.Find("[data-part=batch-error]").TextContent.Trim());
    }

    [Fact]
    public async Task Arrange_ShouldKeepTheAddedRow_WhenTheRowsAreSortedOrRefreshedWhileItWaits()
    {
        var cut = RenderHikes();
        var grid = cut.Tb().Grid("Hikes");
        await grid.AddRowAsync();
        await TypeNameAsync(cut, "Fern Gully");

        await grid.SortAsync("Name");
        await cut.InvokeAsync(() => cut.Instance.RefreshAsync());

        Assert.Equal("Fern Gully", grid.NewRow.QuerySelector("td[data-column=Name]")!.TextContent.Trim());
        Assert.Equal([["Eagle Ridge"], ["Pine Hollow"]], grid.Rows);
    }

    [Fact]
    public async Task SaveChanges_ShouldKeepAnAddedRowsNewerEditAndTheRow_WhenUsedWhileItsSaveIsOnItsWay()
    {
        _server = new();
        var cut = RenderHikes();
        var grid = cut.Tb().Grid("Hikes");
        await grid.AddRowAsync();
        await TypeNameAsync(cut, "Fern");

        var saving = grid.SaveChangesAsync();
        await cut.Find("tr[data-new] td[data-column=Name]").DoubleClickAsync(new());
        await TypeNameAsync(cut, "Fern Gully");
        await grid.DeleteRowAsync("Fern Gully");
        _server.SetResult();
        await saving;

        Assert.Equal("Fern", Assert.Single(Assert.Single(_saved).Changes).Value);
        Assert.Equal("Fern Gully", grid.Cell("Fern Gully", "Name").TextContent.Trim());
        Assert.True(grid.Cell("Fern Gully", "Name").HasAttribute("data-changed"));
        Assert.Equal([["Pine Hollow"], ["Eagle Ridge"], ["Fern Gully"]], grid.Rows);
        Assert.Equal("Unsaved changes: 1", Count(cut));
    }

    [Fact]
    public async Task DiscardChanges_ShouldRemoveEveryAddedRow_WhenCalled()
    {
        var cut = RenderHikes();
        var grid = cut.Tb().Grid("Hikes");
        await grid.AddRowAsync();
        await TypeNameAsync(cut, "Fern Gully");

        await grid.DiscardChangesAsync();

        Assert.Empty(cut.FindAll("tr[data-new]"));
        Assert.False(cut.Instance.HasChanges);
    }

    [Fact]
    public async Task AddRowAsync_ShouldThrow_WhenGivenABatchGridWithoutOnBatchSave()
    {
        var cut = RenderHikes(handler: false);

        await Assert.ThrowsAsync<InvalidOperationException>(() => cut.Tb().Grid("Hikes").AddRowAsync());
    }
}
