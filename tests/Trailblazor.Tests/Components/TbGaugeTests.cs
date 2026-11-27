using System.Diagnostics.Metrics;
using System.Globalization;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.Metrics.Testing;

namespace Trailblazor.Tests.Components;

public class TbGaugeTests : BunitContext
{
    public TbGaugeTests() => this.AddTrailblazor();

    [Fact]
    public void Render_ShouldBeAMeterFilledUpToItsValue_WhenGivenAValue()
    {
        var cut = Render<TbGauge>(parameters => parameters.Add(gauge => gauge.Value, 72)
            .Add(gauge => gauge.Label, "Trailhead parking"));

        var meter = cut.Find("[role=meter]");
        Assert.Equal(("0", "100", "72", "72"), (meter.GetAttribute("aria-valuemin"),
            meter.GetAttribute("aria-valuemax"), meter.GetAttribute("aria-valuenow"),
            meter.GetAttribute("aria-valuetext")));
        Assert.Equal("tb-gauge tb-gauge--primary", meter.ClassName);
        Assert.Equal("--_tb-gauge-percent: 72", meter.GetAttribute("style"));
        // The visible label names it.
        var label = cut.Find("[data-part=label]");
        Assert.Equal(("Trailhead parking", label.Id, null), (label.TextContent,
            meter.GetAttribute("aria-labelledby"), meter.GetAttribute("aria-label")));
        // The track and the filled part are the same whole arc; the styles fill it up to the value.
        var arc = GaugeArc.Path(0, 1, 45);
        var indicator = cut.Find("[data-part=indicator]");
        Assert.Equal((arc, arc, "100"), (cut.Find(".tb-gauge__track").GetAttribute("d"), indicator.GetAttribute("d"),
            indicator.GetAttribute("pathLength")));
        Assert.Equal("72", cut.Find("[data-part=value]").TextContent);
        Assert.Empty(cut.FindAll("[data-part=range], [data-part=min], [data-part=max], .tb-gauge__band"));
    }

    [Fact]
    public void Render_ShouldBeNamedByItsAriaLabel_WhenItHasALabelToo()
    {
        var cut = Render<TbGauge>(parameters => parameters.Add(gauge => gauge.Value, 1)
            .Add(gauge => gauge.Label, "Load").Add(gauge => gauge.AriaLabel, "Load on the first server"));

        var meter = cut.Find("[role=meter]");
        Assert.Equal(("Load on the first server", null), (meter.GetAttribute("aria-label"),
            meter.GetAttribute("aria-labelledby")));
        Assert.Equal("Load", cut.Find("[data-part=label]").TextContent);
    }

    [Theory]
    [InlineData(null, null)]
    [InlineData(" ", "")]
    public void Render_ShouldThrow_WhenItHasNoName(string? label, string? ariaLabel)
    {
        var thrown = Assert.Throws<InvalidOperationException>(() => Render<TbGauge>(parameters => parameters
            .Add(gauge => gauge.Value, 1).Add(gauge => gauge.Label, label).Add(gauge => gauge.AriaLabel, ariaLabel)));

        Assert.Equal("TbGauge needs a Label or an AriaLabel to name it.", thrown.Message);
    }

    [Theory]
    [InlineData(10, 10)]
    [InlineData(10, 5)]
    [InlineData(0, double.NaN)]
    [InlineData(double.NegativeInfinity, 100)]
    [InlineData(0, double.PositiveInfinity)]
    public void Render_ShouldThrow_WhenMaxIsNotAboveMinOrAnEndIsNotFinite(double min, double max)
    {
        var thrown = Assert.Throws<InvalidOperationException>(() => Render<TbGauge>(parameters => parameters
            .Add(gauge => gauge.Label, "Load").Add(gauge => gauge.Min, min).Add(gauge => gauge.Max, max)));

        Assert.Equal("TbGauge needs a Max above its Min, and both finite.", thrown.Message);
    }

