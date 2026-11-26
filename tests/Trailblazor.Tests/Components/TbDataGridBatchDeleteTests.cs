using System.Diagnostics.Metrics;
using AngleSharp.Dom;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.Metrics.Testing;
using Trailblazor.Testing;

namespace Trailblazor.Tests.Components;

// Rows the user deletes in a grid that edits in batches, kept as changes until they're saved.
public class TbDataGridBatchDeleteTests : BunitContext
{
    public TbDataGridBatchDeleteTests()
    {
        this.AddTrailblazor();
    }

    public sealed class Hike(int id, string name)
    {
        public int Id { get; } = id;

        public string Name { get; set; } = name;
    }

    private List<Hike> _hikes = [new(1, "Pine Hollow"), new(2, "Eagle Ridge"), new(3, "Cedar Loop")];

    private readonly List<GridBatchEdit<Hike>> _saved = [];

    private string? _refusal;

    private IRenderedComponent<TbDataGrid<Hike>> RenderHikes(bool deletable = true, bool handler = true,
        string? telemetryName = null) =>
        Render<TbDataGrid<Hike>>(parameters =>
        {
            parameters.Add(grid => grid.Items, _hikes).Add(grid => grid.AriaLabel, "Hikes")
                .Add(grid => grid.KeySelector, hike => hike.Id).Add(grid => grid.EditMode, GridEditMode.Batch)
                .Add(grid => grid.Deletable, deletable).Add(grid => grid.TelemetryName, telemetryName)
                .AddChildContent<TbColumn<Hike>>(column => column.Add(name => name.Field, hike => hike.Name)
                    .Add(name => name.Editable, true));
            if (handler)
            {
                parameters.Add(grid => grid.OnBatchSave, (GridBatchEdit<Hike> batch) =>
                {
                    _saved.Add(batch);
                    if (_refusal is not null)
                    {
                        batch.Error = _refusal;
                        return;
                    }

                    _hikes.RemoveAll(hike => batch.Deleted.Contains(hike));
                    foreach (var change in batch.Changes)
                    {
                        change.Item.Name = (string)change.Value!;
                    }
                });
            }
        });

    private static IElement Row(IRenderedComponent<TbDataGrid<Hike>> cut, int id) =>
        cut.Find($"tbody tr[data-key='{id}']");

    private static string Count(IRenderedComponent<TbDataGrid<Hike>> cut) =>
        cut.Find("[data-part=change-count]").TextContent.Trim();

    private static async Task RenameAsync(IRenderedComponent<TbDataGrid<Hike>> cut, int id, string name)
    {
        await cut.Find($"tr[data-key='{id}'] td[data-column=Name]").DoubleClickAsync(new());
        await cut.Find("[data-part=editor] input").InputAsync(new ChangeEventArgs { Value = name });
        await cut.Find("[data-part=editor]").TriggerEventAsync("ontbeditkey",
            new GridEditKeyEventArgs { Key = "Enter" });
    }

    [Fact]
    public async Task Delete_ShouldMarkTheRowAndCountIt_WhenPressedInABatchGrid()
    {
        var host = Render<AnnouncerHost>();
        var cut = RenderHikes();

        await cut.Tb().Grid("Hikes").DeleteRowAsync("Eagle Ridge");

        var row = Row(cut, 2);
        Assert.True(row.HasAttribute("data-deleted"));
        Assert.Contains("tb-data-grid__row--deleted", row.ClassList);
        Assert.Equal("true", row.QuerySelector("[data-part=delete]")!.GetAttribute("aria-pressed"));
        Assert.False(row.QuerySelector("td[data-column=Name]")!.HasAttribute("data-editable"));
        Assert.Equal("Unsaved changes: 1", Count(cut));
        Assert.True(cut.Instance.HasChanges);
        Assert.Equal(3, cut.Tb().Grid("Hikes").RowCount);
        host.WaitForAssertion(() =>
            Assert.Contains("Eagle Ridge marked for deletion.", host.Find("[role=status]").TextContent));
    }

    [Fact]
    public async Task Delete_ShouldRestoreTheRow_WhenPressedAgain()
    {
        var host = Render<AnnouncerHost>();
        var cut = RenderHikes();
        var grid = cut.Tb().Grid("Hikes");
        await grid.DeleteRowAsync("Eagle Ridge");

        await grid.DeleteRowAsync("Eagle Ridge");

        Assert.False(Row(cut, 2).HasAttribute("data-deleted"));
        Assert.Equal("false", Row(cut, 2).QuerySelector("[data-part=delete]")!.GetAttribute("aria-pressed"));
        Assert.Equal("Unsaved changes: 0", Count(cut));
        Assert.False(cut.Instance.HasChanges);
        host.WaitForAssertion(() => Assert.Contains("Eagle Ridge restored.", host.Find("[role=status]").TextContent));
    }

    [Fact]
    public async Task SaveChanges_ShouldSendTheDeletedItemsWithoutTheirChanges_WhenRowsAreDeleted()
    {
        var cut = RenderHikes();
        var grid = cut.Tb().Grid("Hikes");
        await RenameAsync(cut, 2, "Eagle Peak");
        await RenameAsync(cut, 1, "Pine Loop");
        await grid.DeleteRowAsync("Eagle Peak");
        Assert.Equal("Unsaved changes: 2", Count(cut));

        await grid.SaveChangesAsync();

        var batch = Assert.Single(_saved);
        Assert.Equal([2], batch.Deleted.Select(hike => hike.Id));
        Assert.Equal([1], batch.Changes.Select(change => change.Item.Id));
        Assert.Equal([["Pine Loop"], ["Cedar Loop"]], grid.Rows);
        Assert.Empty(cut.FindAll("[data-deleted]"));
        Assert.False(cut.Instance.HasChanges);
    }

