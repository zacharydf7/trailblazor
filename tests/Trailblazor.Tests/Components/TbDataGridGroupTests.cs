using System.Globalization;
using System.Linq.Expressions;
using AngleSharp.Dom;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Rendering;
using Trailblazor.Testing;

namespace Trailblazor.Tests.Components;

public class TbDataGridGroupTests : BunitContext
{
    public TbDataGridGroupTests()
    {
        this.AddTrailblazor();
    }

    public enum Difficulty
    {
        Easy,
        Moderate,
        Hard,
    }

    public sealed record Hike(int Id, string Name, Difficulty Difficulty, bool Loop);

    private static IReadOnlyList<Hike> Hikes =>
    [
        new(1, "Pine Hollow", Difficulty.Easy, true),
        new(2, "Eagle Ridge", Difficulty.Hard, false),
        new(3, "Cedar Flats", Difficulty.Easy, false),
        new(4, "Ridge Loop", Difficulty.Moderate, true),
    ];

    [Fact]
    public void Groups_ShouldHeadEachGroupWithItsValueAndCountAndKeepItsRowsSorted_WhenGivenAColumn()
    {
        var cut = RenderHikes(parameters => parameters.Add(grid => grid.Groups, ["Difficulty"])
            .Add(grid => grid.Sorts, [new GridSort("Name", SortDirection.Ascending)]));

        Assert.Equal(["Difficulty: Easy Rows: 2", "Cedar Flats", "Pine Hollow", "Difficulty: Moderate Rows: 1",
            "Ridge Loop", "Difficulty: Hard Rows: 1", "Eagle Ridge"], Lines(cut));
    }

    [Fact]
    public void Groups_ShouldComeLargestFirst_WhenTheirColumnSortsDescending()
    {
        var cut = RenderHikes(parameters => parameters.Add(grid => grid.Groups, ["Difficulty"])
            .Add(grid => grid.Sorts, [new GridSort("Difficulty", SortDirection.Descending)]));

        Assert.Equal(["Difficulty: Hard Rows: 1", "Difficulty: Moderate Rows: 1", "Difficulty: Easy Rows: 2"],
            cut.FindAll("tr[data-part=group]").Select(Text));
    }

    [Fact]
    public void Groups_ShouldNestInTheOrderGiven_WhenGivenTwoColumns()
    {
        var cut = RenderHikes(parameters => parameters.Add(grid => grid.Groups, ["Loop", "Difficulty"]));

        Assert.Equal(["Loop: False Rows: 2", "Difficulty: Easy Rows: 1", "Cedar Flats", "Difficulty: Hard Rows: 1",
            "Eagle Ridge", "Loop: True Rows: 2", "Difficulty: Easy Rows: 1", "Pine Hollow",
            "Difficulty: Moderate Rows: 1", "Ridge Loop"], Lines(cut));
        Assert.Equal(["1", "2", "2", "1", "2", "2"],
            cut.FindAll("tr[data-part=group]").Select(group => group.GetAttribute("data-level")));
    }

    [Fact]
    public async Task GroupToggle_ShouldHideItsRowsAndShowThemAgain_WhenClickedTwice()
    {
        var cut = RenderHikes(parameters => parameters.Add(grid => grid.Groups, ["Difficulty"]));
        IElement Toggle() => cut.FindAll("[data-part=group-toggle]")[0];

        await Toggle().ClickAsync(new());

        Assert.Equal("false", Toggle().GetAttribute("aria-expanded"));
        Assert.Equal(["Difficulty: Easy Rows: 2", "Difficulty: Moderate Rows: 1", "Ridge Loop",
            "Difficulty: Hard Rows: 1", "Eagle Ridge"], Lines(cut));
        await Toggle().ClickAsync(new());
        Assert.Equal("true", Toggle().GetAttribute("aria-expanded"));
        Assert.Equal(7, Lines(cut).Count);
    }

    [Fact]
    public async Task GroupBy_ShouldOfferTheGroupableColumnsAndGroupInTheOrderPicked_WhenTheUserPicksThem()
    {
        IReadOnlyList<string>? groups = null;
        var cut = RenderHikes(parameters => parameters
            .Add(grid => grid.GroupsChanged, (IReadOnlyList<string> changed) => groups = changed));
        var groupBy = cut.Tb().Select("Group by");
        Assert.Equal(["Difficulty", "Loop"], groupBy.Options);

        await groupBy.ChooseAsync("Loop", "Difficulty");

        Assert.Equal(["Loop", "Difficulty"], groups);
        Assert.Equal("Loop: False Rows: 2", Text(cut.Find("tr[data-part=group]")));
    }

