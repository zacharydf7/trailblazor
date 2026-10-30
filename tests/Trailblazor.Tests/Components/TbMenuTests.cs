using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Trailblazor.Primitives;

namespace Trailblazor.Tests.Components;

public class TbMenuTests : BunitContext
{
    public TbMenuTests()
    {
        this.AddTrailblazor();
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
    public void Render_ShouldWireASubMenusItemToItsList_WhenCalled()
    {
        var cut = Render<TbMenu>(parameters => parameters
            .Add(menu => menu.Label, "Actions")
            .AddChildContent<TbSubMenu>(submenu => submenu
                .Add(sub => sub.Label, "Share")
                .AddChildContent<TbMenuItem>(item => item.AddChildContent("Email"))));

        var item = cut.Find("[data-part=submenu-trigger]");
        var list = cut.Find(".tb-submenu [role=menu]");
        Assert.Equal("none", cut.Find(".tb-submenu").GetAttribute("role"));
        Assert.Equal("menuitem", item.GetAttribute("role"));
        Assert.Equal("menu", item.GetAttribute("aria-haspopup"));
        Assert.Equal("false", item.GetAttribute("aria-expanded"));
        Assert.Equal(list.Id, item.GetAttribute("popovertarget"));
        Assert.Equal(list.Id, item.GetAttribute("aria-controls"));
        Assert.Equal("auto", list.GetAttribute("popover"));
        Assert.Equal(item.Id, list.GetAttribute("aria-labelledby"));
        Assert.Equal("Share", item.QuerySelector("[data-part=label]")!.TextContent);
        Assert.Equal("Email", list.QuerySelector("[role=menuitem]")!.TextContent.Trim());
    }

    [Theory]
    [InlineData(Placement.Bottom, PlacementAlign.Start, "Bottom", "Start")]
    [InlineData(Placement.Top, PlacementAlign.Center, "Top", "Center")]
    [InlineData(Placement.Start, PlacementAlign.End, "Start", "End")]
    [InlineData(Placement.End, PlacementAlign.Start, "End", "Start")]
    public void Render_ShouldSetUpTheMenuOnTheRequestedSide_WhenGivenAPlacementAndAlignment(Placement placement,
        PlacementAlign align, string side, string alignment)
    {
        var module = JSInterop.SetupModule("./_content/Trailblazor/menu.js");
        module.SetupModule("createMenu", _ => true);

        Render<TbMenu>(parameters => parameters
            .Add(menu => menu.Label, "Actions")
            .Add(menu => menu.Placement, placement)
            .Add(menu => menu.Align, align));

        var options = Assert.IsType<AnchoredPositionOptions>(module.VerifyInvoke("createMenu").Arguments[2]);
        Assert.Equal(side, options.Side.ToString());
        Assert.Equal(alignment, options.Align.ToString());
    }

    [Fact]
    public void Render_ShouldMarkTheMenuFullWidth_WhenFullWidthIsSet()
    {
        var cut = Render<TbMenu>(parameters => parameters
            .Add(menu => menu.Label, "Actions")
            .Add(menu => menu.FullWidth, true));

        Assert.Contains("tb-menu--full-width", cut.Find(".tb-menu").ClassList);
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
    public void Render_ShouldDescribeTheTrigger_WhenGivenADescription()
    {
        var cut = Render<TbMenu>(parameters => parameters
            .Add(menu => menu.Label, "Move")
            .Add(menu => menu.Description, "Arrow keys move it."));

        var trigger = cut.Find("[data-part=trigger]");
        Assert.Equal("Arrow keys move it.", cut.Find($"#{trigger.GetAttribute("aria-describedby")}").TextContent);
    }

    [Fact]
    public void Render_ShouldLeaveTheTriggerUndescribed_WhenGivenNoDescription()
    {
        var cut = Render<TbMenu>(parameters => parameters.Add(menu => menu.Label, "Actions"));

        Assert.Null(cut.Find("[data-part=trigger]").GetAttribute("aria-describedby"));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Render_ShouldSizeTheTrigger_WhenGivenASize(bool icon)
    {
        var cut = Render<TbMenu>(parameters => parameters
            .Add(menu => menu.Label, "Actions")
            .Add(menu => menu.Icon, icon ? new Icon("<path d=\"M4 4h16v16H4z\" />") : null)
            .Add(menu => menu.Size, Size.Small));

        Assert.Contains("tb-button--small", cut.Find("[data-part=trigger]").ClassList);
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
