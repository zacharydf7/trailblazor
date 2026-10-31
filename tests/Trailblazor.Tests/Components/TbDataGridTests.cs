using AngleSharp.Dom;
using Bunit;
using Microsoft.AspNetCore.Components;
using System.Globalization;
using System.Linq.Expressions;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;
using Trailblazor.Testing;

namespace Trailblazor.Tests.Components;

public class TbDataGridTests : BunitContext
{
    public TbDataGridTests()
    {
        this.AddTrailblazor();
        SetRendererInfo(new RendererInfo("Server", isInteractive: true));
    }

    private sealed record Trail(int Id, string Name, double LengthKm, DateOnly LastHiked);

    // A class compared by reference, so a new one never equals the last.
    private sealed class Holder(string name)
    {
        public string Name { get; } = name;
    }

    // A plain class, whose text is its type's name, so two of them can't tell rows apart without a key.
    private sealed class Place(string name)
    {
        public string Name { get; } = name;
    }

    private static IReadOnlyList<Trail> Trails =>
    [
        new(1, "Pine Hollow", 6.4, new(2026, 5, 2)),
        new(2, "Eagle Ridge", 14.2, new(2026, 6, 14)),
        new(3, "Cedar Flats", 3.1, new(2026, 4, 20)),
        new(4, "Fern Gully", 5.8, new(2026, 7, 4)),
        new(5, "Lake Camp", 8.5, new(2026, 8, 23)),
    ];

    [Fact]
    public void Render_ShouldTitleFormatAndAlignEachColumn_WhenGivenFields()
    {
        var culture = CultureInfo.CurrentCulture;
        CultureInfo.CurrentCulture = new CultureInfo("en-US");
        try
        {
            var cut = RenderTrails();

            Assert.Equal(["Name", "Length km", "Last hiked"],
                cut.FindAll("th").Select(header => header.TextContent.Trim()));
            var first = cut.FindAll("tr[data-part=row]")[0];
            Assert.Equal(["Pine Hollow", "6.4", "5/2/2026"],
                first.QuerySelectorAll("td").Select(cell => cell.TextContent));
            Assert.Contains("tb-data-grid__cell--end", first.QuerySelector("td[data-column=LengthKm]")!.ClassList);
            Assert.Equal("1", first.GetAttribute("data-key"));
        }
        finally
        {
            CultureInfo.CurrentCulture = culture;
        }
    }

    [Fact]
    public void Render_ShouldThrow_WhenTwoRowsHaveTheSameKey()
    {
        var error = Assert.Throws<InvalidOperationException>(() => Render<TbDataGrid<Place>>(parameters => parameters
            .Add(grid => grid.Items, [new Place("North"), new Place("South")]).Add(grid => grid.AriaLabel, "Places")));

        Assert.Contains("KeySelector", error.Message);
    }

    [Fact]
    public void DataProvider_ShouldThrowNotOfferToTryAgain_WhenTwoRowsHaveTheSameKey()
    {
        var error = Assert.Throws<InvalidOperationException>(() => Render<TbDataGrid<Place>>(parameters => parameters
            .Add(grid => grid.AriaLabel, "Places")
            .Add(grid => grid.DataProvider, (GridRequest _, CancellationToken _) =>
                Task.FromResult(new GridResult<Place>([new("North"), new("South")], 2)))));

        Assert.Contains("KeySelector", error.Message);
    }

    [Fact]
    public void Render_ShouldShowEachColumnsOwnValues_WhenLoopBuiltColumnsAreReordered()
    {
        List<string> order = ["Name", "Length"];
        RenderFragment columns = builder =>
        {
            foreach (var column in order)
            {
                builder.OpenComponent<TbColumn<Trail>>(0);
                builder.AddAttribute(1, nameof(TbColumn<Trail>.Title), column);
                builder.AddAttribute(2, nameof(TbColumn<Trail>.Field), (Expression<Func<Trail, object?>>)(trail =>
                    column == "Name" ? trail.Name : trail.LengthKm.ToString(CultureInfo.InvariantCulture)));
                builder.CloseComponent();
            }
        };
        var cut = Render<TbDataGrid<Trail>>(parameters => parameters.Add(grid => grid.Items, Trails)
            .Add(grid => grid.AriaLabel, "Trails").Add(grid => grid.KeySelector, trail => trail.Id)
            .Add(grid => grid.ChildContent, columns));

        order = ["Length", "Name"];
        cut.Render(parameters => parameters.Add(grid => grid.ChildContent, columns));

        Assert.Equal(["6.4", "Pine Hollow"], Row(cut, "1").QuerySelectorAll("td").Select(cell => cell.TextContent));
    }

