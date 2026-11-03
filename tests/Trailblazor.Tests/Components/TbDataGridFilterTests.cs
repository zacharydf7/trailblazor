using System.Globalization;
using System.Linq.Expressions;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Trailblazor.Testing;

namespace Trailblazor.Tests.Components;

public class TbDataGridFilterTests : BunitContext
{
    public TbDataGridFilterTests()
    {
        this.AddTrailblazor();
    }

    public enum Difficulty
    {
        Easy,
        Moderate,
        Hard,
    }

    public sealed record Hike(int Id, string Name, double LengthKm, DateOnly Date, Difficulty Difficulty, bool Loop);

    private static IReadOnlyList<Hike> Hikes =>
    [
        new(1, "Pine Hollow", 6.4, new(2026, 5, 2), Difficulty.Easy, true),
        new(2, "Eagle Ridge", 14.2, new(2026, 6, 14), Difficulty.Hard, false),
        new(3, "Cedar Flats", 3.1, new(2026, 4, 20), Difficulty.Easy, false),
        new(4, "Ridge Loop", 8.5, new(2026, 8, 23), Difficulty.Moderate, true),
    ];

    [Fact]
    public void FilterRow_ShouldNameAFieldForEachFilterableColumn_WhenRendered()
    {
        var cut = RenderHikes(filterable: id => id != "Date");

        var labels = cut.FindAll("[data-part=filter] label").Select(label => label.TextContent.Trim());

        Assert.Equal(["Filter Name", "Filter Length km", "Filter Difficulty", "Filter Loop"], labels);
        Assert.Empty(cut.Find("[data-part=filter][data-column=Date]").Children);
    }

    [Fact]
    public void FilterRow_ShouldNotShow_WhenNoColumnIsFilterable()
    {
        var cut = RenderHikes(filterable: _ => false);

        Assert.Empty(cut.FindAll("[data-part=filters]"));
    }

    [Fact]
    public async Task TextFilter_ShouldShowRowsWhoseTextContainsItIgnoringCase_WhenTyped()
    {
        IReadOnlyList<GridFilter>? filters = null;
        var cut = RenderHikes(configure: parameters => parameters
            .Add(grid => grid.FiltersChanged, (IReadOnlyList<GridFilter> changed) => filters = changed));

        await cut.Tb().Grid("Hikes").FilterAsync("Name", "RIDGE");

        Assert.Equal(["Eagle Ridge", "Ridge Loop"], Names(cut));
        Assert.Equal([new GridFilter("Name", FilterOperator.Contains, "RIDGE")], filters);
        Assert.Equal("", cut.Find("th[data-column=Name]").GetAttribute("data-filtered"));
    }

    [Fact]
    public async Task NumberFilter_ShouldShowRowsWithThatNumber_WhenEntered()
    {
        var cut = RenderHikes();

        await cut.Tb().Grid("Hikes").FilterAsync("Length km", "8.5");

        Assert.Equal(["Ridge Loop"], Names(cut));
    }

    [Fact]
    public async Task ChoiceFilter_ShouldShowRowsWithTheChosenEnumValue_WhenChosen()
    {
        var cut = RenderHikes();

        await cut.Tb().Select("Filter Difficulty").ChooseAsync("Easy");

        Assert.Equal(["Pine Hollow", "Cedar Flats"], Names(cut));
    }

    [Fact]
    public async Task ChoiceFilter_ShouldShowRowsThatAreNot_WhenNoIsChosenForAYesOrNoColumn()
    {
        IReadOnlyList<GridFilter>? filters = null;
        var cut = RenderHikes(configure: parameters => parameters
            .Add(grid => grid.FiltersChanged, (IReadOnlyList<GridFilter> changed) => filters = changed));

        await cut.Tb().Select("Filter Loop").ChooseAsync("No");

        Assert.Equal(["Eagle Ridge", "Cedar Flats"], Names(cut));
        Assert.Equal([new GridFilter("Loop", FilterOperator.EqualTo, false)], filters);
    }

    [Fact]
    public void DateFilter_ShouldShowRowsInTheRangeWithAFilterForEachEnd_WhenBothEndsAreTyped()
    {
        var culture = CultureInfo.CurrentCulture;
        CultureInfo.CurrentCulture = new CultureInfo("en-US");
        try
        {
            IReadOnlyList<GridFilter>? filters = null;
            var cut = RenderHikes(configure: parameters => parameters
                .Add(grid => grid.FiltersChanged, (IReadOnlyList<GridFilter> changed) => filters = changed));

            cut.Find("[data-part=filter][data-column=Date] [data-part=start-input]").Change("5/1/2026");
            cut.Find("[data-part=filter][data-column=Date] [data-part=end-input]").Change("6/30/2026");

            Assert.Equal(["Pine Hollow", "Eagle Ridge"], Names(cut));
            Assert.Equal([new GridFilter("Date", FilterOperator.GreaterThanOrEqualTo, new DateOnly(2026, 5, 1)),
                new GridFilter("Date", FilterOperator.LessThanOrEqualTo, new DateOnly(2026, 6, 30))], filters);
        }
        finally
        {
            CultureInfo.CurrentCulture = culture;
        }
    }

