using Bunit;
using Microsoft.AspNetCore.Components;
using Trailblazor.Testing;

namespace Trailblazor.Tests.Components;

// A provider that doesn't count its rows says only whether more follow each page.
public class TbDataGridUncountedTests : BunitContext
{
    private static readonly string[] Trails = ["Pine Hollow", "Eagle Ridge", "Cedar Flats", "Ridge Loop", "Aspen Run"];

    public TbDataGridUncountedTests()
    {
        this.AddTrailblazor();
        JSInterop.Mode = JSRuntimeMode.Loose;
    }

    [Fact]
    public void Pager_ShouldOfferTheNextPageAndCountNoTotal_WhenMoreFollow()
    {
        var cut = RenderTrails();

        Assert.Equal("1–2 of many", cut.Find("[data-part=summary]").TextContent.Trim());
        Assert.Equal("2", cut.Find("[data-part=pager] nav").GetAttribute("data-page-count"));
        Assert.Empty(cut.FindAll("[data-part=pager] [data-part=last]"));
    }

    [Fact]
    public async Task Pager_ShouldOfferOneMorePage_WhenTheNextPageAlsoHasMore()
    {
        var cut = RenderTrails();

        await cut.Tb().Grid("Trails").GoToPageAsync(2);

        Assert.Equal("3–4 of many", cut.Find("[data-part=summary]").TextContent.Trim());
        Assert.Equal("3", cut.Find("[data-part=pager] nav").GetAttribute("data-page-count"));
    }

    [Fact]
    public async Task Pager_ShouldCountTheRows_WhenTheLastPageSaysNoMoreFollow()
    {
        var cut = RenderTrails();
        await cut.Tb().Grid("Trails").GoToPageAsync(2);

        await cut.Tb().Grid("Trails").GoToPageAsync(3);

        Assert.Equal("5–5 of 5", cut.Find("[data-part=summary]").TextContent.Trim());
        Assert.Equal("3", cut.Find("[data-part=pager] nav").GetAttribute("data-page-count"));
    }

    [Fact]
    public async Task Pager_ShouldKeepThePagesAndCountItLearned_WhenTheUserGoesBack()
    {
        var cut = RenderTrails();
        var trails = cut.Tb().Grid("Trails");
        await trails.GoToPageAsync(2);
        await trails.GoToPageAsync(3);

        await trails.GoToPageAsync(1);

        Assert.Equal("1–2 of 5", cut.Find("[data-part=summary]").TextContent.Trim());
        Assert.Equal("3", cut.Find("[data-part=pager] nav").GetAttribute("data-page-count"));
    }

    [Fact]
    public async Task Pager_ShouldStartCountingAgain_WhenThePageSizeChanges()
    {
        var size = 2;
        var cut = RenderTrails(pageSize: size);
        var trails = cut.Tb().Grid("Trails");
        await trails.GoToPageAsync(2);
        await trails.GoToPageAsync(3);

        cut.Render(parameters => parameters.Add(grid => grid.PageSize, 4));
        await trails.GoToPageAsync(2);

        Assert.Equal("5–5 of 5", cut.Find("[data-part=summary]").TextContent.Trim());
        Assert.Equal("2", cut.Find("[data-part=pager] nav").GetAttribute("data-page-count"));
    }

    [Fact]
    public async Task Pager_ShouldOfferTheNextPageAgain_WhenTheFurthestPageSaysMoreFollowAfterAll()
    {
        var trails = Trails.ToList();
        var cut = RenderTrails(trails: null, source: trails);
        var grid = cut.Tb().Grid("Trails");
        await grid.GoToPageAsync(2);
        await grid.GoToPageAsync(3);
        trails.AddRange(["Larch Bench", "Tamarack Rim"]);

        await grid.GoToPageAsync(1);
        await grid.GoToPageAsync(3);

        Assert.Equal("5–6 of many", cut.Find("[data-part=summary]").TextContent.Trim());
        Assert.Equal("4", cut.Find("[data-part=pager] nav").GetAttribute("data-page-count"));
    }

    [Fact]
    public async Task Pager_ShouldCountTheRows_WhenAPageAfterTheLastFullOneComesBackEmpty()
    {
        var cut = RenderTrails(trails: Trails[..4], moreWhenFull: true);
        var trails = cut.Tb().Grid("Trails");
        await trails.GoToPageAsync(2);

        await cut.FindAll("[data-part=pager] [data-part=page]").Single(page => page.TextContent.Trim() == "3")
            .ClickAsync(new());

        Assert.Equal("2", cut.Find("[data-part=pager] nav").GetAttribute("data-page"));
        Assert.Equal("3–4 of 4", cut.Find("[data-part=summary]").TextContent.Trim());
        Assert.Equal("2", cut.Find("[data-part=pager] nav").GetAttribute("data-page-count"));
    }