    [Fact]
    public async Task GroupBy_ShouldLeaveThePagesFormUnmodified_WhenTheGridIsInsideIt()
    {
        var form = new EditContext(new object());
        var cut = Render<CascadingValue<EditContext>>(parameters => parameters.Add(value => value.Value, form)
            .Add(value => value.ChildContent, (RenderFragment)(builder =>
            {
                builder.OpenComponent<TbDataGrid<Hike>>(0);
                builder.AddComponentParameter(1, nameof(TbDataGrid<Hike>.Items), Hikes);
                builder.AddComponentParameter(2, nameof(TbDataGrid<Hike>.AriaLabel), "Hikes");
                builder.AddComponentParameter(3, nameof(TbDataGrid<Hike>.ChildContent), (RenderFragment)Columns);
                builder.CloseComponent();
            })));

        await cut.Tb().Select("Group by").ChooseAsync("Loop");

        Assert.Equal(2, cut.FindAll("tr[data-part=group]").Count);
        Assert.False(form.IsModified());
    }

    [Fact]
    public void GroupBy_ShouldNotShow_WhenNoColumnIsGroupable()
    {
        var cut = Render<TbDataGrid<Hike>>(parameters => parameters.Add(grid => grid.Items, Hikes)
            .Add(grid => grid.AriaLabel, "Hikes").Add(grid => grid.ChildContent, (RenderFragment)(builder =>
            {
                builder.OpenComponent<TbColumn<Hike>>(0);
                builder.AddAttribute(1, nameof(TbColumn<Hike>.Field), Field(hike => hike.Name));
                builder.CloseComponent();
            })));

        Assert.Empty(cut.FindAll("[data-part=group-by]"));
    }

    [Fact]
    public void Groups_ShouldThrow_WhenRowsComeFromAProvider()
    {
        var error = Assert.Throws<InvalidOperationException>(() => Render<TbDataGrid<Hike>>(parameters => parameters
            .Add(grid => grid.DataProvider, (_, _) => Task.FromResult(new GridResult<Hike>(Hikes, Hikes.Count)))
            .Add(grid => grid.AriaLabel, "Hikes").Add(grid => grid.Groups, ["Difficulty"])
            .Add(grid => grid.ChildContent, Columns)));

        Assert.Contains("Items", error.Message);
    }

    [Fact]
    public void Groups_ShouldCountHeadingsAmongAPagesRows_WhenPaged()
    {
        var cut = RenderHikes(parameters => parameters.Add(grid => grid.Groups, ["Difficulty"])
            .Add(grid => grid.PageSize, 3));

        Assert.Equal(["Difficulty: Easy Rows: 2", "Pine Hollow", "Cedar Flats"], Lines(cut));
        Assert.Equal("1–3 of 7", cut.Find("[data-part=summary]").TextContent.Trim());
    }

    [Fact]
    public async Task ScrollToAsync_ShouldOpenTheGroupsHoldingTheRow_WhenTheyAreClosed()
    {
        var cut = RenderHikes(parameters => parameters.Add(grid => grid.Groups, ["Difficulty"]));
        await cut.FindAll("[data-part=group-toggle]")[2].ClickAsync(new());
        Assert.DoesNotContain("Eagle Ridge", Lines(cut));

        _ = cut.InvokeAsync(() => cut.Instance.ScrollToAsync(Hikes[1]));

        cut.WaitForAssertion(() => Assert.Contains("Eagle Ridge", Lines(cut)));
        Assert.Equal("true", cut.FindAll("[data-part=group-toggle]")[2].GetAttribute("aria-expanded"));
    }

    [Fact]
    public async Task ScrollToAsync_ShouldDrawTheRowsOfTheGroupsItOpens_WhenTheRowsAreVirtualized()
    {
        var cut = RenderHikes(parameters => parameters.Add(grid => grid.Groups, ["Difficulty"])
            .Add(grid => grid.Virtualize, true).Add(grid => grid.Height, "20rem"));
        await cut.FindAll("[data-part=group-toggle]")[2].ClickAsync(new());

        await cut.InvokeAsync(() => cut.Instance.ScrollToAsync(Hikes[1]));

        Assert.Equal("true", cut.FindAll("[data-part=group-toggle]")[2].GetAttribute("aria-expanded"));
        Assert.Contains("Eagle Ridge", Lines(cut));
    }

    [Fact]
    public void Groups_ShouldLeaveRowsWithoutHandles_WhenRowsCouldOtherwiseMove()
    {
        List<Hike> hikes = [.. Hikes];
        var cut = RenderHikes(parameters => parameters.Add(grid => grid.Groups, ["Difficulty"])
            .Add(grid => grid.OnRowMove, (SortableMove<Hike> move) => move.ApplyTo(hikes)));

        Assert.Empty(cut.FindAll("[data-part=order]"));
    }

