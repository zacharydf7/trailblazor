using Bunit;
using Microsoft.Extensions.DependencyInjection;

namespace Trailblazor.Tests.Components;

public class TbSwitchTests : BunitContext
{
    public TbSwitchTests() => this.AddTrailblazor();

    [Fact]
    public void Render_ShouldBeASwitch_WhenCalled()
    {
        var cut = Render<TbSwitch>(parameters => parameters.Add(toggle => toggle.Label, "Auto-save"));

        Assert.Contains("tb-switch", cut.Find("div").ClassList);
        Assert.Equal("switch", cut.Find("input").GetAttribute("role"));
    }

    [Fact]
    public void Change_ShouldRaiseValueChanged_WhenToggled()
    {
        var value = false;
        var cut = Render<TbSwitch>(parameters => parameters
            .Add(toggle => toggle.ValueChanged, (bool next) => value = next));

        cut.Find("input").Change(true);

        Assert.True(value);
    }
}
