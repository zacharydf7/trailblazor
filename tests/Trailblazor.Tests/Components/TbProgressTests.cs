using System.Globalization;
using Bunit;

namespace Trailblazor.Tests.Components;

public class TbProgressTests : BunitContext
{
    [Fact]
    public void Render_ShouldBeAnIndeterminateLinearBar_WhenNoValueIsSet()
    {
        var cut = Render<TbProgress>(parameters => parameters.Add(progress => progress.AriaLabel, "Loading"));

        var progress = cut.Find("[role=progressbar]");
        Assert.Equal("Loading", progress.GetAttribute("aria-label"));
        Assert.Null(progress.GetAttribute("aria-valuenow"));
        Assert.Null(progress.GetAttribute("style"));
        Assert.Contains("tb-progress--linear", progress.ClassList);
        Assert.Contains("tb-progress--indeterminate", progress.ClassList);
    }

    [Fact]
    public void Render_ShouldReportTheValueAndPercent_WhenGivenAValueInARange()
    {
        var cut = Render<TbProgress>(parameters => parameters
            .Add(progress => progress.AriaLabel, "Uploading")
            .Add(progress => progress.Value, 15)
            .Add(progress => progress.Min, 10)
            .Add(progress => progress.Max, 30));

        var progress = cut.Find("[role=progressbar]");
        Assert.Equal("10", progress.GetAttribute("aria-valuemin"));
        Assert.Equal("30", progress.GetAttribute("aria-valuemax"));
        Assert.Equal("15", progress.GetAttribute("aria-valuenow"));
        Assert.Equal("--tb-progress-percent: 25", progress.GetAttribute("style"));
        Assert.DoesNotContain("tb-progress--indeterminate", progress.ClassList);
    }

    [Fact]
    public void Render_ShouldClampTheValue_WhenGivenAValuePastMax()
    {
        var cut = Render<TbProgress>(parameters => parameters
            .Add(progress => progress.AriaLabel, "Uploading")
            .Add(progress => progress.Value, 140));

        var progress = cut.Find("[role=progressbar]");
        Assert.Equal("100", progress.GetAttribute("aria-valuenow"));
        Assert.Equal("--tb-progress-percent: 100", progress.GetAttribute("style"));
    }

    [Fact]
    public void Render_ShouldUseAPeriodForDecimals_WhenTheCultureUsesACommaForThem()
    {
        var culture = CultureInfo.CurrentCulture;
        CultureInfo.CurrentCulture = new CultureInfo("de-DE");
        try
        {
            var cut = Render<TbProgress>(parameters => parameters
                .Add(progress => progress.AriaLabel, "Uploading")
                .Add(progress => progress.Value, 12.5));

            var progress = cut.Find("[role=progressbar]");
            Assert.Equal("12.5", progress.GetAttribute("aria-valuenow"));
            Assert.Equal("--tb-progress-percent: 12.5", progress.GetAttribute("style"));
        }
        finally
        {
            CultureInfo.CurrentCulture = culture;
        }
    }

    [Fact]
    public void Render_ShouldShowTheLabelAndThePercentInTheUsersCulture_WhenAskedToShowTheValue()
    {
        var culture = CultureInfo.CurrentCulture;
        // Turkish puts the percent sign first, unlike the invariant culture.
        CultureInfo.CurrentCulture = new CultureInfo("tr-TR");
        try
        {
            var cut = Render<TbProgress>(parameters => parameters.Add(progress => progress.AriaLabel, "Upload")
                .Add(progress => progress.Value, 25).Add(progress => progress.Label, "report.pdf")
                .Add(progress => progress.ShowValue, true));

            Assert.Equal("report.pdf", cut.Find("[data-part=label]").TextContent);
            Assert.Equal("%25", cut.Find("[data-part=value]").TextContent);
            Assert.Null(cut.Find("[role=progressbar]").GetAttribute("aria-valuetext"));
        }
        finally
        {
            CultureInfo.CurrentCulture = culture;
        }
    }