    [Fact]
    public void Groups_ShouldNameAGroupWithoutAValue_WhenItsRowsHaveNone()
    {
        var cut = Render<TbDataGrid<Hike>>(parameters => parameters.Add(grid => grid.Items, Hikes)
            .Add(grid => grid.AriaLabel, "Hikes").Add(grid => grid.Groups, ["Kind"])
            .Add(grid => grid.ChildContent, (RenderFragment)(builder =>
            {
                builder.OpenComponent<TbColumn<Hike>>(0);
                builder.AddAttribute(1, nameof(TbColumn<Hike>.Field), Field(hike => hike.Name));
                builder.CloseComponent();
                builder.OpenComponent<TbColumn<Hike>>(2);
                builder.AddAttribute(3, nameof(TbColumn<Hike>.Title), "Kind");
                builder.AddAttribute(4, nameof(TbColumn<Hike>.Groupable), true);
                builder.AddAttribute(5, nameof(TbColumn<Hike>.GroupBy),
                    (Func<Hike, object?>)(hike => hike.Loop ? "Loop" : null));
                builder.CloseComponent();
            })));

        Assert.Equal(["Kind: None Rows: 2", "Kind: Loop Rows: 2"], cut.FindAll("tr[data-part=group]").Select(Text));
    }

    [Fact]
    public void Groups_ShouldPlaceHeadingsAmongTheRows_WhenTheRowsAreVirtualized()
    {
        var cut = RenderHikes(parameters => parameters.Add(grid => grid.Groups, ["Difficulty"])
            .Add(grid => grid.Virtualize, true).Add(grid => grid.Height, "20rem"));

        Assert.Equal("8", cut.Find("table").GetAttribute("aria-rowcount"));
        Assert.Equal(["2", "3", "4"], cut.FindAll("tbody tr").Where(row => row.HasAttribute("aria-rowindex")).Take(3)
            .Select(row => row.GetAttribute("aria-rowindex")));
        Assert.Equal("group", cut.Find("tbody tr[aria-rowindex='2']").GetAttribute("data-part"));
    }

    [Fact]
    public async Task Groups_ShouldShowOnlyTheirHeadings_WhenEveryGroupIsClosed()
    {
        var cut = RenderHikes(parameters => parameters.Add(grid => grid.Groups, ["Difficulty"]));

        foreach (var at in Enumerable.Range(0, 3))
        {
            await cut.FindAll("[data-part=group-toggle]")[at].ClickAsync(new());
        }

        Assert.Equal(3, Lines(cut).Count);
        Assert.Empty(cut.FindAll("[data-part=empty]"));
    }

    [Fact]
    public async Task Filter_ShouldSayHowManyRowsItLeaves_WhenTheRowsAreGrouped()
    {
        var announcer = Render<AnnouncerHost>();
        var cut = RenderHikes(parameters => parameters.Add(grid => grid.Groups, ["Difficulty"]), filterable: true);

        await cut.Tb().Grid("Hikes").FilterAsync("Name", "ridge");

        announcer.WaitForAssertion(() =>
            Assert.Equal("Rows shown: 2.", announcer.Find("[role=status] p").TextContent));
    }

    [Fact]
    public async Task Groups_ShouldKeepGroupsApart_WhenTheirValuesDifferButLookAlike()
    {
        var cut = Render<TbDataGrid<Hike>>(parameters => parameters.Add(grid => grid.Items, Hikes)
            .Add(grid => grid.AriaLabel, "Hikes").Add(grid => grid.Groups, ["Note"])
            .Add(grid => grid.ChildContent, (RenderFragment)(builder =>
            {
                builder.OpenComponent<TbColumn<Hike>>(0);
                builder.AddAttribute(1, nameof(TbColumn<Hike>.Field), Field(hike => hike.Name));
                builder.CloseComponent();
                builder.OpenComponent<TbColumn<Hike>>(2);
                builder.AddAttribute(3, nameof(TbColumn<Hike>.Title), "Note");
                builder.AddAttribute(4, nameof(TbColumn<Hike>.Groupable), true);
                builder.AddAttribute(5, nameof(TbColumn<Hike>.GroupBy),
                    (Func<Hike, object?>)(hike => hike.Id switch { 1 => null, 2 => "", _ => "Steep" }));
                builder.CloseComponent();
            })));

        await cut.FindAll("[data-part=group-toggle]")[0].ClickAsync(new());

        Assert.Equal(["false", "true", "true"],
            cut.FindAll("[data-part=group-toggle]").Select(toggle => toggle.GetAttribute("aria-expanded")));
        Assert.Equal(["Note: None Rows: 1", "Note: Rows: 1", "Eagle Ridge", "Note: Steep Rows: 2", "Cedar Flats",
            "Ridge Loop"], Lines(cut));
    }

