using System.Diagnostics;
using System.Diagnostics.Metrics;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.Metrics.Testing;

namespace Trailblazor.Tests.Spike;

// SPIKE (#101): how a consumer's test would see what the components report, and what reporting costs.
public class TelemetrySpikeTests(ITestOutputHelper output) : BunitContext
{
    [Fact]
    public void Button_ShouldCountItsFeatureShownAndUsed_WhenClicked()
    {
        this.AddTrailblazor();
        using var shown = new MetricCollector<long>(null, "Trailblazor", "trailblazor.feature.shown");
        using var used = new MetricCollector<long>(null, "Trailblazor", "trailblazor.feature.used");
        var cut = Render<TbButton>(parameters => parameters.Add(button => button.Feature, "export-csv")
            .Add(button => button.ChildContent, (RenderFragment)(builder => builder.AddContent(0, "Export"))));

        cut.Find("button").Click();

        Assert.Contains(shown.GetMeasurementSnapshot(), m => Equals(m.Tags["tb.feature"], "export-csv"));
        Assert.Single(used.GetMeasurementSnapshot(), m => Equals(m.Tags["tb.feature"], "export-csv"));
    }

    [Fact]
    public async Task Dialog_ShouldCountHowItClosed_WhenConfirmedOrCancelled()
    {
        this.AddTrailblazor();
        using var closed = new MetricCollector<long>(null, "Trailblazor", "trailblazor.dialog.closed");
        var dialogs = Services.GetRequiredService<IDialogService>();

        var first = dialogs.Show<SpikeDialog, bool>("Delete the trail?");
        await first.CloseAsync(true);
        var second = dialogs.Show<SpikeDialog, bool>("Delete the trail?");
        await second.CancelAsync();

        Assert.Equal(["confirmed", "cancelled"], closed.GetMeasurementSnapshot()
            .Where(m => Equals(m.Tags["tb.dialog.name"], nameof(SpikeDialog))).Select(m => m.Tags["tb.dialog.outcome"]));
    }

    [Fact]
    public void Counter_ShouldCostLittle_WhenNothingListensAndWhenSomethingDoes()
    {
        var meter = new Meter("Trailblazor.Spike.Cost");
        var counter = meter.CreateCounter<long>("spike.used");
        var rounds = 2_000_000;
        var idle = Time(() => Add(counter));
        using var listener = new MeterListener();
        listener.InstrumentPublished = (instrument, l) =>
        {
            if (instrument.Meter == meter)
            {
                l.EnableMeasurementEvents(instrument);
            }
        };
        long total = 0;
        listener.SetMeasurementEventCallback<long>((_, value, _, _) => total += value);
        listener.Start();
        var listened = Time(() => Add(counter));

        File.WriteAllText(Path.Combine(Path.GetTempPath(), "tb-telemetry-cost.txt"), $"idle: {idle:F1} ns per add, listened: {listened:F1} ns per add");
        output.WriteLine($"idle {idle:F1} ns, listened {listened:F1} ns");
        Assert.True(total > 0);

        double Time(Action action)
        {
            action();
            var watch = Stopwatch.StartNew();
            for (var i = 0; i < rounds; i++)
            {
                action();
            }

            return watch.Elapsed.TotalNanoseconds / rounds;
        }

        static void Add(Counter<long> counter)
        {
            if (counter.Enabled)
            {
                counter.Add(1, new("tb.feature", "export-csv"), new("tb.component", "TbButton"));
            }
        }
    }

    private sealed class SpikeDialog : TbDialogContent<bool>
    {
    }
}
