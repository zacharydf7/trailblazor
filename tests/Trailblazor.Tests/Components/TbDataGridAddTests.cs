using System.Diagnostics.Metrics;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.Metrics.Testing;
using Trailblazor.Testing;

namespace Trailblazor.Tests.Components;

// Rows the user adds with the Add row button, in a grid that edits by row.
public class TbDataGridAddTests : BunitContext
{
    public TbDataGridAddTests()
    {
        this.AddTrailblazor();
    }

    public sealed class Hike
    {
        public int Id { get; set; }

        public string Name { get; set; } = "";

        public int LengthKm { get; set; }
    }

    public sealed record Trail(int Id, string Name);

    private readonly List<Hike> _hikes = [new() { Id = 1, Name = "Pine Hollow", LengthKm = 6 }];

    private readonly List<GridRowEdit<Hike>> _added = [];

    private readonly List<GridRowEdit<Hike>> _edited = [];

    private IRenderedComponent<TbDataGrid<Hike>> RenderHikes(Func<GridRowEdit<Hike>, Task>? add = null,
        GridEditMode mode = GridEditMode.Row, string? telemetryName = null) =>
        Render<TbDataGrid<Hike>>(parameters => parameters.Add(grid => grid.Items, _hikes)
            .Add(grid => grid.AriaLabel, "Hikes").Add(grid => grid.KeySelector, hike => hike.Id)
            .Add(grid => grid.EditMode, mode).Add(grid => grid.TelemetryName, telemetryName)
            .Add(grid => grid.NewItem, () => new Hike { Id = _hikes.Count + 1, LengthKm = 5 })
            .Add(grid => grid.OnRowEdit, (GridRowEdit<Hike> row) => _edited.Add(row))
            .Add(grid => grid.OnBatchSave, (GridBatchEdit<Hike> _) => { })
            .Add(grid => grid.OnRowAdd, add ?? (row =>
            {
                _added.Add(row);
                _hikes.Add(row.Item);
                return Task.CompletedTask;
            }))
            .Add(grid => grid.ChildContent, (RenderFragment)(builder =>
            {
                builder.OpenComponent<TbColumn<Hike>>(0);
                builder.AddComponentParameter(1, nameof(TbColumn<Hike>.Field), Field(hike => hike.Id));
                builder.CloseComponent();
                builder.OpenComponent<TbColumn<Hike>>(2);
                builder.AddComponentParameter(3, nameof(TbColumn<Hike>.Field), Field(hike => hike.Name));
                builder.AddComponentParameter(4, nameof(TbColumn<Hike>.Editable), true);
                builder.AddComponentParameter(5, nameof(TbColumn<Hike>.Validate), (Func<Hike, object?, string?>)(
                    (_, value) => string.IsNullOrWhiteSpace(value as string) ? "Give the hike a name." : null));
                builder.CloseComponent();
                builder.OpenComponent<TbColumn<Hike>>(6);
                builder.AddComponentParameter(7, nameof(TbColumn<Hike>.Field), Field(hike => hike.LengthKm));
                builder.AddComponentParameter(8, nameof(TbColumn<Hike>.Editable), true);
                builder.CloseComponent();
            })));

    private static System.Linq.Expressions.Expression<Func<Hike, object?>> Field(
        System.Linq.Expressions.Expression<Func<Hike, object?>> field) => field;

    private static Task NameAsync(IRenderedComponent<TbDataGrid<Hike>> cut, string name) =>
        cut.Find("tr[data-new] td[data-column=Name] input").InputAsync(new ChangeEventArgs { Value = name });

    private static Task EditorKeyAsync(IRenderedComponent<TbDataGrid<Hike>> cut, string key) =>
        cut.Find("tr[data-new] [data-part=editor]").TriggerEventAsync("ontbeditkey",
            new GridEditKeyEventArgs { Key = key });

    [Fact]
    public async Task AddRow_ShouldOpenANewRowAtTheTopWithAFieldPerEditableCell_WhenPressed()
    {
        var cut = RenderHikes();
        var grid = cut.Tb().Grid("Hikes");

        await grid.AddRowAsync();

        Assert.True(cut.Find("tbody > tr").HasAttribute("data-new"));
        Assert.Equal(2, grid.NewRow.QuerySelectorAll("[data-part=editor]").Length);
        Assert.Equal("5", grid.NewRow.QuerySelector("td[data-column=LengthKm] input")!.GetAttribute("value"));
        Assert.Equal("Save new row", grid.NewRow.QuerySelector("[data-part=row-actions] button")!
            .GetAttribute("aria-label"));
        Assert.Equal(1, grid.RowCount);
    }