    [Fact]
    public void Groups_ShouldGroupByTheTextCellsShow_WhenTheColumnHasAFormat()
    {
        var culture = CultureInfo.CurrentCulture;
        CultureInfo.CurrentCulture = new CultureInfo("en-US");
        try
        {
            var cut = Render<TbDataGrid<Hike>>(parameters => parameters.Add(grid => grid.Items, Hikes)
                .Add(grid => grid.AriaLabel, "Hikes").Add(grid => grid.Groups, ["Size"])
                .Add(grid => grid.ChildContent, (RenderFragment)(builder =>
                {
                    builder.OpenComponent<TbColumn<Hike>>(0);
                    builder.AddAttribute(1, nameof(TbColumn<Hike>.Field), Field(hike => hike.Id * 0.4));
                    builder.AddAttribute(2, nameof(TbColumn<Hike>.Title), "Size");
                    builder.AddAttribute(3, nameof(TbColumn<Hike>.Format), "N0");
                    builder.AddAttribute(4, nameof(TbColumn<Hike>.Groupable), true);
                    builder.CloseComponent();
                })));

            Assert.Equal(["Size: 0 Rows: 1", "Size: 1 Rows: 2", "Size: 2 Rows: 1"],
                cut.FindAll("tr[data-part=group]").Select(Text));
        }
        finally
        {
            CultureInfo.CurrentCulture = culture;
        }
    }

    [Fact]
    public async Task Groups_ShouldPlaceEachRowAnew_WhenAGroupOfVirtualizedRowsCloses()
    {
        var cut = RenderHikes(parameters => parameters.Add(grid => grid.Groups, ["Difficulty"])
            .Add(grid => grid.Virtualize, true).Add(grid => grid.Height, "20rem"));

        await cut.FindAll("[data-part=group-toggle]")[0].ClickAsync(new());

        Assert.Equal("6", cut.Find("table").GetAttribute("aria-rowcount"));
        Assert.Equal("4", cut.Find("tr[data-key='4']").GetAttribute("aria-rowindex"));
        Assert.Equal("6", cut.Find("tr[data-key='2']").GetAttribute("aria-rowindex"));
    }

    private static string Text(IElement element) => string.Join(" ", element.TextContent.Split(
        (char[]?)null, StringSplitOptions.RemoveEmptyEntries));

    // Each line's text: a heading's, or a row's first cell.
    private static List<string> Lines(IRenderedComponent<TbDataGrid<Hike>> cut) =>
        [.. cut.FindAll("tbody tr[data-part=group], tbody tr[data-part=row]").Select(line =>
            line.GetAttribute("data-part") == "group" ? Text(line) : line.QuerySelector("td[data-column=Name]")!
                .TextContent)];

    private static Expression<Func<Hike, object?>> Field(Expression<Func<Hike, object?>> field) => field;

    private IRenderedComponent<TbDataGrid<Hike>> RenderHikes(
        Action<ComponentParameterCollectionBuilder<TbDataGrid<Hike>>>? configure = null, bool filterable = false) =>
        Render<TbDataGrid<Hike>>(parameters =>
        {
            parameters.Add(grid => grid.Items, Hikes).Add(grid => grid.AriaLabel, "Hikes")
                .Add(grid => grid.KeySelector, hike => hike.Id)
                .Add(grid => grid.ChildContent, (RenderFragment)(builder => Columns(builder, filterable)));
            configure?.Invoke(parameters);
        });

    private static void Columns(RenderTreeBuilder builder) => Columns(builder, filterable: false);

    private static void Columns(RenderTreeBuilder builder, bool filterable)
    {
        builder.OpenComponent<TbColumn<Hike>>(0);
        builder.AddAttribute(1, nameof(TbColumn<Hike>.Field), Field(hike => hike.Name));
        builder.AddAttribute(2, nameof(TbColumn<Hike>.Sortable), true);
        builder.AddAttribute(10, nameof(TbColumn<Hike>.Filterable), filterable);
        builder.CloseComponent();
        builder.OpenComponent<TbColumn<Hike>>(3);
        builder.AddAttribute(4, nameof(TbColumn<Hike>.Field), Field(hike => hike.Difficulty));
        builder.AddAttribute(5, nameof(TbColumn<Hike>.Groupable), true);
        builder.AddAttribute(6, nameof(TbColumn<Hike>.Sortable), true);
        builder.CloseComponent();
        builder.OpenComponent<TbColumn<Hike>>(7);
        builder.AddAttribute(8, nameof(TbColumn<Hike>.Field), Field(hike => hike.Loop));
        builder.AddAttribute(9, nameof(TbColumn<Hike>.Groupable), true);
        builder.CloseComponent();
    }
}