    [Fact]
    public void Render_ShouldShowTheField_WhenItGuardsACapturedValueThatIsNull()
    {
        Holder? unit = null;
        var cut = Render<TbDataGrid<Trail>>(parameters => parameters.Add(grid => grid.Items, Trails)
            .Add(grid => grid.AriaLabel, "Trails").Add(grid => grid.KeySelector, trail => trail.Id)
            .Add(grid => grid.ChildContent, (RenderFragment)(builder =>
            {
                builder.OpenComponent<TbColumn<Trail>>(0);
                builder.AddAttribute(1, nameof(TbColumn<Trail>.Id), "name");
                builder.AddAttribute(2, nameof(TbColumn<Trail>.Field),
                    (Expression<Func<Trail, object?>>)(trail => unit == null ? trail.Name : unit.Name));
                builder.CloseComponent();
            })));

        Assert.Equal("Pine Hollow", Row(cut, "1").QuerySelector("td")!.TextContent);
    }

    [Fact]
    public void Render_ShouldShowTheField_WhenItReadsACapturedNullableThatHasAValue()
    {
        int? limit = 5;
        var cut = Render<TbDataGrid<Trail>>(parameters => parameters.Add(grid => grid.Items, Trails)
            .Add(grid => grid.AriaLabel, "Trails").Add(grid => grid.KeySelector, trail => trail.Id)
            .Add(grid => grid.ChildContent, (RenderFragment)(builder =>
            {
                builder.OpenComponent<TbColumn<Trail>>(0);
                builder.AddAttribute(1, nameof(TbColumn<Trail>.Id), "name");
                builder.AddAttribute(2, nameof(TbColumn<Trail>.Field), (Expression<Func<Trail, object?>>)(trail =>
                    limit.HasValue ? trail.Name.Substring(0, limit.Value) : trail.Name));
                builder.CloseComponent();
            })));

        Assert.Equal("Pine ", Row(cut, "1").QuerySelector("td")!.TextContent);
    }

    [Fact]
    public void Render_ShouldStopRendering_WhenAColumnReadsAnObjectMadeAnewOnEveryRender()
    {
        RenderFragment columns = builder =>
        {
            var holder = new Holder("Name");
            builder.OpenComponent<TbColumn<Trail>>(0);
            builder.AddAttribute(1, nameof(TbColumn<Trail>.Id), "name");
            builder.AddAttribute(2, nameof(TbColumn<Trail>.Field),
                (Expression<Func<Trail, object?>>)(trail => holder.Equals(trail) ? null : trail.Name));
            builder.CloseComponent();
        };
        var cut = Render<TbDataGrid<Trail>>(parameters => parameters.Add(grid => grid.Items, Trails)
            .Add(grid => grid.AriaLabel, "Trails").Add(grid => grid.KeySelector, trail => trail.Id)
            .Add(grid => grid.ChildContent, columns));

        cut.Render(parameters => parameters.Add(grid => grid.Striped, true));

        Assert.InRange(cut.RenderCount, 1, 8);
        Assert.Equal("Pine Hollow", Row(cut, "1").QuerySelector("td")!.TextContent);
    }

    [Fact]
    public async Task ScrollTo_ShouldScrollNothing_WhenTheRowWasOnAnotherProviderPage()
    {
        var cut = RenderProvided(request =>
            Task.FromResult(new GridResult<Trail>([.. Trails.Skip(request.StartIndex).Take(2)], Trails.Count)));
        cut.Find("nav [data-part=next]").Click();

        await cut.InvokeAsync(() => cut.Instance.ScrollToAsync(1));

        Assert.Empty(Services.GetRequiredService<FakeBrowserService>().ScrolledTo);
    }

