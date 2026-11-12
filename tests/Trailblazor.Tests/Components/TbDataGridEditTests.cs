using System.ComponentModel.DataAnnotations;
using System.Linq.Expressions;
using AngleSharp.Dom;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;
using Trailblazor.Testing;

namespace Trailblazor.Tests.Components;

public class TbDataGridEditTests : BunitContext
{
    public TbDataGridEditTests()
    {
        this.AddTrailblazor();
    }

    public enum Difficulty
    {
        Easy,
        Hard,
    }

    // A class whose values the grid can set in place.
    public sealed class Hike(int id, string name, int lengthKm, Difficulty difficulty, bool loop)
    {
        public int Id { get; } = id;

        public string Name { get; set; } = name;

        [Range(1, 100)]
        public int LengthKm { get; set; } = lengthKm;

        public Difficulty Difficulty { get; set; } = difficulty;

        public bool Loop { get; set; } = loop;
    }

    public sealed record Camp(int Id, string Name);

    private readonly List<Hike> _hikes =
    [
        new(1, "Pine Hollow", 6, Difficulty.Easy, true),
        new(2, "Eagle Ridge", 14, Difficulty.Hard, false),
    ];

    [Fact]
    public async Task DoubleClick_ShouldOpenAFieldHoldingTheValue_WhenTheCellIsEditable()
    {
        var cut = RenderHikes();

        await Cell(cut, 1, "Name").DoubleClickAsync(new());

        Assert.Equal("Pine Hollow", cut.Find("[data-part=editor] input").GetAttribute("value"));
        Assert.Equal("grid", cut.Find("table").GetAttribute("role"));
    }

    [Fact]
    public async Task Enter_ShouldSetTheValueOnTheItem_WhenTheGridHasNoEditHandler()
    {
        var cut = RenderHikes();
        await Cell(cut, 1, "Name").TriggerEventAsync("ontbeditstart", EventArgs.Empty);

        await cut.Find("[data-part=editor] input").InputAsync(new ChangeEventArgs { Value = "Pine Hollow loop" });
        await EditorKeyAsync(cut, "Enter");

        Assert.Equal("Pine Hollow loop", _hikes[0].Name);
        Assert.Empty(cut.FindAll("[data-part=editor]"));
        Assert.Equal("Pine Hollow loop", Cell(cut, 1, "Name").TextContent.Trim());
    }

    [Fact]
    public async Task EditStart_ShouldSaveTheOpenEditAndOpenTheCell_WhenRaisedOnAnotherCell()
    {
        var cut = RenderHikes();
        await Cell(cut, 1, "Name").DoubleClickAsync(new());
        await cut.Find("[data-part=editor] input").InputAsync(new ChangeEventArgs { Value = "Pine Hollow loop" });

        await Cell(cut, 1, "LengthKm").TriggerEventAsync("ontbeditstart", EventArgs.Empty);

        Assert.Equal("Pine Hollow loop", _hikes[0].Name);
        Assert.Empty(Cell(cut, 1, "Name").QuerySelectorAll("[data-part=editor]"));
        Assert.NotEmpty(Cell(cut, 1, "LengthKm").QuerySelectorAll("[data-part=editor]"));
    }

    [Fact]
    public async Task EditStart_ShouldOpenNoEdit_WhenTheSaveTookItsRowAway()
    {
        var saves = 0;
        var cut = RenderHikes(parameters => parameters.Add(grid => grid.OnCellEdit, (GridCellEdit<Hike> edit) =>
        {
            saves++;
            _hikes.RemoveAll(hike => hike.Id == edit.Item.Id);
        }));
        var length = Cell(cut, 1, "LengthKm");
        await Cell(cut, 1, "Name").DoubleClickAsync(new());

        await length.TriggerEventAsync("ontbeditstart", EventArgs.Empty);

        Assert.Empty(cut.FindAll("[data-part=editor]"));
        await Cell(cut, 2, "Name").DoubleClickAsync(new());
        Assert.Single(cut.FindAll("[data-part=editor]"));
        Assert.Equal(1, saves);
    }

    [Fact]
    public async Task EditStart_ShouldLeaveTheEditOpen_WhenItsValueFailsItsCheck()
    {
        var cut = RenderHikes(validate: (_, _) => "Not today.");
        await Cell(cut, 1, "LengthKm").DoubleClickAsync(new());

        await Cell(cut, 2, "LengthKm").TriggerEventAsync("ontbeditstart", EventArgs.Empty);

        Assert.Contains("Not today.", Cell(cut, 1, "LengthKm").TextContent);
        Assert.Empty(Cell(cut, 2, "LengthKm").QuerySelectorAll("[data-part=editor]"));
    }

