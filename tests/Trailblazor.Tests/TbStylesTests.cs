using Bunit;

namespace Trailblazor.Tests;

public class TbStylesTests : BunitContext
{
    [Fact]
    public void Render_ShouldLinkTheStylesheet_WhenCalled()
    {
        var cut = Render<TbStyles>();

        var link = cut.Find("link[rel=stylesheet]");
        Assert.Equal("_content/Trailblazor/trailblazor.css", link.GetAttribute("href"));
    }
}
