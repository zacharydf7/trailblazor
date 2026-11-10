using Bunit;
using Microsoft.AspNetCore.Components;
using Trailblazor.Testing;

namespace Trailblazor.Tests.Components;

public class TbDataGridTotalsTests : BunitContext
{
    public TbDataGridTotalsTests()
    {
        this.AddTrailblazor();
    }

    public sealed record Hike(int Id, string Name, double LengthKm, int Climb, bool Done, decimal Fee = 0,
        TimeSpan Time = default);

    private static readonly Hike[] Hikes =
    [
        new(1, "Pine Hollow", 6.4, 210, true, 0.1m, TimeSpan.FromMinutes(90)),
        new(2, "Eagle Ridge", 14.2, 980, false, 0.2m, TimeSpan.FromMinutes(300)),
        new(3, "Cedar Flats", 3.1, 40, true, 0.1m, TimeSpan.FromMinutes(45)),
        new(4, "Summit Gate", 11.9, 1250, false, 0.2m, TimeSpan.FromMinutes(285)),
    ];

    private IRenderedComponent<TbDataGrid<Hike>> RenderHikes(
        Action<ComponentParameterCollectionBuilder<TbColumn<Hike>>> length,
        Action<ComponentParameterCollectionBuilder<TbColumn<Hike>>>? climb = null, IReadOnlyList<Hike>? items = null) =>
        Render<TbDataGrid<Hike>>(parameters => parameters.Add(grid => grid.Items, items ?? Hikes)
            .Add(grid => grid.AriaLabel, "Hikes").Add(grid => grid.KeySelector, hike => hike.Id)
            .Add(grid => grid.PageSize, 2)
            .AddChildContent<TbColumn<Hike>>(column => column.Add(name => name.Field, hike => hike.Name)
                .Add(name => name.Filterable, true))
            .AddChildContent<TbColumn<Hike>>(column =>
            {
                column.Add(km => km.Field, hike => hike.LengthKm).Add(km => km.Format, "N1");
                length(column);
            })
            .AddChildContent<TbColumn<Hike>>(column =>
                (climb ?? (climb => climb.Add(up => up.Field, hike => hike.Climb))).Invoke(column)));

    [Theory]
    [InlineData(ColumnAggregate.Sum, "Sum 35.6")]
    [InlineData(ColumnAggregate.Average, "Average 8.9")]
    [InlineData(ColumnAggregate.Count, "Count 4")]
    [InlineData(ColumnAggregate.Min, "Min 3.1")]
    [InlineData(ColumnAggregate.Max, "Max 14.2")]
    public void Totals_ShouldShowTheColumnsTotalOverEveryPage_WhenItHasAnAggregate(ColumnAggregate aggregate,
        string expected)
    {
        var cut = RenderHikes(column => column.Add(km => km.Aggregate, aggregate));

        Assert.Equal(2, cut.FindAll("tbody tr[data-part=row]").Count);
        Assert.Equal(expected, cut.Tb().Grid("Hikes").Total("Length km"));
        Assert.Null(cut.Tb().Grid("Hikes").Total("Climb"));
    }

    [Fact]
    public void Totals_ShouldAddUpDecimalsExactly_WhenTheColumnHoldsDecimals()
    {
        var cut = RenderHikes(_ => { }, column => column.Add(up => up.Field, hike => hike.Fee)
            .Add(up => up.Title, "Fee").Add(up => up.Aggregate, ColumnAggregate.Sum));

        Assert.Equal("Sum 0.6", cut.Tb().Grid("Hikes").Total("Fee"));
    }

    [Fact]
    public void Totals_ShouldKeepAWholeSumWhole_WhenTheColumnHasAnIntegerFormat()
    {
        var cut = RenderHikes(_ => { }, column => column.Add(up => up.Field, hike => hike.Climb)
            .Add(up => up.Format, "D5").Add(up => up.Aggregate, ColumnAggregate.Sum));

        Assert.Equal("Sum 02480", cut.Tb().Grid("Hikes").Total("Climb"));
    }

    [Fact]
    public void Totals_ShouldAskTheProviderAgain_WhenAColumnWithAnAggregateJoinsLater()
    {
        var withLength = false;
        List<GridRequest> asked = [];
        var cut = Render<TbDataGrid<Hike>>(parameters => parameters.Add(grid => grid.AriaLabel, "Hikes")
            .Add(grid => grid.KeySelector, hike => hike.Id)
            .Add(grid => grid.DataProvider, (GridRequest request, CancellationToken _) =>
            {
                asked.Add(request);
                return Task.FromResult(new GridResult<Hike>(Hikes, 4));
            })
            .Add(grid => grid.ChildContent, (RenderFragment)(builder =>
            {
                builder.OpenComponent<TbColumn<Hike>>(0);
                builder.AddAttribute(1, nameof(TbColumn<Hike>.Field),
                    (System.Linq.Expressions.Expression<Func<Hike, object?>>)(hike => hike.Name));
                builder.CloseComponent();
                if (withLength)
                {
                    builder.OpenComponent<TbColumn<Hike>>(2);
                    builder.AddAttribute(3, nameof(TbColumn<Hike>.Field),
                        (System.Linq.Expressions.Expression<Func<Hike, object?>>)(hike => hike.LengthKm));
                    builder.AddAttribute(4, nameof(TbColumn<Hike>.Aggregate), ColumnAggregate.Sum);
                    builder.CloseComponent();
                }
            })));
        cut.WaitForAssertion(() => Assert.Single(asked));

        withLength = true;
        cut.Render();

        cut.WaitForAssertion(() => Assert.Equal([new GridAggregate("LengthKm", ColumnAggregate.Sum)],
            asked[^1].Aggregates));
    }

