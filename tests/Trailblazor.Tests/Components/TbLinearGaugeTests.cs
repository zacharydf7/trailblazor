using System.Diagnostics.Metrics;
using System.Globalization;
using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.Metrics.Testing;

namespace Trailblazor.Tests.Components;

public class TbLinearGaugeTests : BunitContext
{
    public TbLinearGaugeTests() => this.AddTrailblazor();

    [Fact]
    public void Render_ShouldBeAMeterWithItsBarFilledUpToItsValue_WhenGivenAValue()
    {
        var cut = Render<TbLinearGauge>(parameters => parameters.Add(gauge => gauge.Value, 72)
            .Add(gauge => gauge.Label, "Trailhead parking").Add(gauge => gauge.Format, "0'%'"));

        var meter = cut.Find("[role=meter]");
        Assert.Equal(("0", "100", "72", "72%"), (meter.GetAttribute("aria-valuemin"),
            meter.GetAttribute("aria-valuemax"), meter.GetAttribute("aria-valuenow"),
            meter.GetAttribute("aria-valuetext")));
        Assert.Equal("tb-linear-gauge tb-linear-gauge--primary", meter.ClassName);
        Assert.Equal("--_tb-gauge-percent: 72", meter.GetAttribute("style"));
        // The visible label names it, and the value's text is beside the label, above the bar.
        var label = cut.Find("[data-part=label]");
        Assert.Equal(("Trailhead parking", label.Id, null), (label.TextContent,
            meter.GetAttribute("aria-labelledby"), meter.GetAttribute("aria-label")));
        Assert.Equal("72%", cut.Find(".tb-linear-gauge__header [data-part=value]").TextContent);
        Assert.Single(cut.FindAll("[data-part=bar] > [data-part=indicator]"));
        Assert.Empty(cut.FindAll("[data-part=target], [data-part=range], [data-part=min], .tb-linear-gauge__band"));
    }

    [Fact]
    public void Render_ShouldBeNamedByItsAriaLabelWithNoLabelPart_WhenGivenOnlyAnAriaLabel()
    {
        var cut = Render<TbLinearGauge>(parameters => parameters.Add(gauge => gauge.Value, 5)
            .Add(gauge => gauge.AriaLabel, "Load on the first server"));

        Assert.Equal("Load on the first server", cut.Find("[role=meter]").GetAttribute("aria-label"));
        Assert.Empty(cut.FindAll("[data-part=label]"));
        Assert.Equal("5", cut.Find(".tb-linear-gauge__header > .tb-linear-gauge__reading").TextContent.Trim());
    }

    [Fact]
    public void Render_ShouldThrowInItsOwnName_WhenItHasNoNameOrItsEndsAreOutOfOrder()
    {
        var unnamed = Assert.Throws<InvalidOperationException>(() => Render<TbLinearGauge>());
        var backward = Assert.Throws<InvalidOperationException>(() => Render<TbLinearGauge>(parameters => parameters
            .Add(gauge => gauge.Label, "Load").Add(gauge => gauge.Min, 10).Add(gauge => gauge.Max, 10)));

        Assert.Equal("TbLinearGauge needs a Label or an AriaLabel to name it.", unnamed.Message);
        Assert.Equal("TbLinearGauge needs a Max above its Min, and both finite.", backward.Message);
    }

    [Theory]
    [InlineData(-45, "-30", "0")]
    [InlineData(45, "30", "100")]
    [InlineData(-15, "-15", "25")]
    public void Render_ShouldDrawWithinItsEndsAndSayTheValueItself_WhenGivenAnyValue(double value, string now,
        string percent)
    {
        var cut = Render<TbLinearGauge>(parameters => parameters.Add(gauge => gauge.Value, value)
            .Add(gauge => gauge.Min, -30).Add(gauge => gauge.Max, 30).Add(gauge => gauge.Label, "Temperature"));

        var meter = cut.Find("[role=meter]");
        var text = value.ToString(CultureInfo.CurrentCulture);
        Assert.Equal((now, text, $"--_tb-gauge-percent: {percent}"), (meter.GetAttribute("aria-valuenow"),
            meter.GetAttribute("aria-valuetext"), meter.GetAttribute("style")));
    }

    [Theory]
    [InlineData(null)]
    [InlineData(double.NaN)]
    public void Render_ShouldSayThereIsNoValueAndFillNothing_WhenItHasNoValue(double? value)
    {
        var cut = Render<TbLinearGauge>(parameters => parameters.Add(gauge => gauge.Value, value)
            .Add(gauge => gauge.Label, "Summit wind"));

        var meter = cut.Find("[role=meter]");
        Assert.Equal(("0", "No value"), (meter.GetAttribute("aria-valuenow"), meter.GetAttribute("aria-valuetext")));
        Assert.Equal("tb-linear-gauge tb-linear-gauge--primary tb-linear-gauge--empty", meter.ClassName);
        Assert.Null(meter.GetAttribute("style"));
        Assert.Equal("No value", cut.Find("[data-part=value]").TextContent);
        Assert.Empty(cut.FindAll("[data-part=indicator]"));
        Assert.Single(cut.FindAll("[data-part=bar]"));
    }

