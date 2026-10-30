namespace Trailblazor.Tests.Components;

public class TooltipSettingsTests
{
    [Theory]
    [InlineData(-1)]
    [InlineData(int.MaxValue + 1L)]
    public void Delays_ShouldThrow_WhenGivenADelayTheBrowserCantWaitFor(long milliseconds)
    {
        var settings = new TooltipSettings();

        Assert.Throws<ArgumentOutOfRangeException>(() => settings.ShowDelay = TimeSpan.FromMilliseconds(milliseconds));
        Assert.Throws<ArgumentOutOfRangeException>(() => settings.HideDelay = TimeSpan.FromMilliseconds(milliseconds));
    }
}
