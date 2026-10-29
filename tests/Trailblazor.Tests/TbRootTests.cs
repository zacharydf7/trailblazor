using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Components.Web;

namespace Trailblazor.Tests;

public class TbRootTests : BunitContext
{
    [Fact]
    public void Render_ShouldAddStylesheetToHead_WhenHeadOutletIsPresent()
    {
        // HeadOutlet calls Blazor's own page-title script.
        JSInterop.Mode = JSRuntimeMode.Loose;
        this.AddTrailblazor();

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
