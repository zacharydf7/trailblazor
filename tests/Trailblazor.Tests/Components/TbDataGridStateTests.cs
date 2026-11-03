using System.Linq.Expressions;
using System.Text.Json;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Trailblazor.Testing;

namespace Trailblazor.Tests.Components;

public class TbDataGridStateTests : BunitContext
{
    public TbDataGridStateTests()
    {
        this.AddTrailblazor();
    }

    public enum Difficulty
    {
        Easy,
        Moderate,
        Hard,
    }

    public sealed record Hike(int Id, string Name, Difficulty Difficulty, double LengthKm, DateOnly Opened);

    private static IReadOnlyList<Hike> Hikes =>
    [
        new(1, "Pine Hollow", Difficulty.Easy, 6.4, new(2019, 5, 1)),
        new(2, "Eagle Ridge", Difficulty.Hard, 14.2, new(2021, 6, 12)),
        new(3, "Cedar Flats", Difficulty.Easy, 3.1, new(2023, 4, 20)),
        new(4, "Ridge Loop", Difficulty.Moderate, 9.8, new(2024, 8, 3)),
        new(5, "Lake Shore", Difficulty.Hard, 11.5, new(2025, 3, 9)),
    ];

    // A state with every kind of value a filter holds, as an app would save it.
    private static GridState Saved => new()
    {
        Sorts = [new GridSort("Name", SortDirection.Descending)],
        Filters =
        [
            new GridFilter("Difficulty", FilterOperator.IsAnyOf, new List<object> { Difficulty.Easy, Difficulty.Hard }),
            new GridFilter("LengthKm", FilterOperator.GreaterThan, 5m),
            new GridFilter("Opened", FilterOperator.GreaterThanOrEqualTo, new DateOnly(2020, 1, 15)),
        ],
        PageSize = 2,
    };

    [Fact]
    public void State_ShouldSortFilterAndPageTheRows_WhenSet()
    {
        var cut = RenderHikes(parameters => parameters.Add(grid => grid.State, Saved with { PageSize = 1, Page = 2 }));

        Assert.Equal(["Eagle Ridge"], Names(cut));
    }

    [Fact]
    public void State_ShouldReadChoicesByNameAndKeepValuesItCantRead_WhenItsJsonWasWrittenElsewhere()
    {
        var state = JsonSerializer.Deserialize<GridState>(
            """{"Filters":[{"Column":"Difficulty","Operator":"IsAnyOf","Value":["hard",2.5]}]}""");

        var cut = RenderHikes(parameters => parameters.Add(grid => grid.State, state));

        Assert.Equal(["Eagle Ridge", "Lake Shore"], Names(cut));
    }

    [Fact]
    public void State_ShouldMatchTheSameRowsAndShowItsFilters_WhenReadBackFromJson()
    {
        var json = JsonSerializer.Serialize(Saved);

        var cut = RenderHikes(parameters => parameters.Add(grid => grid.State,
            JsonSerializer.Deserialize<GridState>(json)));

        Assert.Contains("\"Direction\":\"Descending\"", json);
        Assert.Contains("\"Operator\":\"IsAnyOf\"", json);
        Assert.Equal(["Lake Shore", "Eagle Ridge"], Names(cut));
        Assert.Equal("Easy Hard", cut.Find("td[data-part=filter][data-column=Difficulty]").GetAttribute("data-value"));
    }

    [Fact]
    public async Task State_ShouldGiveAProviderTheColumnsTypes_WhenReadBackFromJson()
    {
        List<GridRequest> requests = [];
        var json = JsonSerializer.Serialize(Saved with { PageSize = null });

        var cut = Render<TbDataGrid<Hike>>(parameters => parameters.Add(grid => grid.AriaLabel, "Hikes")
            .Add(grid => grid.KeySelector, hike => hike.Id)
            .Add(grid => grid.DataProvider, (request, _) =>
            {
                requests.Add(request);
                return Task.FromResult(new GridResult<Hike>([], 0));
            })
            .Add(grid => grid.State, JsonSerializer.Deserialize<GridState>(json))
            .Add(grid => grid.ChildContent, (RenderFragment)Columns));
        await cut.InvokeAsync(() => { });

        var request = Assert.Single(requests);
        Assert.Equal(Saved.Filters, request.Filters);
    }

    [Fact]
    public async Task State_ShouldLoadOnceTheColumnsRender_WhenAProvidersColumnsComeLater()
    {
        List<GridRequest> requests = [];
        var cut = Render<TbDataGrid<Hike>>(parameters => parameters.Add(grid => grid.AriaLabel, "Hikes")
            .Add(grid => grid.KeySelector, hike => hike.Id)
            .Add(grid => grid.DataProvider, (request, _) =>
            {
                requests.Add(request);
                return Task.FromResult(new GridResult<Hike>([], 0));
            })
            .Add(grid => grid.State, JsonSerializer.Deserialize<GridState>(JsonSerializer.Serialize(Saved))));
        await cut.InvokeAsync(() => { });

        Assert.Empty(requests);
        Assert.Single(cut.FindAll("[data-part=loading]"));

        cut.Render(parameters => parameters.Add(grid => grid.ChildContent, (RenderFragment)Columns));

        cut.WaitForAssertion(() => Assert.Equal(Saved.Filters, Assert.Single(requests).Filters));
    }

