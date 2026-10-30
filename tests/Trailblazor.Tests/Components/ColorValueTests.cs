namespace Trailblazor.Tests.Components;

public class ColorValueTests
{
    [Theory]
    [InlineData("#369", false, "#336699")]
    [InlineData("336699", false, "#336699")]
    [InlineData(" #33669A ", false, "#33669a")]
    [InlineData("rgb(51, 102, 153)", false, "#336699")]
    [InlineData("#33669980", true, "#33669980")]
    [InlineData("#3698", true, "#33669988")]
    [InlineData("#336699ff", true, "#336699")]
    public void TryParse_ShouldReadTheColorAsHex_WhenGivenACssColor(string text, bool alpha, string hex)
    {
        Assert.True(ColorValue.TryParse(text, alpha, out var color));

        Assert.Equal(hex, color.ToHex(alpha));
    }

    [Theory]
    [InlineData("#12")]
    [InlineData("#33669980")]
    [InlineData("rgb(300, 0, 0)")]
    [InlineData("rgb(٥١, 0, 0)")]
    [InlineData("blue")]
    [InlineData("")]
    public void TryParse_ShouldFail_WhenGivenTextThatIsntAnOpaqueColor(string text)
    {
        Assert.False(ColorValue.TryParse(text, alpha: false, out _));
    }

    [Theory]
    [InlineData("#336699")]
    [InlineData("#ffcc00")]
    [InlineData("#000000")]
    [InlineData("#ffffff")]
    [InlineData("#808080")]
    public void FromHsv_ShouldGiveBackTheColor_WhenGivenItsOwnHsv(string hex)
    {
        ColorValue.TryParse(hex, false, out var color);

        var (hue, saturation, brightness) = color.ToHsv();

        Assert.Equal(hex, ColorValue.FromHsv(hue, saturation, brightness, 255).ToHex(false));
    }
}
