using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Trailblazor.Testing;

namespace Trailblazor.Tests.Components;

// Rows edited in a dialog, in a grid whose EditMode is Form.
public class TbDataGridFormEditTests : BunitContext
{
    public TbDataGridFormEditTests()
    {
        this.AddTrailblazor();
        Render<DialogHost>();
    }

    public sealed class Hike
    {
        public int Id { get; set; }

        public string Name { get; set; } = "";

        public int LengthKm { get; set; }

        public bool Loop { get; set; }
    }

    private readonly List<Hike> _hikes = [new() { Id = 1, Name = "Pine Hollow", LengthKm = 6 }];

    private readonly List<GridRowEdit<Hike>> _edited = [];

    private readonly List<GridRowEdit<Hike>> _added = [];

    private string? _refusal;

    private IRenderedComponent<TbDataGrid<Hike>> RenderHikes(GridEditMode mode = GridEditMode.Form) =>
        Render<TbDataGrid<Hike>>(parameters => parameters.Add(grid => grid.Items, _hikes)
            .Add(grid => grid.AriaLabel, "Hikes").Add(grid => grid.KeySelector, hike => hike.Id)
            .Add(grid => grid.EditMode, mode)
            .Add(grid => grid.NewItem, () => new Hike { Id = 2 })
            .Add(grid => grid.OnRowEdit, (GridRowEdit<Hike> row) =>
            {
                row.Error = _refusal;
                _edited.Add(row);
            })
            .Add(grid => grid.OnRowAdd, (GridRowEdit<Hike> row) =>
            {
                _added.Add(row);
                _hikes.Add(row.Item);
            })
            .Add(grid => grid.ChildContent, (RenderFragment)(builder =>
            {
                builder.OpenComponent<TbColumn<Hike>>(0);
                builder.AddComponentParameter(1, nameof(TbColumn<Hike>.Field), Field(hike => hike.Name));
                builder.AddComponentParameter(2, nameof(TbColumn<Hike>.Editable), true);
                builder.AddComponentParameter(9, nameof(TbColumn<Hike>.Validate), (Func<Hike, object?, string?>)(
                    (_, value) => string.IsNullOrWhiteSpace(value as string) ? "Give the hike a name." : null));
                builder.CloseComponent();
                builder.OpenComponent<TbColumn<Hike>>(3);
                builder.AddComponentParameter(4, nameof(TbColumn<Hike>.Field), Field(hike => hike.LengthKm));
                builder.AddComponentParameter(5, nameof(TbColumn<Hike>.Editable), true);
                builder.CloseComponent();
                builder.OpenComponent<TbColumn<Hike>>(6);
                builder.AddComponentParameter(7, nameof(TbColumn<Hike>.Field), Field(hike => hike.Loop));
                builder.AddComponentParameter(8, nameof(TbColumn<Hike>.Editable), true);
                builder.CloseComponent();
            })));

    private static System.Linq.Expressions.Expression<Func<Hike, object?>> Field(
        System.Linq.Expressions.Expression<Func<Hike, object?>> field) => field;

    private static AngleSharp.Dom.IElement Form(IRenderedComponent<TbDataGrid<Hike>> cut) =>
        cut.Find("[data-part=form]");

    private static Task OpenAsync(IRenderedComponent<TbDataGrid<Hike>> cut) =>
        cut.Find("tr[data-key='1'] td[data-column=Name]").DoubleClickAsync(new());

    [Fact]
    public async Task Edit_ShouldOpenAFormWithALabeledFieldPerEditableColumn_WhenTheGridEditsByForm()
    {
        var cut = RenderHikes();

        await OpenAsync(cut);

        Assert.Equal("Edit Pine Hollow", cut.Find("dialog h2").TextContent.Trim());
        Assert.Equal(["Name", "LengthKm", "Loop"], Form(cut).QuerySelectorAll(".tb-data-grid__form-field")
            .Select(field => field.GetAttribute("data-column")));
        Assert.Equal("Pine Hollow", cut.Tb().TextField("Name").Text);
        Assert.Contains("Loop", Form(cut).QuerySelector("[data-column=Loop]")!.TextContent);
        Assert.Contains("Length km", Form(cut).QuerySelector("[data-column=LengthKm]")!.TextContent);
        Assert.Empty(Form(cut).QuerySelectorAll(".tb-visually-hidden"));
        Assert.Empty(cut.FindAll("table [data-part=editor]"));
    }