    [Fact]
    public void Page_ShouldSayItMovedToTheLastPage_WhenTheItemsShrinkBelowTheCurrentOne()
    {
        var page = 3;
        var cut = RenderTrails(configure: parameters => parameters.Add(grid => grid.PageSize, 2)
            .Add(grid => grid.Page, 3).Add(grid => grid.PageChanged, (int changed) => page = changed));

        cut.Render(parameters => parameters.Add(grid => grid.Items, Trails.Take(3).ToList()));

        Assert.Equal(2, page);
        Assert.Equal(["Cedar Flats"], Names(cut));
    }

    [Fact]
    public void Sort_ShouldOrderTheRowsTurnThemAroundAndStop_WhenAHeaderIsClickedThreeTimes()
    {
        var cut = RenderTrails();

        cut.Find("th[data-column=Name] button").Click();
        Assert.Equal("ascending", cut.Find("th[data-column=Name]").GetAttribute("aria-sort"));
        Assert.Equal(["Cedar Flats", "Eagle Ridge", "Fern Gully", "Lake Camp", "Pine Hollow"], Names(cut));
        cut.Find("th[data-column=Name] button").Click();
        Assert.Equal("descending", cut.Find("th[data-column=Name]").GetAttribute("aria-sort"));
        Assert.Equal("Pine Hollow", Names(cut)[0]);
        cut.Find("th[data-column=Name] button").Click();

        Assert.Null(cut.Find("th[data-column=Name]").GetAttribute("aria-sort"));
        Assert.Equal(Trails.Select(trail => trail.Name), Names(cut));
    }

    [Fact]
    public void Sort_ShouldBreakTiesWithASecondColumnAndSayItsPlace_WhenShiftClicked()
    {
        IReadOnlyList<GridSort> sorts = [];
        Trail[] trails = [new(1, "B", 2, new(2026, 1, 1)), new(2, "A", 2, new(2026, 1, 2)), new(3, "C", 1, default)];
        var cut = RenderTrails(trails, parameters => parameters
            .Add(grid => grid.SortsChanged, (IReadOnlyList<GridSort> changed) => sorts = changed));

        cut.Find("th[data-column=LengthKm] button").Click();
        cut.Find("th[data-column=Name] button").Click(new MouseEventArgs { ShiftKey = true });

        Assert.Equal([new GridSort("LengthKm", SortDirection.Ascending), new GridSort("Name", SortDirection.Ascending)],
            sorts);
        Assert.Equal(["C", "A", "B"], Names(cut));
        Assert.Null(cut.Find("th[data-column=Name]").GetAttribute("aria-sort"));
        Assert.Contains("sort 2 of 2, ascending", cut.Find("th[data-column=Name] button").TextContent);
    }

    [Fact]
    public void Sort_ShouldSayHowTheRowsAreSorted_WhenAHeaderIsClicked()
    {
        var announcer = Render<AnnouncerHost>();
        var cut = RenderTrails();

        cut.Find("th[data-column=Name] button").Click();

        announcer.WaitForAssertion(() =>
            Assert.Equal("Sorted by Name, ascending.", announcer.Find("[role=status] p").TextContent));
    }

    [Fact]
    public void Page_ShouldShowThePagesRowsAndRange_WhenPagedAndMovedToAnotherPage()
    {
        var page = 1;
        var cut = RenderTrails(configure: parameters => parameters.Add(grid => grid.PageSize, 2)
            .Add(grid => grid.PageChanged, (int changed) => page = changed));
        Assert.Equal(["Pine Hollow", "Eagle Ridge"], Names(cut));
        Assert.Equal("1–2 of 5", cut.Find("[data-part=summary]").TextContent.Trim());

        cut.Find("nav[aria-label='Pages of Trails'] [data-part=next]").Click();

        Assert.Equal(2, page);
        Assert.Equal(["Cedar Flats", "Fern Gully"], Names(cut));
        Assert.Equal("3–4 of 5", cut.Find("[data-part=summary]").TextContent.Trim());
    }

