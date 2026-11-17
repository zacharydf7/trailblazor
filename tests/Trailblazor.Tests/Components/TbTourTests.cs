using System.Diagnostics.Metrics;
using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.Metrics.Testing;
using Microsoft.JSInterop;
using Trailblazor.Primitives;
using Trailblazor.Testing;

namespace Trailblazor.Tests.Components;

public class TbTourTests : BunitContext
{
    private static readonly TourStep[] Steps =
    [
        new("#new-trail", "Add a trail") { Body = "Start here to map a new trail." },
        new("#search", "Find trails") { Placement = Placement.End, Align = PlacementAlign.Center },
        new("#map", "See the map") { Body = "Every trail you add shows here." },
    ];

    private readonly BunitJSModuleInterop _handle;
    private readonly List<int> _steps = [];
    private readonly List<bool> _opens = [];
    private readonly List<TourEnd> _ends = [];

    public TbTourTests()
    {
        this.AddTrailblazor();
        _handle = JSInterop.SetupModule("./_content/Trailblazor/tour.js").SetupModule("createTour", _ => true);
        _handle.SetupVoid("show", _ => true).SetVoidResult();
        _handle.SetupVoid("dispose", _ => true).SetVoidResult();
    }

    [Fact]
    public void Render_ShouldShowTheStepAsADialogNamedByItsTitle_WhenOpen()
    {
        var cut = RenderTour();

        var step = cut.Find("[role=dialog]");
        Assert.Equal(("Add a trail", "Start here to map a new trail."),
            (cut.Find($"#{step.GetAttribute("aria-labelledby")}").TextContent,
                cut.Find($"#{step.GetAttribute("aria-describedby")}").TextContent));
        Assert.Equal(("#new-trail", "Bottom", "Start", "-1"), (step.GetAttribute("data-target"),
            step.GetAttribute("data-side"), step.GetAttribute("data-align"), step.GetAttribute("tabindex")));
        Assert.Equal("1 of 3", cut.Find("[data-part=position]").TextContent);
        Assert.Empty(cut.FindAll("[data-part=back]"));
        Assert.Single(_handle.Invocations["show"]);
    }

    [Fact]
    public async Task NextAsync_ShouldMoveThroughTheStepsAndShowEach_WhenPressed()
    {
        var cut = RenderTour();
        var tour = cut.Tb().Tour();

        await tour.NextAsync();
        await tour.NextAsync();
        await tour.BackAsync();

        Assert.Equal((1, "Find trails"), (tour.Step, tour.Title));
        Assert.Null(cut.Find("[role=dialog]").GetAttribute("aria-describedby"));
        Assert.Equal([1, 2, 1], _steps);
        Assert.Equal(4, _handle.Invocations["show"].Count);
    }

    [Fact]
    public async Task NextAsync_ShouldEndTheTourAsCompleted_WhenPressedOnTheLastStep()
    {
        var cut = RenderTour(step: 2);
        Assert.Equal("Done", cut.Find("[data-part=next]").TextContent.Trim());

        await cut.Tb().Tour().NextAsync();

        Assert.Equal([false], _opens);
        Assert.Equal([TourEnd.Completed], _ends);
        Assert.False(cut.Tb().Tour().IsOpen);
        Assert.Single(_handle.Invocations["dispose"]);
    }

    [Fact]
    public async Task SkipAsync_ShouldEndTheTourAsSkipped_WhenPressed()
    {
        var cut = RenderTour(step: 1);

        await cut.Tb().Tour().SkipAsync();

        Assert.Equal([false], _opens);
        Assert.Equal([TourEnd.Skipped], _ends);
    }

    [Fact]
    public async Task Escape_ShouldEndTheTourAsSkipped_WhenTheScriptHearsIt()
    {
        var cut = RenderTour();
        var events = (DotNetObjectReference<TourEvents>)JSInterop.Invocations["createTour"][0].Arguments[1]!;

        await cut.InvokeAsync(events.Value.OnEscape);

        Assert.Equal([TourEnd.Skipped], _ends);
    }

    [Fact]
    public void Render_ShouldShowNothing_WhenClosed()
    {
        var cut = Render<TbTour>(parameters => parameters.Add(tour => tour.Steps, Steps));

        Assert.Empty(cut.FindAll(".tb-tour"));
        Assert.Empty(JSInterop.Invocations["createTour"]);
    }

    [Fact]
    public void Labels_ShouldWordTheButtonsAndPosition_WhenGiven()
    {
        var cut = Render<TbTour>(parameters => parameters.Add(tour => tour.Steps, Steps)
            .Add(tour => tour.Open, true).Add(tour => tour.Step, 1)
            .Add(tour => tour.Labels, new TourLabels { Next = "Onward", Skip = "Not now", Position = "{0}/{1}" }));

        Assert.Equal(("Onward", "Not now", "Back", "2/3"), (cut.Find("[data-part=next]").TextContent.Trim(),
            cut.Find("[data-part=skip]").TextContent.Trim(), cut.Find("[data-part=back]").TextContent.Trim(),
            cut.Find("[data-part=position]").TextContent));
    }