    [Fact]
    public async Task Save_ShouldKeepTheFormOpenWithTheFieldsError_WhenACellFailsItsCheck()
    {
        var cut = RenderHikes();
        await OpenAsync(cut);

        await cut.Tb().TextField("Name").EnterAsync("");
        await cut.Find("[data-part=form-save]").ClickAsync(new MouseEventArgs { Detail = 1 });

        Assert.Empty(_edited);
        Assert.Equal("Give the hike a name.", cut.Tb().TextField("Name").Error);
    }

    [Fact]
    public async Task RowEdit_ShouldNameItsCheckboxForItsColumn_WhenTheGridEditsByRow()
    {
        var cut = RenderHikes(GridEditMode.Row);

        await OpenAsync(cut);

        Assert.Equal("Loop", cut.Find("tr[data-key='1'] td[data-column=Loop] .tb-visually-hidden").TextContent.Trim());
    }

    [Fact]
    public async Task Save_ShouldRaiseOnRowEditWithEveryValueAndClose_WhenTheFormIsSaved()
    {
        var cut = RenderHikes();
        await OpenAsync(cut);

        await cut.Tb().TextField("Name").EnterAsync("Pine Loop");
        await cut.Find("[data-part=form-save]").ClickAsync(new MouseEventArgs { Detail = 1 });

        Assert.Equal(new Dictionary<string, object?> { ["Name"] = "Pine Loop", ["LengthKm"] = 6, ["Loop"] = false },
            Assert.Single(_edited).Values);
        Assert.Empty(cut.FindAll("dialog [data-part=form]"));
    }

    [Fact]
    public async Task Save_ShouldKeepTheFormOpenWithWhy_WhenTheAppTurnsItDown()
    {
        _refusal = "The server is down.";
        var cut = RenderHikes();
        await OpenAsync(cut);

        await cut.Find("[data-part=form] [data-part=editor]").TriggerEventAsync("ontbeditkey",
            new GridEditKeyEventArgs { Key = "Enter" });

        Assert.Equal("The server is down.", Form(cut).QuerySelector("[data-part=row-error]")!.TextContent.Trim());
    }

    [Fact]
    public async Task Cancel_ShouldCloseTheFormWithoutSaving_WhenPressed()
    {
        var cut = RenderHikes();
        await OpenAsync(cut);
        await cut.Tb().TextField("Name").EnterAsync("Pine Loop");

        await cut.Find("[data-part=form-cancel]").ClickAsync(new MouseEventArgs { Detail = 1 });

        Assert.Empty(_edited);
        Assert.Empty(cut.FindAll("dialog [data-part=form]"));
        Assert.Equal("Pine Hollow", cut.Find("tr[data-key='1'] td[data-column=Name]").TextContent.Trim());
    }

    [Fact]
    public async Task AddRow_ShouldOpenTheFormForTheNewRowAndRaiseOnRowAdd_WhenSaved()
    {
        var cut = RenderHikes();

        await cut.Tb().Grid("Hikes").AddRowAsync();
        Assert.Equal("Add row", cut.Find("dialog h2").TextContent.Trim());
        Assert.Empty(cut.FindAll("tr[data-new]"));
        await cut.Tb().TextField("Name").EnterAsync("Fern Gully");
        await cut.Find("[data-part=form-save]").ClickAsync(new MouseEventArgs { Detail = 1 });

        Assert.Equal("Fern Gully", Assert.Single(_added).Item.Name);
        Assert.Equal([["Pine Hollow", "6", "No"], ["Fern Gully", "0", "No"]], cut.Tb().Grid("Hikes").Rows);
    }
}