    [Fact]
    public void Filters_ShouldShowOnlyRowsInTheRangeAndFillTheFields_WhenSetByTheParent()
    {
        var cut = RenderHikes(configure: parameters => parameters.Add(grid => grid.Filters,
        [
            new GridFilter("Date", FilterOperator.GreaterThanOrEqualTo, new DateOnly(2026, 5, 1)),
            new GridFilter("Date", FilterOperator.LessThanOrEqualTo, new DateOnly(2026, 6, 30)),
            new GridFilter("Name", FilterOperator.Contains, "e"),
        ]));

        Assert.Equal(["Pine Hollow", "Eagle Ridge"], Names(cut));
        Assert.Equal("e", cut.Find("[data-part=filter][data-column=Name] input").GetAttribute("value"));
        Assert.Equal("", cut.Find("th[data-column=Date]").GetAttribute("data-filtered"));
    }

    [Fact]
    public async Task TextFilter_ShouldMatchTheFilterByValue_WhenTheColumnHasOne()
    {
        var cut = RenderHikes(columns: builder =>
        {
            builder.OpenComponent<TbColumn<Hike>>(0);
            builder.AddAttribute(1, nameof(TbColumn<Hike>.Field),
                (Expression<Func<Hike, object?>>)(hike => hike.Name));
            builder.AddAttribute(2, nameof(TbColumn<Hike>.Filterable), true);
            builder.AddAttribute(3, nameof(TbColumn<Hike>.FilterBy),
                (Func<Hike, object?>)(hike => hike.Difficulty.ToString()));
            builder.CloseComponent();
        });

        await cut.Tb().Grid("Hikes").FilterAsync("Name", "easy");

        Assert.Equal(["Pine Hollow", "Cedar Flats"], Names(cut));
    }

    [Fact]
    public async Task TextFilter_ShouldFilter_WhenTheColumnHasOnlyAFilterByAndACell()
    {
        var cut = RenderHikes(columns: builder =>
        {
            builder.OpenComponent<TbColumn<Hike>>(0);
            builder.AddAttribute(1, nameof(TbColumn<Hike>.Title), "Trail");
            builder.AddAttribute(2, nameof(TbColumn<Hike>.Filterable), true);
            builder.AddAttribute(3, nameof(TbColumn<Hike>.FilterBy), (Func<Hike, object?>)(hike => hike.Name));
            builder.AddAttribute(4, nameof(TbColumn<Hike>.Cell),
                (RenderFragment<Hike>)(hike => cell => cell.AddContent(0, hike.Name)));
            builder.CloseComponent();
        });

        await cut.Tb().Grid("Hikes").FilterAsync("Trail", "ridge");

        Assert.Equal(["Eagle Ridge", "Ridge Loop"],
            cut.FindAll("tr[data-part=row] td[data-column=Trail]").Select(cell => cell.TextContent));
    }

    [Fact]
    public async Task Reorder_ShouldNotBeOffered_WhenAFilterHidesRows()
    {
        var cut = RenderHikes(configure: parameters => parameters
            .Add(grid => grid.OnRowMove, (SortableMove<Hike> _) => { }));
        Assert.NotEmpty(cut.FindAll("[data-part=order]"));

        await cut.Tb().Grid("Hikes").FilterAsync("Name", "ridge");

        Assert.Empty(cut.FindAll("[data-part=order]"));
        Assert.Empty(cut.FindAll("tr[data-part=row] .tb-data-grid__handle"));
    }

    [Fact]
    public void FilterCell_ShouldHoldTheFilterAsInvariantText_WhenTheCultureWritesNumbersOtherwise()
    {
        var culture = CultureInfo.CurrentCulture;
        CultureInfo.CurrentCulture = new CultureInfo("de-DE");
        try
        {
            var cut = RenderHikes(configure: parameters => parameters.Add(grid => grid.Filters,
                [new GridFilter("LengthKm", FilterOperator.EqualTo, 14.2m)]));

            Assert.Equal("14.2", cut.Find("[data-part=filter][data-column=LengthKm]").GetAttribute("data-value"));
        }
        finally
        {
            CultureInfo.CurrentCulture = culture;
        }
    }