    [Fact]
    public void Totals_ShouldShowAVirtualizedProvidersTotals_WhenItGivesThem()
    {
        List<GridRequest> asked = [];
        var cut = Render<TbDataGrid<Hike>>(parameters => parameters.Add(grid => grid.AriaLabel, "Hikes")
            .Add(grid => grid.KeySelector, hike => hike.Id).Add(grid => grid.Virtualize, true)
            .Add(grid => grid.Height, "10rem")
            .Add(grid => grid.DataProvider, (GridRequest request, CancellationToken _) =>
            {
                asked.Add(request);
                return Task.FromResult(new GridResult<Hike>([.. Hikes.Skip(request.StartIndex)], 4)
                {
                    Totals = new Dictionary<string, object?> { ["LengthKm"] = 35.6 },
                });
            })
            .AddChildContent<TbColumn<Hike>>(column => column.Add(km => km.Field, hike => hike.LengthKm)
                .Add(km => km.Format, "N1").Add(km => km.Aggregate, ColumnAggregate.Sum)));

        cut.WaitForAssertion(() => Assert.Equal("Sum 35.6", cut.Tb().Grid("Hikes").Total("Length km")));
        Assert.Equal([new GridAggregate("LengthKm", ColumnAggregate.Sum)], asked[^1].Aggregates);
    }

    [Fact]
    public void Totals_ShouldAddUpDurations_WhenTheColumnHoldsTimeSpans()
    {
        var cut = RenderHikes(_ => { }, column => column.Add(up => up.Field, hike => hike.Time)
            .Add(up => up.Title, "Time").Add(up => up.Format, @"h\:mm").Add(up => up.Aggregate, ColumnAggregate.Sum));

        Assert.Equal("Sum 12:00", cut.Tb().Grid("Hikes").Total("Time"));
    }

    [Fact]
    public void Totals_ShouldThrowAndNameTheColumn_WhenItsValuesCantBeAddedUp()
    {
        var error = Assert.Throws<InvalidOperationException>(() => RenderHikes(_ => { },
            column => column.Add(up => up.Field, hike => hike.Name).Add(up => up.Id, "Trail")
                .Add(up => up.Aggregate, ColumnAggregate.Sum)));

        Assert.Contains("\"Trail\" column can't show the Sum of its String values", error.Message);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("D4")]
    public void Totals_ShouldShowAnAverageOfWholeNumbersToTwoDecimals_WhenTheFormatCantWriteDecimals(string? format)
    {
        var cut = RenderHikes(_ => { }, column => column.Add(up => up.Field, hike => hike.Climb)
                .Add(up => up.Format, format).Add(up => up.Aggregate, ColumnAggregate.Average),
            [Hikes[0] with { Climb = 1 }, Hikes[1] with { Climb = 2 }, Hikes[2] with { Climb = 2 }]);

        Assert.Equal("Average 1.67", cut.Tb().Grid("Hikes").Total("Climb"));
    }

    [Fact]
    public void Totals_ShouldShowNoTotalsRow_WhenTheOnlyColumnWithATotalIsHidden()
    {
        var cut = Render<TbDataGrid<Hike>>(parameters => parameters.Add(grid => grid.Items, Hikes)
            .Add(grid => grid.AriaLabel, "Hikes").Add(grid => grid.KeySelector, hike => hike.Id)
            .Add(grid => grid.HideableColumns, true)
            .Add(grid => grid.State, new GridState { HiddenColumns = ["LengthKm"] })
            .AddChildContent<TbColumn<Hike>>(column => column.Add(name => name.Field, hike => hike.Name))
            .AddChildContent<TbColumn<Hike>>(column => column.Add(km => km.Field, hike => hike.LengthKm)
                .Add(km => km.Aggregate, ColumnAggregate.Sum)));

        Assert.Empty(cut.FindAll("tfoot"));
    }