    [Fact]
    public async Task StateChanged_ShouldCarryEveryChange_WhenTheUserSortsPagesAndFilters()
    {
        List<GridState> states = [];
        var cut = RenderHikes(parameters => parameters.Add(grid => grid.PageSize, 2)
            .Add(grid => grid.StateChanged, (GridState state) => states.Add(state)));
        GridSort[] byName = [new GridSort("Name", SortDirection.Ascending)];

        cut.Find("th[data-column=Name] button").Click();
        cut.Find("[data-part=pager] button[aria-label='Page 2']").Click();
        await cut.Tb().Select("Filter Difficulty").ChooseAsync("Hard");

        Assert.Equal([
            new GridState { Sorts = byName, PageSize = 2 },
            new GridState { Sorts = byName, Page = 2, PageSize = 2 },
            new GridState
            {
                Sorts = byName, Filters = [new GridFilter("Difficulty", FilterOperator.EqualTo, Difficulty.Hard)],
                PageSize = 2,
            },
        ], states);
    }

    [Fact]
    public void State_ShouldKeepClosedGroupsClosed_WhenTheAppPassesTheGridsStateBack()
    {
        GridState? bound = null;
        var cut = RenderHikes(parameters => parameters.Add(grid => grid.Groups, ["Difficulty"])
            .Add(grid => grid.StateChanged, (GridState state) => bound = state));
        cut.Find("th[data-column=Name] button").Click();
        cut.Find("tr[data-part=group] [data-part=group-toggle]").Click();

        cut.Render(parameters => parameters.Add(grid => grid.State, bound! with { }));

        Assert.Equal("false", cut.Find("tr[data-part=group] [data-part=group-toggle]").GetAttribute("aria-expanded"));
    }

    public static TheoryData<string, Type, Type> JsonValues => new()
    {
        { "\"2024-05-01\"", typeof(DateTime), typeof(DateOnly) },
        { "\"2024-05-01T10:30:00\"", typeof(DateTime), typeof(DateTime) },
        { "\"2024-05-01T10:30:00+02:00\"", typeof(DateTimeOffset), typeof(DateTimeOffset) },
        { "\"10:30:00\"", typeof(TimeOnly), typeof(TimeOnly) },
        { "\"6f9619ff-8b86-d011-b42d-00c04fc964ff\"", typeof(Guid), typeof(Guid) },
        { "true", typeof(bool), typeof(bool) },
        { "\"not a day\"", typeof(DateOnly), typeof(string) },
    };

    [Theory, MemberData(nameof(JsonValues))]
    public void FilterJson_ShouldGiveAValueOfTheColumnsType_WhenGivenItsJson(string json, Type column, Type expected)
    {
        var filter = new GridFilter("Column", FilterOperator.EqualTo, JsonDocument.Parse(json).RootElement.Clone());

        var value = FilterJson.Typed(filter, column).Value;

        Assert.IsType(expected, value);
        Assert.Equal(json, JsonSerializer.Serialize(value));
    }

    [Fact]
    public void GridState_ShouldBeEqual_WhenItsListsHoldEqualItems()
    {
        Assert.Equal(Saved, Saved);
        Assert.NotEqual(Saved, Saved with { Groups = ["Difficulty"] });
    }

    [Fact]
    public void GridState_ShouldReadAMissingOrNullListAsEmpty_WhenReadFromJson()
    {
        var state = JsonSerializer.Deserialize<GridState>("""{"Sorts":null,"Page":3}""")!;

        Assert.Equal(new GridState { Page = 3 }, state);
    }

    private static List<string> Names(IRenderedComponent<TbDataGrid<Hike>> cut) =>
        [.. cut.FindAll("tbody td[data-column=Name]").Select(cell => cell.TextContent)];

    private static Expression<Func<Hike, object?>> Field(Expression<Func<Hike, object?>> field) => field;

    private IRenderedComponent<TbDataGrid<Hike>> RenderHikes(
        Action<ComponentParameterCollectionBuilder<TbDataGrid<Hike>>>? configure = null) =>
        Render<TbDataGrid<Hike>>(parameters =>
        {
            parameters.Add(grid => grid.Items, Hikes).Add(grid => grid.AriaLabel, "Hikes")
                .Add(grid => grid.KeySelector, hike => hike.Id)
                .Add(grid => grid.ChildContent, (RenderFragment)Columns);
            configure?.Invoke(parameters);
        });

    private static void Columns(RenderTreeBuilder builder)
    {
        Expression<Func<Hike, object?>>[] fields = [Field(hike => hike.Name), Field(hike => hike.Difficulty),
            Field(hike => hike.LengthKm), Field(hike => hike.Opened)];
        var sequence = 0;
        foreach (var field in fields)
        {
            builder.OpenComponent<TbColumn<Hike>>(sequence++);
            builder.AddAttribute(sequence++, nameof(TbColumn<Hike>.Field), field);
            builder.AddAttribute(sequence++, nameof(TbColumn<Hike>.Sortable), true);
            builder.AddAttribute(sequence++, nameof(TbColumn<Hike>.Filterable), true);
            builder.AddAttribute(sequence++, nameof(TbColumn<Hike>.Groupable), true);
            builder.CloseComponent();
        }
    }
}
