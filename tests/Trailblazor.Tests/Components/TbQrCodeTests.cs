using Bunit;

namespace Trailblazor.Tests.Components;

public class TbQrCodeTests : BunitContext
{
    public TbQrCodeTests() => this.AddTrailblazor();

    [Fact]
    public void Render_ShouldDrawAnImageNamedByItsValueWithAQuietZone_WhenGivenAValue()
    {
        var cut = Render<TbQrCode>(parameters => parameters.Add(code => code.Value, "HELLO WORLD"));

        var svg = cut.Find("svg");
        Assert.Equal(("img", "HELLO WORLD", "HELLO WORLD", "0 0 29 29"), (svg.GetAttribute("role"),
            svg.GetAttribute("aria-label"), svg.GetAttribute("data-value"), svg.GetAttribute("viewBox")));
        Assert.StartsWith("M4 4h7v1h-7z", cut.Find("[data-part=modules]").GetAttribute("d"));
    }

    [Fact]
    public void Render_ShouldNameTheImageWithTheAriaLabel_WhenOneIsGiven()
    {
        var cut = Render<TbQrCode>(parameters => parameters.Add(code => code.Value, "https://trailblazor.dev/")
            .Add(code => code.AriaLabel, "Link to the trail map"));

        Assert.Equal("Link to the trail map", cut.Find("svg").GetAttribute("aria-label"));
    }

    [Fact]
    public void Render_ShouldDrawALargerCode_WhenTheErrorCorrectionIsHigher()
    {
        var cut = Render<TbQrCode>(parameters => parameters.Add(code => code.Value, "https://trailblazor.dev/"));
        var medium = cut.Find("svg").GetAttribute("viewBox");

        cut.Render(parameters => parameters.Add(code => code.ErrorCorrection, QrErrorCorrection.High));

        Assert.Equal(("0 0 33 33", "0 0 37 37"), (medium, cut.Find("svg").GetAttribute("viewBox")));
    }

    [Fact]
    public void Render_ShouldSizeTheCode_WhenGivenAWidth()
    {
        var cut = Render<TbQrCode>(parameters => parameters.Add(code => code.Value, "HELLO WORLD")
            .Add(code => code.Width, "6rem").Add(code => code.Style, "margin: 1rem"));

        Assert.Equal("inline-size: 6rem; margin: 1rem", cut.Find("svg").GetAttribute("style"));
    }

    [Fact]
    public void Render_ShouldGiveEachModuleFourPixels_WhenItHasNoWidth()
    {
        var cut = Render<TbQrCode>(parameters => parameters.Add(code => code.Value, "HELLO WORLD"));

        Assert.Equal("inline-size: 116px", cut.Find("svg").GetAttribute("style"));
    }

    [Theory]
    [InlineData("https://trailblazor.dev/", QrErrorCorrection.High, true)]
    [InlineData("", QrErrorCorrection.Low, false)]
    public void CanEncode_ShouldSayWhetherTheCodeHoldsTheValue_WhenGivenAValue(string value, QrErrorCorrection level,
        bool expected)
    {
        Assert.Equal(expected, TbQrCode.CanEncode(value, level));
    }

    [Fact]
    public void CanEncode_ShouldBeFalse_WhenTheValueIsTooLongForTheLevel()
    {
        var value = new string('a', 1300);

        Assert.Equal((true, false), (TbQrCode.CanEncode(value, QrErrorCorrection.Low),
            TbQrCode.CanEncode(value, QrErrorCorrection.High)));
    }

    [Fact]
    public void Render_ShouldThrow_WhenItHasNoValue()
    {
        var thrown = Assert.Throws<InvalidOperationException>(() => Render<TbQrCode>());

        Assert.Equal("TbQrCode needs a Value.", thrown.Message);
    }
}
