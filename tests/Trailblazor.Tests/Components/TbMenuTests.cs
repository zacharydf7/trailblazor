using Bunit;
using Microsoft.Extensions.DependencyInjection;

namespace Trailblazor.Tests.Components;

public class TbMenuTests : BunitContext
{
    public TbMenuTests()
    {
        Services.AddTrailblazor();
        JSInterop.SetupModule("./_content/Trailblazor/menu.js").SetupModule("createMenu", _ => true);
    }

    [Fact]
    public void Render_ShouldWireTheTriggerToTheMenu_WhenCalled()
    {
        var cut = Render<TbMenu>(parameters => parameters
            .Add(menu => menu.Label, "Actions")
            .AddChildContent<TbMenuItem>(item => item.AddChildContent("Rename")));

        var trigger = cut.Find("[data-part=trigger]");
        var menu = cut.Find("[role=menu]");
        Assert.Equal("menu", trigger.GetAttribute("aria-haspopup"));
        Assert.Equal(menu.Id, trigger.GetAttribute("popovertarget"));
        Assert.Equal(menu.Id, trigger.GetAttribute("aria-controls"));
        Assert.Equal("auto", menu.GetAttribute("popover"));
        Assert.Equal(trigger.Id, menu.GetAttribute("aria-labelledby"));
        Assert.Contains("Actions", trigger.TextContent);
    }

    [Fact]
    public void Render_ShouldNameTheIconTriggerWithTheLabel_WhenAnIconIsSet()
    {
        var cut = Render<TbMenu>(parameters => parameters
            .Add(menu => menu.Label, "More options")
            .Add(menu => menu.Icon, new Icon("<path d=\"M4 4h16v16H4z\" />")));

        Assert.Equal("More options", cut.Find("[data-part=trigger]").GetAttribute("aria-label"));
    }

    [Fact]
    public void Click_ShouldRaiseOnClick_WhenTheItemIsEnabled()
    {
        var chosen = false;
        var cut = Render<TbMenuItem>(parameters => parameters
            .Add(item => item.OnClick, () => chosen = true)
            .AddChildContent("Rename"));

        var item = cut.Find("button");
        item.Click();

        Assert.True(chosen);
        Assert.Equal("menuitem", item.GetAttribute("role"));
        Assert.Equal("-1", item.GetAttribute("tabindex"));
    }

    [Fact]
    public void Click_ShouldDoNothing_WhenTheItemIsDisabled()
    {
        var chosen = false;
        var cut = Render<TbMenuItem>(parameters => parameters
            .Add(item => item.Disabled, true)
            .Add(item => item.OnClick, () => chosen = true));

        cut.Find("button").Click();

        Assert.False(chosen);
        Assert.Equal("true", cut.Find("button").GetAttribute("aria-disabled"));
    }
}