    [Fact]
    public async Task Enter_ShouldRaiseTheEditWithAValueOfTheFieldsType_WhenTheGridHandlesEdits()
    {
        GridCellEdit<Hike>? saved = null;
        var cut = RenderHikes(parameters => parameters.Add(grid => grid.OnCellEdit,
            (GridCellEdit<Hike> edit) => saved = edit));
        await Cell(cut, 1, "LengthKm").TriggerEventAsync("ontbeditstart", EventArgs.Empty);

        await cut.Tb().NumberField("Length km").EnterAsync("12");
        await EditorKeyAsync(cut, "Enter");

        Assert.Equal(new GridCellEdit<Hike>(_hikes[0], "LengthKm", 12), saved);
        Assert.Equal(6, _hikes[0].LengthKm);
    }

    [Fact]
    public async Task Escape_ShouldLeaveTheValue_WhenPressedInAnEdit()
    {
        var cut = RenderHikes();
        await Cell(cut, 1, "Name").DoubleClickAsync(new());

        await cut.Find("[data-part=editor] input").InputAsync(new ChangeEventArgs { Value = "Changed" });
        await EditorKeyAsync(cut, "Escape");

        Assert.Equal("Pine Hollow", _hikes[0].Name);
        Assert.Empty(cut.FindAll("[data-part=editor]"));
    }

    [Fact]
    public async Task Escape_ShouldKeepTheEditOpen_WhenAnImeIsComposing()
    {
        var cut = RenderHikes();
        await Cell(cut, 1, "Name").DoubleClickAsync(new());

        await EditorKeyAsync(cut, "Escape", composing: true);

        Assert.Single(cut.FindAll("[data-part=editor]"));
    }

    [Theory]
    [InlineData("500", "The field Length km must be between 1 and 100.")]
    [InlineData("", "Length km is required.")]
    [InlineData("7", "Odd lengths are booked out.")]
    [InlineData("6.5", "Length km can't hold that value.")]
    public async Task Enter_ShouldKeepTheEditOpenWithWhy_WhenACheckFails(string typed, string error)
    {
        var cut = RenderHikes(validate: (_, value) => value is int length && length % 2 == 1
            ? "Odd lengths are booked out." : null);
        await Cell(cut, 1, "LengthKm").DoubleClickAsync(new());

        await cut.Tb().NumberField("Length km").EnterAsync(typed);
        await EditorKeyAsync(cut, "Enter");

        Assert.Contains(error, cut.Find("[data-part=editor]").TextContent);
        Assert.Equal(6, _hikes[0].LengthKm);
    }

    [Fact]
    public async Task FocusLeavingTheEdit_ShouldSaveIt_WhenItMovesOutOfTheField()
    {
        var cut = RenderHikes();
        await Cell(cut, 1, "Name").DoubleClickAsync(new());
        await cut.Find("[data-part=editor] input").InputAsync(new ChangeEventArgs { Value = "Pine Hollow loop" });

        await cut.Find("[data-part=editor]").TriggerEventAsync("ontbfocusleave",
            new FieldFocusEventArgs { CrossesField = true });

        Assert.Equal("Pine Hollow loop", _hikes[0].Name);
    }

    [Fact]
    public async Task Choose_ShouldSaveAnEnumsValue_WhenTheCellEditsAChoice()
    {
        var cut = RenderHikes();
        await Cell(cut, 1, "Difficulty").DoubleClickAsync(new());

        await cut.Tb().Select("Difficulty").ChooseAsync("Hard");

        Assert.Equal(Difficulty.Hard, _hikes[0].Difficulty);
        Assert.Empty(cut.FindAll("[data-part=editor]"));
    }

    [Fact]
    public void Editable_ShouldNotOfferAnEdit_WhenTheFieldIsInitOnlyAndNoHandlerTakesIt()
    {
        var cut = RenderCamps(handled: false);

        Assert.Null(cut.Find("td[data-column=Name]").GetAttribute("data-editable"));
        Assert.Null(cut.Find("table").GetAttribute("role"));
    }

    [Fact]
    public void Editable_ShouldOfferAnEdit_WhenAHandlerTakesAnInitOnlyField()
    {
        var cut = RenderCamps(handled: true);

        Assert.Equal("", cut.Find("td[data-column=Name]").GetAttribute("data-editable"));
    }

