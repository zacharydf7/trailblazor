using Bunit;
using Microsoft.Extensions.DependencyInjection;

namespace Trailblazor.Tests.Components;

public class TbSwitchTests : BunitContext
{
    public TbSwitchTests() => this.AddTrailblazor();

    [Fact]
    public void Render_ShouldPutAHiddenIconForEachStateOverTheInput_WhenGivenThumbIcons()
    {
        var cut = Render<TbSwitch>(parameters => parameters.Add(item => item.Label, "Dark trail maps")
            .Add(item => item.ThumbIcon, BuiltInIcons.Check).Add(item => item.OffThumbIcon, BuiltInIcons.Close));

        var icons = cut.FindAll(".tb-switch__control > input ~ [data-part=thumb-icon]");
        Assert.Equal(["on", "off"], icons.Select(icon => icon.GetAttribute("data-state")));
        Assert.All(icons, icon => Assert.Equal("true", icon.GetAttribute("aria-hidden")));
    }

    [Fact]
    public void Render_ShouldLeaveTheInputUnwrapped_WhenGivenNoThumbIcon()
    {
        var cut = Render<TbSwitch>(parameters => parameters.Add(item => item.Label, "Dark trail maps"));

        Assert.Empty(cut.FindAll(".tb-switch__control"));
        Assert.Equal("LABEL", cut.Find("input").ParentElement!.TagName);
    }

    [Fact]
    public void Render_ShouldNameTheToneAndSizeAfterTheSwitch_WhenSet()
    {
        var cut = Render<TbSwitch>(parameters => parameters.Add(item => item.Tone, Tone.Danger)
            .Add(item => item.Size, Size.Large));

        Assert.Equal(["tb-switch", "tb-switch--danger", "tb-switch--large"], cut.Find(".tb-switch").ClassList);
    }

    [Fact]
    public void Render_ShouldShowRichContentAsTheLabelAndStayASwitch_WhenGivenChildContent()
    {
        var cut = Render<TbSwitch>(parameters => parameters.AddChildContent("Share with <strong>guides</strong>"));

        Assert.Equal("guides", cut.Find("[data-part=label] strong").TextContent);
        Assert.Equal("switch", cut.Find("input").GetAttribute("role"));
    }

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
