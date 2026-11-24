using System.Linq.Expressions;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;

namespace Trailblazor.Tests.Components;

// Rows of a type that loses a property on its way to JSON and back.
public class TbDataGridPrerenderRoundTripTests : BunitContext
{
    private readonly FakeLogCollector _logs = new();
    private int _asked;

    public TbDataGridPrerenderRoundTripTests()
    {
        this.AddTrailblazor();
        Services.AddSingleton<ILogger<TbDataGrid<LossyHike>>>(new FakeLogger<TbDataGrid<LossyHike>>(_logs));
    }

    // System.Text.Json skips a private setter, so Name would come back empty and Id as 0.
    public sealed class LossyHike
    {
        public int Id { get; private set; }

        public string Name { get; private set; } = "";

        public static LossyHike Of(int id, string name) => new() { Id = id, Name = name };
    }

    private static readonly LossyHike[] Hikes = [LossyHike.Of(1, "Pine Hollow"), LossyHike.Of(2, "Eagle Ridge")];

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void PrerenderKey_ShouldAskTheProviderAgainAndWarn_WhenTheRowTypeLosesAPropertyInJson(bool keyed)
    {
        Prerender(keyed);

        var cut = RenderGrid(keyed);

        // Without a KeySelector the kept rows would show blank cells, and with one that reads a lost property they
        // would all share a key, which the grid refuses.
        Assert.Equal(2, _asked);
        Assert.Equal(["Pine Hollow", "Eagle Ridge"],
            cut.FindAll("[data-part=row]").Select(row => row.Children[0].TextContent.Trim()));
        var warning = Assert.Single(_logs.GetSnapshot(), log => log.Level == LogLevel.Warning);
        Assert.Contains("PrerenderKey", warning.Message);
        Assert.Contains(nameof(LossyHike), warning.Message);
    }

    [Fact]
    public void PrerenderKey_ShouldAskTheProviderAgainAndSayWhy_WhenTheRowsCannotBeWrittenToJson()
    {
        var logs = new FakeLogCollector();
        Services.AddSingleton<ILogger<TbDataGrid<Loop>>>(new FakeLogger<TbDataGrid<Loop>>(logs));
        var asked = 0;
        IRenderedComponent<TbDataGrid<Loop>> Render() => Render<TbDataGrid<Loop>>(parameters => parameters
            .Add(grid => grid.DataProvider, (_, _) =>
            {
                asked++;
                return Task.FromResult(new GridResult<Loop>([new Loop()], 1));
            }).Add(grid => grid.AriaLabel, "Loops").Add(grid => grid.PrerenderKey, "loops")
            .Add(grid => grid.ChildContent, (RenderFragment)(builder =>
            {
                builder.OpenComponent<TbColumn<Loop>>(0);
                builder.AddAttribute(1, nameof(TbColumn<Loop>.Field),
                    (Expression<Func<Loop, object?>>)(loop => loop.Name));
                builder.CloseComponent();
            })));
        var state = AddBunitPersistentComponentState();
        SetRendererInfo(new RendererInfo("Static", isInteractive: false));
        Render();
        state.TriggerOnPersisting();
        SetRendererInfo(new RendererInfo("Server", isInteractive: true));

        Render();

        // The warning carries the serializer's own account, and doesn't blame the type's setters.
        Assert.Equal(2, asked);
        var warning = Assert.Single(logs.GetSnapshot(), log => log.Level == LogLevel.Warning);
        Assert.IsType<System.Text.Json.JsonException>(warning.Exception);
        Assert.DoesNotContain("setter", warning.Message);
    }

    // A row that refers back to itself, which System.Text.Json refuses to write.
    public sealed class Loop
    {
        public string Name => "Loop";

        public Loop Self => this;
    }

    private void Prerender(bool keyed)
    {
        var state = AddBunitPersistentComponentState();
        SetRendererInfo(new RendererInfo("Static", isInteractive: false));
        RenderGrid(keyed);
        state.TriggerOnPersisting();
        SetRendererInfo(new RendererInfo("Server", isInteractive: true));
    }

    private IRenderedComponent<TbDataGrid<LossyHike>> RenderGrid(bool keyed) =>
        Render<TbDataGrid<LossyHike>>(parameters =>
        {
            parameters.Add(grid => grid.DataProvider, (_, _) =>
                {
                    _asked++;
                    return Task.FromResult(new GridResult<LossyHike>([.. Hikes], Hikes.Length));
                })
                .Add(grid => grid.AriaLabel, "Hikes").Add(grid => grid.PrerenderKey, "hikes")
                .Add(grid => grid.ChildContent, (RenderFragment)Columns);
            if (keyed)
            {
                parameters.Add(grid => grid.KeySelector, hike => hike.Id);
            }
        });

    private static void Columns(RenderTreeBuilder builder)
    {
        builder.OpenComponent<TbColumn<LossyHike>>(0);
        builder.AddAttribute(1, nameof(TbColumn<LossyHike>.Field),
            (Expression<Func<LossyHike, object?>>)(hike => hike.Name));
        builder.CloseComponent();
    }
}
