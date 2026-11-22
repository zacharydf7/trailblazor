using Bunit;
using Bunit.TestDoubles;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;

namespace Trailblazor.Tests.Components;

public class TbTreeViewPrerenderTests : BunitContext
{
    private static readonly Place[] Regions = [new(1, "North"), new(2, "South")];
    private readonly List<string> _loaded = [];
    private BunitPersistentComponentState? _state;

    public TbTreeViewPrerenderTests()
    {
        this.AddTrailblazor();
    }

    public sealed record Place(int Id, string Name);

    private BunitPersistentComponentState State => _state ??= AddBunitPersistentComponentState();

    [Fact]
    public void PrerenderKey_ShouldShowTheKeptChildrenAndNotLoadAgain_WhenARowStartsOpen()
    {
        Prerender();

        var cut = RenderPlaces(LoadAsync);

        Assert.Equal(["North"], _loaded);
        Assert.Equal(["North", "North loop", "South"], Texts(cut));
        Assert.Empty(cut.FindAll("[data-part=loading]"));
    }

    [Fact]
    public void PrerenderKey_ShouldLoadAsUsual_WhenAnotherRowIsOpenedAfterTheKeptChildrenShow()
    {
        Prerender();
        var cut = RenderPlaces(LoadAsync);

        cut.Find("[data-part=item][data-key='2'] [data-part=toggle]").Click();

        Assert.Equal(["North", "South"], _loaded);
        cut.WaitForAssertion(() => Assert.Equal(["North", "North loop", "South", "South loop"], Texts(cut)));
    }

    [Fact]
    public void PrerenderKey_ShouldKeepNothing_WhenThePrerenderedLoadFailed()
    {
        Prerender(_ => Task.FromException<IEnumerable<Place>>(new InvalidOperationException("No trails today.")));

        var cut = RenderPlaces(LoadAsync);

        Assert.Equal(["North"], _loaded);
        Assert.Equal(["North", "North loop", "South"], Texts(cut));
    }

    [Fact]
    public void PrerenderKey_ShouldKeepNothing_WhenTheTreeIsInteractiveAlready()
    {
        _ = State;
        SetRendererInfo(new RendererInfo("Server", isInteractive: true));
        RenderPlaces(LoadAsync);
        State.TriggerOnPersisting();

        RenderPlaces(LoadAsync);

        Assert.Equal(["North", "North"], _loaded);
    }

    [Fact]
    public void PrerenderKey_ShouldLoadAndWarn_WhenWhatWasKeptCannotBeRead()
    {
        var logs = new FakeLogCollector();
        Services.AddSingleton<ILogger<TbTreeView<Place, int>>>(new FakeLogger<TbTreeView<Place, int>>(logs));
        State.Persist("places", "not children");
        SetRendererInfo(new RendererInfo("Server", isInteractive: true));

        var cut = RenderPlaces(LoadAsync);

        Assert.Equal(["North"], _loaded);
        Assert.Equal(["North", "North loop", "South"], Texts(cut));
        var warning = Assert.Single(logs.GetSnapshot(), log => log.Level == LogLevel.Warning);
        Assert.Contains("PrerenderKey", warning.Message);
    }

    [Fact]
    public void PrerenderKey_ShouldLoadAsUsual_WhenTheAppKeepsNoStateAcrossPrerendering()
    {
        // No PersistentComponentState is registered, as in a host that doesn't provide one.
        SetRendererInfo(new RendererInfo("Static", isInteractive: false));
        RenderPlaces(LoadAsync);
        SetRendererInfo(new RendererInfo("Server", isInteractive: true));

        var cut = RenderPlaces(LoadAsync);

        Assert.Equal(["North", "North"], _loaded);
        Assert.Equal(["North", "North loop", "South"], Texts(cut));
    }

    [Fact]
    public void PrerenderKey_ShouldCheckTheKeptChildren_WhenTheirRowIsCheckedAndChecksCascade()
    {
        List<IReadOnlyCollection<int>> reported = [];
        void Checked(ComponentParameterCollectionBuilder<TbTreeView<Place, int>> parameters) => parameters
            .Add(tree => tree.Checkable, true).Add(tree => tree.CheckedKeys, [1])
            .Add(tree => tree.CheckedKeysChanged, (IReadOnlyCollection<int> keys) => reported.Add(keys));
        Prerender(more: Checked);
        reported.Clear();

        var cut = RenderPlaces(LoadAsync, Checked);

        Assert.Equal("true", cut.Find("[data-part=item][data-key='1']").GetAttribute("aria-checked"));
        Assert.Equal("true", cut.Find("[data-part=item][data-key='10']").GetAttribute("aria-checked"));
        Assert.Equal([1, 10], Assert.Single(reported).Order());
    }

    // Renders the tree as a static pass, keeps what it persists, and leaves the renderer interactive for the next tree.
    private void Prerender(Func<Place, Task<IEnumerable<Place>>>? load = null,
        Action<ComponentParameterCollectionBuilder<TbTreeView<Place, int>>>? more = null)
    {
        _ = State;
        SetRendererInfo(new RendererInfo("Static", isInteractive: false));
        RenderPlaces(load ?? LoadAsync, more);
        State.TriggerOnPersisting();
        SetRendererInfo(new RendererInfo("Server", isInteractive: true));
    }

    private Task<IEnumerable<Place>> LoadAsync(Place region)
    {
        _loaded.Add(region.Name);
        return Task.FromResult<IEnumerable<Place>>([new(region.Id * 10, $"{region.Name} loop")]);
    }

    private static string[] Texts(IRenderedComponent<TbTreeView<Place, int>> cut) =>
        [.. cut.FindAll("[data-part=item]").Select(row => row.GetAttribute("data-text")!)];

    private IRenderedComponent<TbTreeView<Place, int>> RenderPlaces(Func<Place, Task<IEnumerable<Place>>> load,
        Action<ComponentParameterCollectionBuilder<TbTreeView<Place, int>>>? more = null) =>
        Render<TbTreeView<Place, int>>(parameters =>
        {
            parameters.Add(tree => tree.Items, Regions).Add(tree => tree.KeySelector, place => place.Id)
                .Add(tree => tree.ItemText, place => place.Name).Add(tree => tree.AriaLabel, "Places")
                .Add(tree => tree.LoadChildren, (place, _) => load(place))
                .Add(tree => tree.HasChildren, place => place.Id < 10).Add(tree => tree.ExpandedKeys, [1])
                .Add(tree => tree.PrerenderKey, "places");
            more?.Invoke(parameters);
        });
}
