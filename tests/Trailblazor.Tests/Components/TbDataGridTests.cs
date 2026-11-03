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
    }

    private sealed record Trail(int Id, string Name, double LengthKm, DateOnly LastHiked);

    private sealed record Town(string City);

    private sealed record Trip(Town From, Town To);

    // A class compared by reference, so a new one never equals the last.
    private sealed class Holder(string name)
    {
        public string Name { get; } = name;
    }

    // A class whose name can change in place, as an entity's can.
    private sealed class Shelter(int id, string name)
    {
        public int Id { get; } = id;

        public string Name { get; set; } = name;
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
        var error = Assert.Throws<InvalidOperationException>(() => RenderTrails([Trails[0], Trails[0] with { }]));

        Assert.Contains("KeySelector", error.Message);
    }

    [Fact]
    public async Task RowClick_ShouldFindTheRowClicked_WhenPlainItemsAreTheirOwnKeys()
    {
        Place? clicked = null;
        var cut = Render<TbDataGrid<Place>>(parameters => parameters
            .Add(grid => grid.Items, [new Place("North"), new Place("South")]).Add(grid => grid.AriaLabel, "Places")
            .Add(grid => grid.OnRowClick, (Place place) => clicked = place)
            .Add(grid => grid.ChildContent, (RenderFragment)(builder =>
            {
                builder.OpenComponent<TbColumn<Place>>(0);
                builder.AddAttribute(1, nameof(TbColumn<Place>.Field), (Expression<Func<Place, object?>>)(place =>
                    place.Name));
                builder.CloseComponent();
            })));
        var keys = cut.FindAll("tr[data-part=row]").Select(row => row.GetAttribute("data-key")!).ToList();

        await ClickAsync(cut.FindAll("tr[data-part=row]")[1]);

        Assert.Equal(2, keys.Distinct().Count());
        Assert.Equal("South", clicked?.Name);
    }

    [Fact]
    public async Task RowClick_ShouldFindTheRowClicked_WhenTwoKeysHaveTheSameText()
    {
        Trail? clicked = null;
        var cut = Render<TbDataGrid<Trail>>(parameters => parameters.Add(grid => grid.Items, [Trails[0], Trails[1]])
            .Add(grid => grid.AriaLabel, "Trails").Add(grid => grid.ChildContent, (RenderFragment)Columns)
            .Add(grid => grid.KeySelector, trail => trail.Id == 2 ? "1" : trail.Id)
            .Add(grid => grid.OnRowClick, (Trail trail) => clicked = trail));
        var keys = cut.FindAll("tr[data-part=row]").Select(row => row.GetAttribute("data-key")!).ToList();

        await ClickAsync(cut.FindAll("tr[data-part=row]")[1]);

        Assert.Equal("1", keys[0]);
        Assert.NotEqual(keys[0], keys[1]);
        Assert.Equal(2, clicked?.Id);
    }

    [Fact]
    public void Render_ShouldNameAColumnByItsLastMember_WhenItsFieldIsntAPlainChain()
    {
        var cut = Render<TbDataGrid<Trail>>(parameters => parameters.Add(grid => grid.Items, Trails)
            .Add(grid => grid.AriaLabel, "Trails").Add(grid => grid.KeySelector, trail => trail.Id)
            .Add(grid => grid.ChildContent, (RenderFragment)(builder =>
            {
                builder.OpenComponent<TbColumn<Trail>>(0);
                builder.AddAttribute(1, nameof(TbColumn<Trail>.Field), (Expression<Func<Trail, object?>>)(trail =>
                    new[] { trail }[0].Name));
                builder.CloseComponent();
            })));

        Assert.Equal("Name", cut.Find("th").GetAttribute("data-column"));
    }

    [Fact]
    public void Render_ShouldKeepARecordKeysFieldsOutOfThePage_WhenTheItemIsItsOwnKey()
    {
        var cut = Render<TbDataGrid<Trail>>(parameters => parameters.Add(grid => grid.Items, Trails)
            .Add(grid => grid.AriaLabel, "Trails").Add(grid => grid.ChildContent, (RenderFragment)Columns));

        Assert.DoesNotContain("LengthKm", cut.Find("tr[data-part=row]").GetAttribute("data-key"));
    }

    [Fact]
    public void Render_ShouldNameColumnsByTheirFieldsPath_WhenTwoFieldsEndInTheSameName()
    {
        var cut = Render<TbDataGrid<Trip>>(parameters => parameters
            .Add(grid => grid.Items, [new Trip(new Town("Bend"), new Town("Sisters"))])
            .Add(grid => grid.AriaLabel, "Trips")
            .Add(grid => grid.KeySelector, trip => trip.From.City)
            .Add(grid => grid.ChildContent, (RenderFragment)(builder =>
            {
                builder.OpenComponent<TbColumn<Trip>>(0);
                builder.AddAttribute(1, nameof(TbColumn<Trip>.Field), (Expression<Func<Trip, object?>>)(trip =>
                    trip.From.City));
                builder.CloseComponent();
                builder.OpenComponent<TbColumn<Trip>>(2);
                builder.AddAttribute(3, nameof(TbColumn<Trip>.Field), (Expression<Func<Trip, object?>>)(trip =>
                    trip.To.City));
                builder.CloseComponent();
            })));

        Assert.Equal(["From.City", "To.City"],
            cut.FindAll("th").Select(header => header.GetAttribute("data-column")!));
    }

    [Fact]
    public void Render_ShouldThrow_WhenTwoColumnsShareAnId()
    {
        var error = Assert.Throws<InvalidOperationException>(() => RenderTrails(configure: parameters => parameters
            .Add(grid => grid.ChildContent, (RenderFragment)(builder =>
            {
                builder.OpenComponent<TbColumn<Trail>>(0);
                builder.AddAttribute(1, nameof(TbColumn<Trail>.Id), "name");
                builder.CloseComponent();
                builder.OpenComponent<TbColumn<Trail>>(2);
                builder.AddAttribute(3, nameof(TbColumn<Trail>.Id), "name");
                builder.CloseComponent();
            }))));

        Assert.Contains("\"name\"", error.Message);
    }

    [Fact]
    public void DataProvider_ShouldThrowNotOfferToTryAgain_WhenTwoRowsHaveTheSameKey()
    {
        var error = Assert.Throws<InvalidOperationException>(() => RenderProvided(_ =>
            Task.FromResult(new GridResult<Trail>([Trails[0], Trails[0] with { }], 2))));

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

        await cut.InvokeAsync(() => cut.Instance.ScrollToAsync(Trails.First(trail => trail.Id == 1)));

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
    public async Task Sort_ShouldMarkTheTableBusy_WhenTheSortsChangedHandlerIsStillWaiting()
    {
        var gate = new TaskCompletionSource();
        var rows = new TaskCompletionSource<GridResult<Trail>>();
        var cut = RenderProvided(request => request.Sorts.Count == 0
                ? Task.FromResult(new GridResult<Trail>(Trails, Trails.Count)) : rows.Task,
            parameters => parameters.Add(grid => grid.SortsChanged, (IReadOnlyList<GridSort> _) => gate.Task));

        var click = cut.Find("th[data-column=Name] button").ClickAsync(new());

        cut.WaitForAssertion(() => Assert.Equal("true", cut.Find("table").GetAttribute("aria-busy")));
        gate.SetResult();
        rows.SetResult(new GridResult<Trail>(Trails, Trails.Count));
        await click;
        cut.WaitForAssertion(() => Assert.Null(cut.Find("table").GetAttribute("aria-busy")));
    }

    [Fact]
    public async Task Page_ShouldMarkTheTableBusy_WhenThePageChangedHandlerIsStillWaiting()
    {
        var gate = new TaskCompletionSource();
        var rows = new TaskCompletionSource<GridResult<Trail>>();
        var cut = RenderProvided(request => request.StartIndex == 0
                ? Task.FromResult(new GridResult<Trail>([.. Trails.Take(2)], Trails.Count)) : rows.Task,
            parameters => parameters.Add(grid => grid.PageChanged, (int _) => gate.Task));

        var click = cut.Find("nav [data-part=next]").ClickAsync(new());

        cut.WaitForAssertion(() => Assert.Equal("true", cut.Find("table").GetAttribute("aria-busy")));
        gate.SetResult();
        rows.SetResult(new GridResult<Trail>([.. Trails.Skip(2).Take(2)], Trails.Count));
        await click;
        cut.WaitForAssertion(() => Assert.Null(cut.Find("table").GetAttribute("aria-busy")));
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
            .Add(grid => grid.SelectionMode, SelectionMode.Multiple).Add(grid => grid.PageSize, 2)
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
    public void Select_ShouldKeepTheOtherRows_WhenOneOfSeveralSelectedRowsIsUnchecked()
    {
        IReadOnlyCollection<Trail> selected = [];
        var cut = RenderTrails(configure: parameters => parameters
            .Add(grid => grid.SelectionMode, SelectionMode.Multiple)
            .Add(grid => grid.SelectedItemsChanged, (IReadOnlyCollection<Trail> changed) => selected = changed));

        Row(cut, "1").QuerySelector("input")!.Change(true);
        Row(cut, "2").QuerySelector("input")!.Change(true);
        Row(cut, "1").QuerySelector("input")!.Change(false);

        Assert.Equal([2], selected.Select(trail => trail.Id));
    }

    [Fact]
    public async Task RowClick_ShouldSelectTheRowAndSayItWasClicked_WhenTheBrowserReportsAClick()
    {
        Trail? clicked = null;
        Trail? selected = null;
        var cut = RenderTrails(configure: parameters => parameters
            .Add(grid => grid.SelectionMode, SelectionMode.Single)
            .Add(grid => grid.OnRowClick, (Trail trail) => clicked = trail)
            .Add(grid => grid.SelectedItemChanged, (Trail? trail) => selected = trail));

        await ClickAsync(Row(cut, "3"));

        Assert.Equal(3, clicked?.Id);
        Assert.Equal(3, selected?.Id);
        Assert.Equal("radio", Row(cut, "3").QuerySelector("input")!.GetAttribute("type"));
        Assert.NotNull(Row(cut, "3").GetAttribute("data-selected"));
    }

    [Fact]
    public async Task RowClick_ShouldLeaveTheClickToTheControl_WhenItLandsOnAControlInTheRow()
    {
        Trail? clicked = null;
        var cut = RenderTrails(configure: parameters => parameters
            .Add(grid => grid.SelectionMode, SelectionMode.Single)
            .Add(grid => grid.OnRowClick, (Trail trail) => clicked = trail));

        await ClickAsync(Row(cut, "3"), onControl: true);

        Assert.Null(clicked);
        Assert.Null(Row(cut, "3").GetAttribute("data-selected"));
    }

    [Fact]
    public async Task Select_ShouldKeepTheRow_WhenOnlySelectedItemIsBoundAndPassedBack()
    {
        var cut = RenderTrails(configure: parameters => parameters
            .Add(grid => grid.SelectionMode, SelectionMode.Single));

        await ClickAsync(Row(cut, "3"));
        cut.Render(parameters => parameters.Add(grid => grid.SelectedItem, Trails[2]));

        Assert.NotNull(Row(cut, "3").GetAttribute("data-selected"));
    }

    [Fact]
    public void Select_ShouldFollowTheKey_WhenTheItemsAreReplacedByNewInstances()
    {
        var cut = RenderTrails(configure: parameters => parameters
            .Add(grid => grid.SelectionMode, SelectionMode.Multiple).Add(grid => grid.SelectedItems, [Trails[1]]));

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
    public async Task DataProvider_ShouldIgnoreAReplacedLoadsFailure_WhenItEndsAfterTheNewerLoad()
    {
        var first = new TaskCompletionSource<GridResult<Trail>>();
        var failures = new List<Exception>();
        var calls = 0;
        var cut = RenderProvided((_, _) => ++calls == 1 ? first.Task
                : Task.FromResult(new GridResult<Trail>([.. Trails.Take(2)], Trails.Count)),
            parameters => parameters.Add(grid => grid.OnLoadFailed, (Exception error) => failures.Add(error)));
        cut.Find("th[data-column=Name] button").Click();

        // The replaced load ends with a provider's own error for the cancel.
        await cut.InvokeAsync(() => first.SetException(new InvalidOperationException("Cancelled upstream")));

        Assert.Empty(failures);
        Assert.Empty(cut.FindAll("[data-part=failed]"));
        Assert.Equal(2, cut.FindAll("tr[data-part=row]").Count);
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
    public void DataProvider_ShouldSayTheLoadFailed_WhenALoadFails()
    {
        var announcer = Render<AnnouncerHost>();

        RenderProvided(_ => throw new InvalidOperationException("Down"));

        announcer.WaitForAssertion(() =>
            Assert.Equal("Couldn't load the rows.", announcer.Find("[role=status] p").TextContent));
    }

    [Fact]
    public void DataProvider_ShouldSayThereIsNothingToShow_WhenALoadFindsNoRows()
    {
        var announcer = Render<AnnouncerHost>();

        RenderProvided(_ => Task.FromResult(new GridResult<Trail>([], 0)));

        announcer.WaitForAssertion(() =>
            Assert.Equal("Nothing to show.", announcer.Find("[role=status] p").TextContent));
    }

    [Fact]
    public void DataProvider_ShouldSayNothing_WhenALoadFindsRows()
    {
        var announcer = Render<AnnouncerHost>();

        RenderProvided(_ => Task.FromResult(new GridResult<Trail>(Trails, Trails.Count)));

        Assert.Empty(announcer.FindAll("[role=status] p"));
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

        await cut.InvokeAsync(() => cut.Instance.ScrollToAsync(Trails.First(trail => trail.Id == 5)));

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

    // The browser reports a row's clicks through its tbrowclick event, saying whether they landed on a control.
    private static Task ClickAsync(IElement row, bool onControl = false) =>
        row.TriggerEventAsync("ontbrowclick", new RowClickEventArgs { OnControl = onControl });

    [Fact]
    public void Sort_ShouldSortTheItemsOnce_WhenTheParentRendersAgainAndARowIsSelected()
    {
        var sorted = 0;
        IReadOnlyCollection<Trail> selected = [];
        RenderFragment columns = builder =>
        {
            Columns(builder);
            builder.OpenComponent<TbColumn<Trail>>(10);
            builder.AddAttribute(11, nameof(TbColumn<Trail>.Id), "length");
            builder.AddAttribute(12, nameof(TbColumn<Trail>.Sortable), true);
            builder.AddAttribute(13, nameof(TbColumn<Trail>.SortBy), (Func<Trail, object?>)(trail =>
            {
                sorted++;
                return trail.LengthKm;
            }));
            builder.CloseComponent();
        };
        var cut = RenderTrails(columns: columns, configure: parameters => parameters
            .Add(grid => grid.Sorts, [new GridSort("length", SortDirection.Ascending)])
            .Add(grid => grid.SelectionMode, SelectionMode.Multiple)
            .Add(grid => grid.SelectedItemsChanged, (IReadOnlyCollection<Trail> changed) => selected = changed));

        cut.Render(parameters => parameters.Add(grid => grid.Striped, true));
        Row(cut, "2").QuerySelector("input")!.Change(true);
        cut.Render(parameters => parameters.Add(grid => grid.SelectedItems, selected));

        Assert.Equal(Trails.Count, sorted);
        Assert.Equal(["Cedar Flats", "Fern Gully", "Pine Hollow", "Lake Camp", "Eagle Ridge"], Names(cut));
    }

    [Fact]
    public void Select_ShouldDrawOnlyTheSelectedRowAgain_WhenTheParentPassesTheSelectionBack()
    {
        var drawn = new List<int>();
        IReadOnlyCollection<Trail> selected = [];
        RenderFragment columns = builder =>
        {
            builder.OpenComponent<TbColumn<Trail>>(0);
            builder.AddAttribute(1, nameof(TbColumn<Trail>.Id), "name");
            builder.AddAttribute(2, nameof(TbColumn<Trail>.Cell), (RenderFragment<Trail>)(trail => cell =>
            {
                drawn.Add(trail.Id);
                cell.AddContent(0, trail.Name);
            }));
            builder.CloseComponent();
        };
        var cut = RenderTrails(columns: columns, configure: parameters => parameters
            .Add(grid => grid.SelectionMode, SelectionMode.Multiple)
            .Add(grid => grid.SelectedItemsChanged, (IReadOnlyCollection<Trail> changed) => selected = changed));
        drawn.Clear();

        Row(cut, "3").QuerySelector("input")!.Change(true);
        cut.Render(parameters => parameters.Add(grid => grid.SelectedItems, selected));

        Assert.Equal([3], drawn);
        Assert.NotNull(Row(cut, "3").GetAttribute("data-selected"));
    }

    [Fact]
    public async Task RenderRows_ShouldDrawTheCellsAgain_WhenWhatTheyShowChanged()
    {
        var editing = 0;
        RenderFragment columns = builder =>
        {
            builder.OpenComponent<TbColumn<Trail>>(0);
            builder.AddAttribute(1, nameof(TbColumn<Trail>.Id), "name");
            builder.AddAttribute(2, nameof(TbColumn<Trail>.Cell), (RenderFragment<Trail>)(trail => cell =>
                cell.AddContent(0, trail.Id == editing ? "Editing" : trail.Name)));
            builder.CloseComponent();
        };
        var cut = RenderTrails(columns: columns);

        editing = 2;
        await cut.InvokeAsync(cut.Instance.RenderRows);

        Assert.Equal("Editing", Row(cut, "2").QuerySelector("td")!.TextContent);
        Assert.Equal("Pine Hollow", Row(cut, "1").QuerySelector("td")!.TextContent);
    }

    [Fact]
    public void RowClass_ShouldRedrawTheRowsWhoseClassChanged_WhenWhatItReadsFromOutsideTheItemChanges()
    {
        var current = 1;
        var cut = RenderTrails(configure: parameters =>
            parameters.Add(grid => grid.RowClass, trail => trail.Id == current ? "is-current" : null));

        current = 2;
        cut.Render();

        Assert.DoesNotContain("is-current", Row(cut, "1").ClassList);
        Assert.Contains("is-current", Row(cut, "2").ClassList);
    }

    [Fact]
    public void RowLabel_ShouldRenameTheRowsWhoseLabelChanged_WhenWhatItReadsFromOutsideTheItemChanges()
    {
        var suffix = "";
        var cut = RenderTrails(configure: parameters => parameters.Add(grid => grid.SelectionMode,
            SelectionMode.Multiple).Add(grid => grid.RowLabel, trail => trail.Name + suffix));

        suffix = " (closed)";
        cut.Render();

        Assert.Equal("Select Pine Hollow (closed)",
            Row(cut, "1").QuerySelector("[data-part=select] input")!.GetAttribute("aria-label"));
    }

    [Fact]
    public async Task Items_ShouldShowChangesMadeInPlace_WhenRefreshed()
    {
        List<Shelter> shelters = [new(1, "Pine Hut"), new(2, "Ridge Hut")];
        var cut = Render<TbDataGrid<Shelter>>(parameters => parameters.Add(grid => grid.Items, shelters)
            .Add(grid => grid.AriaLabel, "Shelters").Add(grid => grid.KeySelector, shelter => shelter.Id)
            .Add(grid => grid.ChildContent, (RenderFragment)(builder =>
            {
                builder.OpenComponent<TbColumn<Shelter>>(0);
                builder.AddAttribute(1, nameof(TbColumn<Shelter>.Field),
                    (Expression<Func<Shelter, object?>>)(shelter => shelter.Name));
                builder.CloseComponent();
            })));
        shelters[0].Name = "Pine Lodge";
        shelters.Add(new(3, "Lake Hut"));

        cut.Render(parameters => parameters.Add(grid => grid.Striped, true));
        Assert.Equal(["Pine Hut", "Ridge Hut"], cut.FindAll("tr[data-part=row] td").Select(cell => cell.TextContent));
        await cut.InvokeAsync(cut.Instance.RefreshAsync);

        Assert.Equal(["Pine Lodge", "Ridge Hut", "Lake Hut"],
            cut.FindAll("tr[data-part=row] td").Select(cell => cell.TextContent));
    }

    [Fact]
    public void Sort_ShouldSortTheItemsOnce_WhenTheParentPassesTheSortsBack()
    {
        var sorted = 0;
        IReadOnlyList<GridSort> sorts = [];
        var cut = RenderTrails(columns: builder =>
        {
            builder.OpenComponent<TbColumn<Trail>>(0);
            builder.AddAttribute(1, nameof(TbColumn<Trail>.Id), "length");
            builder.AddAttribute(2, nameof(TbColumn<Trail>.Sortable), true);
            builder.AddAttribute(3, nameof(TbColumn<Trail>.SortBy), (Func<Trail, object?>)(trail =>
            {
                sorted++;
                return trail.LengthKm;
            }));
            builder.CloseComponent();
        }, configure: parameters => parameters
            .Add(grid => grid.SortsChanged, (IReadOnlyList<GridSort> changed) => sorts = changed));

        cut.Find("th[data-column=length] button").Click();
        cut.Render(parameters => parameters.Add(grid => grid.Sorts, [.. sorts]));

        Assert.Equal(Trails.Count, sorted);
    }

    [Fact]
    public void Render_ShouldDrawEditedItems_WhenTheParentPassesANewCollection()
    {
        List<Shelter> shelters = [new(1, "Pine Hut"), new(2, "Ridge Hut")];
        var cut = Render<TbDataGrid<Shelter>>(parameters => parameters.Add(grid => grid.Items, shelters)
            .Add(grid => grid.AriaLabel, "Shelters").Add(grid => grid.KeySelector, shelter => shelter.Id)
            .Add(grid => grid.ChildContent, (RenderFragment)(builder =>
            {
                builder.OpenComponent<TbColumn<Shelter>>(0);
                builder.AddAttribute(1, nameof(TbColumn<Shelter>.Field),
                    (Expression<Func<Shelter, object?>>)(shelter => shelter.Name));
                builder.CloseComponent();
            })));
        shelters[0].Name = "Pine Lodge";

        cut.Render(parameters => parameters.Add(grid => grid.Items, [.. shelters]));

        Assert.Equal(["Pine Lodge", "Ridge Hut"], cut.FindAll("tr[data-part=row] td").Select(cell => cell.TextContent));
    }

    [Fact]
    public void Render_ShouldDrawARowAgain_WhenItsItemIsReplaced()
    {
        var cut = RenderTrails();

        cut.Render(parameters => parameters.Add(grid => grid.Items,
            [Trails[0] with { Name = "Pine Hollow Loop" }, .. Trails.Skip(1)]));

        Assert.Equal("Pine Hollow Loop", Names(cut)[0]);
    }

    [Fact]
    public void Render_ShouldDropAColumnsCells_WhenTheColumnIsRemoved()
    {
        var showLength = true;
        var cut = RenderTrails(columns: builder =>
        {
            Columns(builder);
            if (showLength)
            {
                builder.OpenComponent<TbColumn<Trail>>(10);
                builder.AddAttribute(11, nameof(TbColumn<Trail>.Id), "length");
                builder.AddAttribute(12, nameof(TbColumn<Trail>.Field),
                    (Expression<Func<Trail, object?>>)(trail => trail.LengthKm));
                builder.CloseComponent();
            }
        });

        showLength = false;
        cut.Render(parameters => parameters.Add(grid => grid.Striped, true));

        Assert.Empty(cut.FindAll("td[data-column=length]"));
    }

    [Fact]
    public void Render_ShouldGiveEveryRowACheckbox_WhenSelectionIsTurnedOn()
    {
        var cut = RenderTrails();

        cut.Render(parameters => parameters.Add(grid => grid.SelectionMode, SelectionMode.Multiple));

        Assert.Equal(Trails.Count, cut.FindAll("tr[data-part=row] input[type=checkbox]").Count);
    }

    [Fact]
    public async Task RowMove_ShouldMoveAmongAllTheItemsAndFollowTheRowToItsPage_WhenAHandleKeyIsPressed()
    {
        List<Trail> trails = [.. Trails];
        var announcer = Render<AnnouncerHost>();
        var cut = RenderTrails(trails, parameters => parameters.Add(grid => grid.PageSize, 2)
            .Add(grid => grid.OnRowMove, (SortableMove<Trail> move) => move.ApplyTo(trails)));
        Assert.Equal("Move Pine Hollow", cut.Find("[data-part=handle] > [data-part=trigger]")
            .GetAttribute("aria-label"));

        await cut.InvokeAsync(() => MoveEvents().OnKey("1", "End"));

        Assert.Equal("Pine Hollow", trails[^1].Name);
        Assert.Equal(["Pine Hollow"], Names(cut));
        announcer.WaitForAssertion(() => Assert.Equal("Pine Hollow moved to position 5 of 5.",
            announcer.FindAll("[role=status] p").Last().TextContent));
    }

    [Fact]
    public async Task RowMove_ShouldCountAmongAllTheItems_WhenTheRowIsOnALaterPage()
    {
        List<Trail> trails = [.. Trails];
        var cut = RenderTrails(trails, parameters => parameters.Add(grid => grid.PageSize, 2)
            .Add(grid => grid.Page, 2).Add(grid => grid.OnRowMove, (SortableMove<Trail> move) => move.ApplyTo(trails)));

        await cut.InvokeAsync(() => MoveEvents().OnKey("4", "Home"));

        Assert.Equal("Fern Gully", trails[0].Name);
        Assert.Equal(["Fern Gully", "Pine Hollow"], Names(cut));
    }

    [Fact]
    public async Task RowMove_ShouldAskFirstAndLeaveTheOrder_WhenTheMoveIsRefused()
    {
        List<Trail> trails = [.. Trails];
        var cut = RenderTrails(trails, parameters => parameters
            .Add(grid => grid.OnRowMove, (SortableMove<Trail> move) => move.ApplyTo(trails))
            .Add(grid => grid.CanMoveRow, move => Task.FromResult(move.To == 0
                ? MoveCheck.Refuse("Pine Hollow stays first.") : MoveCheck.Allow)));

        var refused = await cut.InvokeAsync(() => MoveEvents().OnDragOver("3", "1", before: true));
        await cut.InvokeAsync(() => MoveEvents().OnDrop("3", "1", before: true));
        await cut.InvokeAsync(() => MoveEvents().OnDrop("1", "3", before: false));

        Assert.False(refused.Allowed);
        Assert.Equal(["Eagle Ridge", "Cedar Flats", "Pine Hollow", "Fern Gully", "Lake Camp"], Names(cut));
    }

    [Fact]
    public async Task RowMove_ShouldOfferNoHandles_WhenTheGridIsSorted()
    {
        List<Trail> trails = [.. Trails];
        var cut = RenderTrails(trails, parameters => parameters
            .Add(grid => grid.OnRowMove, (SortableMove<Trail> move) => move.ApplyTo(trails))
            .Add(grid => grid.Sorts, [new GridSort("Name", SortDirection.Ascending)]));

        await cut.InvokeAsync(() => MoveEvents().OnKey("1", "ArrowDown"));

        Assert.Empty(cut.FindAll("[data-part=handle]"));
        Assert.Equal(Trails, trails);
    }

    [Fact]
    public async Task RowMove_ShouldDropAndBringBackTheHandles_WhenTheGridIsSortedAndUnsortedWhileShown()
    {
        List<Trail> trails = [.. Trails];
        var cut = RenderTrails(trails, parameters => parameters
            .Add(grid => grid.OnRowMove, (SortableMove<Trail> move) => move.ApplyTo(trails)));
        var sort = cut.Find("th[data-column=Name] [data-part=sort]");

        await sort.ClickAsync(new());
        Assert.Empty(cut.FindAll("[data-part=handle], [data-part=order]"));
        Assert.All(cut.FindAll("tr[data-part=row]"), row =>
            Assert.Equal(cut.FindAll("thead th").Count, row.QuerySelectorAll("td").Length));
        await cut.Find("th[data-column=Name] [data-part=sort]").ClickAsync(new());
        await cut.Find("th[data-column=Name] [data-part=sort]").ClickAsync(new());

        Assert.Equal(Trails.Count, cut.FindAll("[data-part=handle]").Count);
        Assert.Single(cut.FindAll("[data-part=order]"));
    }

    [Fact]
    public void Render_ShouldMovePinnedColumnsToTheirSides_WhenColumnsArePinned()
    {
        var cut = RenderTrails(columns: PinnedColumns(ColumnPin.End, ColumnPin.None, ColumnPin.Start));

        var headers = cut.FindAll("thead tr:first-child th");
        Assert.Equal(["LastHiked", "LengthKm", "Name"], headers.Select(header => header.GetAttribute("data-column")));
        Assert.Equal(["start", null, "end"], headers.Select(header => header.GetAttribute("data-pinned")));
        Assert.Equal("width: 9rem; inset-inline-start: var(--tb-data-grid-pin-0, 0px)",
            headers[0].GetAttribute("style"));
        Assert.Null(headers[1].GetAttribute("style"));
        var cells = Row(cut, "1").QuerySelectorAll("td");
        Assert.Equal(["LastHiked", "LengthKm", "Name"], cells.Select(cell => cell.GetAttribute("data-column")));
        Assert.Equal(["start", null, "end"], cells.Select(cell => cell.GetAttribute("data-pinned")));
        Assert.Equal("inset-inline-end: var(--tb-data-grid-pin-end-0, 0px)", cells[2].GetAttribute("style"));
    }

    [Fact]
    public void Render_ShouldPinTheOrderAndSelectionColumnsFirst_WhenAColumnIsPinnedAtTheStart()
    {
        List<Trail> trails = [.. Trails];
        var cut = RenderTrails(trails, parameters => parameters.Add(grid => grid.SelectionMode,
            SelectionMode.Multiple).Add(grid => grid.OnRowMove, (SortableMove<Trail> move) => move.ApplyTo(trails)),
            PinnedColumns(ColumnPin.Start, ColumnPin.Start, ColumnPin.None));

        var styles = Enumerable.Range(0, 4).Select(slot => $"inset-inline-start: var(--tb-data-grid-pin-{slot}, 0px)");
        Assert.All(cut.FindAll("tr"), row =>
        {
            var cells = row.Children;
            Assert.Equal(["start", "start", "start", "start", null],
                cells.Select(cell => cell.GetAttribute("data-pinned")));
            Assert.Equal(styles, cells.Take(4).Select(cell => cell.GetAttribute("style")));
        });
    }

    [Fact]
    public void Render_ShouldCountEndPinsFromTheEndAndLeaveTheSelectionColumnUnpinned_WhenOnlyEndIsPinned()
    {
        var cut = RenderTrails(configure: parameters => parameters.Add(grid => grid.SelectionMode,
            SelectionMode.Single), columns: PinnedColumns(ColumnPin.None, ColumnPin.End, ColumnPin.End));

        var cells = Row(cut, "1").Children;
        Assert.Equal([null, null, "end", "end"], cells.Select(cell => cell.GetAttribute("data-pinned")));
        Assert.Equal([null, null, "inset-inline-end: var(--tb-data-grid-pin-end-1, 0px)",
            "inset-inline-end: var(--tb-data-grid-pin-end-0, 0px)"], cells.Select(cell => cell.GetAttribute("style")));
    }

    [Fact]
    public void Render_ShouldMoveTheColumnInEveryRow_WhenItIsPinnedAfterTheFirstRender()
    {
        var cut = RenderTrails(columns: PinnedColumns(ColumnPin.None, ColumnPin.None, ColumnPin.None));

        cut.Render(parameters => parameters.Add(grid => grid.ChildContent,
            PinnedColumns(ColumnPin.None, ColumnPin.Start, ColumnPin.None)));

        Assert.Equal(["LengthKm", "Name", "LastHiked"],
            Row(cut, "1").QuerySelectorAll("td").Select(cell => cell.GetAttribute("data-column")));
        Assert.Equal("start", Row(cut, "1").QuerySelector("td[data-column=LengthKm]")!.GetAttribute("data-pinned"));
    }

    [Theory]
    [InlineData(null, null)]
    [InlineData("10rem", 2)]
    public void Virtualize_ShouldThrow_WhenGivenNoHeightOrAPageSize(string? height, int? pageSize)
    {
        var error = Assert.Throws<InvalidOperationException>(() => RenderTrails(configure: parameters => parameters
            .Add(grid => grid.Virtualize, true).Add(grid => grid.Height, height).Add(grid => grid.PageSize, pageSize)));

        Assert.Contains("Height", error.Message);
    }

    [Fact]
    public void Virtualize_ShouldCountEveryRowAndPlaceEachDrawnOne_WhenItemsAreHeld()
    {
        var cut = RenderTrails(configure: parameters => parameters.Add(grid => grid.Virtualize, true)
            .Add(grid => grid.Height, "10rem").Add(grid => grid.Striped, true)
            .Add(grid => grid.SelectionMode, SelectionMode.Multiple));

        Assert.Equal("6", cut.Find("table").GetAttribute("aria-rowcount"));
        Assert.Equal("2", Row(cut, "1").GetAttribute("aria-rowindex"));
        Assert.Equal("3", Row(cut, "2").GetAttribute("aria-rowindex"));
        Assert.DoesNotContain("tb-data-grid__row--stripe", Row(cut, "1").ClassList);
        Assert.Contains("tb-data-grid__row--stripe", Row(cut, "2").ClassList);
        Assert.Single(cut.FindAll("[data-part=select-all]"));
    }

    [Fact]
    public void Virtualize_ShouldOfferNoBoxThatSelectsEveryRow_WhenAProviderLoadsThem()
    {
        var cut = Render<TbDataGrid<Trail>>(parameters => parameters.Add(grid => grid.AriaLabel, "Trails")
            .Add(grid => grid.DataProvider, (_, _) => Task.FromResult(new GridResult<Trail>(Trails, Trails.Count)))
            .Add(grid => grid.Virtualize, true).Add(grid => grid.Height, "10rem")
            .Add(grid => grid.SelectionMode, SelectionMode.Multiple).Add(grid => grid.ChildContent, Columns));

        Assert.Empty(cut.FindAll("[data-part=select-all]"));
        Assert.Equal("Select", cut.Find("th[data-part=select]").TextContent.Trim());
    }

    [Fact]
    public async Task Virtualize_ShouldAskTheProviderAgainSorted_WhenAColumnIsSorted()
    {
        List<GridRequest> requests = [];
        var cut = RenderVirtualProvided((request, _) =>
        {
            requests.Add(request);
            return Task.FromResult(new GridResult<Trail>([.. Trails.Skip(request.StartIndex)], Trails.Count));
        });
        cut.WaitForAssertion(() => Assert.Equal("Pine Hollow", Names(cut).FirstOrDefault()));

        await cut.Find("th[data-column=Name] [data-part=sort]").ClickAsync(new());

        cut.WaitForAssertion(() => Assert.Equal([new GridSort("Name", SortDirection.Ascending)], requests[^1].Sorts));
        Assert.Equal(0, requests[^1].StartIndex);
    }

    [Fact]
    public void Virtualize_ShouldOfferToTryAgain_WhenTheProviderFails()
    {
        Exception? failed = null;
        var cut = Render<TbDataGrid<Trail>>(parameters => parameters.Add(grid => grid.AriaLabel, "Trails")
            .Add(grid => grid.DataProvider, (_, _) => Task.FromException<GridResult<Trail>>(new IOException("Down")))
            .Add(grid => grid.OnLoadFailed, (Exception exception) => failed = exception)
            .Add(grid => grid.Virtualize, true).Add(grid => grid.Height, "10rem")
            .Add(grid => grid.ChildContent, Columns));

        cut.WaitForAssertion(() => Assert.NotNull(cut.Find("[data-part=failed]")));
        Assert.IsType<IOException>(failed);
        Assert.Null(cut.Find("table").GetAttribute("aria-busy"));
    }

    [Fact]
    public async Task Virtualize_ShouldKeepTheNewerRows_WhenAnOlderLoadFinishesLast()
    {
        List<TaskCompletionSource<GridResult<Trail>>> loads = [];
        var cut = RenderVirtualProvided((_, _) =>
        {
            loads.Add(new());
            return loads[^1].Task;
        });
        cut.WaitForAssertion(() => Assert.Single(loads));
        // The sort doesn't finish until its rows load, so it's awaited after they arrive.
        var sorting = cut.Find("th[data-column=Name] [data-part=sort]").ClickAsync(new());
        cut.WaitForAssertion(() => Assert.Equal(2, loads.Count));

        await cut.InvokeAsync(() => loads[1].SetResult(new(Trails, Trails.Count)));
        await sorting;
        cut.WaitForAssertion(() => Assert.Equal(Trails.Count, Names(cut).Count));
        await cut.InvokeAsync(() => loads[0].SetResult(new([], 0)));

        Assert.Empty(cut.FindAll("[data-part=empty]"));
        Assert.Equal("6", cut.Find("table").GetAttribute("aria-rowcount"));
    }

    [Fact]
    public void Virtualize_ShouldShowNoEmptyRow_WhenALoadReturnsNoRowsButCountsSome()
    {
        var cut = RenderVirtualProvided((_, _) => Task.FromResult(new GridResult<Trail>([], Trails.Count)));

        cut.WaitForAssertion(() => Assert.Equal("6", cut.Find("table").GetAttribute("aria-rowcount")));
        Assert.Empty(cut.FindAll("[data-part=empty]"));
    }

    [Fact]
    public void Virtualize_ShouldSayThereIsNothingToShow_WhenTheFirstLoadFindsNoRows()
    {
        var announcer = Render<AnnouncerHost>();

        var cut = RenderVirtualProvided((_, _) => Task.FromResult(new GridResult<Trail>([], 0)));

        announcer.WaitForAssertion(() =>
            Assert.Equal("Nothing to show.", announcer.Find("[role=status] p").TextContent));
        Assert.NotNull(cut.Find("[data-part=empty]"));
    }

    [Fact]
    public void CellNavigation_ShouldMakeTheTableAGridWhoseRowsSayIfSelected_WhenOn()
    {
        var cut = RenderTrails(configure: parameters => parameters.Add(grid => grid.CellNavigation, true)
            .Add(grid => grid.SelectionMode, SelectionMode.Multiple).Add(grid => grid.SelectedItems, [Trails[1]]));

        Assert.Equal("grid", cut.Find("table").GetAttribute("role"));
        Assert.Equal("true", cut.Find("table").GetAttribute("aria-multiselectable"));
        Assert.Equal("false", Row(cut, "1").GetAttribute("aria-selected"));
        Assert.Equal("true", Row(cut, "2").GetAttribute("aria-selected"));
    }

    [Fact]
    public void CellNavigation_ShouldTakeSelectionOffTheRows_WhenTurnedOff()
    {
        var cut = RenderTrails(configure: parameters => parameters.Add(grid => grid.CellNavigation, true)
            .Add(grid => grid.SelectionMode, SelectionMode.Multiple).Add(grid => grid.SelectedItems, [Trails[1]]));

        cut.Render(parameters => parameters.Add(grid => grid.CellNavigation, false));

        Assert.Null(Row(cut, "2").GetAttribute("aria-selected"));
    }

    [Fact]
    public void CellNavigation_ShouldLeaveAPlainTable_WhenOff()
    {
        var cut = RenderTrails(configure: parameters => parameters
            .Add(grid => grid.SelectionMode, SelectionMode.Multiple).Add(grid => grid.SelectedItems, [Trails[1]]));

        Assert.Null(cut.Find("table").GetAttribute("role"));
        Assert.Null(cut.Find("table").GetAttribute("aria-multiselectable"));
        Assert.Null(Row(cut, "2").GetAttribute("aria-selected"));
    }

    private SortableEvents MoveEvents() =>
        ((DotNetObjectReference<SortableEvents>)JSInterop.Invocations["attachSortable"].Single().Arguments[1]!).Value;

    private static List<string> Names(IRenderedComponent<TbDataGrid<Trail>> cut) =>
        [.. cut.FindAll("tr[data-part=row] td[data-column=Name]").Select(cell => cell.TextContent)];

    private static IElement Row(IRenderedComponent<TbDataGrid<Trail>> cut, string key) =>
        cut.Find($"tr[data-part=row][data-key='{key}']");

    private IRenderedComponent<TbDataGrid<Trail>> RenderTrails(IReadOnlyList<Trail>? trails = null,
        Action<ComponentParameterCollectionBuilder<TbDataGrid<Trail>>>? configure = null,
        RenderFragment? columns = null) =>
        Render<TbDataGrid<Trail>>(parameters =>
        {
            parameters.Add(grid => grid.Items, trails ?? Trails).Add(grid => grid.AriaLabel, "Trails")
                .Add(grid => grid.KeySelector, trail => trail.Id)
                .Add(grid => grid.ChildContent, columns ?? Columns);
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

    private IRenderedComponent<TbDataGrid<Trail>> RenderVirtualProvided(
        Func<GridRequest, CancellationToken, Task<GridResult<Trail>>> load) =>
        Render<TbDataGrid<Trail>>(parameters => parameters.Add(grid => grid.AriaLabel, "Trails")
            .Add(grid => grid.DataProvider, load).Add(grid => grid.Virtualize, true).Add(grid => grid.Height, "10rem")
            .Add(grid => grid.ChildContent, Columns));

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

    // Name, which is filterable, then LengthKm and LastHiked, each pinned as given.
    private static RenderFragment PinnedColumns(ColumnPin name, ColumnPin length, ColumnPin hiked) => builder =>
    {
        builder.OpenComponent<TbColumn<Trail>>(0);
        builder.AddAttribute(1, nameof(TbColumn<Trail>.Field),
            (Expression<Func<Trail, object?>>)(trail => trail.Name));
        builder.AddAttribute(2, nameof(TbColumn<Trail>.Filterable), true);
        builder.AddAttribute(3, nameof(TbColumn<Trail>.Pinned), name);
        builder.CloseComponent();
        builder.OpenComponent<TbColumn<Trail>>(4);
        builder.AddAttribute(5, nameof(TbColumn<Trail>.Field),
            (Expression<Func<Trail, object?>>)(trail => trail.LengthKm));
        builder.AddAttribute(6, nameof(TbColumn<Trail>.Pinned), length);
        builder.CloseComponent();
        builder.OpenComponent<TbColumn<Trail>>(7);
        builder.AddAttribute(8, nameof(TbColumn<Trail>.Field),
            (Expression<Func<Trail, object?>>)(trail => trail.LastHiked));
        builder.AddAttribute(9, nameof(TbColumn<Trail>.Width), "9rem");
        builder.AddAttribute(10, nameof(TbColumn<Trail>.Pinned), hiked);
        builder.CloseComponent();
    };

    // Requests hold lists, which records compare by reference, so their sorts are compared one by one.
    private sealed class RequestComparer : IEqualityComparer<GridRequest>
    {
        public bool Equals(GridRequest? x, GridRequest? y) => x is not null && y is not null
            && x.StartIndex == y.StartIndex && x.Count == y.Count && x.Sorts.SequenceEqual(y.Sorts);

        public int GetHashCode(GridRequest request) => request.StartIndex;
    }
}