    [Fact]
    public async Task AddRow_ShouldRaiseOnRowAddWithTheNewItemAndSaySo_WhenTheNewRowIsSaved()
    {
        var meters = Services.GetRequiredService<IMeterFactory>();
        using var used = new MetricCollector<long>(meters, "Trailblazor", "trailblazor.control.used");
        var host = Render<AnnouncerHost>();
        var cut = RenderHikes(telemetryName: "hikes");
        var grid = cut.Tb().Grid("Hikes");
        await grid.AddRowAsync();

        await NameAsync(cut, "Fern Gully");
        await EditorKeyAsync(cut, "Enter");

        var added = Assert.Single(_added);
        Assert.Equal("Fern Gully", added.Item.Name);
        Assert.Equal(new Dictionary<string, object?> { ["Name"] = "Fern Gully", ["LengthKm"] = 5 }, added.Values);
        Assert.Equal([["1", "Pine Hollow", "6"], ["2", "Fern Gully", "5"]], grid.Rows);
        Assert.Empty(cut.FindAll("tr[data-new]"));
        Assert.Empty(_edited);
        host.WaitForAssertion(() => Assert.Contains("Row added.", host.Find("[role=status]").TextContent));
        Assert.Equal(["add"], used.GetMeasurementSnapshot()
            .Where(measurement => Equals(measurement.Tags["trailblazor.telemetry_name"], "hikes"))
            .Select(measurement => measurement.Tags["trailblazor.action"]));
    }

    [Fact]
    public async Task AddRow_ShouldKeepTheNewRowOpenWithWhy_WhenTheAppTurnsItDown()
    {
        var cut = RenderHikes(row =>
        {
            row.Error = "A hike by that name exists.";
            return Task.CompletedTask;
        });
        var grid = cut.Tb().Grid("Hikes");
        await grid.AddRowAsync();

        await NameAsync(cut, "Pine Hollow");
        await EditorKeyAsync(cut, "Enter");

        Assert.Equal("A hike by that name exists.", grid.NewRow.QuerySelector("[data-part=row-error]")!.TextContent);
        Assert.Equal("Pine Hollow", grid.NewRow.QuerySelector("td[data-column=Name] input")!.GetAttribute("value"));
        Assert.Equal(1, grid.RowCount);
    }

    [Fact]
    public async Task AddRow_ShouldNotAskTheApp_WhenACellFailsItsCheck()
    {
        var cut = RenderHikes();
        var grid = cut.Tb().Grid("Hikes");
        await grid.AddRowAsync();

        await EditorKeyAsync(cut, "Enter");

        Assert.Empty(_added);
        Assert.Contains("Give the hike a name.", grid.NewRow.QuerySelector("td[data-column=Name]")!.TextContent);
    }

    [Fact]
    public async Task AddRow_ShouldDiscardTheNewRow_WhenItIsCanceled()
    {
        var cut = RenderHikes();
        var grid = cut.Tb().Grid("Hikes");
        await grid.AddRowAsync();
        await NameAsync(cut, "Fern Gully");

        await EditorKeyAsync(cut, "Escape");

        Assert.Empty(cut.FindAll("tr[data-new]"));
        Assert.Empty(_added);
        Assert.Equal([["1", "Pine Hollow", "6"]], grid.Rows);
    }

    [Fact]
    public async Task AddRow_ShouldSaveTheRowBeingEditedFirst_WhenARowIsBeingEdited()
    {
        var cut = RenderHikes();
        var grid = cut.Tb().Grid("Hikes");
        await grid.Cell("Pine Hollow", "Name").DoubleClickAsync(new());
        await cut.Find("td[data-column=Name] input").InputAsync(new ChangeEventArgs { Value = "Pine Loop" });

        await grid.AddRowAsync();

        Assert.Equal("Pine Loop", Assert.Single(_edited).Values["Name"]);
        Assert.Equal(2, grid.NewRow.QuerySelectorAll("[data-part=editor]").Length);
    }