    [Fact]
    public void Render_ShouldWriteItsEndsUnderTheBarInItsFormat_WhenShowingThem()
    {
        var cut = Render<TbLinearGauge>(parameters => parameters.Add(gauge => gauge.Value, 184)
            .Add(gauge => gauge.Max, 300).Add(gauge => gauge.Format, "0 'cm'").Add(gauge => gauge.ShowEnds, true)
            .Add(gauge => gauge.Label, "Snow at the pass"));

        Assert.Equal(("0 cm", "300 cm"), (cut.Find(".tb-linear-gauge__ends > [data-part=min]").TextContent,
            cut.Find(".tb-linear-gauge__ends > [data-part=max]").TextContent));
    }

    [Fact]
    public void Target_ShouldBeMarkedOnTheBarAndReadAfterTheValue_WhenGiven()
    {
        var cut = Render<TbLinearGauge>(parameters => parameters.Add(gauge => gauge.Value, 310)
            .Add(gauge => gauge.Max, 500).Add(gauge => gauge.Target, 400).Add(gauge => gauge.Label, "Hours"));

        var meter = cut.Find("[role=meter]");
        Assert.Equal("310, target 400", meter.GetAttribute("aria-valuetext"));
        Assert.Equal("--_tb-gauge-percent: 62; --_tb-gauge-target: 80", meter.GetAttribute("style"));
        Assert.Single(cut.FindAll("[data-part=bar] > [data-part=target]"));
        // The value's own text doesn't change.
        Assert.Equal("310", cut.Find("[data-part=value]").TextContent);
    }

    [Theory]
    [InlineData(650, "100")]
    [InlineData(-20, "0")]
    public void Target_ShouldBeDrawnAtTheEndAndSaidAsItIs_WhenItIsBeyondAnEnd(double target, string percent)
    {
        var cut = Render<TbLinearGauge>(parameters => parameters.Add(gauge => gauge.Value, 250)
            .Add(gauge => gauge.Max, 500).Add(gauge => gauge.Target, target).Add(gauge => gauge.Label, "Hours"));

        var meter = cut.Find("[role=meter]");
        Assert.Equal($"250, target {target.ToString(CultureInfo.CurrentCulture)}",
            meter.GetAttribute("aria-valuetext"));
        Assert.Equal($"--_tb-gauge-percent: 50; --_tb-gauge-target: {percent}", meter.GetAttribute("style"));
    }

    [Theory]
    [InlineData(double.NaN)]
    [InlineData(double.NegativeInfinity)]
    public void Target_ShouldNotBeDrawnOrSaid_WhenItIsNotFinite(double target)
    {
        var cut = Render<TbLinearGauge>(parameters => parameters.Add(gauge => gauge.Value, 250)
            .Add(gauge => gauge.Max, 500).Add(gauge => gauge.Target, target).Add(gauge => gauge.Label, "Hours"));

        Assert.Equal("250", cut.Find("[role=meter]").GetAttribute("aria-valuetext"));
        Assert.Equal("--_tb-gauge-percent: 50", cut.Find("[role=meter]").GetAttribute("style"));
        Assert.Empty(cut.FindAll("[data-part=target]"));
    }

    [Fact]
    public void Target_ShouldFollowTheRangeAndKeepItsMark_WhenTheGaugeHasRangesOrNoValue()
    {
        var cut = RenderBeds(29, 30);

        Assert.Equal("29, Busy, target 30", cut.Find("[role=meter]").GetAttribute("aria-valuetext"));

        cut.Render(parameters => parameters.Add(gauge => gauge.Value, (double?)null)
            .Add(gauge => gauge.Labels, new() { NoValue = "Offline", ValueWithTarget = "{0} (goal {1})" }));

        Assert.Equal("Offline (goal 30)", cut.Find("[role=meter]").GetAttribute("aria-valuetext"));
        Assert.Equal("--_tb-gauge-target: 75", cut.Find("[role=meter]").GetAttribute("style"));
        Assert.Single(cut.FindAll("[data-part=target]"));
    }