    [Fact]
    public async Task Enter_ShouldLeaveTheEditOpen_WhenItsFieldUsedTheKey()
    {
        var cut = RenderHikes();
        await Cell(cut, 1, "Difficulty").DoubleClickAsync(new());

        await EditorKeyAsync(cut, "Enter", handled: true);

        Assert.Single(cut.FindAll("[data-part=editor]"));
    }

    [Fact]
    public async Task OnCellEdit_ShouldKeepTheEditOpenWithWhy_WhenTheHandlerTurnsItDown()
    {
        var cut = RenderHikes(parameters => parameters.Add(grid => grid.OnCellEdit,
            (GridCellEdit<Hike> edit) => edit.Error = "That name is taken."));
        await Cell(cut, 1, "Name").DoubleClickAsync(new());

        await cut.Find("[data-part=editor] input").InputAsync(new ChangeEventArgs { Value = "Eagle Ridge" });
        await EditorKeyAsync(cut, "Enter");

        Assert.Contains("That name is taken.", cut.Find("[data-part=editor]").TextContent);
        Assert.Equal("Eagle Ridge", cut.Find("[data-part=editor] input").GetAttribute("value"));
    }

    [Fact]
    public async Task OnCellEdit_ShouldBeAskedOnce_WhenTheEditIsSavedAgainWhileItsHandlerRuns()
    {
        var calls = 0;
        var server = new TaskCompletionSource();
        var cut = RenderHikes(parameters => parameters.Add(grid => grid.OnCellEdit, async (GridCellEdit<Hike> _) =>
        {
            calls++;
            await server.Task;
        }));
        await Cell(cut, 1, "Name").DoubleClickAsync(new());

        // Both saves wait on the handler, so they are awaited once it has finished.
        var enter = EditorKeyAsync(cut, "Enter");
        var leave = cut.Find("[data-part=editor]").TriggerEventAsync("ontbfocusleave",
            new FieldFocusEventArgs { CrossesField = true });
        await cut.InvokeAsync(server.SetResult);
        await Task.WhenAll(enter, leave);

        Assert.Equal(1, calls);
        Assert.Empty(cut.FindAll("[data-part=editor]"));
    }

    [Fact]
    public async Task EditTemplate_ShouldShowSaveAndCheckItsValue_WhenItSetsOne()
    {
        var cut = RenderHikes(template: true, validate: (_, value) => value is "" ? "Name it." : null);
        await Cell(cut, 1, "Name").DoubleClickAsync(new());

        await cut.Find("button[data-value='']").ClickAsync(new());
        Assert.Equal("", cut.Find("[data-part=shown]").TextContent);
        await cut.Find("button[data-part=save]").ClickAsync(new());
        Assert.Equal("Name it.", cut.Find("[data-part=error]").TextContent);
        await cut.Find("button[data-value='Pine loop']").ClickAsync(new());
        Assert.Equal("Pine loop", cut.Find("[data-part=shown]").TextContent);
        await cut.Find("button[data-part=save]").ClickAsync(new());

        Assert.Equal("Pine loop", _hikes[0].Name);
        Assert.Empty(cut.FindAll("[data-part=editor]"));
    }

    [Fact]
    public async Task EditTemplate_ShouldLeaveTheValue_WhenItCancels()
    {
        var cut = RenderHikes(template: true);
        await Cell(cut, 1, "Name").DoubleClickAsync(new());

        await cut.Find("button[data-value='Pine loop']").ClickAsync(new());
        await cut.Find("button[data-part=cancel]").ClickAsync(new());

        Assert.Equal("Pine Hollow", _hikes[0].Name);
        Assert.Empty(cut.FindAll("[data-part=editor]"));
    }

    [Fact]
    public async Task RowEdit_ShouldOpenEveryEditableCellAndSaveThemTogether_WhenEnterIsPressed()
    {
        GridRowEdit<Hike>? saved = null;
        var cut = RenderRows(parameters => parameters.Add(grid => grid.OnRowEdit,
            (GridRowEdit<Hike> edit) => saved = edit));
        await Cell(cut, 1, "Name").DoubleClickAsync(new());
        Assert.Equal(4, cut.FindAll("tr[data-key='1'] [data-part=editor]").Count);

        await cut.Find("td[data-column=Name] input").InputAsync(new ChangeEventArgs { Value = "Pine loop" });
        await cut.Tb().NumberField("Length km").EnterAsync("8");
        await EditorKeyAsync(cut, "Enter");

        Assert.Equal(new Dictionary<string, object?>
        {
            ["Name"] = "Pine loop", ["LengthKm"] = 8, ["Difficulty"] = Difficulty.Easy, ["Loop"] = true,
        }, saved!.Values);
        Assert.Empty(cut.FindAll("[data-part=editor]"));
    }