    [Theory]
    [InlineData('x')]
    [InlineData(double.NaN)]
    [InlineData(1e30)]
    public void NumberFilter_ShouldStayEmpty_WhenTheParentsValueIsntADecimal(object value)
    {
        var cut = RenderHikes(configure: parameters => parameters.Add(grid => grid.Filters,
            [new GridFilter("LengthKm", FilterOperator.EqualTo, value)]));

        Assert.Equal("", cut.Find("[data-part=filter][data-column=LengthKm] input").GetAttribute("value") ?? "");
    }

    [Fact]
    public void Filters_ShouldHideNoRows_WhenTheyNameAColumnThatIsntThere()
    {
        var cut = RenderHikes(configure: parameters => parameters.Add(grid => grid.Filters,
            [new GridFilter("Elevation", FilterOperator.GreaterThan, 1000)]));

        Assert.Equal(4, Names(cut).Count);
    }

    [Fact]
    public async Task Filter_ShouldGoBackToTheFirstPageAndCountTheRowsAloud_WhenChanged()
    {
        var announcer = Render<AnnouncerHost>();
        var pages = new List<int>();
        var cut = RenderHikes(configure: parameters => parameters.Add(grid => grid.PageSize, 1)
            .Add(grid => grid.Page, 3).Add(grid => grid.PageChanged, (int page) => pages.Add(page)));

        await cut.Tb().Grid("Hikes").FilterAsync("Name", "ridge");

        Assert.Equal([1], pages);
        Assert.Equal(["Eagle Ridge"], Names(cut));
        announcer.WaitForAssertion(() =>
            Assert.Equal("Rows shown: 2.", announcer.Find("[role=status] p").TextContent));
    }

    [Fact]
    public async Task DataProvider_ShouldBeAskedForTheFilteredRowsFromTheFirstPage_WhenAFilterChanges()
    {
        var requests = new List<GridRequest>();
        var cut = Render<TbDataGrid<Hike>>(parameters => parameters.Add(grid => grid.AriaLabel, "Hikes")
            .Add(grid => grid.PageSize, 2).Add(grid => grid.Page, 2).Add(grid => grid.ChildContent, Columns(_ => true))
            .Add(grid => grid.DataProvider, (GridRequest request, CancellationToken _) =>
            {
                requests.Add(request);
                return Task.FromResult(new GridResult<Hike>([.. Hikes.Take(2)], Hikes.Count));
            }));

        await cut.Tb().Grid("Hikes").FilterAsync("Name", "ridge");

        var last = requests[^1];
        Assert.Equal(0, last.StartIndex);
        Assert.Equal([new GridFilter("Name", FilterOperator.Contains, "ridge")], last.Filters);
    }

    private static List<string> Names(IRenderedComponent<TbDataGrid<Hike>> cut) =>
        [.. cut.FindAll("tr[data-part=row] td[data-column=Name]").Select(cell => cell.TextContent)];

    private IRenderedComponent<TbDataGrid<Hike>> RenderHikes(Func<string, bool>? filterable = null,
        Action<ComponentParameterCollectionBuilder<TbDataGrid<Hike>>>? configure = null,
        RenderFragment? columns = null) =>
        Render<TbDataGrid<Hike>>(parameters =>
        {
            parameters.Add(grid => grid.Items, Hikes).Add(grid => grid.AriaLabel, "Hikes")
                .Add(grid => grid.KeySelector, hike => hike.Id)
                .Add(grid => grid.ChildContent, columns ?? Columns(filterable ?? (_ => true)));
            configure?.Invoke(parameters);
        });

    // A column for each of a hike's values, filterable when the test says so for its id.
    private static RenderFragment Columns(Func<string, bool> filterable) => builder =>
    {
        Column(builder, hike => hike.Name, "Name", filterable);
        Column(builder, hike => hike.LengthKm, "LengthKm", filterable);
        Column(builder, hike => hike.Date, "Date", filterable);
        Column(builder, hike => hike.Difficulty, "Difficulty", filterable);
        Column(builder, hike => hike.Loop, "Loop", filterable);
    };

    private static void Column(RenderTreeBuilder builder, Expression<Func<Hike, object?>> field, string id,
        Func<string, bool> filterable)
    {
        builder.OpenComponent<TbColumn<Hike>>(0);
        builder.SetKey(id);
        builder.AddAttribute(1, nameof(TbColumn<Hike>.Field), field);
        builder.AddAttribute(2, nameof(TbColumn<Hike>.Filterable), filterable(id));
        builder.CloseComponent();
    }
}
