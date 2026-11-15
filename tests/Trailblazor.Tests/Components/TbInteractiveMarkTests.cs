using Bunit;
using Microsoft.AspNetCore.Components;

namespace Trailblazor.Tests.Components;

public class TbInteractiveMarkTests : BunitContext
{
    [Theory]
    [InlineData(true, 1)]
    [InlineData(false, 0)]
    public void TbInteractiveMark_ShouldMarkThePageOnlyWhileInteractive_WhenGivenARenderMode(bool interactive,
        int marks)
    {
        SetRendererInfo(new RendererInfo(interactive ? "Server" : "Static", interactive));

        var cut = Render<TbInteractiveMark>();

        Assert.Equal(marks, cut.FindAll("span[hidden][data-tb-interactive]:not([data-tb-session])").Count);
    }
}