    [Fact]
    public async Task RowEdit_ShouldSaveNothingAndShowWhy_WhenOneCellFailsItsCheck()
    {
        GridRowEdit<Hike>? saved = null;
        var cut = RenderRows(parameters => parameters.Add(grid => grid.OnRowEdit,
            (GridRowEdit<Hike> edit) => saved = edit));
        await Cell(cut, 1, "Name").DoubleClickAsync(new());

        await cut.Tb().NumberField("Length km").EnterAsync("500");
        await EditorKeyAsync(cut, "Enter");

        Assert.Null(saved);
        Assert.Contains("must be between 1 and 100", Cell(cut, 1, "LengthKm").TextContent);
        Assert.Equal(4, cut.FindAll("[data-part=editor]").Count);
    }

    [Fact]
    public async Task RowEdit_ShouldSetEveryValueOnTheItem_WhenTheGridHasNoRowHandler()
    {
        var cut = RenderRows();
        await Cell(cut, 1, "Loop").DoubleClickAsync(new());

        await cut.Find("td[data-column=Name] input").InputAsync(new ChangeEventArgs { Value = "Pine loop" });
        await cut.Find("td[data-column=Loop] input").ChangeAsync(new ChangeEventArgs { Value = false });
        await EditorKeyAsync(cut, "Enter");

        Assert.Equal("Pine loop", _hikes[0].Name);
        Assert.False(_hikes[0].Loop);
    }

    [Fact]
    public async Task RowEdit_ShouldKeepTheRowOpenWithWhy_WhenTheHandlerTurnsItDown()
    {
        var announcer = Render<AnnouncerHost>();
        var cut = RenderRows(parameters => parameters.Add(grid => grid.OnRowEdit,
            (GridRowEdit<Hike> edit) => edit.Error = "Someone else changed this hike."));
        await Cell(cut, 1, "Name").DoubleClickAsync(new());

        await EditorKeyAsync(cut, "Enter");

        Assert.Equal("Someone else changed this hike.", cut.Find("[data-part=row-error]").TextContent);
        Assert.Equal("Someone else changed this hike.", announcer.Find("[role=alert] p").TextContent);
        Assert.Empty(cut.FindAll("[aria-invalid=true]"));
        Assert.Equal(4, cut.FindAll("[data-part=editor]").Count);
    }

    [Fact]
    public async Task RowEdit_ShouldDropTheHandlersReason_WhenTheRowIsSavedAgain()
    {
        var cut = RenderRows(parameters => parameters.Add(grid => grid.OnRowEdit,
            (GridRowEdit<Hike> edit) => edit.Error = "Someone else changed this hike."));
        await Cell(cut, 1, "Name").DoubleClickAsync(new());
        await EditorKeyAsync(cut, "Enter");

        await cut.Tb().NumberField("Length km").EnterAsync("500");
        await EditorKeyAsync(cut, "Enter");

        Assert.Empty(cut.FindAll("[data-part=row-error]"));
        Assert.Contains("must be between 1 and 100", Cell(cut, 1, "LengthKm").TextContent);
    }

    [Fact]
    public async Task RowEdit_ShouldKeepTheRowOpen_WhenAChoiceIsMade()
    {
        var cut = RenderRows();
        await Cell(cut, 1, "Difficulty").DoubleClickAsync(new());

        await cut.Tb().Select("Difficulty").ChooseAsync("Hard");

        Assert.Equal(4, cut.FindAll("[data-part=editor]").Count);
        Assert.Equal(Difficulty.Easy, _hikes[0].Difficulty);
    }

    [Theory]
    [InlineData("Save Pine Hollow", "Pine loop")]
    [InlineData("Cancel editing Pine Hollow", "Pine Hollow")]
    public async Task RowActions_ShouldSaveOrCancelTheRow_WhenTheirButtonIsClicked(string button, string name)
    {
        var cut = RenderRows();
        await Cell(cut, 1, "Name").DoubleClickAsync(new());

        await cut.Find("td[data-column=Name] input").InputAsync(new ChangeEventArgs { Value = "Pine loop" });
        await cut.Find($"[data-part=row-actions] button[aria-label='{button}']").ClickAsync(new());

        Assert.Equal(name, _hikes[0].Name);
        Assert.Empty(cut.FindAll("[data-part=editor], [data-part=row-actions]"));
    }

    private IRenderedComponent<TbDataGrid<Hike>> RenderRows(
        Action<ComponentParameterCollectionBuilder<TbDataGrid<Hike>>>? configure = null) =>
        RenderHikes(parameters =>
        {
            parameters.Add(grid => grid.EditMode, GridEditMode.Row);
            configure?.Invoke(parameters);
        });

