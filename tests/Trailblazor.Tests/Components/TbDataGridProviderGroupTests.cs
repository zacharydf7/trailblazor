using System.Linq.Expressions;
using AngleSharp.Dom;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;
using Trailblazor.Testing;

namespace Trailblazor.Tests.Components;

// Groups a DataProvider makes itself, a level at a time, loaded as they open.
public class TbDataGridProviderGroupTests : BunitContext
{
    public TbDataGridProviderGroupTests()
    {
        this.AddTrailblazor();
    }

    public enum Difficulty
    {
        Easy,
        Moderate,
        Hard,
    }

    public sealed record Hike(int Id, string Name, Difficulty Difficulty, bool Loop, int LengthKm);

    private readonly List<Hike> _hikes =
    [
        new(1, "Pine Hollow", Difficulty.Easy, true, 6),
        new(2, "Eagle Ridge", Difficulty.Hard, false, 14),
        new(3, "Cedar Flats", Difficulty.Easy, false, 4),
        new(4, "Ridge Loop", Difficulty.Moderate, true, 9),
    ];

    private readonly List<GridRequest> _requests = [];

    // When set, the provider fails a request for a group's contents.
    private bool _failGroups;

    // When set, each request for a group's contents waits for its own gate, ignoring cancellation as a slow call
    // that wasn't given the token would.
    private bool _holdGroups;

    private readonly List<TaskCompletionSource> _gates = [];

    // Groups as a server would: the requested group's rows, or the groups one level in, each with its count and, when
    // asked, its total length.
    private async Task<GridResult<Hike>> ProvideAsync(GridRequest request, CancellationToken token)
    {
        _requests.Add(request);
        if (request.Group.Count > 0 && _holdGroups)
        {
            var gate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            _gates.Add(gate);
            await gate.Task;
        }

        if (_failGroups && request.Group.Count > 0)
        {
            throw new InvalidOperationException("The server is down.");
        }

        IEnumerable<Hike> rows = _hikes;
        for (var at = 0; at < request.Group.Count; at++)
        {
            var (column, value) = (request.Groups[at], request.Group[at]);
            rows = rows.Where(hike => Equals(ValueOf(hike, column), value));
        }

        if (request.Group.Count < request.Groups.Count)
        {
            var column = request.Groups[request.Group.Count];
            var groups = rows.GroupBy(hike => ValueOf(hike, column)).OrderBy(group => group.Key)
                .Select(group => new GridGroup(group.Key, group.Count())
                {
                    Totals = request.Aggregates.Count == 0 ? new Dictionary<string, object?>()
                        : new Dictionary<string, object?> { ["LengthKm"] = group.Sum(hike => hike.LengthKm) },
                });
            return new([], null) { Groups = [.. groups] };
        }

        var descending = request.Sorts.Any(sort => sort.Direction == SortDirection.Descending);
        return new([.. descending ? rows.OrderByDescending(hike => hike.Name) : rows.OrderBy(hike => hike.Name)],
            null);
    }

    private static object? ValueOf(Hike hike, string column) => column switch
    {
        "Difficulty" => hike.Difficulty,
        "Loop" => hike.Loop,
        _ => null,
    };

    private IRenderedComponent<TbDataGrid<Hike>> RenderHikes(IReadOnlyList<string> groups,
        Action<ComponentParameterCollectionBuilder<TbDataGrid<Hike>>>? configure = null) =>
        Render<TbDataGrid<Hike>>(parameters =>
        {
            parameters.Add(grid => grid.DataProvider, ProvideAsync).Add(grid => grid.AriaLabel, "Hikes")
                .Add(grid => grid.KeySelector, hike => hike.Id).Add(grid => grid.Groups, groups)
                .Add(grid => grid.ChildContent, (RenderFragment)Columns);
            configure?.Invoke(parameters);
        });

    private static string Text(IElement element) => string.Join(" ", element.TextContent.Split(
        (char[]?)null, StringSplitOptions.RemoveEmptyEntries));

    private static List<string> Lines(IRenderedComponent<TbDataGrid<Hike>> cut) =>
        [.. cut.FindAll("tbody tr[data-part=group], tbody tr[data-part=row]").Select(line =>
            line.GetAttribute("data-part") == "group" ? Text(line) : line.QuerySelector("td[data-column=Name]")!
                .TextContent)];