    [Fact]
    public void Page_ShouldGoBackInAStepOrTwo_WhenItStartsFarPastTheLastRow()
    {
        var calls = 0;
        var cut = RenderTrails(configure: parameters => parameters.Add(grid => grid.Page, 1000),
            asked: () => calls++);

        Assert.Equal("1–2 of many", cut.Find("[data-part=summary]").TextContent.Trim());
        Assert.InRange(calls, 1, 3);
    }

    [Theory]
    [InlineData("a", "Rows shown: at least 2.")]
    [InlineData("ridge", "Rows shown: 2.")]
    public async Task Filter_ShouldSayAtLeastTheRowsShown_WhenTheProviderDoesNotCountThemAndMoreFollow(string text,
        string spoken)
    {
        var host = Render<AnnouncerHost>();
        var cut = RenderTrails(filterable: true);

        await cut.Tb().Grid("Trails").FilterAsync("Trail", text);

        host.WaitForAssertion(() => Assert.Contains(spoken, host.Find("[role=status]").TextContent));
    }

    [Fact]
    public void Labels_ShouldWordTheRangeOfAnUncountedPage_WhenGiven()
    {
        var cut = RenderTrails(configure: parameters => parameters.Add(grid => grid.Labels,
            new DataGridLabels { RangeOfMany = "Trails {0} to {1}, and more" }));

        Assert.Equal("Trails 1 to 2, and more", cut.Find("[data-part=summary]").TextContent.Trim());
    }

    [Fact]
    public async Task Labels_ShouldWordTheCountOfAnUncountedFilter_WhenGiven()
    {
        var host = Render<AnnouncerHost>();
        var cut = RenderTrails(filterable: true, configure: parameters => parameters.Add(grid => grid.Labels,
            new DataGridLabels { RowsShownAtLeast = "{0} so far" }));

        await cut.Tb().Grid("Trails").FilterAsync("Trail", "a");

        host.WaitForAssertion(() => Assert.Contains("2 so far", host.Find("[role=status]").TextContent));
    }

    [Fact]
    public void Virtualize_ShouldSayItNeedsTheCount_WhenTheProviderGivesNone()
    {
        var failure = Assert.ThrowsAny<Exception>(() => RenderTrails(pageSize: null,
            configure: parameters => parameters.Add(grid => grid.Virtualize, true).Add(grid => grid.Height, "10rem")));

        Assert.Contains("needs the TotalCount", failure.ToString());
    }

    // Each page is two trails that contain the filter's text, and more follow it unless it holds the last one, or,
    // with moreWhenFull, whenever it is full, as a provider that only takes a page's worth of rows says.
    private IRenderedComponent<TbDataGrid<string>> RenderTrails(bool filterable = false, int? pageSize = 2,
        Action<ComponentParameterCollectionBuilder<TbDataGrid<string>>>? configure = null, string[]? trails = null,
        bool moreWhenFull = false, Action? asked = null, List<string>? source = null) =>
        Render<TbDataGrid<string>>((ComponentParameterCollectionBuilder<TbDataGrid<string>> parameters) =>
        {
            parameters.Add(grid => grid.AriaLabel, "Trails").Add(grid => grid.KeySelector, trail => trail)
                .Add(grid => grid.PageSize, pageSize).Add(grid => grid.ShowFirstLast, true)
                .Add(grid => grid.DataProvider, (GridRequest request, CancellationToken _) =>
                {
                    asked?.Invoke();
                    var text = request.Filters.Select(filter => filter.Value as string).FirstOrDefault() ?? "";
                    var matched = (source ?? (IEnumerable<string>)(trails ?? Trails))
                        .Where(trail => trail.Contains(text, StringComparison.OrdinalIgnoreCase)).ToList();
                    var page = matched.Skip(request.StartIndex).Take(request.Count ?? matched.Count).ToList();
                    return Task.FromResult(new GridResult<string>(page, null)
                    {
                        HasMore = moreWhenFull ? page.Count == request.Count
                            : request.StartIndex + page.Count < matched.Count,
                    });
                })
                .Add(grid => grid.ChildContent, (RenderFragment)(builder =>
                {
                    builder.OpenComponent<TbColumn<string>>(0);
                    builder.AddAttribute(1, nameof(TbColumn<string>.Title), "Trail");
                    builder.AddAttribute(2, nameof(TbColumn<string>.Id), "Trail");
                    builder.AddAttribute(3, nameof(TbColumn<string>.FilterBy), (Func<string, object?>)(trail => trail));
                    builder.AddAttribute(4, nameof(TbColumn<string>.Filterable), filterable);
                    builder.CloseComponent();
                }));
            configure?.Invoke(parameters);
        });
}