    [Fact]
    public void Ranges_ShouldNameTheValuesRangeAndDrawABandForEach_WhenTheValueIsInOne()
    {
        var cut = RenderBeds(29, null);

        var meter = cut.Find("[role=meter]");
        Assert.Equal(("29, Busy", "tb-linear-gauge tb-linear-gauge--warning"), (meter.GetAttribute("aria-valuetext"),
            meter.ClassName));
        Assert.Equal(("29", "Busy"), (cut.Find("[data-part=value]").TextContent,
            cut.Find("[data-part=range]").TextContent));
        // Each band has its tone and the hundredths of the bar it runs between: 24 and 34 of 40 are 60 and 85.
        var bands = cut.FindAll(".tb-linear-gauge__bands > .tb-linear-gauge__band");
        Assert.Equal(["tb-linear-gauge__band tb-linear-gauge__band--success",
            "tb-linear-gauge__band tb-linear-gauge__band--warning",
            "tb-linear-gauge__band tb-linear-gauge__band--danger"], bands.Select(band => band.ClassName));
        Assert.Equal(["--_tb-gauge-from: 0; --_tb-gauge-to: 60", "--_tb-gauge-from: 60; --_tb-gauge-to: 85",
            "--_tb-gauge-from: 85; --_tb-gauge-to: 100"], bands.Select(band => band.GetAttribute("style")));
    }

    [Fact]
    public void Ranges_ShouldKeepTheGaugesToneAndDrawANeutralBand_WhenARangeHasNoTone()
    {
        var cut = Render<TbLinearGauge>(parameters => parameters.Add(gauge => gauge.Value, 20)
            .Add(gauge => gauge.Label, "Load").Add(gauge => gauge.Tone, Tone.Info)
            .AddChildContent<TbGaugeRange>(range => range.Add(one => one.To, 50).Add(one => one.Name, "Low")));

        Assert.Equal("tb-linear-gauge tb-linear-gauge--info", cut.Find("[role=meter]").ClassName);
        Assert.Equal("tb-linear-gauge__band", cut.Find(".tb-linear-gauge__band").ClassName);
        Assert.Equal("Low", cut.Find("[data-part=range]").TextContent);
    }

    [Fact]
    public void Render_ShouldWriteNumbersWithAPeriod_WhenTheCultureUsesACommaForDecimals()
    {
        var culture = CultureInfo.CurrentCulture;
        CultureInfo.CurrentCulture = new CultureInfo("de-DE");
        try
        {
            var cut = Render<TbLinearGauge>(parameters => parameters.Add(gauge => gauge.Value, 2.5)
                .Add(gauge => gauge.Max, 8).Add(gauge => gauge.Target, 6.5).Add(gauge => gauge.Label, "Depth")
                .AddChildContent<TbGaugeRange>(range => range.Add(one => one.To, 1.5)));

            // People read the text as their culture writes it; the styles need a period.
            var meter = cut.Find("[role=meter]");
            Assert.Equal("2,5, target 6,5", meter.GetAttribute("aria-valuetext"));
            Assert.Equal("--_tb-gauge-percent: 31.25; --_tb-gauge-target: 81.25", meter.GetAttribute("style"));
            Assert.Equal("--_tb-gauge-from: 0; --_tb-gauge-to: 18.75",
                cut.Find(".tb-linear-gauge__band").GetAttribute("style"));
        }
        finally
        {
            CultureInfo.CurrentCulture = culture;
        }
    }

    [Fact]
    public void Telemetry_ShouldCountAShow_WhenGivenATelemetryName()
    {
        using var shown = new MetricCollector<long>(Services.GetRequiredService<IMeterFactory>(), "Trailblazor",
            "trailblazor.control.shown");

        Render<TbLinearGauge>(parameters => parameters.Add(gauge => gauge.Value, 1).Add(gauge => gauge.Label, "Load")
            .Add(gauge => gauge.TelemetryName, "server-load"));

        Assert.Single(shown.GetMeasurementSnapshot());
    }

    // Beds booked in a hut of 40: quiet up to 24, busy up to 34, and nearly full from there.
    private IRenderedComponent<TbLinearGauge> RenderBeds(double booked, double? target) =>
        Render<TbLinearGauge>(parameters => parameters.Add(gauge => gauge.Value, booked).Add(gauge => gauge.Max, 40)
            .Add(gauge => gauge.Target, target).Add(gauge => gauge.Label, "Beds booked")
            .AddChildContent<TbGaugeRange>(range => range.Add(one => one.To, 24).Add(one => one.Name, "Quiet")
                .Add(one => one.Tone, Tone.Success))
            .AddChildContent<TbGaugeRange>(range => range.Add(one => one.To, 34).Add(one => one.Name, "Busy")
                .Add(one => one.Tone, Tone.Warning))
            .AddChildContent<TbGaugeRange>(range => range.Add(one => one.Name, "Nearly full")
                .Add(one => one.Tone, Tone.Danger)));
}