    [Fact]
    public void Render_ShouldSortByTheGivenSorts_WhenTheyNameALaterColumn()
    {
        var cut = RenderTrails(configure: parameters => parameters
            .Add(grid => grid.Sorts, [new GridSort("LengthKm", SortDirection.Ascending)])
            .Add(grid => grid.PageSize, 2));

        Assert.Equal(["Cedar Flats", "Fern Gully"], Names(cut));
        Assert.Equal("ascending", cut.Find("th[data-column=LengthKm]").GetAttribute("aria-sort"));
    }

    [Fact]
    public void Sort_ShouldGoBackToTheFirstPage_WhenThePagedRowsAreSorted()
    {
        var cut = RenderTrails(configure: parameters => parameters.Add(grid => grid.PageSize, 2)
            .Add(grid => grid.Page, 3));

        cut.Find("th[data-column=Name] button").Click();

        Assert.Equal(["Cedar Flats", "Eagle Ridge"], Names(cut));
    }

    [Fact]
    public void Select_ShouldSelectRowsAndTheWholePage_WhenTheirCheckboxesChange()
    {
        IReadOnlyCollection<Trail> selected = [];
        var cut = RenderTrails(configure: parameters => parameters
            .Add(grid => grid.SelectionMode, GridSelectionMode.Multiple).Add(grid => grid.PageSize, 2)
            .Add(grid => grid.SelectedItemsChanged, (IReadOnlyCollection<Trail> changed) => selected = changed));

        Row(cut, "1").QuerySelector("input")!.Change(true);
        Assert.Equal([1], selected.Select(trail => trail.Id));
        Assert.NotNull(cut.Find("[data-part=select-all]").GetAttribute("data-mixed"));
        Assert.Equal("Select Pine Hollow", Row(cut, "1").QuerySelector("input")!.GetAttribute("aria-label"));
        cut.Find("[data-part=select-all]").Change(true);
        Assert.Equal([1, 2], selected.Select(trail => trail.Id));
        cut.Find("[data-part=select-all]").Change(false);

        Assert.Empty(selected);
    }

    [Fact]
    public async Task RowClick_ShouldSelectTheRowAndSayItWasClicked_WhenTheBrowserReportsAClick()
    {
        Trail? clicked = null;
        Trail? selected = null;
        var cut = RenderTrails(configure: parameters => parameters
            .Add(grid => grid.SelectionMode, GridSelectionMode.Single)
            .Add(grid => grid.OnRowClick, (Trail trail) => clicked = trail)
            .Add(grid => grid.SelectedItemChanged, (Trail? trail) => selected = trail));

        await cut.InvokeAsync(() => Events().OnRowClick("3"));

        Assert.Equal(3, clicked?.Id);
        Assert.Equal(3, selected?.Id);
        Assert.Equal("radio", Row(cut, "3").QuerySelector("input")!.GetAttribute("type"));
        Assert.NotNull(Row(cut, "3").GetAttribute("data-selected"));
    }

    [Fact]
    public async Task Select_ShouldKeepTheRow_WhenOnlySelectedItemIsBoundAndPassedBack()
    {
        var cut = RenderTrails(configure: parameters => parameters
            .Add(grid => grid.SelectionMode, GridSelectionMode.Single));

        await cut.InvokeAsync(() => Events().OnRowClick("3"));
        cut.Render(parameters => parameters.Add(grid => grid.SelectedItem, Trails[2]));

        Assert.NotNull(Row(cut, "3").GetAttribute("data-selected"));
    }

    [Fact]
    public void Select_ShouldFollowTheKey_WhenTheItemsAreReplacedByNewInstances()
    {
        var cut = RenderTrails(configure: parameters => parameters
            .Add(grid => grid.SelectionMode, GridSelectionMode.Multiple).Add(grid => grid.SelectedItems, [Trails[1]]));

        cut.Render(parameters => parameters.Add(grid => grid.Items, Trails.Select(trail => trail with { }).ToList()));

        Assert.NotNull(Row(cut, "2").GetAttribute("data-selected"));
    }

