namespace Trailblazor.Tests;

public class StylesTests
{
    [Fact]
    public void Join_ShouldSeparateTheDeclarationsWithSemicolons_WhenGivenSeveral()
    {
        Assert.Equal("color: red; margin: 0", Styles.Join("color: red", null, "margin: 0"));
    }

    [Fact]
    public void Join_ShouldReturnNull_WhenAllAreBlank()
    {
        Assert.Null(Styles.Join(null, " "));
    }
}