    [Fact]
    public async Task SaveChanges_ShouldKeepTheRowsMarked_WhenTheAppTurnsTheBatchDown()
    {
        _refusal = "The server is down.";
        var cut = RenderHikes();
        var grid = cut.Tb().Grid("Hikes");
        await grid.DeleteRowAsync("Eagle Ridge");

        await grid.SaveChangesAsync();

        Assert.True(Row(cut, 2).HasAttribute("data-deleted"));
        Assert.Equal("Unsaved changes: 1", Count(cut));
    }

    [Fact]
    public async Task DiscardChanges_ShouldRestoreEveryRowMarkedDeleted_WhenCalled()
    {
        var cut = RenderHikes();
        var grid = cut.Tb().Grid("Hikes");
        await grid.DeleteRowAsync("Eagle Ridge");
        await grid.DeleteRowAsync("Cedar Loop");

        await grid.DiscardChangesAsync();

        Assert.Empty(cut.FindAll("[data-deleted]"));
        Assert.False(cut.Instance.HasChanges);
    }

    [Fact]
    public async Task Delete_ShouldDescribeTheRowsCellsAsMarked_WhenTheRowIsMarkedDeleted()
    {
        var cut = RenderHikes();
        await RenameAsync(cut, 2, "Eagle Peak");

        await cut.Tb().Grid("Hikes").DeleteRowAsync("Eagle Peak");

        var describedBy = Row(cut, 2).QuerySelector("td[data-column=Name]")!.GetAttribute("aria-describedby");
        Assert.Equal("Marked for deletion", cut.Find($"#{describedBy}").TextContent);
    }

    [Fact]
    public async Task Delete_ShouldDropTheRowsOpenEdit_WhenTheRowIsMarkedDeleted()
    {
        var cut = RenderHikes();
        await cut.Find("tr[data-key='2'] td[data-column=Name]").DoubleClickAsync(new());

        // The cell being edited holds a field rather than the row's name, so its button is found by the row's key.
        await Row(cut, 2).QuerySelector("[data-part=delete]")!.ClickAsync(new MouseEventArgs { Detail = 1 });

        Assert.Empty(cut.FindAll("[data-part=editor]"));
        Assert.Equal("Unsaved changes: 1", Count(cut));
    }

    [Fact]
    public async Task PasteAsync_ShouldSkipTheRow_WhenItIsMarkedDeleted()
    {
        var cut = RenderHikes();
        var grid = cut.Tb().Grid("Hikes");
        await grid.DeleteRowAsync("Eagle Ridge");

        await grid.PasteAsync("Pine Hollow", "Name", "Pine Loop\nEagle Peak\nCedar Bend");

        Assert.Equal("Eagle Ridge", Row(cut, 2).QuerySelector("td[data-column=Name]")!.TextContent.Trim());
        Assert.False(Row(cut, 2).QuerySelector("td[data-column=Name]")!.HasAttribute("data-changed"));
        Assert.Equal("Unsaved changes: 3", Count(cut));
    }

    [Fact]
    public async Task SaveChanges_ShouldSendTheReloadedItem_WhenTheRowsReloadAfterADelete()
    {
        var cut = RenderHikes();
        await cut.Tb().Grid("Hikes").DeleteRowAsync("Eagle Ridge");
        _hikes = [new(1, "Pine Hollow"), new(2, "Eagle Ridge"), new(3, "Cedar Loop")];
        var reloaded = _hikes[1];
        cut.Render(parameters => parameters.Add(grid => grid.Items, _hikes));

        await cut.Tb().Grid("Hikes").SaveChangesAsync();

        Assert.Same(reloaded, Assert.Single(Assert.Single(_saved).Deleted));
    }

    [Theory]
    [InlineData(false, true)]
    [InlineData(true, false)]
    public void Render_ShouldOfferNoDelete_WhenGivenABatchGridWithoutDeletableOrOnBatchSave(bool deletable,
        bool handler)
    {
        var cut = RenderHikes(deletable, handler);

        Assert.Empty(cut.FindAll("[data-part=delete]"));
    }

    [Fact]
    public async Task TelemetryName_ShouldCountEachDeleteAndUndo_WhenGiven()
    {
        var meters = Services.GetRequiredService<IMeterFactory>();
        using var used = new MetricCollector<long>(meters, "Trailblazor", "trailblazor.control.used");
        var cut = RenderHikes(telemetryName: "hikes");
        var grid = cut.Tb().Grid("Hikes");

        await grid.DeleteRowAsync("Eagle Ridge");
        await grid.DeleteRowAsync("Eagle Ridge");

        Assert.Equal(["delete", "undo_delete"], used.GetMeasurementSnapshot()
            .Where(measurement => Equals(measurement.Tags["trailblazor.telemetry_name"], "hikes"))
            .Select(measurement => measurement.Tags["trailblazor.action"]));
    }
}
