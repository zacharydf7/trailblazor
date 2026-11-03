namespace Trailblazor.Tests;

public class PaletteTests
{
    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public void Palette_ShouldRejectTheName_WhenGivenNoName(string name) =>
        Assert.Throws<ArgumentException>(() => new Palette(name));

    [Fact]
    public void Palette_ShouldEqualTheBuiltInOfTheSameName_WhenNamedLikeOne() =>
        Assert.Equal(Palette.Cobalt, new Palette("cobalt"));
}
