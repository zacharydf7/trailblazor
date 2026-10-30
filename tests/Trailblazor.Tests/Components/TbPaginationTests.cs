using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;

namespace Trailblazor.Tests.Components;

public class TbPaginationTests : BunitContext
{
    public TbPaginationTests()
    {
        this.AddTrailblazor();
        SetRendererInfo(new RendererInfo("Server", isInteractive: true));
    }

    [Fact]
    public void Render_ShouldNameEachPageAndMarkTheCurrentOne_WhenCalled()
    {
        var cut = RenderPagination(page: 5, count: 10);

        Assert.Equal("Pagination", cut.Find("nav").GetAttribute("aria-label"));
        var pages = cut.FindAll("[data-part=page]");
        Assert.Equal(["Page 1", "Page 4", "Page 5", "Page 6", "Page 10"],
            pages.Select(page => page.GetAttribute("aria-label")));
        Assert.Equal("page", Assert.Single(cut.FindAll("[data-part=page][aria-current]")).GetAttribute("aria-current"));
        Assert.Equal("Page 5", cut.Find("[aria-current=page]").GetAttribute("aria-label"));
        Assert.All(cut.FindAll("[data-part=ellipsis]"), gap => Assert.Equal("true", gap.GetAttribute("aria-hidden")));
    }

    [Fact]
    public void Click_ShouldGoToThePage_WhenAPageOrStepButtonIsClicked()
    {
        var page = 5;
        var cut = RenderPagination(page, count: 10, onChange: next => page = next);

        cut.Find("[data-part=page][aria-label='Page 6']").Click();
        Assert.Equal(6, page);
        cut.Find("[data-part=next]").Click();

        Assert.Equal(7, page);
        Assert.Equal("page", cut.Find("[data-part=page][aria-label='Page 7']").GetAttribute("aria-current"));
    }

    [Fact]
    public void Render_ShouldTurnOffTheStepsAtEachEnd_WhenOnTheFirstOrLastPage()
    {
        var cut = RenderPagination(page: 1, count: 3, showFirstLast: true);

        Assert.True(cut.Find("[data-part=previous]").HasAttribute("disabled"));
        Assert.True(cut.Find("[data-part=first]").HasAttribute("disabled"));
        Assert.False(cut.Find("[data-part=next]").HasAttribute("disabled"));
        cut.Render(parameters => parameters.Add(pagination => pagination.Page, 3));

        Assert.True(cut.Find("[data-part=next]").HasAttribute("disabled"));
        Assert.True(cut.Find("[data-part=last]").HasAttribute("disabled"));
    }

    [Fact]
    public void Change_ShouldGoToTheTypedPageOrTheNearestEnd_WhenAPageIsTypedInCompactMode()
    {
        var page = 1;
        var cut = RenderPagination(page, count: 140, compact: true, onChange: next => page = next);
        var box = () => cut.Find("[data-part=page-box] input");

        Assert.Equal("Page", box().GetAttribute("aria-label"));
        Assert.Equal("of 140", cut.Find("[data-part=page-count]").TextContent);
        box().TriggerEvent("ontbnumberchange", new NumberChangeEventArgs { Value = "42" });
        Assert.Equal(42, page);
        box().TriggerEvent("ontbnumberchange", new NumberChangeEventArgs { Value = "900" });

        Assert.Equal(140, page);
        Assert.Empty(cut.FindAll("[data-part=page]"));
    }

    [Fact]
    public void Render_ShouldMakeEveryItemALink_WhenGivenPageHref()
    {
        var cut = RenderPagination(page: 2, count: 3, pageHref: page => $"reports?page={page}");

        Assert.Equal("reports?page=3", cut.Find("a[data-part=page][aria-label='Page 3']").GetAttribute("href"));
        Assert.Equal("reports?page=1", cut.Find("a[data-part=previous]").GetAttribute("href"));
    }

    [Fact]
    public void Render_ShouldUseTheGivenWords_WhenGivenLabels()
    {
        var cut = RenderPagination(page: 1, count: 2, labels: new() { Page = "Seite {0}", Next = "Weiter" });

        Assert.Equal("Seite 2", cut.FindAll("[data-part=page]")[1].GetAttribute("aria-label"));
        Assert.Equal("Weiter", cut.Find("[data-part=next]").GetAttribute("aria-label"));
    }

    [Theory]
    [InlineData(-1, 1, 1)]
    [InlineData(10, -1, 1)]
    [InlineData(10, 1, 0)]
    public void Render_ShouldThrow_WhenACountIsOutOfRange(int count, int siblings, int boundaries)
    {
        Assert.Throws<InvalidOperationException>(() => Render<TbPagination>(parameters => parameters
            .Add(pagination => pagination.PageCount, count).Add(pagination => pagination.SiblingCount, siblings)
            .Add(pagination => pagination.BoundaryCount, boundaries)));
    }

