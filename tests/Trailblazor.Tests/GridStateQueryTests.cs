namespace Trailblazor.Tests;

public class GridStateQueryTests
{
    [Fact]
    public void ToQueryParameters_ShouldWriteTheSortsPageAndSize_WhenTheyAreSet()
    {
        var state = new GridState
        {
            Sorts = [new("Name", SortDirection.Ascending), new("Length", SortDirection.Descending)],
            Page = 3,
            PageSize = 20,
        };

        Assert.Equal(new Dictionary<string, object?> { ["sort"] = "Name,-Length", ["page"] = 3, ["size"] = 20 },
            state.ToQueryParameters());
    }

    [Fact]
    public void ToQueryParameters_ShouldLeaveOutWhatIsAtItsDefault_WhenGivenAPrefix()
    {
        Assert.Equal(new Dictionary<string, object?> { ["trails.sort"] = null, ["trails.page"] = null, ["trails.size"] = null },
            new GridState().ToQueryParameters("trails"));
    }

    [Fact]
    public void FromQuery_ShouldReadBackWhatToQueryParametersWrote_WhenGivenTheSamePrefix()
    {
        var state = new GridState
        {
            Sorts = [new("Last hiked", SortDirection.Descending), new("Name", SortDirection.Ascending)],
            Page = 2,
            PageSize = 10,
        };
        var query = "?other=1&" + string.Join('&', state.ToQueryParameters("trails")
            .Select(pair => $"{Uri.EscapeDataString(pair.Key)}={Uri.EscapeDataString($"{pair.Value}")}"));

        Assert.Equal(state, GridState.FromQuery(query, "trails"));
    }

    [Fact]
    public void FromQuery_ShouldKeepTheDefaults_WhenItsValuesCantBeRead()
    {
        Assert.Equal(new GridState(), GridState.FromQuery("?sort=&page=0&size=many"));
    }
}
