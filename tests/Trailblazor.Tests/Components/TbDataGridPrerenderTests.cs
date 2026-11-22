using System.Linq.Expressions;
using Bunit;
using Bunit.TestDoubles;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;

namespace Trailblazor.Tests.Components;

public class TbDataGridPrerenderTests : BunitContext
{
    private readonly List<GridRequest> _requests = [];
    private BunitPersistentComponentState? _state;

    public TbDataGridPrerenderTests()
    {
        this.AddTrailblazor();
    }

    public sealed record Hike(int Id, string Name, decimal Miles);

    private static readonly Hike[] Hikes =
        [new(1, "Pine Hollow", 4m), new(2, "Eagle Ridge", 8.5m), new(3, "Cedar Flats", 2m), new(4, "Summit Gate", 7m)];

    private BunitPersistentComponentState State => _state ??= AddBunitPersistentComponentState();

    [Fact]
    public void PrerenderKey_ShouldShowTheKeptRowsAndNotAskAgain_WhenTheInteractiveGridAsksForTheSameRows()
    {
        Prerender();

        var cut = RenderGrid(LoadAsync);

        Assert.Single(_requests);
        Assert.Equal(["Pine Hollow", "Eagle Ridge"], Names(cut));
        Assert.Equal("1–2 of 4", cut.Find("[data-part=summary]").TextContent.Trim());
        Assert.Empty(cut.FindAll("[data-part=loading]"));
        Assert.NotEqual("true", cut.Find("table").GetAttribute("aria-busy"));
    }

    [Fact]
    public void PrerenderKey_ShouldAskAgain_WhenTheRequestChangesAfterTheKeptRowsShow()
    {
        Prerender();
        var cut = RenderGrid(LoadAsync);

        cut.Find("th[data-column=Name] button").Click();

        Assert.Equal(2, _requests.Count);
        Assert.Equal([new GridSort("Name", SortDirection.Ascending)], _requests[1].Sorts);
        Assert.Equal(["Cedar Flats", "Eagle Ridge"], Names(cut));
    }

    [Fact]
    public void PrerenderKey_ShouldAskForTheTotalsWithTheKeptRowsInView_WhenAColumnHasAnAggregate()
    {
        Prerender();
        var totals = new TaskCompletionSource<GridResult<Hike>>();

        var cut = RenderGrid(request =>
        {
            _requests.Add(request);
            return totals.Task;
        }, total: true);

        // The kept rows answer a request without totals, so the grid asks for them and shows the rows meanwhile.
        Assert.Equal([new GridAggregate("Miles", ColumnAggregate.Sum)], _requests[1].Aggregates);
        Assert.Equal(["Pine Hollow", "Eagle Ridge"], Names(cut));
        Assert.Empty(cut.FindAll("[data-part=loading]"));
        totals.SetResult(Page(_requests[1]));

        cut.WaitForAssertion(() => Assert.Contains("21.5", cut.Find("[data-part=totals]").TextContent));
    }

    [Fact]
    public void PrerenderKey_ShouldAskAgain_WhenThePrerenderedGridShowedTotals()
    {
        // A second pass while prerendering knows the columns, so it asks for totals, which aren't kept.
        var prerendered = Prerender(total: true, passes: 2);
        Assert.Contains("21.5", prerendered.Find("[data-part=totals]").TextContent);

        var cut = RenderGrid(LoadAsync, total: true);

        Assert.Equal(3, _requests.Count);
        Assert.Equal(["Pine Hollow", "Eagle Ridge"], Names(cut));
        Assert.Contains("21.5", cut.Find("[data-part=totals]").TextContent);
    }

    [Fact]
    public void PrerenderKey_ShouldKeepNothing_WhenThePrerenderedLoadFailed()
    {
        Prerender(_ => Task.FromException<GridResult<Hike>>(new InvalidOperationException("No rows today.")));

        var cut = RenderGrid(LoadAsync);

        Assert.Single(_requests);
        Assert.Equal(["Pine Hollow", "Eagle Ridge"], Names(cut));
    }

    [Fact]
    public void PrerenderKey_ShouldKeepNothing_WhenTheGridIsInteractiveAlready()
    {
        _ = State;
        SetRendererInfo(new RendererInfo("Server", isInteractive: true));
        RenderGrid(LoadAsync);
        State.TriggerOnPersisting();

        RenderGrid(LoadAsync);

        Assert.Equal(2, _requests.Count);
    }

