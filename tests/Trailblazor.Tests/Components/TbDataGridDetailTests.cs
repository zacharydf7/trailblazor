using System.Diagnostics.Metrics;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.Metrics.Testing;

namespace Trailblazor.Tests.Components;

public class TbDataGridDetailTests : BunitContext
{
    public TbDataGridDetailTests()
    {
        this.AddTrailblazor();
    }

    public sealed record Hike(int Id, string Name, string Notes);

    private static readonly Hike[] Hikes =
    [
        new(1, "Pine Hollow", "Shaded the whole way"),
        new(2, "Eagle Ridge", "Exposed above the saddle"),
        new(3, "Cedar Flats", "Flat along the river"),
    ];

    private List<IReadOnlyCollection<Hike>> Reported { get; } = [];

    private IRenderedComponent<TbDataGrid<Hike>> RenderHikes(
        Action<ComponentParameterCollectionBuilder<TbDataGrid<Hike>>>? configure = null,
        RenderFragment<Hike>? detail = null, Func<Hike, object>? key = null) =>
        Render<TbDataGrid<Hike>>(parameters =>
        {
            parameters.Add(grid => grid.Items, Hikes).Add(grid => grid.AriaLabel, "Hikes")
                .Add(grid => grid.KeySelector, key ?? (hike => hike.Id)).Add(grid => grid.RowLabel, hike => hike.Name)
                .Add(grid => grid.SelectionMode, SelectionMode.Multiple)
                .Add(grid => grid.ExpandedItemsChanged, (IReadOnlyCollection<Hike> open) => Reported.Add(open))
                .Add(grid => grid.DetailTemplate, detail ?? (hike => builder => builder.AddContent(0, hike.Notes)))
                .AddChildContent<TbColumn<Hike>>(column => column.Add(name => name.Field, hike => hike.Name));
            configure?.Invoke(parameters);
        });

    private static AngleSharp.Dom.IElement Toggle(IRenderedComponent<TbDataGrid<Hike>> cut, string name) =>
        cut.Find($"button[aria-label='Details for {name}']");

    private static IReadOnlyList<string> Lines(IRenderedComponent<TbDataGrid<Hike>> cut) =>
        [.. cut.FindAll("tbody tr").Select(row => row.GetAttribute("data-part") == "detail"
            ? $"detail: {row.TextContent.Trim()}" : row.QuerySelector("td[data-column=Name]")!.TextContent.Trim())];

    [Fact]
    public void Render_ShouldStartEachRowWithAClosedDetailsButton_WhenTheGridHasADetailTemplate()
    {
        var cut = RenderHikes();

        var toggle = Toggle(cut, "Pine Hollow");
        Assert.Equal(("false", null), (toggle.GetAttribute("aria-expanded"), toggle.GetAttribute("aria-controls")));
        Assert.Equal("detail-toggle", toggle.GetAttribute("data-part"));
        Assert.Equal("Details", cut.Find("thead .tb-data-grid__detail-toggle").TextContent.Trim());
        Assert.Equal(["Pine Hollow", "Eagle Ridge", "Cedar Flats"], Lines(cut));
    }

    [Fact]
    public void Toggle_ShouldShowTheRowsDetailsUnderItAcrossEveryColumn_WhenItsButtonIsClicked()
    {
        var cut = RenderHikes();

        Toggle(cut, "Eagle Ridge").Click();

        Assert.Equal(["Pine Hollow", "Eagle Ridge", "detail: Exposed above the saddle", "Cedar Flats"], Lines(cut));
        var detail = cut.Find("tr[data-part=detail]");
        Assert.Equal(detail.Id, Toggle(cut, "Eagle Ridge").GetAttribute("aria-controls"));
        Assert.Equal("true", Toggle(cut, "Eagle Ridge").GetAttribute("aria-expanded"));
        Assert.Equal(cut.FindAll("thead th").Count.ToString(),
            detail.QuerySelector("[data-part=detail-cell]")!.GetAttribute("colspan"));
        Assert.Equal([2], Reported.Single().Select(hike => hike.Id));
    }

    [Fact]
    public void Toggle_ShouldCloseTheRowsDetails_WhenItsButtonIsClickedAgain()
    {
        var cut = RenderHikes();

        Toggle(cut, "Eagle Ridge").Click();
        Toggle(cut, "Eagle Ridge").Click();

        Assert.Empty(cut.FindAll("tr[data-part=detail]"));
        Assert.Empty(Reported[^1]);
    }