    private static Task EditorKeyAsync(IRenderedComponent<TbDataGrid<Hike>> cut, string key, bool handled = false,
        bool composing = false) => cut.Find("[data-part=editor]").TriggerEventAsync("ontbeditkey",
            new GridEditKeyEventArgs { Key = key, Handled = handled, IsComposing = composing });

    private static IElement Cell(IRenderedComponent<TbDataGrid<Hike>> cut, int id, string column) =>
        cut.Find($"tr[data-key='{id}'] td[data-column={column}]");

    private static Expression<Func<Hike, object?>> Field(Expression<Func<Hike, object?>> field) => field;

    private IRenderedComponent<TbDataGrid<Hike>> RenderHikes(
        Action<ComponentParameterCollectionBuilder<TbDataGrid<Hike>>>? configure = null,
        Func<Hike, object?, string?>? validate = null, bool template = false) =>
        Render<TbDataGrid<Hike>>(parameters =>
        {
            parameters.Add(grid => grid.Items, _hikes).Add(grid => grid.AriaLabel, "Hikes")
                .Add(grid => grid.KeySelector, hike => hike.Id)
                .Add(grid => grid.ChildContent, (RenderFragment)(builder => Columns(builder, validate, template)));
            configure?.Invoke(parameters);
        });

    // A template for the name that shows its value and its error, with buttons that set a value, save and cancel.
    private static RenderFragment<GridCellEditContext<Hike>> NameTemplate => context => builder =>
    {
        builder.OpenElement(0, "span");
        builder.AddAttribute(1, "data-part", "shown");
        builder.AddContent(2, context.Value);
        builder.CloseElement();
        builder.OpenElement(3, "span");
        builder.AddAttribute(4, "data-part", "error");
        builder.AddContent(5, context.Error);
        builder.CloseElement();
        foreach (var name in new[] { "", "Pine loop" })
        {
            builder.OpenElement(6, "button");
            builder.AddAttribute(7, "data-value", name);
            builder.AddAttribute(8, "onclick", EventCallback.Factory.Create(context, () => context.SetValue(name)));
            builder.CloseElement();
        }

        builder.OpenElement(9, "button");
        builder.AddAttribute(10, "data-part", "save");
        builder.AddAttribute(11, "onclick", EventCallback.Factory.Create(context, context.SaveAsync));
        builder.CloseElement();
        builder.OpenElement(12, "button");
        builder.AddAttribute(13, "data-part", "cancel");
        builder.AddAttribute(14, "onclick", EventCallback.Factory.Create(context, context.CancelAsync));
        builder.CloseElement();
    };

    private static void Columns(RenderTreeBuilder builder, Func<Hike, object?, string?>? validate, bool template)
    {
        var sequence = 0;
        Expression<Func<Hike, object?>>[] fields = [Field(hike => hike.Name), Field(hike => hike.LengthKm),
            Field(hike => hike.Difficulty), Field(hike => hike.Loop)];
        foreach (var field in fields)
        {
            builder.OpenComponent<TbColumn<Hike>>(sequence++);
            builder.AddAttribute(sequence++, nameof(TbColumn<Hike>.Field), field);
            builder.AddAttribute(sequence++, nameof(TbColumn<Hike>.Editable), true);
            builder.AddAttribute(sequence++, nameof(TbColumn<Hike>.Validate), validate);
            builder.AddAttribute(sequence++, nameof(TbColumn<Hike>.EditTemplate),
                template && field == fields[0] ? NameTemplate : null);
            builder.CloseComponent();
        }
    }

    private IRenderedComponent<TbDataGrid<Camp>> RenderCamps(bool handled) =>
        Render<TbDataGrid<Camp>>(parameters =>
        {
            parameters.Add(grid => grid.Items, [new Camp(1, "Aspen Flat")]).Add(grid => grid.AriaLabel, "Camps")
                .Add(grid => grid.ChildContent, (RenderFragment)(builder =>
                {
                    builder.OpenComponent<TbColumn<Camp>>(0);
                    builder.AddAttribute(1, nameof(TbColumn<Camp>.Field),
                        (Expression<Func<Camp, object?>>)(camp => camp.Name));
                    builder.AddAttribute(2, nameof(TbColumn<Camp>.Editable), true);
                    builder.CloseComponent();
                }));
            if (handled)
            {
                parameters.Add(grid => grid.OnCellEdit, (GridCellEdit<Camp> _) => { });
            }
        });
}
