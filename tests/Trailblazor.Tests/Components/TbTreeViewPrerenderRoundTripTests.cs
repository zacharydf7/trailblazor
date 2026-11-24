using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;

namespace Trailblazor.Tests.Components;

// Items of a type that loses a property on its way to JSON and back.
public class TbTreeViewPrerenderRoundTripTests : BunitContext
{
    private readonly FakeLogCollector _logs = new();
    private readonly List<string> _loaded = [];

    public TbTreeViewPrerenderRoundTripTests()
    {
        this.AddTrailblazor();
        Services.AddSingleton<ILogger<TbTreeView<LossyPlace, int>>>(
            new FakeLogger<TbTreeView<LossyPlace, int>>(_logs));
    }

    // System.Text.Json skips a private setter, so Name would come back empty, under a key that came back whole.
    public sealed class LossyPlace
    {
        public int Id { get; init; }

        public string Name { get; private set; } = "";

        public static LossyPlace Of(int id, string name) => new() { Id = id, Name = name };
    }

    private static readonly LossyPlace[] Regions = [LossyPlace.Of(1, "North"), LossyPlace.Of(2, "South")];

    [Fact]
    public void PrerenderKey_ShouldLoadTheChildrenAgainAndWarn_WhenTheItemTypeLosesAPropertyInJson()
    {
        var state = AddBunitPersistentComponentState();
        SetRendererInfo(new RendererInfo("Static", isInteractive: false));
        RenderPlaces();
        state.TriggerOnPersisting();
        SetRendererInfo(new RendererInfo("Server", isInteractive: true));

        var cut = RenderPlaces();

        // The kept children would have shown under North without their names, and North wouldn't load again.
        Assert.Equal(["North", "North"], _loaded);
        Assert.Equal(["North", "North loop", "South"],
            cut.FindAll("[data-part=item]").Select(row => row.GetAttribute("data-text")));
        var warning = Assert.Single(_logs.GetSnapshot(), log => log.Level == LogLevel.Warning);
        Assert.Contains("PrerenderKey", warning.Message);
        Assert.Contains(nameof(LossyPlace), warning.Message);
    }

    private IRenderedComponent<TbTreeView<LossyPlace, int>> RenderPlaces() =>
        Render<TbTreeView<LossyPlace, int>>(parameters => parameters.Add(tree => tree.Items, Regions)
            .Add(tree => tree.KeySelector, place => place.Id).Add(tree => tree.ItemText, place => place.Name)
            .Add(tree => tree.AriaLabel, "Places").Add(tree => tree.HasChildren, place => place.Id < 10)
            .Add(tree => tree.ExpandedKeys, [1]).Add(tree => tree.PrerenderKey, "places")
            .Add(tree => tree.LoadChildren, (place, _) =>
            {
                _loaded.Add(place.Name);
                return Task.FromResult<IEnumerable<LossyPlace>>([LossyPlace.Of(place.Id * 10, $"{place.Name} loop")]);
            }));
}