    private static IElement Toggle(IRenderedComponent<TbDataGrid<Hike>> cut, string heading) =>
        cut.FindAll("[data-part=group-toggle]")
            .Single(toggle => Text(toggle).StartsWith(heading, StringComparison.Ordinal));

    private static Task ToggleAsync(IRenderedComponent<TbDataGrid<Hike>> cut, string heading) =>
        Toggle(cut, heading).ClickAsync(new MouseEventArgs { Detail = 1 });

    [Fact]
    public void Groups_ShouldShowTheProvidersGroupsClosed_WhenRowsComeFromAProvider()
    {
        var cut = RenderHikes(["Difficulty"]);

        Assert.Equal(["Difficulty: Easy Rows: 2", "Difficulty: Moderate Rows: 1", "Difficulty: Hard Rows: 1"],
            Lines(cut));
        Assert.All(cut.FindAll("[data-part=group-toggle]"),
            toggle => Assert.Equal("false", toggle.GetAttribute("aria-expanded")));
        var request = Assert.Single(_requests);
        Assert.Equal(["Difficulty"], request.Groups);
        Assert.Empty(request.Group);
    }

    [Fact]
    public async Task GroupToggle_ShouldAskForTheGroupsRowsOnce_WhenTheGroupOpensAgain()
    {
        var cut = RenderHikes(["Difficulty"]);

        await ToggleAsync(cut, "Difficulty: Easy");
        await ToggleAsync(cut, "Difficulty: Easy");
        await ToggleAsync(cut, "Difficulty: Easy");

        Assert.Equal(["Difficulty: Easy Rows: 2", "Cedar Flats", "Pine Hollow", "Difficulty: Moderate Rows: 1",
            "Difficulty: Hard Rows: 1"], Lines(cut));
        Assert.Equal([Difficulty.Easy], Assert.Single(_requests, request => request.Group.Count > 0).Group);
    }

    [Fact]
    public async Task ExpandGroupAsync_ShouldShowTheInnerGroupsThenTheirRows_WhenGivenTwoColumns()
    {
        var cut = RenderHikes(["Loop", "Difficulty"]);
        var grid = cut.Tb().Grid("Hikes");

        await grid.ExpandGroupAsync("Loop: No");
        await grid.ExpandGroupAsync("Difficulty: Hard");

        Assert.Equal(["Loop: No Rows: 2", "Difficulty: Easy Rows: 1", "Difficulty: Hard Rows: 1", "Eagle Ridge",
            "Loop: Yes Rows: 2"], Lines(cut));
        Assert.Equal([false, (object)Difficulty.Hard], _requests[^1].Group);
    }

    [Fact]
    public async Task ExpandGroupAsync_ShouldFindTheInnerGroupByItsOuterOnes_WhenItsHeadingShowsTwice()
    {
        var cut = RenderHikes(["Loop", "Difficulty"]);
        var grid = cut.Tb().Grid("Hikes");
        await grid.ExpandGroupAsync("Loop: No");
        await grid.ExpandGroupAsync("Loop: Yes");

        await Assert.ThrowsAsync<InvalidOperationException>(() => grid.ExpandGroupAsync("Difficulty: Easy"));
        await grid.ExpandGroupAsync("Loop: Yes", "Difficulty: Easy");

        Assert.Equal([true, (object)Difficulty.Easy], _requests[^1].Group);
        Assert.Contains("Pine Hollow", Lines(cut));
        Assert.DoesNotContain("Cedar Flats", Lines(cut));
    }

    [Fact]
    public async Task CollapseGroupAsync_ShouldHideTheGroupsRows_WhenTheGroupIsOpen()
    {
        var cut = RenderHikes(["Difficulty"]);
        var grid = cut.Tb().Grid("Hikes");
        await grid.ExpandGroupAsync("Difficulty: Easy");

        await grid.CollapseGroupAsync("Difficulty: Easy");

        Assert.Empty(cut.FindAll("tbody tr[data-part=row]"));
        Assert.Equal("false", Toggle(cut, "Difficulty: Easy").GetAttribute("aria-expanded"));
    }

