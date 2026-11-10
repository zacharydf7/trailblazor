using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;
using Trailblazor.Primitives;

namespace Trailblazor.Tests.Components;

public class TbMenuTests : BunitContext
{
    public TbMenuTests()
    {
        this.AddTrailblazor();
        JSInterop.SetupModule("./_content/Trailblazor/menu.js").SetupModule("createMenu", _ => true);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Render_ShouldMarkTheTriggerStatic_WhenTheRendererIsntInteractive(bool interactive)
    {
        SetRendererInfo(new RendererInfo(interactive ? "Server" : "Static", interactive));

        var cut = Render<TbMenu>(parameters => parameters.Add(menu => menu.Label, "Actions")
            .AddChildContent<TbMenuItem>(item => item.AddChildContent("Rename")));

        Assert.Equal(!interactive, cut.Find("[data-part=trigger]").HasAttribute("data-static"));
    }

    [Fact]
    public void Render_ShouldPutAGivenIdOnACustomTriggerAndNameTheMenuByIt_WhenTheMenuHasOne()
    {
        var cut = Render<TbMenu>(parameters => parameters.AddUnmatched("id", "account")
            .Add(menu => menu.Trigger, trigger => builder =>
            {
                builder.OpenElement(0, "button");
                builder.AddMultipleAttributes(1, trigger.Attributes);
                builder.AddContent(2, "RB");
                builder.CloseElement();
            })
            .AddChildContent<TbMenuItem>(item => item.AddChildContent("Sign out")));

        Assert.Equal("RB", Assert.Single(cut.FindAll("#account")).TextContent);
        Assert.Equal("account", cut.Find("[role=menu]").GetAttribute("aria-labelledby"));
    }

    [Fact]
    public void AttachOnFirstUse_ShouldStartTheScriptOnlyOnceTheTriggerTakesFocus_WhenSet()
    {
        var cut = Render<TbMenu>(parameters => parameters.Add(menu => menu.Label, "Actions")
            .Add(menu => menu.AttachOnFirstUse, true)
            .AddChildContent<TbMenuItem>(item => item.AddChildContent("Rename")));
        Assert.Empty(JSInterop.Invocations["createMenu"]);
        var trigger = cut.Find("[data-part=trigger]");
        Assert.True(trigger.HasAttribute("data-attach-on-use"));
        Assert.Equal(cut.Find("[role=menu]").Id, trigger.GetAttribute("popovertarget"));

        trigger.Focus();

        Assert.Single(JSInterop.Invocations["createMenu"]);
        Assert.False(cut.Find("[data-part=trigger]").HasAttribute("data-attach-on-use"));
    }

    [Fact]
    public void AttachOnFirstUse_ShouldStartTheScript_WhenThePointerReachesTheTrigger()
    {
        var cut = Render<TbMenu>(parameters => parameters.Add(menu => menu.Label, "Actions")
            .Add(menu => menu.AttachOnFirstUse, true)
            .AddChildContent<TbMenuItem>(item => item.AddChildContent("Rename")));

        cut.Find("[data-part=trigger]").PointerEnter();

        Assert.Single(JSInterop.Invocations["createMenu"]);
    }

    [Fact]
    public void AttachOnFirstUse_ShouldStartTheScript_WhenTheTriggerIsClickedWithoutFocus()
    {
        var cut = Render<TbMenu>(parameters => parameters.Add(menu => menu.Label, "Actions")
            .Add(menu => menu.AttachOnFirstUse, true)
            .AddChildContent<TbMenuItem>(item => item.AddChildContent("Rename")));

        cut.Find("[data-part=trigger]").Click();

        Assert.Single(JSInterop.Invocations["createMenu"]);
    }

    [Fact]
    public void AttachOnFirstUse_ShouldStartTheScriptAndOpen_WhenTheParentOpensItBeforeUse()
    {
        var handle = JSInterop.SetupModule("./_content/Trailblazor/menu.js").SetupModule("createMenu", _ => true);
        handle.SetupVoid("setOpen", _ => true);
        Render<TbMenu>(parameters => parameters.Add(menu => menu.Label, "Actions")
            .Add(menu => menu.AttachOnFirstUse, true).Add(menu => menu.Open, true)
            .AddChildContent<TbMenuItem>(item => item.AddChildContent("Rename")));

        Assert.Single(JSInterop.Invocations["createMenu"]);
        Assert.Equal([true], handle.Invocations["setOpen"].Select(call => call.Arguments[0]));
    }

    [Fact]
    public void AttachOnFirstUse_ShouldStartTheScript_WhenTurnedOffBeforeUse()
    {
        var cut = Render<TbMenu>(parameters => parameters.Add(menu => menu.Label, "Actions")
            .Add(menu => menu.AttachOnFirstUse, true)
            .AddChildContent<TbMenuItem>(item => item.AddChildContent("Rename")));

        cut.Render(parameters => parameters.Add(menu => menu.AttachOnFirstUse, false));

        Assert.Single(JSInterop.Invocations["createMenu"]);
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
    public void Render_ShouldNameTheIconTriggerWithTheAriaLabel_WhenAnIconIsSet()
    {
        var cut = Render<TbMenu>(parameters => parameters
            .Add(menu => menu.AriaLabel, "More options")
            .Add(menu => menu.Icon, new Icon("<path d=\"M4 4h16v16H4z\" />")));

        Assert.Equal("More options", cut.Find("[data-part=trigger]").GetAttribute("aria-label"));
    }

    [Fact]
    public void Render_ShouldThrow_WhenAnIconTriggerHasALabelButNoAriaLabel()
    {
        var error = Assert.Throws<InvalidOperationException>(() => Render<TbMenu>(parameters => parameters
            .Add(menu => menu.Label, "More options")
            .Add(menu => menu.Icon, new Icon("<path d=\"M4 4h16v16H4z\" />"))));

        Assert.Contains("AriaLabel", error.Message);
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
            .Add(menu => menu.Label, icon ? null : "Actions").Add(menu => menu.AriaLabel, icon ? "Actions" : null)
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

    [Fact]
    public void Render_ShouldHandTheTriggerAttributesToACustomTrigger_WhenGivenATrigger()
    {
        var cut = Render<TbMenu>(parameters => parameters
            .Add(menu => menu.Trigger, context => builder =>
            {
                builder.OpenElement(0, "button");
                builder.AddMultipleAttributes(1, context.Attributes);
                builder.AddContent(2, "RB");
                builder.CloseElement();
            })
            .AddChildContent<TbMenuItem>(item => item.AddChildContent("Profile")));

        var trigger = cut.Find("button[data-part=trigger]");
        var menu = cut.Find("[role=menu]");
        Assert.Equal(("menu", menu.Id, menu.Id), (trigger.GetAttribute("aria-haspopup"),
            trigger.GetAttribute("popovertarget"), trigger.GetAttribute("aria-controls")));
        Assert.Equal(trigger.Id, menu.GetAttribute("aria-labelledby"));
        Assert.False(trigger.HasAttribute("disabled"));
    }

    [Fact]
    public void Render_ShouldDisableTheTrigger_WhenDisabled()
    {
        var builtIn = Render<TbMenu>(parameters => parameters.Add(menu => menu.Label, "Actions")
            .Add(menu => menu.Disabled, true));
        var custom = Render<TbMenu>(parameters => parameters.Add(menu => menu.Disabled, true)
            .Add(menu => menu.Trigger, context => builder =>
            {
                builder.OpenElement(0, "button");
                builder.AddMultipleAttributes(1, context.Attributes);
                builder.CloseElement();
            }));

        Assert.True(builtIn.Find("[data-part=trigger]").HasAttribute("disabled"));
        Assert.True(custom.Find("[data-part=trigger]").HasAttribute("disabled"));
    }

    [Fact]
    public void Render_ShouldDisableATbButtonTrigger_WhenItTakesOnlyTheTriggerAttributes()
    {
        var cut = Render<TbMenu>(parameters => parameters.Add(menu => menu.Disabled, true)
            .Add(menu => menu.Trigger, context => builder =>
            {
                builder.OpenComponent<TbButton>(0);
                builder.AddMultipleAttributes(1, context.Attributes);
                builder.CloseComponent();
            }));

        Assert.True(cut.Find("[data-part=trigger]").HasAttribute("disabled"));
    }

    [Fact]
    public async Task Toggle_ShouldDrawTheItemsAgain_WhenAMenuWithNoBindingOpens()
    {
        var label = "Export";
        var cut = Render<TbMenu>(parameters => parameters.Add(menu => menu.Label, "Actions")
            .Add(menu => menu.ChildContent, (RenderFragment)(builder =>
            {
                builder.OpenComponent<TbMenuItem>(0);
                builder.AddAttribute(1, nameof(TbMenuItem.ChildContent),
                    (RenderFragment)(item => item.AddContent(0, label)));
                builder.CloseComponent();
            })));
        var events = ((DotNetObjectReference<MenuEvents>)JSInterop.Invocations["createMenu"].Single()
            .Arguments[3]!).Value;

        label = "Export the report";
        await cut.InvokeAsync(() => events.OnToggle(true));

        Assert.Equal("Export the report", cut.Find("[role=menuitem]").TextContent.Trim());
        Assert.True(cut.Find("[role=menu]").HasAttribute("data-open"));
    }

    [Fact]
    public async Task Toggle_ShouldReportEachOpenAndClose_WhenOpenIsBound()
    {
        var reported = new List<bool>();
        var cut = Render<TbMenu>(parameters => parameters.Add(menu => menu.Label, "Actions")
            .Add(menu => menu.OpenChanged, (bool open) => reported.Add(open))
            .AddChildContent<TbMenuItem>(item => item.AddChildContent("Export")));
        var events = ((DotNetObjectReference<MenuEvents>)JSInterop.Invocations["createMenu"].Single()
            .Arguments[3]!).Value;

        await cut.InvokeAsync(() => events.OnToggle(true));
        await cut.InvokeAsync(() => events.OnToggle(false));

        Assert.Equal([true, false], reported);
    }

    [Fact]
    public async Task Open_ShouldLeaveTheMenuAsTheUserLeftIt_WhenTheParentPassesTheSameValueAgain()
    {
        var handle = JSInterop.SetupModule("./_content/Trailblazor/menu.js").SetupModule("createMenu", _ => true);
        handle.SetupVoid("setOpen", _ => true);
        var cut = Render<TbMenu>(parameters => parameters.Add(menu => menu.Label, "Actions")
            .Add(menu => menu.Open, true).AddChildContent<TbMenuItem>(item => item.AddChildContent("Export")));
        var events = ((DotNetObjectReference<MenuEvents>)JSInterop.Invocations["createMenu"].Single()
            .Arguments[3]!).Value;

        await cut.InvokeAsync(() => events.OnToggle(false));
        cut.Render(parameters => parameters.Add(menu => menu.Open, true));

        Assert.Equal([true], handle.Invocations["setOpen"].Select(call => call.Arguments[0]));
        Assert.False(cut.Find("[role=menu]").HasAttribute("data-open"));
    }

    [Fact]
    public void Open_ShouldOpenTheMenu_WhenTheParentSetsIt()
    {
        var handle = JSInterop.SetupModule("./_content/Trailblazor/menu.js").SetupModule("createMenu", _ => true);
        handle.SetupVoid("setOpen", _ => true);
        var cut = Render<TbMenu>(parameters => parameters.Add(menu => menu.Label, "Actions")
            .AddChildContent<TbMenuItem>(item => item.AddChildContent("Export")));

        cut.Render(parameters => parameters.Add(menu => menu.Open, true));

        Assert.Equal([true], handle.Invocations["setOpen"].Select(call => call.Arguments[0]));
    }

    [Fact]
    public void Render_ShouldThrow_WhenThereIsNoLabelOrTrigger()
    {
        var failure = Assert.Throws<InvalidOperationException>(() => Render<TbMenu>());

        Assert.Equal("TbMenu needs a Label or a Trigger.", failure.Message);
    }

    [Fact]
    public void Render_ShouldDrawALinkThatOpensANewTabAndSaysSo_WhenGivenAnHrefAndNewTab()
    {
        var cut = Render<TbMenuItem>(parameters => parameters.Add(item => item.Href, "/reports")
            .Add(item => item.NewTab, true).AddChildContent("Reports"));

        var link = cut.Find("a");
        Assert.Equal(("menuitem", "-1", "/reports"), (link.GetAttribute("role"), link.GetAttribute("tabindex"),
            link.GetAttribute("href")));
        Assert.Equal(("_blank", "noopener noreferrer"), (link.GetAttribute("target"), link.GetAttribute("rel")));
        Assert.Equal("(opens in a new tab)", link.QuerySelector(".tb-visually-hidden")!.TextContent.Trim());
    }

    [Theory]
    [InlineData(false, "_top", "nofollow")]
    [InlineData(true, "_blank", "nofollow noopener noreferrer")]
    public void Render_ShouldKeepTheConsumersTargetAndRel_WhenGivenThem(bool newTab, string target, string rel)
    {
        var cut = Render<TbMenuItem>(parameters => parameters.Add(item => item.Href, "/reports")
            .Add(item => item.NewTab, newTab).AddUnmatched("target", "_top").AddUnmatched("rel", "nofollow")
            .AddChildContent("Reports"));

        var link = cut.Find("a");
        Assert.Equal((target, rel), (link.GetAttribute("target"), link.GetAttribute("rel")));
    }

    [Fact]
    public void Render_ShouldDropTheLinksAddress_WhenTheLinkItemIsDisabled()
    {
        var cut = Render<TbMenuItem>(parameters => parameters.Add(item => item.Href, "/reports")
            .Add(item => item.Disabled, true).AddChildContent("Reports"));

        var link = cut.Find("a");
        Assert.False(link.HasAttribute("href"));
        Assert.Equal("true", link.GetAttribute("aria-disabled"));
    }

    [Fact]
    public void Render_ShouldNameTheGroupByItsLabel_WhenGivenALabel()
    {
        var cut = Render<TbMenuGroup>(parameters => parameters.Add(group => group.Label, "Show")
            .AddChildContent<TbMenuItem>(item => item.AddChildContent("Map")));

        var group = cut.Find("[role=group]");
        var label = cut.Find("[data-part=group-label]");
        Assert.Equal(label.Id, group.GetAttribute("aria-labelledby"));
        Assert.Equal(("Show", "presentation"), (label.TextContent, label.GetAttribute("role")));
    }

    [Fact]
    public void Render_ShouldLeaveTheGroupUnnamed_WhenGivenNoLabel()
    {
        var cut = Render<TbMenuGroup>(parameters => parameters
            .AddChildContent<TbMenuItem>(item => item.AddChildContent("Map")));

        Assert.False(cut.Find("[role=group]").HasAttribute("aria-labelledby"));
        Assert.Empty(cut.FindAll("[data-part=group-label]"));
    }

    [Fact]
    public void Render_ShouldDrawASeparator_WhenCalled()
    {
        var cut = Render<TbMenuSeparator>();

        Assert.Equal("separator", cut.Find(".tb-menu__separator").GetAttribute("role"));
    }
}
