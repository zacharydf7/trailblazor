namespace Trailblazor.Tests.Components;

public class ViewportTests
{
    [Theory]
    [InlineData(639, Breakpoint.Xs)]
    [InlineData(640, Breakpoint.Sm)]
    [InlineData(767, Breakpoint.Sm)]
    [InlineData(768, Breakpoint.Md)]
    [InlineData(1024, Breakpoint.Lg)]
    [InlineData(1280, Breakpoint.Xl)]
    public void Breakpoint_ShouldBeTheRangeTheWidthFallsIn_WhenGivenAWidth(int width, Breakpoint breakpoint)
    {
        Assert.Equal(breakpoint, new Viewport(width, 600).Breakpoint);
    }
}