    [Fact]
    public async Task DataProvider_ShouldAskForThePageAndSortsOnce_WhenParametersAreSetAgainWithoutAChange()
    {
        var requests = new List<GridRequest>();
        var cut = RenderProvided(request =>
        {
            requests.Add(request);
            return Task.FromResult(new GridResult<Trail>([.. Trails.Skip(request.StartIndex).Take(2)], Trails.Count));
        });
        cut.Render(parameters => parameters.Add(grid => grid.Striped, true));

        cut.Find("th[data-column=Name] button").Click();
        await cut.InvokeAsync(() => cut.Instance.RefreshAsync());

        Assert.Equal([new(0, 2, []), new(0, 2, [new("Name", SortDirection.Ascending)]),
            new(0, 2, [new("Name", SortDirection.Ascending)])], requests, new RequestComparer());
        Assert.Equal("1–2 of 5", cut.Find("[data-part=summary]").TextContent.Trim());
    }

    [Fact]
    public void DataProvider_ShouldCancelTheLastRequest_WhenANewOneStartsFirst()
    {
        var tokens = new List<CancellationToken>();
        var cut = RenderProvided((_, cancel) =>
        {
            tokens.Add(cancel);
            return tokens.Count == 1 ? new TaskCompletionSource<GridResult<Trail>>().Task
                : Task.FromResult(new GridResult<Trail>(Trails, Trails.Count));
        });
        Assert.Equal("true", cut.Find("table").GetAttribute("aria-busy"));
        Assert.Equal("Loading…", cut.Find("[data-part=loading]").TextContent.Trim());

        cut.Find("th[data-column=Name] button").Click();

        Assert.True(tokens[0].IsCancellationRequested);
        Assert.Null(cut.Find("table").GetAttribute("aria-busy"));
    }

    [Fact]
    public void DataProvider_ShouldOfferToTryAgainAndRaiseTheFailure_WhenALoadFails()
    {
        Exception? failure = null;
        var fail = true;
        var cut = RenderProvided(_ => fail ? throw new InvalidOperationException("Down")
                : Task.FromResult(new GridResult<Trail>(Trails, Trails.Count)),
            parameters => parameters.Add(grid => grid.OnLoadFailed, (Exception error) => failure = error));
        Assert.Equal("Down", failure?.Message);
        fail = false;

        cut.Find("[data-part=failed] button").Click();

        Assert.Equal(5, cut.FindAll("tr[data-part=row]").Count);
    }

    [Fact]
    public void Render_ShouldShowTheEmptyContent_WhenThereAreNoRows()
    {
        var cut = RenderTrails([], parameters => parameters
            .Add(grid => grid.EmptyContent, (RenderFragment)(builder => builder.AddContent(0, "No trails yet."))));

        Assert.Equal("No trails yet.", cut.Find("[data-part=empty] td").TextContent.Trim());
        Assert.Equal("3", cut.Find("[data-part=empty] td").GetAttribute("colspan"));
    }

    [Fact]
    public async Task ScrollTo_ShouldGoToTheRowsPageAndScrollToIt_WhenItIsOnAnotherPage()
    {
        var cut = RenderTrails(configure: parameters => parameters.Add(grid => grid.PageSize, 2));

        await cut.InvokeAsync(() => cut.Instance.ScrollToAsync(5));

        Assert.Equal(["Lake Camp"], Names(cut));
        Assert.Single(Services.GetRequiredService<FakeBrowserService>().ScrolledTo);
    }

    [Fact]
    public void Render_ShouldDrawACellTemplateAndAHeaderOfItsOwn_WhenAColumnHasThem()
    {
        var cut = Render<TbDataGrid<Trail>>(parameters => parameters
            .Add(grid => grid.Items, Trails).Add(grid => grid.AriaLabel, "Trails")
            .Add(grid => grid.KeySelector, trail => trail.Id).Add(grid => grid.ChildContent, (RenderFragment)(builder =>
            {
                builder.OpenComponent<TbColumn<Trail>>(0);
                builder.AddAttribute(1, nameof(TbColumn<Trail>.Id), "plan");
                builder.AddAttribute(2, nameof(TbColumn<Trail>.Header),
                    (RenderFragment)(header => header.AddContent(0, "Plan it")));
                builder.AddAttribute(3, nameof(TbColumn<Trail>.Cell), (RenderFragment<Trail>)(trail => cell =>
                    cell.AddMarkupContent(0, $"<button>Plan {trail.Name}</button>")));
                builder.CloseComponent();
            })));

        Assert.Equal("Plan it", cut.Find("th").TextContent.Trim());
        Assert.Equal("Plan Cedar Flats", Row(cut, "3").QuerySelector("button")!.TextContent);
    }

