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
    public void Render_ShouldDrawARing_WhenCircular()
    {
        var cut = Render<TbProgress>(parameters => parameters
            .Add(progress => progress.AriaLabel, "Loading")
            .Add(progress => progress.Circular, true));

        Assert.Contains("tb-progress--circular", cut.Find("[role=progressbar]").ClassList);
        Assert.Equal("true", cut.Find("svg").GetAttribute("aria-hidden"));
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public void Render_ShouldThrow_WhenGivenABlankAriaLabel(string ariaLabel)
    {
        Assert.Throws<InvalidOperationException>(() => Render<TbProgress>(parameters => parameters
            .Add(progress => progress.AriaLabel, ariaLabel)));
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
