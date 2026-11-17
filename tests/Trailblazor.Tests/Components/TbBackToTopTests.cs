using System.Diagnostics.Metrics;
using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.Metrics.Testing;
using Microsoft.JSInterop;
using Trailblazor.Testing;

namespace Trailblazor.Tests.Components;

public class TbBackToTopTests : BunitContext
{
    private readonly BunitJSModuleInterop _handle;

    public TbBackToTopTests()
    {
        this.AddTrailblazor();
        _handle = JSInterop.SetupModule("./_content/Trailblazor/back-to-top.js")
            .SetupModule("attachBackToTop", _ => true);
        _handle.SetupVoid("top", _ => true).SetVoidResult();
        _handle.SetupVoid("set", _ => true).SetVoidResult();
        _handle.SetupVoid("dispose").SetVoidResult();
    }

    [Fact]
    public void Render_ShouldNameTheButton_WhenCalled()
    {
        var cut = Render<TbBackToTop>();
        var named = Render<TbBackToTop>(parameters => parameters.Add(top => top.Label, "Top of the log"));

        Assert.Equal(("Back to top", "Top of the log"),
            (cut.Find("[data-part=button]").GetAttribute("aria-label"),
                named.Find("[data-part=button]").GetAttribute("aria-label")));
        Assert.Contains("tb-fab--bottomend", cut.Find("[data-part=button]").ClassList);
    }

    [Fact]
    public void Render_ShouldPassTheScrollContainerAndThresholdToTheScript_WhenGiven()
    {
        Render<TbBackToTop>(parameters => parameters.Add(top => top.ScrollContainer, "#trail-log")
            .Add(top => top.Threshold, 200));

        Assert.Equal(["#trail-log", 200], JSInterop.Invocations["attachBackToTop"].Single().Arguments.Skip(2));
    }

    [Fact]
    public void Render_ShouldSendTheScriptANewContainerOrThreshold_WhenTheyChange()
    {
        var cut = Render<TbBackToTop>();

        cut.Render(parameters => parameters.Add(top => top.ScrollContainer, "#trail-log"));
        cut.Render(parameters => parameters.Add(top => top.Threshold, 100));
        cut.Render(parameters => parameters.Add(top => top.Threshold, 100));

        Assert.Equal([["#trail-log", 400], ["#trail-log", 100]],
            _handle.Invocations["set"].Select(call => call.Arguments.ToArray()));
    }

    [Fact]
    public async Task TelemetryName_ShouldCountTheButtonShownOnlyOnceItShows_WhenGiven()
    {
        using var shown = new MetricCollector<long>(Services.GetRequiredService<IMeterFactory>(), "Trailblazor",
            "trailblazor.control.shown");
        var cut = Render<TbBackToTop>(parameters => parameters.Add(top => top.TelemetryName, "article-top"));
        var events = (DotNetObjectReference<BackToTopEvents>)JSInterop.Invocations["attachBackToTop"]
            .Single().Arguments[1]!;
        Assert.Empty(shown.GetMeasurementSnapshot());

        await cut.InvokeAsync(events.Value.OnShown);

        Assert.Equal(["article-top"],
            shown.GetMeasurementSnapshot().Select(measurement => measurement.Tags["trailblazor.telemetry_name"]));
    }

    [Fact]
    public async Task TelemetryName_ShouldCountTheButtonShown_WhenGivenAfterItShows()
    {
        using var shown = new MetricCollector<long>(Services.GetRequiredService<IMeterFactory>(), "Trailblazor",
            "trailblazor.control.shown");
        var cut = Render<TbBackToTop>();
        var events = (DotNetObjectReference<BackToTopEvents>)JSInterop.Invocations["attachBackToTop"]
            .Single().Arguments[1]!;
        await cut.InvokeAsync(events.Value.OnShown);

        cut.Render(parameters => parameters.Add(top => top.TelemetryName, "article-top"));

        Assert.Equal(["article-top"],
            shown.GetMeasurementSnapshot().Select(measurement => measurement.Tags["trailblazor.telemetry_name"]));
    }

    [Fact]
    public async Task ClickAsync_ShouldTakeThePageToTheTopAndCountIt_WhenPressed()
    {
        using var used = new MetricCollector<long>(Services.GetRequiredService<IMeterFactory>(), "Trailblazor",
            "trailblazor.control.used");
        var cut = Render<TbBackToTop>(parameters => parameters.Add(top => top.FocusTarget, "#page-title")
            .Add(top => top.TelemetryName, "article-top"));

        await cut.Tb().BackToTop().ClickAsync();

        Assert.Equal(["#page-title"], _handle.Invocations["top"].Single().Arguments);
        Assert.Single(used.GetMeasurementSnapshot());
    }
}