    [Fact]
    public void Render_ShouldShowOnePage_WhenThereAreNoPages()
    {
        var cut = RenderPagination(page: 1, count: 0);

        Assert.Equal("Page 1", Assert.Single(cut.FindAll("[data-part=page]")).GetAttribute("aria-label"));
        Assert.Equal("1", cut.Find("nav").GetAttribute("data-page-count"));
    }

    [Fact]
    public void Render_ShouldTurnEveryControlOff_WhenDisabled()
    {
        var cut = Render<TbPagination>(parameters => parameters.Add(pagination => pagination.Page, 2)
            .Add(pagination => pagination.PageCount, 3).Add(pagination => pagination.Disabled, true));

        Assert.All(cut.FindAll("button"), button => Assert.True(button.HasAttribute("disabled")));
    }

    [Fact]
    public void Render_ShouldLeaveOutPreviousAndNext_WhenShowPreviousNextIsOff()
    {
        var cut = Render<TbPagination>(parameters => parameters.Add(pagination => pagination.PageCount, 3)
            .Add(pagination => pagination.ShowPreviousNext, false));

        Assert.Empty(cut.FindAll("[data-part=previous], [data-part=next]"));
    }

    [Fact]
    public void Click_ShouldBringTheParentBack_WhenItsPageIsPastAShrunkenCount()
    {
        var page = 15;
        var cut = RenderPagination(page, count: 3, showFirstLast: true, onChange: next => page = next);

        Assert.Equal("3", cut.Find("nav").GetAttribute("data-page"));
        Assert.False(cut.Find("[data-part=last]").HasAttribute("disabled"));
        cut.Find("[data-part=page][aria-label='Page 3']").Click();

        Assert.Equal(3, page);
    }

    [Theory]
    [InlineData(true, true)]
    [InlineData(false, false)]
    public void Render_ShouldMarkItselfReady_WhenTheRendererIsInteractive(bool interactive, bool ready)
    {
        SetRendererInfo(new RendererInfo(interactive ? "Server" : "Static", interactive));

        var cut = RenderPagination(page: 1, count: 3);

        Assert.Equal(ready, cut.Find("nav").HasAttribute("data-ready"));
    }

    [Fact]
    public void Render_ShouldNameTheNavigationFromTheLabels_WhenNoAriaLabelIsGiven()
    {
        var cut = RenderPagination(page: 1, count: 2, labels: new() { Navigation = "Seiten" });

        Assert.Equal("Seiten", cut.Find("nav").GetAttribute("aria-label"));
    }

    [Fact]
    public void Change_ShouldStayOnTheSameAddress_WhenTheTypedPageClampsToTheCurrentOne()
    {
        var navigation = Services.GetRequiredService<NavigationManager>();
        var cut = RenderPagination(page: 140, count: 140, compact: true, pageHref: page => $"photos?page={page}");
        var before = navigation.Uri;

        cut.Find("[data-part=page-box] input").TriggerEvent("ontbnumberchange",
            new NumberChangeEventArgs { Value = "999" });

        Assert.Equal(before, navigation.Uri);
    }

    [Fact]
    public void Render_ShouldDescribeThePageBoxByThePageCount_WhenCompact()
    {
        var cut = RenderPagination(page: 1, count: 140, compact: true);

        var count = cut.Find("[data-part=page-count]");
        Assert.Contains(count.Id!, cut.Find("[data-part=page-box] input").GetAttribute("aria-describedby"));
    }

    [Fact]
    public void Change_ShouldNavigateToTheTypedPagesLink_WhenCompactWithPageHref()
    {
        var cut = RenderPagination(page: 1, count: 140, compact: true, pageHref: page => $"photos?page={page}");

        cut.Find("[data-part=page-box] input").TriggerEvent("ontbnumberchange",
            new NumberChangeEventArgs { Value = "42" });

        Assert.EndsWith("photos?page=42", Services.GetRequiredService<NavigationManager>().Uri);
    }

    private IRenderedComponent<TbPagination> RenderPagination(int page, int count, bool showFirstLast = false,
        bool compact = false, Action<int>? onChange = null, Func<int, string>? pageHref = null,
        PaginationLabels? labels = null) =>
        Render<TbPagination>(parameters => parameters.Add(pagination => pagination.Page, page)
            .Add(pagination => pagination.PageCount, count).Add(pagination => pagination.ShowFirstLast, showFirstLast)
            .Add(pagination => pagination.Compact, compact).Add(pagination => pagination.PageHref, pageHref)
            .Add(pagination => pagination.Labels, labels ?? new())
            .Add(pagination => pagination.PageChanged, (int next) => onChange?.Invoke(next)));
}