    [Theory]
    [InlineData(-45, "-30", "0")]
    [InlineData(45, "30", "100")]
    [InlineData(0, "0", "50")]
    public void Render_ShouldDrawWithinItsEndsAndSayTheValueItself_WhenGivenAnyValue(double value, string now,
        string percent)
    {
        var cut = Render<TbGauge>(parameters => parameters.Add(gauge => gauge.Value, value)
            .Add(gauge => gauge.Min, -30).Add(gauge => gauge.Max, 30).Add(gauge => gauge.Label, "Temperature"));

        var meter = cut.Find("[role=meter]");
        var text = value.ToString(CultureInfo.CurrentCulture);
        Assert.Equal(("-30", "30", now, text), (meter.GetAttribute("aria-valuemin"),
            meter.GetAttribute("aria-valuemax"), meter.GetAttribute("aria-valuenow"),
            meter.GetAttribute("aria-valuetext")));
        Assert.Equal($"--_tb-gauge-percent: {percent}", meter.GetAttribute("style"));
        Assert.Equal(text, cut.Find("[data-part=value]").TextContent);
    }

    [Theory]
    [InlineData(null)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    public void Render_ShouldSayThereIsNoValueAndFillNothing_WhenItHasNoValue(double? value)
    {
        var cut = Render<TbGauge>(parameters => parameters.Add(gauge => gauge.Value, value)
            .Add(gauge => gauge.Min, 5).Add(gauge => gauge.Label, "Summit wind"));

        // A meter has to have a value, so it is at its lowest, and screen readers hear the text in its place.
        var meter = cut.Find("[role=meter]");
        Assert.Equal(("5", "No value"), (meter.GetAttribute("aria-valuenow"), meter.GetAttribute("aria-valuetext")));
        Assert.Equal("tb-gauge tb-gauge--primary tb-gauge--empty", meter.ClassName);
        Assert.Null(meter.GetAttribute("style"));
        Assert.Equal("No value", cut.Find("[data-part=value]").TextContent);
        Assert.Empty(cut.FindAll("[data-part=indicator]"));
        Assert.Single(cut.FindAll(".tb-gauge__track"));
    }

    [Fact]
    public void Render_ShouldWriteItsValueAndEndsInItsFormat_WhenGivenOne()
    {
        var cut = Render<TbGauge>(parameters => parameters.Add(gauge => gauge.Value, 184.4)
            .Add(gauge => gauge.Max, 300).Add(gauge => gauge.Format, "0 'cm'").Add(gauge => gauge.ShowEnds, true)
            .Add(gauge => gauge.Label, "Snow at the pass"));

        Assert.Equal("184 cm", cut.Find("[role=meter]").GetAttribute("aria-valuetext"));
        Assert.Equal(("184 cm", "0 cm", "300 cm"), (cut.Find("[data-part=value]").TextContent,
            cut.Find("[data-part=min]").TextContent, cut.Find("[data-part=max]").TextContent));
        // The number screen readers and the styles get keeps its decimals.
        Assert.Equal("184.4", cut.Find("[role=meter]").GetAttribute("aria-valuenow"));
        Assert.Equal("--_tb-gauge-percent: 61.47", cut.Find("[role=meter]").GetAttribute("style"));
    }

    [Fact]
    public void Render_ShouldWriteTextAsTheCultureDoesAndNumbersWithAPeriod_WhenTheCultureUsesACommaForDecimals()
    {
        var culture = CultureInfo.CurrentCulture;
        CultureInfo.CurrentCulture = new CultureInfo("de-DE");
        try
        {
            var cut = Render<TbGauge>(parameters => parameters.Add(gauge => gauge.Value, 1234.5)
                .Add(gauge => gauge.Max, 2000.5).Add(gauge => gauge.Label, "Hikers"));

            // People read the value as their culture writes it.
            var meter = cut.Find("[role=meter]");
            Assert.Equal(("1234,5", "1234,5"), (cut.Find("[data-part=value]").TextContent,
                meter.GetAttribute("aria-valuetext")));
            // ARIA, the styles and the drawing need a period.
            Assert.Equal(("1234.5", "2000.5", "--_tb-gauge-percent: 61.71"), (meter.GetAttribute("aria-valuenow"),
                meter.GetAttribute("aria-valuemax"), meter.GetAttribute("style")));
            Assert.Equal("M18.18 81.82 A45 45 0 1 1 81.82 81.82", cut.Find(".tb-gauge__track").GetAttribute("d"));
        }
        finally
        {
            CultureInfo.CurrentCulture = culture;
        }
    }

    [Fact]
    public void Render_ShouldTakeItsToneAndDiameter_WhenTheyAreGiven()
    {
        var cut = Render<TbGauge>(parameters => parameters.Add(gauge => gauge.Value, 50)
            .Add(gauge => gauge.Tone, Tone.Info).Add(gauge => gauge.Diameter, "7rem")
            .Add(gauge => gauge.Label, "Water tank").Add(gauge => gauge.Class, "mine")
            .Add(gauge => gauge.Style, "margin: 0"));

        var meter = cut.Find("[role=meter]");
        Assert.Equal("tb-gauge tb-gauge--info mine", meter.ClassName);
        Assert.Equal("--tb-gauge-diameter: 7rem; --_tb-gauge-percent: 50; margin: 0", meter.GetAttribute("style"));
    }

    [Fact]
    public void Ranges_ShouldNameTheValuesRangeAndGiveItsTone_WhenTheValueIsInOne()
    {
        var cut = RenderBeds(29);

        var meter = cut.Find("[role=meter]");
        Assert.Equal(("29, Busy", "tb-gauge tb-gauge--warning"), (meter.GetAttribute("aria-valuetext"),
            meter.ClassName));
        Assert.Equal(("29", "Busy"), (cut.Find("[data-part=value]").TextContent,
            cut.Find("[data-part=range]").TextContent));
        // A band for each range, in its tone, and the value's arc inside them.
        Assert.Equal(["tb-gauge__band tb-gauge__band--success", "tb-gauge__band tb-gauge__band--warning",
            "tb-gauge__band tb-gauge__band--danger"], cut.FindAll(".tb-gauge__band").Select(band => band.ClassName));
        Assert.Equal(GaugeArc.Path(0, 1, 39.5), cut.Find("[data-part=indicator]").GetAttribute("d"));
    }

    [Fact]
    public void Ranges_ShouldStopShortOfEachOtherAndReachTheDialsEnds_WhenDrawn()
    {
        var cut = RenderBeds(29);

        // 24 of 40 and 34 of 40 are 0.6 and 0.85 of the arc; a band leaves 0.004 of it clear where it meets another.
        Assert.Equal([GaugeArc.Path(0, 0.596, 48), GaugeArc.Path(0.604, 0.846, 48), GaugeArc.Path(0.854, 1, 48)],
            cut.FindAll(".tb-gauge__band").Select(band => band.GetAttribute("d")));
    }

    [Theory]
    [InlineData(0, "Quiet", "success")]
    [InlineData(24, "Quiet", "success")]
    [InlineData(24.5, "Busy", "warning")]
    [InlineData(34, "Busy", "warning")]
    [InlineData(40, "Nearly full", "danger")]
    [InlineData(55, "Nearly full", "danger")]
    [InlineData(-3, "Quiet", "success")]
    public void Ranges_ShouldHoldTheValueTheyEndAt_WhenTheValueIsOnTheLineBetweenTwo(double value, string name,
        string tone)
    {
        var cut = RenderBeds(value);

        Assert.Equal(name, cut.Find("[data-part=range]").TextContent);
        Assert.Contains($"tb-gauge--{tone}", cut.Find("[role=meter]").ClassList);
    }

    [Fact]
    public void Ranges_ShouldLeaveTheValueUnnamedInItsOwnTone_WhenTheyStopShortOfIt()
    {
        var cut = Render<TbGauge>(parameters => parameters.Add(gauge => gauge.Value, 80)
            .Add(gauge => gauge.Label, "Load").Add(gauge => gauge.Tone, Tone.Info)
            .AddChildContent<TbGaugeRange>(range => range.Add(one => one.To, 50).Add(one => one.Name, "Low")
                .Add(one => one.Tone, Tone.Success)));

        var meter = cut.Find("[role=meter]");
        Assert.Equal(("80", "tb-gauge tb-gauge--info"), (meter.GetAttribute("aria-valuetext"), meter.ClassName));
        Assert.Empty(cut.FindAll("[data-part=range]"));
        // The one band runs to 50 in full, with no band after it to stop short of.
        Assert.Equal(GaugeArc.Path(0, 0.5, 48), cut.Find(".tb-gauge__band").GetAttribute("d"));
    }

    [Fact]
    public void Ranges_ShouldKeepTheGaugesToneAndSayOnlyTheValue_WhenARangeHasNoToneOrName()
    {
        var cut = Render<TbGauge>(parameters => parameters.Add(gauge => gauge.Value, 20)
            .Add(gauge => gauge.Label, "Load").AddChildContent<TbGaugeRange>(range => range.Add(one => one.To, 50)));

        var meter = cut.Find("[role=meter]");
        Assert.Equal(("20", "tb-gauge tb-gauge--primary"), (meter.GetAttribute("aria-valuetext"), meter.ClassName));
        Assert.Empty(cut.FindAll("[data-part=range]"));
        Assert.Equal("tb-gauge__band", cut.Find(".tb-gauge__band").ClassName);
    }

    [Fact]
    public void Ranges_ShouldRunInTheOrderOfTheirEnds_WhenWrittenInAnotherOrder()
    {
        var cut = Render<TbGauge>(parameters => parameters.Add(gauge => gauge.Value, 50)
            .Add(gauge => gauge.Label, "Load")
            .AddChildContent<TbGaugeRange>(range => range.Add(one => one.To, 60).Add(one => one.Name, "Middle"))
            .AddChildContent<TbGaugeRange>(range => range.Add(one => one.To, 150).Add(one => one.Name, "High"))
            .AddChildContent<TbGaugeRange>(range => range.Add(one => one.To, 40).Add(one => one.Name, "Low"))
            .AddChildContent<TbGaugeRange>(range => range.Add(one => one.To, -5).Add(one => one.Name, "Below")));

        // Low runs to 40, Middle to 60 and High to the dial's end, which it would pass. The range that ends below
        // the dial has no length and no band.
        Assert.Equal([GaugeArc.Path(0, 0.396, 48), GaugeArc.Path(0.404, 0.596, 48), GaugeArc.Path(0.604, 1, 48)],
            cut.FindAll(".tb-gauge__band").Select(band => band.GetAttribute("d")));
        Assert.Equal("Middle", cut.Find("[data-part=range]").TextContent);
    }

    [Fact]
    public void Ranges_ShouldPlaceARangeByItsEnd_WhenItAppearsAfterTheOthers()
    {
        var showBusy = false;
        var cut = Render<TbGauge>(parameters => parameters.Add(gauge => gauge.Value, 29)
            .Add(gauge => gauge.Max, 40).Add(gauge => gauge.Label, "Beds booked").AddChildContent(Ranges()));
        Assert.Equal("29, Nearly full", cut.Find("[role=meter]").GetAttribute("aria-valuetext"));

        showBusy = true;
        cut.Render(parameters => parameters.AddChildContent(Ranges()));

        // The range written between the two others, and drawn after them, holds the values between theirs.
        Assert.Equal("29, Busy", cut.Find("[role=meter]").GetAttribute("aria-valuetext"));
        Assert.Equal([GaugeArc.Path(0, 0.596, 48), GaugeArc.Path(0.604, 0.846, 48), GaugeArc.Path(0.854, 1, 48)],
            cut.FindAll(".tb-gauge__band").Select(band => band.GetAttribute("d")));

        RenderFragment Ranges() => builder =>
        {
            builder.OpenComponent<TbGaugeRange>(0);
            builder.AddComponentParameter(1, nameof(TbGaugeRange.To), (double?)24);
            builder.AddComponentParameter(2, nameof(TbGaugeRange.Name), "Quiet");
            builder.CloseComponent();
            if (showBusy)
            {
                builder.OpenComponent<TbGaugeRange>(3);
                builder.AddComponentParameter(4, nameof(TbGaugeRange.To), (double?)34);
                builder.AddComponentParameter(5, nameof(TbGaugeRange.Name), "Busy");
                builder.CloseComponent();
            }

            builder.OpenComponent<TbGaugeRange>(6);
            builder.AddComponentParameter(7, nameof(TbGaugeRange.Name), "Nearly full");
            builder.CloseComponent();
        };
    }

    [Fact]
    public void Ranges_ShouldDrawAgain_WhenARangeChangesOrLeaves()
    {
        var showLast = true;
        var cut = Render<TbGauge>(parameters => parameters.Add(gauge => gauge.Value, 30)
            .Add(gauge => gauge.Label, "Load").AddChildContent(Ranges(50)));

        cut.Render(parameters => parameters.AddChildContent(Ranges(20)));

        Assert.Equal("30, High", cut.Find("[role=meter]").GetAttribute("aria-valuetext"));
        Assert.Equal(2, cut.FindAll(".tb-gauge__band").Count);

        showLast = false;
        cut.Render(parameters => parameters.AddChildContent(Ranges(20)));

        Assert.Equal("30", cut.Find("[role=meter]").GetAttribute("aria-valuetext"));
        Assert.Single(cut.FindAll(".tb-gauge__band"));

        RenderFragment Ranges(double lowEnds) => builder =>
        {
            builder.OpenComponent<TbGaugeRange>(0);
            builder.AddComponentParameter(1, nameof(TbGaugeRange.To), (double?)lowEnds);
            builder.AddComponentParameter(2, nameof(TbGaugeRange.Name), "Low");
            builder.CloseComponent();
            if (showLast)
            {
                builder.OpenComponent<TbGaugeRange>(3);
                builder.AddComponentParameter(4, nameof(TbGaugeRange.Name), "High");
                builder.CloseComponent();
            }
        };
    }

    [Fact]
    public void Labels_ShouldRewordWhatTheGaugeShowsAndSays_WhenGiven()
    {
        var cut = Render<TbGauge>(parameters => parameters.Add(gauge => gauge.Label, "Summit wind")
            .Add(gauge => gauge.Labels, new() { NoValue = "Offline", ValueInRange = "{0} ({1})" })
            .AddChildContent<TbGaugeRange>(range => range.Add(one => one.Name, "Calm")));

        Assert.Equal(("Offline", "Offline"), (cut.Find("[role=meter]").GetAttribute("aria-valuetext"),
            cut.Find("[data-part=value]").TextContent));

        cut.Render(parameters => parameters.Add(gauge => gauge.Value, 12));

        Assert.Equal("12 (Calm)", cut.Find("[role=meter]").GetAttribute("aria-valuetext"));
    }

    [Fact]
    public void Range_ShouldThrow_WhenItIsOutsideAGauge()
    {
        var thrown = Assert.Throws<InvalidOperationException>(() => Render<TbGaugeRange>());

        Assert.Equal("TbGaugeRange must be inside a gauge.", thrown.Message);
    }

    [Fact]
    public void Telemetry_ShouldCountAShow_WhenGivenATelemetryName()
    {
        using var shown = new MetricCollector<long>(Services.GetRequiredService<IMeterFactory>(), "Trailblazor",
            "trailblazor.control.shown");

        Render<TbGauge>(parameters => parameters.Add(gauge => gauge.Value, 1).Add(gauge => gauge.Label, "Load")
            .Add(gauge => gauge.TelemetryName, "server-load"));

        Assert.Single(shown.GetMeasurementSnapshot());
    }

    // Beds booked in a hut of 40: quiet up to 24, busy up to 34, and nearly full from there.
    private IRenderedComponent<TbGauge> RenderBeds(double booked) => Render<TbGauge>(parameters => parameters
        .Add(gauge => gauge.Value, booked).Add(gauge => gauge.Max, 40).Add(gauge => gauge.Label, "Beds booked")
        .AddChildContent<TbGaugeRange>(range => range.Add(one => one.To, 24).Add(one => one.Name, "Quiet")
            .Add(one => one.Tone, Tone.Success))
        .AddChildContent<TbGaugeRange>(range => range.Add(one => one.To, 34).Add(one => one.Name, "Busy")
            .Add(one => one.Tone, Tone.Warning))
        .AddChildContent<TbGaugeRange>(range => range.Add(one => one.Name, "Nearly full")
            .Add(one => one.Tone, Tone.Danger)));
}