    [Fact]
    public async Task GroupToggle_ShouldCloseTheGroupReportItAndSaySo_WhenItsRowsFailToLoad()
    {
        var host = Render<AnnouncerHost>();
        Exception? failed = null;
        _failGroups = true;
        var cut = RenderHikes(["Difficulty"], parameters => parameters.Add(grid => grid.OnLoadFailed,
            (Exception exception) => failed = exception));

        await ToggleAsync(cut, "Difficulty: Easy");

        Assert.Equal("The server is down.", failed?.Message);
        Assert.Equal("false", Toggle(cut, "Difficulty: Easy").GetAttribute("aria-expanded"));
        Assert.Empty(cut.FindAll("tbody tr[data-part=row]"));
        host.WaitForAssertion(() => Assert.Contains("Couldn't load the rows.", host.Find("[role=status]").TextContent));
    }

    [Fact]
    public async Task GroupToggle_ShouldMarkTheHeadingBusy_WhileTheGroupsRowsLoad()
    {
        _holdGroups = true;
        var cut = RenderHikes(["Difficulty"]);

        var opening = ToggleAsync(cut, "Difficulty: Easy");

        var heading = cut.Find("tbody tr[data-part=group]");
        Assert.Equal("true", heading.GetAttribute("aria-busy"));
        Assert.NotNull(heading.QuerySelector(".tb-data-grid__tree-spinner"));
        _gates.Single().SetResult();
        await opening;
        Assert.Null(cut.Find("tbody tr[data-part=group]").GetAttribute("aria-busy"));
    }

    [Fact]
    public async Task GroupToggle_ShouldLoadTheRowsAgain_WhenTheGroupOpensAgainBeforeItsCanceledLoadEnds()
    {
        _holdGroups = true;
        var cut = RenderHikes(["Difficulty"]);
        _ = ToggleAsync(cut, "Difficulty: Easy");
        await ToggleAsync(cut, "Difficulty: Easy");

        var reopening = ToggleAsync(cut, "Difficulty: Easy");
        _gates[0].SetResult();
        _gates[1].SetResult();
        await reopening;

        Assert.Equal(2, _requests.Count(request => request.Group.Count > 0));
        Assert.Equal(["Difficulty: Easy Rows: 2", "Cedar Flats", "Pine Hollow", "Difficulty: Moderate Rows: 1",
            "Difficulty: Hard Rows: 1"], Lines(cut));
    }

    [Fact]
    public async Task Sorts_ShouldLoadTheOpenGroupsAgain_WhenTheRowsSortAnotherWay()
    {
        var cut = RenderHikes(["Difficulty"]);
        await ToggleAsync(cut, "Difficulty: Easy");

        cut.Render(parameters => parameters.Add(grid => grid.Sorts,
            [new GridSort("Name", SortDirection.Descending)]));

        cut.WaitForAssertion(() => Assert.Equal(["Difficulty: Easy Rows: 2", "Pine Hollow", "Cedar Flats",
            "Difficulty: Moderate Rows: 1", "Difficulty: Hard Rows: 1"], Lines(cut)));
    }

    [Fact]
    public async Task Sorts_ShouldLoadTheGroupAgain_WhenTheRowsSortWhileItsRowsLoad()
    {
        _holdGroups = true;
        var cut = RenderHikes(["Difficulty"]);
        _ = ToggleAsync(cut, "Difficulty: Easy");

        cut.Render(parameters => parameters.Add(grid => grid.Sorts,
            [new GridSort("Name", SortDirection.Descending)]));
        cut.WaitForAssertion(() => Assert.Equal(2, _gates.Count));
        _gates[0].SetResult();
        _gates[1].SetResult();

        cut.WaitForAssertion(() => Assert.Equal(["Difficulty: Easy Rows: 2", "Pine Hollow", "Cedar Flats",
            "Difficulty: Moderate Rows: 1", "Difficulty: Hard Rows: 1"], Lines(cut)));
    }

    [Fact]
    public async Task Refresh_ShouldCloseAnOpenGroup_WhenTheRowsNoLongerHoldIt()
    {
        var cut = RenderHikes(["Difficulty"]);
        await ToggleAsync(cut, "Difficulty: Moderate");
        var moderate = _hikes.Single(hike => hike.Difficulty == Difficulty.Moderate);

        _hikes.Remove(moderate);
        await cut.InvokeAsync(() => cut.Instance.RefreshAsync());
        _hikes.Add(moderate);
        await cut.InvokeAsync(() => cut.Instance.RefreshAsync());

        Assert.Equal("false", Toggle(cut, "Difficulty: Moderate").GetAttribute("aria-expanded"));
        Assert.Empty(cut.FindAll("tbody tr[data-part=row]"));
    }