    [Fact]
    public void Toggle_ShouldKeepEarlierDetailsOpen_WhenAnotherRowsDetailsOpen()
    {
        var cut = RenderHikes();

        Toggle(cut, "Pine Hollow").Click();
        Toggle(cut, "Cedar Flats").Click();

        Assert.Equal(2, cut.FindAll("tr[data-part=detail]").Count);
        Assert.Equal([1, 3], Reported[^1].Select(hike => hike.Id).Order());
    }

    [Fact]
    public void SingleDetail_ShouldCloseTheOpenDetails_WhenAnotherRowsDetailsOpen()
    {
        var cut = RenderHikes(parameters => parameters.Add(grid => grid.SingleDetail, true));

        Toggle(cut, "Pine Hollow").Click();
        Toggle(cut, "Cedar Flats").Click();

        Assert.Equal(["Pine Hollow", "Eagle Ridge", "Cedar Flats", "detail: Flat along the river"], Lines(cut));
        Assert.Equal("false", Toggle(cut, "Pine Hollow").GetAttribute("aria-expanded"));
        Assert.Equal([3], Reported[^1].Select(hike => hike.Id));
    }

    [Fact]
    public void ExpandedItems_ShouldOpenTheRowsDetails_WhenTheParentSetsThem()
    {
        var cut = RenderHikes(parameters => parameters.Add(grid => grid.ExpandedItems, [Hikes[0]]));

        Assert.Equal("detail: Shaded the whole way", Lines(cut)[1]);

        cut.Render(parameters => parameters.Add(grid => grid.ExpandedItems, []));

        Assert.Empty(cut.FindAll("tr[data-part=detail]"));
        Assert.Empty(Reported);
    }

    [Fact]
    public void Toggle_ShouldCountTheDetailsOpenedAndClosed_WhenTheGridHasATelemetryName()
    {
        using var used = new MetricCollector<long>(Services.GetRequiredService<IMeterFactory>(), "Trailblazor",
            "trailblazor.control.used");
        var cut = RenderHikes(parameters => parameters.Add(grid => grid.TelemetryName, "hikes"));

        Toggle(cut, "Pine Hollow").Click();
        Toggle(cut, "Pine Hollow").Click();

        Assert.Equal(["open_detail", "close_detail"],
            used.GetMeasurementSnapshot().Select(measurement => measurement.Tags["trailblazor.action"]));
    }

    [Fact]
    public void DetailTemplate_ShouldShowThePagesStateAgain_WhenTheGridDrawsAgain()
    {
        var loaded = "Loading…";
        var cut = RenderHikes(parameters => parameters.Add(grid => grid.ExpandedItems, [Hikes[0]]),
            detail: _ => builder => builder.AddContent(0, loaded));

        loaded = "Two stages";
        cut.Render();

        Assert.Equal("Two stages", cut.Find("[data-part=detail-cell]").TextContent.Trim());
    }

    [Fact]
    public void Toggle_ShouldPointAtTheDetailsByAnIdWithoutSpaces_WhenTheRowsKeyHasThem()
    {
        var cut = RenderHikes(key: hike => hike.Name);

        Toggle(cut, "Pine Hollow").Click();

        var controls = Toggle(cut, "Pine Hollow").GetAttribute("aria-controls")!;
        Assert.DoesNotContain(' ', controls);
        Assert.Equal(controls, cut.Find("tr[data-part=detail]").Id);
    }

    [Fact]
    public void Details_ShouldKeepWhatTheyHold_WhenTheRowsReorder()
    {
        var cut = RenderHikes(parameters => parameters.Add(grid => grid.ExpandedItems, [Hikes[0], Hikes[2]]),
            detail: _ => builder =>
            {
                builder.OpenComponent<Probe>(0);
                builder.CloseComponent();
            });
        var made = Probe.Made;

        cut.Render(parameters => parameters.Add(grid => grid.Items, [.. Hikes.Reverse()]));

        Assert.Equal(made, Probe.Made);
        Assert.Equal(["Cedar Flats", "detail: ", "Eagle Ridge", "Pine Hollow", "detail: "], Lines(cut));
    }

    // Counts how many were made, so a test can tell details that moved from details drawn anew.
    private sealed class Probe : ComponentBase
    {
        public static int Made { get; private set; }

        protected override void OnInitialized() => Made++;
    }

    [Fact]
    public void Render_ShouldThrow_WhenTheGridVirtualizesItsRows()
    {
        Assert.Throws<InvalidOperationException>(() => RenderHikes(parameters => parameters
            .Add(grid => grid.Virtualize, true).Add(grid => grid.Height, "10rem")));
    }
}
