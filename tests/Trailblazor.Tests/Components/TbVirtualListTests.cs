using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web.Virtualization;

namespace Trailblazor.Tests.Components;

public class TbVirtualListTests : BunitContext
{
    private static readonly string[] Trails = ["Pine", "Cedar", "Eagle"];
    private readonly BunitJSModuleInterop _module;

    public TbVirtualListTests()
    {
        this.AddTrailblazor();
        _module = JSInterop.SetupModule("./_content/Trailblazor/virtual-list.js");
        _module.Mode = JSRuntimeMode.Loose;
    }

    private IRenderedComponent<TbVirtualList<string>> RenderList(
        Action<ComponentParameterCollectionBuilder<TbVirtualList<string>>> configure) =>
        Render<TbVirtualList<string>>(parameters =>
        {
            parameters.Add(list => list.AriaLabel, "Trail reports")
                .Add(list => list.ChildContent, trail => builder => builder.AddContent(0, trail));
            configure(parameters);
        });

    [Fact]
    public void Render_ShouldBeANamedFeedOfArticlesWithTheirPlaces_WhenGivenItems()
    {
        var cut = RenderList(parameters => parameters.Add(list => list.Items, Trails)
            .Add(list => list.ItemLabel, trail => $"{trail} loop"));

        var feed = cut.Find("[role=feed]");
        Assert.Equal(("Trail reports", "false"), (feed.GetAttribute("aria-label"), feed.GetAttribute("aria-busy")));
        var items = cut.FindAll("article[data-part=item]");
        Assert.Equal(["Pine", "Cedar", "Eagle"], items.Select(item => item.TextContent.Trim()));
        Assert.Equal(["1", "2", "3"], items.Select(item => item.GetAttribute("aria-posinset")));
        Assert.All(items, item => Assert.Equal("3", item.GetAttribute("aria-setsize")));
        Assert.Equal(["0", "1", "2"], items.Select(item => item.GetAttribute("data-index")));
        Assert.Equal("Cedar loop", items[1].GetAttribute("aria-label"));
    }

    [Fact]
    public void Render_ShouldShowTheItemsAgain_WhenTheListChangesInPlace()
    {
        var trails = new List<string>(Trails);
        var cut = RenderList(parameters => parameters.Add(list => list.Items, trails));

        trails.Add("Ridge");
        cut.Render(parameters => parameters.Add(list => list.Items, trails));

        cut.WaitForAssertion(() => Assert.Equal(4, cut.FindAll("article[data-part=item]").Count));
        Assert.Equal("4", cut.Find("article[data-part=item]").GetAttribute("aria-setsize"));
    }

    [Fact]
    public async Task Render_ShouldBeBusyWhileItLoadsAndThenShowTheTotal_WhenGivenAProvider()
    {
        var loaded = new TaskCompletionSource<ItemsProviderResult<string>>();
        var cut = RenderList(parameters => parameters.Add(list => list.ItemsProvider,
            (ItemsProviderDelegate<string>)(_ => new ValueTask<ItemsProviderResult<string>>(loaded.Task))));

        cut.WaitForAssertion(() => Assert.Equal("true", cut.Find("[role=feed]").GetAttribute("aria-busy")));
        await cut.InvokeAsync(() => loaded.SetResult(new(Trails, 120)));

        cut.WaitForAssertion(() => Assert.Equal("false", cut.Find("[role=feed]").GetAttribute("aria-busy")));
        Assert.Equal("120", cut.Find("article[data-part=item]").GetAttribute("aria-setsize"));
        Assert.Equal("120", cut.Find("[role=feed]").GetAttribute("data-count"));
    }

    [Fact]
    public void Render_ShouldShowTheEmptyContent_WhenThereAreNoItems()
    {
        var cut = RenderList(parameters => parameters.Add(list => list.Items, Array.Empty<string>())
            .Add(list => list.EmptyContent, (RenderFragment)(builder => builder.AddContent(0, "No reports yet."))));

        Assert.Equal("No reports yet.", cut.Find("[data-part=empty]").TextContent.Trim());
        Assert.Equal(("group", "Trail reports"), (cut.Find("[data-part=feed]").GetAttribute("role"),
            cut.Find("[data-part=feed]").GetAttribute("aria-label")));
        Assert.Empty(cut.FindAll("article"));
    }

    [Fact]
    public void Render_ShouldGiveTheListItsHeightToScrollIn_WhenGivenAHeight()
    {
        var cut = RenderList(parameters => parameters.Add(list => list.Items, Trails).Add(list => list.Height, "20rem"));

        var feed = cut.Find("[role=feed]");
        Assert.Contains("block-size: 20rem", feed.GetAttribute("style"));
        Assert.Contains("tb-virtual-list--scrolls", feed.ClassList);
    }

    [Fact]
    public async Task ScrollToAsync_ShouldAskTheBrowserToScrollAndFocus_WhenCalled()
    {
        var handle = _module.SetupModule("createVirtualList", _ => true);
        handle.SetupVoid("scrollToIndex", _ => true).SetVoidResult();
        var cut = RenderList(parameters => parameters.Add(list => list.Items, Trails));
        cut.WaitForAssertion(() => Assert.Single(_module.Invocations["createVirtualList"]));

        await cut.InvokeAsync(() => cut.Instance.ScrollToAsync(2, focus: true));

        Assert.Equal([2, true], handle.Invocations["scrollToIndex"].Single().Arguments);
    }

    [Fact]
    public async Task ScrollToAsync_ShouldThrow_WhenThePlaceIsPastTheLastItem()
    {
        var cut = RenderList(parameters => parameters.Add(list => list.Items, Trails));

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => cut.InvokeAsync(() => cut.Instance.ScrollToAsync(3)));
    }

    [Fact]
    public void Render_ShouldThrow_WhenItHasNoNameOrNotExactlyOneSourceOfItems()
    {
        Assert.Throws<InvalidOperationException>(() => Render<TbVirtualList<string>>(parameters => parameters
            .Add(list => list.Items, Trails)));
        Assert.Throws<InvalidOperationException>(() => RenderList(_ => { }));
        Assert.Throws<InvalidOperationException>(() => RenderList(parameters => parameters.Add(list => list.Items, Trails)
            .Add(list => list.ItemsProvider, (ItemsProviderDelegate<string>)(_ => default))));
    }
}