    private DataGridEvents Events() =>
        ((DotNetObjectReference<DataGridEvents>)JSInterop.Invocations["createDataGrid"].Single().Arguments[1]!).Value;

    private static List<string> Names(IRenderedComponent<TbDataGrid<Trail>> cut) =>
        [.. cut.FindAll("tr[data-part=row] td[data-column=Name]").Select(cell => cell.TextContent)];

    private static IElement Row(IRenderedComponent<TbDataGrid<Trail>> cut, string key) =>
        cut.Find($"tr[data-part=row][data-key='{key}']");

    private IRenderedComponent<TbDataGrid<Trail>> RenderTrails(IReadOnlyList<Trail>? trails = null,
        Action<ComponentParameterCollectionBuilder<TbDataGrid<Trail>>>? configure = null) =>
        Render<TbDataGrid<Trail>>(parameters =>
        {
            parameters.Add(grid => grid.Items, trails ?? Trails).Add(grid => grid.AriaLabel, "Trails")
                .Add(grid => grid.KeySelector, trail => trail.Id)
                .Add(grid => grid.ChildContent, (RenderFragment)Columns);
            configure?.Invoke(parameters);
        });

    private IRenderedComponent<TbDataGrid<Trail>> RenderProvided(Func<GridRequest, Task<GridResult<Trail>>> load,
        Action<ComponentParameterCollectionBuilder<TbDataGrid<Trail>>>? configure = null) =>
        RenderProvided((request, _) => load(request), configure);

    private IRenderedComponent<TbDataGrid<Trail>> RenderProvided(
        Func<GridRequest, CancellationToken, Task<GridResult<Trail>>> load,
        Action<ComponentParameterCollectionBuilder<TbDataGrid<Trail>>>? configure = null) =>
        Render<TbDataGrid<Trail>>(parameters =>
        {
            parameters.Add(grid => grid.DataProvider, load).Add(grid => grid.AriaLabel, "Trails")
                .Add(grid => grid.PageSize, 2).Add(grid => grid.ChildContent, (RenderFragment)Columns);
            configure?.Invoke(parameters);
        });

    private static void Columns(RenderTreeBuilder builder)
    {
        builder.OpenComponent<TbColumn<Trail>>(0);
        builder.AddAttribute(1, nameof(TbColumn<Trail>.Field),
            (Expression<Func<Trail, object?>>)(trail => trail.Name));
        builder.AddAttribute(2, nameof(TbColumn<Trail>.Sortable), true);
        builder.CloseComponent();
        builder.OpenComponent<TbColumn<Trail>>(3);
        builder.AddAttribute(4, nameof(TbColumn<Trail>.Field),
            (Expression<Func<Trail, object?>>)(trail => trail.LengthKm));
        builder.AddAttribute(5, nameof(TbColumn<Trail>.Sortable), true);
        builder.CloseComponent();
        builder.OpenComponent<TbColumn<Trail>>(6);
        builder.AddAttribute(7, nameof(TbColumn<Trail>.Field),
            (Expression<Func<Trail, object?>>)(trail => trail.LastHiked));
        builder.AddAttribute(8, nameof(TbColumn<Trail>.Format), "d");
        builder.CloseComponent();
    }

    // Requests hold lists, which records compare by reference, so their sorts are compared one by one.
    private sealed class RequestComparer : IEqualityComparer<GridRequest>
    {
        public bool Equals(GridRequest? x, GridRequest? y) => x is not null && y is not null
            && x.StartIndex == y.StartIndex && x.Count == y.Count && x.Sorts.SequenceEqual(y.Sorts);

        public int GetHashCode(GridRequest request) => request.StartIndex;
    }
}