    [Fact]
    public async Task AddRow_ShouldTakeThePlaceOfTheEmptyRow_WhenTheGridHasNoRows()
    {
        _hikes.Clear();
        var cut = RenderHikes();

        await cut.Tb().Grid("Hikes").AddRowAsync();

        Assert.Empty(cut.FindAll("[data-part=empty]"));
        Assert.Single(cut.FindAll("tbody > tr"));
    }

    [Theory]
    [InlineData(GridEditMode.Cell)]
    [InlineData(GridEditMode.Batch)]
    public async Task AddRowAsync_ShouldThrow_WhenGivenAGridThatDoesntEditByRow(GridEditMode mode)
    {
        var cut = RenderHikes(mode: mode);

        await Assert.ThrowsAsync<InvalidOperationException>(() => cut.Tb().Grid("Hikes").AddRowAsync());
        Assert.Empty(cut.FindAll("[data-part=add-row]"));
    }

    [Fact]
    public async Task AddRow_ShouldDropTheNewRow_WhenFocusLeavesItUntouched()
    {
        var cut = RenderHikes();
        await cut.Tb().Grid("Hikes").AddRowAsync();

        await cut.Find("tr[data-new]").TriggerEventAsync("ontbfocusleave",
            new FieldFocusEventArgs { CrossesField = true, CrossesEdit = true });

        Assert.Empty(cut.FindAll("tr[data-new]"));
        Assert.Empty(_added);
    }

    [Fact]
    public async Task AddRow_ShouldSaveTheNewRow_WhenFocusLeavesItFilledIn()
    {
        var cut = RenderHikes();
        await cut.Tb().Grid("Hikes").AddRowAsync();
        await NameAsync(cut, "Fern Gully");

        await cut.Find("tr[data-new]").TriggerEventAsync("ontbfocusleave",
            new FieldFocusEventArgs { CrossesField = true, CrossesEdit = true });

        Assert.Equal("Fern Gully", Assert.Single(_added).Item.Name);
    }

    [Theory]
    [InlineData("virtualized")]
    [InlineData("no NewItem")]
    [InlineData("no OnRowAdd")]
    [InlineData("no editable column")]
    public void Render_ShouldOfferNoAddRow_WhenGivenAGridThatCantAdd(string setup)
    {
        var cut = RenderTrails(setup);

        Assert.Empty(cut.FindAll("[data-part=add-row]"));
    }

    [Fact]
    public void Render_ShouldTakeTheGridRole_WhenTheGridOnlyAddsRows()
    {
        var cut = RenderTrails();

        Assert.Equal("grid", cut.Find("table").GetAttribute("role"));
    }

    private IRenderedComponent<TbDataGrid<Trail>> RenderTrails(string? setup = null,
        Action<GridRowEdit<Trail>>? add = null) =>
        Render<TbDataGrid<Trail>>(parameters =>
        {
            parameters.Add(grid => grid.Items, new List<Trail>()).Add(grid => grid.AriaLabel, "Trails")
                .Add(grid => grid.KeySelector, trail => trail.Id).Add(grid => grid.EditMode, GridEditMode.Row)
                .Add(grid => grid.Virtualize, setup == "virtualized")
                .Add(grid => grid.Height, setup == "virtualized" ? "20rem" : null)
                .AddChildContent<TbColumn<Trail>>(column => column.Add(name => name.Field, trail => trail.Name)
                    .Add(name => name.Editable, setup != "no editable column"));
            if (setup != "no NewItem")
            {
                parameters.Add(grid => grid.NewItem, () => new Trail(7, ""));
            }

            if (setup != "no OnRowAdd")
            {
                parameters.Add(grid => grid.OnRowAdd, add ?? (_ => { }));
            }
        });

    [Fact]
    public async Task AddRow_ShouldPassTheValuesAndLeaveTheItem_WhenTheItemsFieldsCantBeSet()
    {
        GridRowEdit<Trail>? added = null;
        var cut = RenderTrails(add: row => added = row);
        await cut.Tb().Grid("Trails").AddRowAsync();

        await cut.Find("tr[data-new] input").InputAsync(new ChangeEventArgs { Value = "Moss Bend" });
        await cut.Find("tr[data-new] [data-part=editor]").TriggerEventAsync("ontbeditkey",
            new GridEditKeyEventArgs { Key = "Enter" });

        Assert.Equal("Moss Bend", added!.Values["Name"]);
        Assert.Equal(new Trail(7, ""), added.Item);
    }
}