    [Fact]
    public async Task GroupTotals_ShouldShowEachGroupsTotalsFromTheProvider_WhenTheGroupIsOpen()
    {
        var cut = RenderHikes(["Difficulty"], parameters => parameters.Add(grid => grid.GroupTotals, true));

        await ToggleAsync(cut, "Difficulty: Easy");

        Assert.Equal(["Sum 10"], cut.Tb().Grid("Hikes").GroupTotals("Length km"));
    }

    [Fact]
    public async Task Groups_ShouldCountHeadingsAmongAPagesRows_WhenPaged()
    {
        var cut = RenderHikes(["Difficulty"], parameters => parameters.Add(grid => grid.PageSize, 2));

        await ToggleAsync(cut, "Difficulty: Easy");

        Assert.Equal(["Difficulty: Easy Rows: 2", "Cedar Flats"], Lines(cut));
    }

    [Fact]
    public async Task Refresh_ShouldSayThePageMoved_WhenFewerLinesLeaveThePagePastTheLast()
    {
        int? page = null;
        var cut = RenderHikes(["Difficulty"], parameters => parameters.Add(grid => grid.PageSize, 2)
            .Add(grid => grid.PageChanged, (int changed) => page = changed));
        await ToggleAsync(cut, "Difficulty: Easy");
        await cut.Tb().Grid("Hikes").GoToPageAsync(2);

        _hikes.RemoveAll(hike => hike.Name != "Pine Hollow");
        await cut.InvokeAsync(() => cut.Instance.RefreshAsync());

        Assert.Equal(1, page);
        Assert.Equal(["Difficulty: Easy Rows: 1", "Pine Hollow"], Lines(cut));
    }

    [Fact]
    public async Task ExportCsvAsync_ShouldWriteTheRowsGrouped_WhenTheProviderGroupsThem()
    {
        var cut = RenderHikes(["Difficulty"]);

        var csv = await cut.InvokeAsync(() => cut.Instance.ExportCsvAsync());

        var names = csv.Split('\n').Skip(1).Where(line => line.Length > 0).Select(line => line.Split(',')[0]);
        Assert.Equal(["Cedar Flats", "Pine Hollow", "Ridge Loop", "Eagle Ridge"], names);
    }

    [Fact]
    public void GroupBy_ShouldBeOffered_WhenRowsComeFromAProvider()
    {
        var cut = RenderHikes([]);

        Assert.NotNull(cut.Find("[data-part=group-by]"));
    }

    private static Expression<Func<Hike, object?>> Field(Expression<Func<Hike, object?>> field) => field;

    private static void Columns(RenderTreeBuilder builder)
    {
        builder.OpenComponent<TbColumn<Hike>>(0);
        builder.AddAttribute(1, nameof(TbColumn<Hike>.Field), Field(hike => hike.Name));
        builder.AddAttribute(2, nameof(TbColumn<Hike>.Sortable), true);
        builder.CloseComponent();
        builder.OpenComponent<TbColumn<Hike>>(3);
        builder.AddAttribute(4, nameof(TbColumn<Hike>.Field), Field(hike => hike.Difficulty));
        builder.AddAttribute(5, nameof(TbColumn<Hike>.Groupable), true);
        builder.CloseComponent();
        builder.OpenComponent<TbColumn<Hike>>(6);
        builder.AddAttribute(7, nameof(TbColumn<Hike>.Field), Field(hike => hike.Loop));
        builder.AddAttribute(8, nameof(TbColumn<Hike>.Groupable), true);
        builder.CloseComponent();
        builder.OpenComponent<TbColumn<Hike>>(9);
        builder.AddAttribute(10, nameof(TbColumn<Hike>.Field), Field(hike => hike.LengthKm));
        builder.AddAttribute(11, nameof(TbColumn<Hike>.Aggregate), ColumnAggregate.Sum);
        builder.CloseComponent();
    }
}
