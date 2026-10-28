using Bunit;
using Microsoft.AspNetCore.Components.Web;

namespace Trailblazor.Tests;

public class TbRootTests : BunitContext
{
    [Fact]
    public void Render_ShouldAddStylesheetToHead_WhenHeadOutletIsPresent()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;

        var cut = Render(builder =>
        {
            builder.OpenComponent<HeadOutlet>(0);
            builder.CloseComponent();
            builder.OpenComponent<TbRoot>(1);
            builder.CloseComponent();
        });

        var link = cut.Find("link[rel=stylesheet]");
        Assert.Equal("_content/Trailblazor/trailblazor.css", link.GetAttribute("href"));
    }
}