    [Fact]
    public void PrerenderKey_ShouldAskTheProviderAndWarn_WhenWhatWasKeptCannotBeRead()
    {
        var logs = new FakeLogCollector();
        Services.AddSingleton<ILogger<TbDataGrid<Hike>>>(new FakeLogger<TbDataGrid<Hike>>(logs));
        State.Persist("hikes", "not rows");
        SetRendererInfo(new RendererInfo("Server", isInteractive: true));

        var cut = RenderGrid(LoadAsync);

        Assert.Single(_requests);
        Assert.Equal(["Pine Hollow", "Eagle Ridge"], Names(cut));
        var warning = Assert.Single(logs.GetSnapshot(), log => log.Level == LogLevel.Warning);
        Assert.Contains("PrerenderKey", warning.Message);
    }

    [Fact]
    public void PrerenderKey_ShouldLoadAsUsual_WhenTheAppKeepsNoStateAcrossPrerendering()
    {
        // No PersistentComponentState is registered, as in a host that doesn't provide one.
        SetRendererInfo(new RendererInfo("Static", isInteractive: false));
        RenderGrid(LoadAsync);
        SetRendererInfo(new RendererInfo("Server", isInteractive: true));

        var cut = RenderGrid(LoadAsync);

        Assert.Equal(2, _requests.Count);
        Assert.Equal(["Pine Hollow", "Eagle Ridge"], Names(cut));
    }

    // Renders the grid as a static pass, keeps what it persists, and leaves the renderer interactive for the next grid.
    private IRenderedComponent<TbDataGrid<Hike>> Prerender(Func<GridRequest, Task<GridResult<Hike>>>? load = null,
        bool total = false, int passes = 1)
    {
        _ = State;
        SetRendererInfo(new RendererInfo("Static", isInteractive: false));
        var cut = RenderGrid(load ?? LoadAsync, total);
        for (var pass = 1; pass < passes; pass++)
        {
            cut.Render();
        }

        State.TriggerOnPersisting();
        SetRendererInfo(new RendererInfo("Server", isInteractive: true));
        return cut;
    }

    private Task<GridResult<Hike>> LoadAsync(GridRequest request)
    {
        _requests.Add(request);
        return Task.FromResult(Page(request));
    }

    private static GridResult<Hike> Page(GridRequest request)
    {
        var sorted = request.Sorts.Count == 0 ? Hikes : [.. Hikes.OrderBy(hike => hike.Name)];
        return new([.. sorted.Skip(request.StartIndex).Take(request.Count ?? Hikes.Length)], Hikes.Length)
        {
            Totals = request.Aggregates.ToDictionary(asked => asked.Column, _ => (object?)Hikes.Sum(hike => hike.Miles)),
        };
    }

    private static string[] Names(IRenderedComponent<TbDataGrid<Hike>> cut) =>
        [.. cut.FindAll("[data-part=row]").Select(row => row.Children[0].TextContent.Trim())];

    private IRenderedComponent<TbDataGrid<Hike>> RenderGrid(Func<GridRequest, Task<GridResult<Hike>>> load,
        bool total = false) =>
        Render<TbDataGrid<Hike>>(parameters => parameters.Add(grid => grid.DataProvider, (request, _) => load(request))
            .Add(grid => grid.AriaLabel, "Hikes").Add(grid => grid.PageSize, 2).Add(grid => grid.PrerenderKey, "hikes")
            .Add(grid => grid.KeySelector, hike => hike.Id)
            .Add(grid => grid.ChildContent, (RenderFragment)(builder => Columns(builder, total))));

    private static void Columns(RenderTreeBuilder builder, bool total)
    {
        builder.OpenComponent<TbColumn<Hike>>(0);
        builder.AddAttribute(1, nameof(TbColumn<Hike>.Field), (Expression<Func<Hike, object?>>)(hike => hike.Name));
        builder.AddAttribute(2, nameof(TbColumn<Hike>.Sortable), true);
        builder.CloseComponent();
        builder.OpenComponent<TbColumn<Hike>>(3);
        builder.AddAttribute(4, nameof(TbColumn<Hike>.Field), (Expression<Func<Hike, object?>>)(hike => hike.Miles));
        builder.AddAttribute(5, nameof(TbColumn<Hike>.Aggregate), total ? ColumnAggregate.Sum : ColumnAggregate.None);
        builder.CloseComponent();
    }
}