    [Fact]
    public void Totals_ShouldCountTheTotalsRowAfterTheLastRow_WhenTheGridIsVirtualized()
    {
        var cut = Render<TbDataGrid<Hike>>(parameters => parameters.Add(grid => grid.Items, Hikes)
            .Add(grid => grid.AriaLabel, "Hikes").Add(grid => grid.KeySelector, hike => hike.Id)
            .Add(grid => grid.Virtualize, true).Add(grid => grid.Height, "10rem")
            .AddChildContent<TbColumn<Hike>>(column => column.Add(km => km.Field, hike => hike.LengthKm)
                .Add(km => km.Aggregate, ColumnAggregate.Sum)));

        Assert.Equal("6", cut.Find("table").GetAttribute("aria-rowcount"));
        Assert.Equal("6", cut.Find("tfoot tr").GetAttribute("aria-rowindex"));
    }

    [Fact]
    public async Task Totals_ShouldCoverOnlyTheRowsTheFiltersLeave_WhenRowsAreFiltered()
    {
        var cut = RenderHikes(column => column.Add(km => km.Aggregate, ColumnAggregate.Sum));

        await cut.Tb().Grid("Hikes").FilterAsync("Name", "ri");

        cut.WaitForAssertion(() => Assert.Equal("Sum 14.2", cut.Tb().Grid("Hikes").Total("Length km")));
    }

    [Fact]
    public void Totals_ShouldShowTheValueOfAggregateByWithoutALabel_WhenTheColumnHasOne()
    {
        var cut = RenderHikes(column => column.Add(km => km.AggregateBy,
            rows => rows.Count(hike => hike.Done) / (double)rows.Count * 10));

        Assert.Equal("5.0", cut.Tb().Grid("Hikes").Total("Length km"));
    }

    [Fact]
    public void Totals_ShouldDrawTheFooterTemplate_WhenTheColumnHasOne()
    {
        var cut = RenderHikes(column => column.Add(km => km.Aggregate, ColumnAggregate.Max)
            .Add(km => km.FooterTemplate, total => builder => builder.AddContent(0, $"Longest: {total}")));

        Assert.Equal("Longest: 14.2", cut.Tb().Grid("Hikes").Total("Length km"));
    }

    [Fact]
    public void Totals_ShouldShowNoTotalsRow_WhenNoColumnHasATotal()
    {
        var cut = RenderHikes(_ => { });

        Assert.Empty(cut.FindAll("tfoot"));
    }

    [Fact]
    public void Totals_ShouldAskTheProviderAndShowWhatItGives_WhenTheGridHasADataProvider()
    {
        List<GridRequest> asked = [];
        var cut = Render<TbDataGrid<Hike>>(parameters => parameters.Add(grid => grid.AriaLabel, "Hikes")
            .Add(grid => grid.KeySelector, hike => hike.Id)
            .Add(grid => grid.DataProvider, (GridRequest request, CancellationToken _) =>
            {
                asked.Add(request);
                return Task.FromResult(new GridResult<Hike>(Hikes[..1], 4)
                {
                    Totals = new Dictionary<string, object?> { ["LengthKm"] = 35.6, ["Climb"] = 4L },
                });
            })
            .AddChildContent<TbColumn<Hike>>(column => column.Add(name => name.Field, hike => hike.Name))
            .AddChildContent<TbColumn<Hike>>(column => column.Add(km => km.Field, hike => hike.LengthKm)
                .Add(km => km.Format, "N1").Add(km => km.Aggregate, ColumnAggregate.Sum))
            .AddChildContent<TbColumn<Hike>>(column => column.Add(up => up.Field, hike => hike.Climb)
                .Add(up => up.Format, "N1").Add(up => up.Aggregate, ColumnAggregate.Count)));

        cut.WaitForAssertion(() => Assert.Equal("Sum 35.6", cut.Tb().Grid("Hikes").Total("Length km")));
        Assert.Equal("Count 4", cut.Tb().Grid("Hikes").Total("Climb"));
        var request = Assert.Single(asked);
        Assert.Equal(
            [new GridAggregate("LengthKm", ColumnAggregate.Sum), new GridAggregate("Climb", ColumnAggregate.Count)],
            request.Aggregates);
    }

    [Fact]
    public async Task Totals_ShouldBeCleared_WhenALoadFails()
    {
        var fail = false;
        var cut = Render<TbDataGrid<Hike>>(parameters => parameters.Add(grid => grid.AriaLabel, "Hikes")
            .Add(grid => grid.KeySelector, hike => hike.Id)
            .Add(grid => grid.DataProvider, (GridRequest _, CancellationToken _) => fail
                ? throw new InvalidOperationException("Down")
                : Task.FromResult(new GridResult<Hike>(Hikes, 4)
                {
                    Totals = new Dictionary<string, object?> { ["LengthKm"] = 35.6 },
                }))
            .AddChildContent<TbColumn<Hike>>(column => column.Add(km => km.Field, hike => hike.LengthKm)
                .Add(km => km.Aggregate, ColumnAggregate.Sum)));
        cut.WaitForAssertion(() => Assert.Equal("Sum 35.6", cut.Tb().Grid("Hikes").Total("Length km")));
        fail = true;

        await cut.InvokeAsync(cut.Instance.RefreshAsync);

        cut.WaitForAssertion(() => Assert.Null(cut.Tb().Grid("Hikes").Total("Length km")));
    }
}
