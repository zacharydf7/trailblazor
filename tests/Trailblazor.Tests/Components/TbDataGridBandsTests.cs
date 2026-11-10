using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace Trailblazor.Tests.Components;

public class TbDataGridBandsTests : BunitContext
{
    public TbDataGridBandsTests()
    {
        this.AddTrailblazor();
    }

    public sealed record Hike(int Id, string Name, double LengthKm, int Climb, string Region);

    private static readonly Hike[] Hikes = [new(1, "Pine Hollow", 6.4, 210, "North ridge")];

    private IRenderedComponent<TbDataGrid<Hike>> RenderHikes(
        Action<ComponentParameterCollectionBuilder<TbDataGrid<Hike>>>? configure = null, bool pinLength = false) =>
        Render<TbDataGrid<Hike>>(parameters =>
        {
            parameters.Add(grid => grid.Items, Hikes).Add(grid => grid.AriaLabel, "Hikes")
                .Add(grid => grid.KeySelector, hike => hike.Id)
                .Add(grid => grid.ChildContent, (RenderFragment)(builder => Columns(builder, pinLength)));
            configure?.Invoke(parameters);
        });

    // Name, then Effort over Distance (Length km, Climb), then Region in no group.
    private static void Columns(RenderTreeBuilder builder, bool pinLength)
    {
        Column(builder, 0, hike => hike.Name);
        builder.OpenComponent<TbColumnGroup<Hike>>(10);
        builder.AddAttribute(11, nameof(TbColumnGroup<Hike>.Title), "Effort");
        builder.AddAttribute(12, nameof(TbColumnGroup<Hike>.ChildContent), (RenderFragment)(effort =>
        {
            effort.OpenComponent<TbColumnGroup<Hike>>(0);
            effort.AddAttribute(1, nameof(TbColumnGroup<Hike>.Title), "Distance");
            effort.AddAttribute(2, nameof(TbColumnGroup<Hike>.ChildContent), (RenderFragment)(distance =>
            {
                Column(distance, 0, hike => hike.LengthKm, pinLength ? ColumnPin.Start : ColumnPin.None);
                Column(distance, 5, hike => hike.Climb);
            }));
            effort.CloseComponent();
        }));
        builder.CloseComponent();
        Column(builder, 20, hike => hike.Region);
    }

    private static void Column(RenderTreeBuilder builder, int sequence,
        System.Linq.Expressions.Expression<Func<Hike, object?>> field, ColumnPin pin = ColumnPin.None)
    {
        builder.OpenComponent<TbColumn<Hike>>(sequence);
        builder.AddAttribute(sequence + 1, nameof(TbColumn<Hike>.Field), field);
        builder.AddAttribute(sequence + 2, nameof(TbColumn<Hike>.Pinned), pin);
        builder.CloseComponent();
    }

    // A header row's cells as "text×span", with "-" for a blank cell.
    private static IReadOnlyList<string> Row(IRenderedComponent<TbDataGrid<Hike>> cut, int at) =>
    [
        .. cut.FindAll("thead tr")[at].Children.Select(cell =>
            $"{(cell.TextContent.Trim() is { Length: > 0 } text ? text : "-")}×{cell.GetAttribute("colspan") ?? "1"}"),
    ];

    [Fact]
    public void Render_ShouldHeadEachGroupsColumnsInARowOfItsOwn_WhenGroupsNest()
    {
        var cut = RenderHikes();

        Assert.Equal(["-×1", "Effort×2", "-×1"], Row(cut, 0));
        Assert.Equal(["-×1", "Distance×2", "-×1"], Row(cut, 1));
        Assert.Equal(["Name×1", "Length km×1", "Climb×1", "Region×1"], Row(cut, 2));
        Assert.All(cut.FindAll("th[data-part=band]"), band => Assert.Equal("col", band.GetAttribute("scope")));
        Assert.Equal(2, cut.FindAll("thead tr[data-part=bands]").Count);
    }

    [Fact]
    public void Render_ShouldSpanOnlyTheColumnsThatShow_WhenOneIsHidden()
    {
        var cut = RenderHikes(parameters => parameters.Add(grid => grid.HideableColumns, true)
            .Add(grid => grid.State, new GridState { HiddenColumns = ["Climb"] }));

        Assert.Equal(["-×1", "Effort×1", "-×1"], Row(cut, 0));
    }

    [Fact]
    public void Render_ShouldSplitAHeading_WhenSomeOfItsColumnsArePinned()
    {
        var cut = RenderHikes(pinLength: true);

        Assert.Equal(["Effort×1", "-×1", "Effort×1", "-×1"], Row(cut, 0));
        Assert.Equal("start", cut.FindAll("thead tr")[0].Children[0].GetAttribute("data-pinned"));
    }

    [Fact]
    public void Render_ShouldLeaveTheLeadColumnsBlank_WhenRowsCanBeSelected()
    {
        var cut = RenderHikes(parameters => parameters.Add(grid => grid.SelectionMode, SelectionMode.Multiple));

        Assert.Equal(["-×1", "-×1", "Effort×2", "-×1"], Row(cut, 0));
    }

    [Fact]
    public void Render_ShouldShowTheGroupsNewTitle_WhenItChanges()
    {
        var title = "Effort";
        var cut = Render<TbDataGrid<Hike>>(parameters => parameters.Add(grid => grid.Items, Hikes)
            .Add(grid => grid.AriaLabel, "Hikes").Add(grid => grid.KeySelector, hike => hike.Id)
            .Add(grid => grid.ChildContent, (RenderFragment)(builder =>
            {
                builder.OpenComponent<TbColumnGroup<Hike>>(0);
                builder.AddAttribute(1, nameof(TbColumnGroup<Hike>.Title), title);
                builder.AddAttribute(2, nameof(TbColumnGroup<Hike>.ChildContent),
                    (RenderFragment)(inner => Column(inner, 0, hike => hike.LengthKm)));
                builder.CloseComponent();
            })));

        title = "Distance";
        cut.Render();

        Assert.Equal("Distance", cut.Find("th[data-part=band]").TextContent);
    }

    [Fact]
    public void Render_ShouldNumberTheHeaderRowsAfterTheHeadings_WhenTheGridVirtualizes()
    {
        var cut = RenderHikes(parameters => parameters.Add(grid => grid.Virtualize, true)
            .Add(grid => grid.Height, "10rem"));

        Assert.Equal(["1", "2", "3"], cut.FindAll("thead tr").Select(row => row.GetAttribute("aria-rowindex")));
        Assert.Equal("4", cut.Find("tr[data-part=row]").GetAttribute("aria-rowindex"));
    }
}