    [Fact]
    public void Render_ShouldShowAndAnnounceTheFormattedValue_WhenGivenAValueFormatter()
    {
        var cut = Render<TbProgress>(parameters => parameters.Add(progress => progress.AriaLabel, "Upload")
            .Add(progress => progress.Value, 3).Add(progress => progress.Max, 10)
            .Add(progress => progress.ShowValue, true)
            .Add(progress => progress.ValueFormatter, value => $"{value} of 10 files"));

        Assert.Equal("3 of 10 files", cut.Find("[data-part=value]").TextContent);
        Assert.Equal("3 of 10 files", cut.Find("[role=progressbar]").GetAttribute("aria-valuetext"));
    }

    [Fact]
    public void Render_ShouldShowTheLabelButNoValue_WhenIndeterminate()
    {
        var cut = Render<TbProgress>(parameters => parameters.Add(progress => progress.AriaLabel, "Upload")
            .Add(progress => progress.Label, "Preparing").Add(progress => progress.ShowValue, true));

        Assert.Equal("Preparing", cut.Find("[data-part=label]").TextContent);
        Assert.Empty(cut.FindAll("[data-part=value]"));
    }

    [Fact]
    public void Render_ShouldShowTheValueInsideTheRing_WhenCircular()
    {
        var cut = Render<TbProgress>(parameters => parameters.Add(progress => progress.AriaLabel, "Export")
            .Add(progress => progress.Value, 60).Add(progress => progress.Circular, true)
            .Add(progress => progress.ShowValue, true).Add(progress => progress.Label, "Exporting"));

        Assert.NotNull(cut.Find("[data-part=ring]").QuerySelector("[data-part=value]"));
        Assert.Equal("Exporting", cut.Find("[data-part=label]").TextContent);
    }

    [Fact]
    public void Render_ShouldDrawARing_WhenCircular()
    {
        var cut = Render<TbProgress>(parameters => parameters
            .Add(progress => progress.AriaLabel, "Loading")
            .Add(progress => progress.Circular, true));

        Assert.Contains("tb-progress--circular", cut.Find("[role=progressbar]").ClassList);
        Assert.Equal("true", cut.Find("svg").GetAttribute("aria-hidden"));
    }

    [Theory]
    [InlineData(false, null)]
    [InlineData(true, null)]
    [InlineData(false, " ")]
    public void Render_ShouldBeNamedByItsLabel_WhenItHasNoAriaLabel(bool circular, string? ariaLabel)
    {
        var cut = Render<TbProgress>(parameters => parameters.Add(progress => progress.Label, "Importing trails")
            .Add(progress => progress.Circular, circular).Add(progress => progress.AriaLabel, ariaLabel));

        var progress = cut.Find("[role=progressbar]");
        Assert.Equal(cut.Find("[data-part=label]").Id, progress.GetAttribute("aria-labelledby"));
        Assert.Null(progress.GetAttribute("aria-label"));
    }

    [Fact]
    public void Render_ShouldBeNamedByItsAriaLabel_WhenItHasBoth()
    {
        var cut = Render<TbProgress>(parameters => parameters.Add(progress => progress.Label, "report.pdf")
            .Add(progress => progress.AriaLabel, "Attachment"));

        var progress = cut.Find("[role=progressbar]");
        Assert.Equal("Attachment", progress.GetAttribute("aria-label"));
        Assert.Null(progress.GetAttribute("aria-labelledby"));
    }

    [Theory]
    [InlineData("", null)]
    [InlineData(" ", null)]
    [InlineData(null, " ")]
    public void Render_ShouldThrow_WhenGivenNeitherALabelNorAnAriaLabel(string? ariaLabel, string? label)
    {
        Assert.Throws<InvalidOperationException>(() => Render<TbProgress>(parameters => parameters
            .Add(progress => progress.AriaLabel, ariaLabel).Add(progress => progress.Label, label)));
    }

    [Fact]
    public void Render_ShouldThrow_WhenGivenAMaxNotAboveMin()
    {
        var exception = Assert.Throws<ArgumentOutOfRangeException>(() => Render<TbProgress>(parameters => parameters
            .Add(progress => progress.AriaLabel, "Loading")
            .Add(progress => progress.Min, 5)
            .Add(progress => progress.Max, 5)));

        Assert.Equal(nameof(TbProgress.Max), exception.ParamName);
    }
}