    [Fact]
    public void Render_ShouldNotStartTheScript_WhenOpenWithNoSteps()
    {
        var cut = Render<TbTour>(parameters => parameters.Add(tour => tour.Steps, []).Add(tour => tour.Open, true));

        Assert.Empty(cut.FindAll(".tb-tour"));
        Assert.Empty(JSInterop.Invocations["createTour"]);
    }

    [Fact]
    public void Render_ShouldThrow_WhenAStepHasNoTitle()
    {
        var thrown = Assert.Throws<InvalidOperationException>(() => Render<TbTour>(parameters => parameters
            .Add(tour => tour.Steps, [new TourStep("#map", " ")])));

        Assert.Equal("Each TourStep in a TbTour needs a Title.", thrown.Message);
    }

    [Fact]
    public async Task Step_ShouldStayWhereTheTourMoved_WhenTheParentRendersAgainWithoutBindingIt()
    {
        var cut = Render<TbTour>(parameters => parameters.Add(tour => tour.Steps, Steps).Add(tour => tour.Open, true));
        var tour = cut.Tb().Tour();

        await tour.NextAsync();
        cut.Render(parameters => parameters.Add(tour => tour.Steps, Steps).Add(tour => tour.Open, true));

        Assert.Equal(1, tour.Step);
    }

    [Fact]
    public async Task DisposeAsync_ShouldEndTheScript_WhenTheTourIsDisposedWhileItStarts()
    {
        var script = new HeldScript();
        Services.AddScoped(_ => new JsModuleLoader(script));
        RenderTour();

        await DisposeComponentsAsync();
        script.Starting.SetResult(script);

        await script.Ended.Task.WaitAsync(TimeSpan.FromSeconds(5), Xunit.TestContext.Current.CancellationToken);
    }

    [Fact]
    public void Render_ShouldLeaveTheAppsOutsidePopupsUsable_WhenTheyAreConfigured()
    {
        Services.Configure<TrailblazorOptions>(options => options.Dialogs.OutsidePopups.Add(".date-popup"));

        RenderTour();

        Assert.Equal([".date-popup"], (IEnumerable<string>)JSInterop.Invocations["createTour"][0].Arguments[2]!);
    }

    [Fact]
    public async Task Render_ShouldStartTheScriptOnce_WhenRenderedAgainWhileItStarts()
    {
        var script = new HeldScript();
        Services.AddScoped(_ => new JsModuleLoader(script));
        var cut = RenderTour();

        cut.Render();
        await cut.InvokeAsync(() => script.Starting.SetResult(script));

        Assert.Equal(1, script.Starts);
    }

    [Fact]
    public async Task Render_ShouldStartANewScript_WhenTheTourReopensWhileItStarts()
    {
        var script = new HeldScript();
        Services.AddScoped(_ => new JsModuleLoader(script));
        var cut = RenderTour();

        cut.Render(parameters => parameters.Add(tour => tour.Open, false));
        cut.Render(parameters => parameters.Add(tour => tour.Open, true));
        await cut.InvokeAsync(() => script.Starting.SetResult(script));

        await script.Ended.Task.WaitAsync(TimeSpan.FromSeconds(5), Xunit.TestContext.Current.CancellationToken);
        Assert.Equal((2, 1), (script.Starts, script.Disposes));
    }

    [Fact]
    public async Task TelemetryName_ShouldCountTheTourShownAndHowItEnded_WhenGiven()
    {
        var meters = Services.GetRequiredService<IMeterFactory>();
        using var shown = new MetricCollector<long>(meters, "Trailblazor", "trailblazor.control.shown");
        using var used = new MetricCollector<long>(meters, "Trailblazor", "trailblazor.control.used");
        var cut = Render<TbTour>(parameters => parameters.Add(tour => tour.Steps, Steps).Add(tour => tour.Open, true)
            .Add(tour => tour.TelemetryName, "first-run"));

        await cut.Tb().Tour().SkipAsync();

        Assert.Equal(["first-run"], shown.GetMeasurementSnapshot().Select(m => m.Tags["trailblazor.telemetry_name"]));
        Assert.Equal(["skip"], used.GetMeasurementSnapshot().Select(m => m.Tags["trailblazor.action"]));
    }

    private IRenderedComponent<TbTour> RenderTour(int step = 0) =>
        Render<TbTour>(parameters => parameters.Add(tour => tour.Steps, Steps)
            .Add(tour => tour.Open, true).Add(tour => tour.OpenChanged, open => _opens.Add(open))
            .Add(tour => tour.Step, step).Add(tour => tour.StepChanged, index => _steps.Add(index))
            .Add(tour => tour.OnFinished, end => _ends.Add(end)));

    // The tour's script, whose createTour waits until the test lets it return.
    private sealed class HeldScript : IJSRuntime, IJSObjectReference
    {
        public TaskCompletionSource<IJSObjectReference> Starting { get; } = new();

        public TaskCompletionSource Ended { get; } = new();

        public int Starts { get; private set; }

        public int Disposes { get; private set; }

        public async ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args)
        {
            switch (identifier)
            {
                case "import":
                    return (TValue)(object)this;
                case "createTour":
                    Starts++;
                    return (TValue)await Starting.Task;
                case "dispose":
                    Disposes++;
                    Ended.TrySetResult();
                    break;
            }

            return default!;
        }

        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken,
            object?[]? args) => InvokeAsync<TValue>(identifier, args);

        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }
}
