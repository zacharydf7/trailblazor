namespace Trailblazor.Tests.Components;

public class ToastSettingsTests
{
    [Fact]
    public void MaxVisible_ShouldThrow_WhenGivenLessThanOne()
    {
        var settings = new ToastSettings();

        Assert.Throws<ArgumentOutOfRangeException>(() => settings.MaxVisible = 0);
    }
}
