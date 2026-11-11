namespace Trailblazor.Tests;

public class OklchColorTests
{
    [Theory]
    [InlineData("#ff0000", 0.628, 0.2577, 29.23)]
    [InlineData("#F00", 0.628, 0.2577, 29.23)]
    [InlineData("#ffffff", 1, 0, null)]
    [InlineData("oklch(0.52 0.17 260)", 0.52, 0.17, 260.0)]
    [InlineData("OKLCH(52% 0.17 260)", 0.52, 0.17, 260.0)]
    public void TryParse_ShouldReadTheColor_WhenGivenHexOrOklch(string text, double l, double c, double? h)
    {
        Assert.True(OklchColor.TryParse(text, out var color));

        Assert.Equal(l, color.L, 0.001);
        Assert.Equal(c, color.C, 0.001);
        if (h is not null)
        {
            Assert.Equal(h.Value, color.H, 0.05);
        }
    }

    [Theory]
    [InlineData(null)]
    [InlineData("blue")]
    [InlineData("#12345")]
    [InlineData("oklch(1.2 0.1 20)")]
    [InlineData("rgb(0 0 255)")]
    public void TryParse_ShouldFail_WhenGivenSomethingElse(string? text) =>
        Assert.False(OklchColor.TryParse(text, out _));

    [Theory]
    [InlineData("#ffffff", "#000000", 21)]
    [InlineData("#ffffff", "#767676", 4.54)]
    [InlineData("#1f5fad", "#ffffff", 6.37)]
    public void ContrastWith_ShouldGiveTheWcagRatio_WhenGivenTwoColors(string first, string second, double ratio)
    {
        OklchColor.TryParse(first, out var one);
        OklchColor.TryParse(second, out var other);

        Assert.Equal(ratio, one.ContrastWith(other), 0.05);
    }

    [Fact]
    public void Luminance_ShouldStayInRange_WhenTheColorIsOutsideSrgb()
    {
        var vivid = new OklchColor(0.7, 0.4, 150);

        Assert.InRange(vivid.Luminance(), 0.3, 0.5);
    }
}
