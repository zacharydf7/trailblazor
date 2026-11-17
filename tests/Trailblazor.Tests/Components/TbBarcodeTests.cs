using Bunit;

namespace Trailblazor.Tests.Components;

public class TbBarcodeTests : BunitContext
{
    public TbBarcodeTests() => this.AddTrailblazor();

    [Fact]
    public void Render_ShouldDrawAnImageNamedByItsValueAndPrintTheValueHidden_WhenGivenAValue()
    {
        var cut = Render<TbBarcode>(parameters => parameters.Add(code => code.Value, "12345678"));

        var svg = cut.Find("svg");
        Assert.Equal(("img", "12345678", "12345678", "0 0 99 1"), (svg.GetAttribute("role"),
            svg.GetAttribute("aria-label"), svg.GetAttribute("data-value"), svg.GetAttribute("viewBox")));
        Assert.StartsWith("M10 0h2v1h-2z", cut.Find("svg path").GetAttribute("d"));
        var text = cut.Find("[data-part=text]");
        Assert.Equal(("12345678", "true"), (text.TextContent, text.GetAttribute("aria-hidden")));
    }

    [Fact]
    public void Render_ShouldAddTheCheckDigitToItsNameAndText_WhenGivenTwelveEanDigits()
    {
        var cut = Render<TbBarcode>(parameters => parameters.Add(code => code.Value, "400638133393")
            .Add(code => code.Format, BarcodeFormat.Ean13));

        Assert.Equal(("4006381333931", "4006381333931", "0 0 113 1"), (cut.Find("svg").GetAttribute("aria-label"),
            cut.Find("[data-part=text]").TextContent, cut.Find("svg").GetAttribute("viewBox")));
    }

    [Fact]
    public void Render_ShouldLeaveOutTheText_WhenShowTextIsOff()
    {
        var cut = Render<TbBarcode>(parameters => parameters.Add(code => code.Value, "PERMIT-1042")
            .Add(code => code.ShowText, false).Add(code => code.AriaLabel, "Permit 1042"));

        Assert.Empty(cut.FindAll("[data-part=text]"));
        Assert.Equal("Permit 1042", cut.Find("svg").GetAttribute("aria-label"));
    }

    [Fact]
    public void Render_ShouldSizeTheBarsAndPutStylesOnTheRoot_WhenGivenAWidthAndHeight()
    {
        var cut = Render<TbBarcode>(parameters => parameters.Add(code => code.Value, "PERMIT-1042")
            .Add(code => code.Width, "20rem").Add(code => code.Height, "3rem").Add(code => code.Class, "ticket"));

        var root = cut.Find(".tb-barcode");
        Assert.Equal(("inline-size: 20rem", "tb-barcode ticket", "block-size: 3rem"), (root.GetAttribute("style"),
            root.GetAttribute("class"), cut.Find("svg").GetAttribute("style")));
    }

    [Fact]
    public void Render_ShouldGiveEachModuleTwoPixelsAndEanItsOwnQuietZones_WhenItHasNoWidth()
    {
        var cut = Render<TbBarcode>(parameters => parameters.Add(code => code.Value, "400638133393")
            .Add(code => code.Format, BarcodeFormat.Ean13));

        Assert.Equal("inline-size: 226px", cut.Find(".tb-barcode").GetAttribute("style"));
        Assert.StartsWith("M11 0h1v1h-1z", cut.Find("svg path").GetAttribute("d"));
    }

    [Theory]
    [InlineData("PERMIT-1042", BarcodeFormat.Code128, true)]
    [InlineData("Café", BarcodeFormat.Code128, false)]
    [InlineData("4006381333931", BarcodeFormat.Ean13, true)]
    [InlineData("4006381333932", BarcodeFormat.Ean13, false)]
    public void CanEncode_ShouldSayWhetherTheFormatHoldsTheValue_WhenGivenAValue(string value, BarcodeFormat format,
        bool expected)
    {
        Assert.Equal(expected, TbBarcode.CanEncode(value, format));
    }

    [Fact]
    public void Render_ShouldThrow_WhenItHasNoValue()
    {
        var thrown = Assert.Throws<InvalidOperationException>(() => Render<TbBarcode>());

        Assert.Equal("TbBarcode needs a Value.", thrown.Message);
    }

    [Fact]
    public void Render_ShouldThrow_WhenTheFormatCantHoldTheValue()
    {
        var thrown = Assert.Throws<ArgumentException>(() => Render<TbBarcode>(parameters =>
            parameters.Add(code => code.Value, "PERMIT-1042").Add(code => code.Format, BarcodeFormat.Ean13)));

        Assert.StartsWith("An EAN-13 barcode takes 12 digits, or 13 with the check digit.", thrown.Message);
    }
}
