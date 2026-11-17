using System.Diagnostics.Metrics;
using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.Metrics.Testing;

namespace Trailblazor.Tests.Components;

public class TbWatermarkTests : BunitContext
{
    public TbWatermarkTests() => this.AddTrailblazor();

    [Fact]
    public void Render_ShouldTileTheTextOverTheContentAndSayItOnce_WhenGivenText()
    {
        var cut = Render<TbWatermark>(parameters => parameters.Add(mark => mark.Text, "Draft\nDo not share")
            .AddChildContent("<p>Trail report</p>"));

        var marks = cut.Find("[data-part=marks]");
        Assert.Equal("true", marks.GetAttribute("aria-hidden"));
        Assert.Contains("tb-watermark__marks--text", marks.ClassList);
        var tile = Uri.UnescapeDataString(marks.GetAttribute("style")!);
        Assert.Contains(">Draft</tspan>", tile);
        Assert.Contains(">Do not share</tspan>", tile);
        Assert.Contains("rotate(-22 ", tile);
        Assert.Equal("Draft Do not share", cut.Find("[data-part=label]").TextContent);
        Assert.Equal("Trail report", cut.Find("p").TextContent);
    }

    [Fact]
    public void Render_ShouldEscapeTheTextInTheTile_WhenItHasMarkup()
    {
        var cut = Render<TbWatermark>(parameters => parameters.Add(mark => mark.Text, "<script>&\"'</script>"));

        var tile = Uri.UnescapeDataString(cut.Find("[data-part=marks]").GetAttribute("style")!);
        Assert.DoesNotContain("<script>", tile);
        Assert.Contains("&lt;script&gt;&amp;&quot;&#39;&lt;/script&gt;", tile);
    }

    [Fact]
    public void Render_ShouldTileTheImageAndSayItsAlt_WhenGivenAnImage()
    {
        var cut = Render<TbWatermark>(parameters => parameters.Add(mark => mark.Image, "/logo\".png\f)")
            .Add(mark => mark.Alt, "Confidential").Add(mark => mark.ImageWidth, 100));

        var marks = cut.Find("[data-part=marks]");
        Assert.DoesNotContain("tb-watermark__marks--text", marks.ClassList);
        Assert.Equal("--tb-watermark-tile: url(\"/logo\\\".png\\c )\"); --tb-watermark-size: 100px",
            marks.GetAttribute("style"));
        Assert.Equal("Confidential", cut.Find("[data-part=label]").TextContent);
    }

    [Fact]
    public void Render_ShouldSizeTheTileToHoldTheSlantedText_WhenRotated()
    {
        var cut = Render<TbWatermark>(parameters => parameters.Add(mark => mark.Text, new string('W', 20))
            .Add(mark => mark.Rotate, -90).Add(mark => mark.Gap, 10));

        var tile = Uri.UnescapeDataString(cut.Find("[data-part=marks]").GetAttribute("style")!);
        Assert.Contains("width='32' height='330'", tile);
    }

    [Fact]
    public void Render_ShouldSayNothing_WhenDecorative()
    {
        var cut = Render<TbWatermark>(parameters => parameters.Add(mark => mark.Image, "/logo.png")
            .Add(mark => mark.Decorative, true));

        Assert.Empty(cut.FindAll("[data-part=label]"));
    }

    [Fact]
    public void TelemetryName_ShouldCountTheWatermarkShown_WhenGiven()
    {
        using var shown = new MetricCollector<long>(Services.GetRequiredService<IMeterFactory>(), "Trailblazor",
            "trailblazor.control.shown");

        Render<TbWatermark>(parameters => parameters.Add(mark => mark.Text, "Draft")
            .Add(mark => mark.TelemetryName, "draft-report"));

        Assert.Equal(["draft-report"],
            shown.GetMeasurementSnapshot().Select(measurement => measurement.Tags["trailblazor.telemetry_name"]));
    }

    [Theory]
    [InlineData(null, null, "TbWatermark needs a Text or an Image.")]
    [InlineData(null, "/logo.png", "TbWatermark needs Alt text for its Image, or Decorative for marks that say nothing.")]
    public void Render_ShouldThrow_WhenItHasNothingToShowOrNoAlt(string? text, string? image, string message)
    {
        var thrown = Assert.Throws<InvalidOperationException>(() => Render<TbWatermark>(parameters => parameters
            .Add(mark => mark.Text, text).Add(mark => mark.Image, image)));

        Assert.Equal(message, thrown.Message);
    }
}
