using Bunit;
using Trailblazor.Testing;

namespace Trailblazor.Tests.Components;

// Rows the user deletes with their Delete button.
public class TbDataGridDeleteTests : BunitContext
{
    public TbDataGridDeleteTests()
    {
        this.AddTrailblazor();
    }

    public sealed class Hike(int id, string name)
    {
        public int Id { get; } = id;

        public string Name { get; set; } = name;
    }

    private readonly List<Hike> _hikes = [new(1, "Pine Hollow"), new(2, "Eagle Ridge"), new(3, "Cedar Loop")];

    private readonly List<GridRowEdit<Hike>> _edited = [];

    private IRenderedComponent<TbDataGrid<Hike>> RenderHikes(Func<GridRowDelete<Hike>, Task>? delete = null,
        GridEditMode mode = GridEditMode.Cell) =>
        Render<TbDataGrid<Hike>>(parameters => parameters.Add(grid => grid.Items, _hikes)
            .Add(grid => grid.AriaLabel, "Hikes").Add(grid => grid.KeySelector, hike => hike.Id)
            .Add(grid => grid.EditMode, mode).Add(grid => grid.OnBatchSave, (GridBatchEdit<Hike> _) => { })
            .Add(grid => grid.OnRowEdit, (GridRowEdit<Hike> row) => _edited.Add(row))
            .Add(grid => grid.OnRowDelete, delete ?? (row =>
            {
                _hikes.Remove(row.Item);
                return Task.CompletedTask;
            }))
            .AddChildContent<TbColumn<Hike>>(column => column.Add(name => name.Field, hike => hike.Name)
                .Add(name => name.Editable, true)));

    private static List<string> Names(IRenderedComponent<TbDataGrid<Hike>> cut) =>
        [.. cut.FindAll("tbody tr[data-part=row] td[data-column=Name]").Select(cell => cell.TextContent.Trim())];

    [Fact]
    public void Render_ShouldStartEachRowWithANamedDeleteButton_WhenOnRowDeleteIsSet()
    {
        var cut = RenderHikes();

        Assert.Equal(["Delete Pine Hollow", "Delete Eagle Ridge", "Delete Cedar Loop"],
            cut.FindAll("[data-part=delete]").Select(button => button.GetAttribute("aria-label")));
        Assert.Equal("Delete", cut.Find("thead th.tb-data-grid__delete").TextContent.Trim());
    }

    [Fact]
    public async Task Delete_ShouldRemoveTheRowAndSaySo_WhenTheAppDeletesTheItem()
    {
        var host = Render<AnnouncerHost>();
        var cut = RenderHikes();

        await cut.Tb().Grid("Hikes").DeleteRowAsync("Eagle Ridge");

        Assert.Equal(["Pine Hollow", "Cedar Loop"], Names(cut));
        host.WaitForAssertion(() => Assert.Contains("Eagle Ridge deleted.", host.Find("[role=status]").TextContent));
    }

    [Fact]
    public async Task Delete_ShouldKeepTheRowAndShowWhy_WhenTheAppRefusesIt()
    {
        var host = Render<AnnouncerHost>();
        var cut = RenderHikes(row =>
        {
            row.Error = "Eagle Ridge has trips booked.";
            return Task.CompletedTask;
        });
        var button = () => cut.Tb().Grid("Hikes").Row("Eagle Ridge").QuerySelector("[data-part=delete]")!;

        await button().ClickAsync(new());

        Assert.Equal(["Pine Hollow", "Eagle Ridge", "Cedar Loop"], Names(cut));
        var error = cut.Find("[data-part=delete-error]");
        Assert.Equal("Eagle Ridge has trips booked.", error.TextContent.Trim());
        Assert.Equal(error.Id, button().GetAttribute("aria-describedby"));
        host.WaitForAssertion(() => Assert.Contains("Eagle Ridge has trips booked.",
            host.Find("[role=alert]").TextContent));
    }

    [Fact]
    public async Task Delete_ShouldAskTheAppOnce_WhenPressedAgainWhileItsDeleteRuns()
    {
        var server = new TaskCompletionSource();
        var asked = 0;
        var cut = RenderHikes(async row =>
        {
            asked++;
            await server.Task;
            _hikes.Remove(row.Item);
        });
        var button = cut.Tb().Grid("Hikes").Row("Eagle Ridge").QuerySelector("[data-part=delete]")!;

        var first = button.ClickAsync(new());
        await button.ClickAsync(new());
        server.SetResult();
        await first;

        Assert.Equal(1, asked);
        Assert.Equal(["Pine Hollow", "Cedar Loop"], Names(cut));
    }

    [Fact]
    public async Task Delete_ShouldSayNothing_WhenTheAppKeepsTheRowWithoutAnError()
    {
        var host = Render<AnnouncerHost>();
        var cut = RenderHikes(_ => Task.CompletedTask);

        await cut.Tb().Grid("Hikes").DeleteRowAsync("Eagle Ridge");

        Assert.Equal(["Pine Hollow", "Eagle Ridge", "Cedar Loop"], Names(cut));
        Assert.DoesNotContain("deleted", host.Find("[role=status]").TextContent);
    }

    [Fact]
    public async Task Delete_ShouldDropTheRowsEdit_WhenTheRowBeingEditedIsDeleted()
    {
        var cut = RenderHikes(mode: GridEditMode.Row);
        await cut.Find("tr[data-key='2'] td[data-column=Name]").DoubleClickAsync(new());

        await cut.Find("tr[data-key='2'] [data-part=delete]").ClickAsync(new());
        await cut.Find("tr[data-key='1'] td[data-column=Name]").DoubleClickAsync(new());

        Assert.Empty(_edited);
        Assert.Equal(2, cut.FindAll("tbody tr[data-part=row]").Count);
    }

    [Fact]
    public void Render_ShouldOfferNoDelete_WhenTheGridEditsInBatches()
    {
        var cut = RenderHikes(mode: GridEditMode.Batch);

        Assert.Empty(cut.FindAll("[data-part=delete]"));
        Assert.Empty(cut.FindAll(".tb-data-grid__delete"));
    }
}
