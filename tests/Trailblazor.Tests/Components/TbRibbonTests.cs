using System.Diagnostics.Metrics;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.Metrics.Testing;
using Microsoft.JSInterop;
using Trailblazor.Testing;

namespace Trailblazor.Tests.Components;

public class TbRibbonTests : BunitContext
{
    private static readonly Icon Square = new("<path d=\"M4 4h16v16H4z\" />");

    public TbRibbonTests()
    {
        this.AddTrailblazor();
    }

    [Fact]
    public void Render_ShouldNameItsTabsCommandsAndGroups_WhenCalled()
    {
        var cut = RenderRibbon();

        Assert.Equal("Trail planner", cut.Find("[role=tablist]").GetAttribute("aria-label"));
        Assert.Contains("tb-tabs--small", cut.Find(".tb-tabs").ClassList);
        var panel = cut.Find("[role=tabpanel]:not([hidden])");
        var commands = panel.QuerySelector("[role=toolbar][data-part=commands]")!;
        Assert.Equal("Notes", commands.GetAttribute("aria-label"));
        var group = commands.QuerySelector("[role=group][data-part=group]")!;
        var label = group.QuerySelector("[data-part=group-label]")!;
        Assert.Equal("Write", label.TextContent);
        Assert.Equal(label.Id, group.GetAttribute("aria-labelledby"));
        Assert.Equal(3, group.QuerySelectorAll("[data-part=items] button").Length);
    }

    [Fact]
    public void Render_ShouldLeaveThePanelOutOfTheTabOrder_WhenCalled()
    {
        var cut = RenderRibbon();

        // The commands are the next tab stop after the tabs, so the panel around them isn't one.
        Assert.All(cut.FindAll("[role=tabpanel]"), panel => Assert.False(panel.HasAttribute("tabindex")));
    }

    [Fact]
    public void Render_ShouldAttachTheRibbonsScriptToTheShownTabsCommands_WhenCalled()
    {
        var module = JSInterop.SetupModule("./_content/Trailblazor/ribbon.js");
        module.Mode = JSRuntimeMode.Loose;
        module.SetupModule("attachRibbon", _ => true).Mode = JSRuntimeMode.Loose;

        RenderRibbon();

        Assert.Single(module.Invocations["attachRibbon"]);
    }

    [Fact]
    public void Collapsible_ShouldEndTheTabsWithAButtonThatSaysTheCommandsShow_WhenSet()
    {
        var plain = RenderRibbon();
        var cut = RenderRibbon(parameters => parameters.Add(ribbon => ribbon.Collapsible, true));

        Assert.Empty(plain.FindAll("[data-part=collapse]"));
        Assert.False(plain.Find("[data-part=ribbon]").HasAttribute("data-collapsible"));
        Assert.False(plain.Find("[data-part=ribbon]").HasAttribute("data-static"));
        var button = cut.Find("[role=tablist] ~ .tb-tabs__actions > [data-part=collapse]");
        Assert.Equal(("Ribbon commands", "true", "Control+F1"), (button.GetAttribute("aria-label"),
            button.GetAttribute("aria-expanded"), button.GetAttribute("aria-keyshortcuts")));
        Assert.True(cut.Find("[data-part=ribbon]").HasAttribute("data-collapsible"));
    }

    [Fact]
    public void Start_ShouldComeBeforeTheTabsAndEndBeforeTheCollapseButton_WhenGiven()
    {
        var cut = RenderRibbon(parameters => parameters.Add(ribbon => ribbon.Collapsible, true)
            .Add(ribbon => ribbon.Start, "<button type=\"button\">File</button>")
            .Add(ribbon => ribbon.End, "<button type=\"button\">Save</button>"));

        var list = cut.Find("[role=tablist]");
        Assert.Equal("File", list.PreviousElementSibling!.TextContent);
        var actions = list.NextElementSibling!;
        Assert.Equal("Save", actions.FirstElementChild!.TextContent);
        Assert.Equal("collapse", actions.LastElementChild!.GetAttribute("data-part"));
    }

    [Fact]
    public void End_ShouldEndTheRowAlone_WhenTheRibbonIsNotCollapsible()
    {
        var cut = RenderRibbon(parameters => parameters
            .Add(ribbon => ribbon.End, "<button type=\"button\">Save</button>"));

        Assert.Equal("Save", cut.Find("[role=tablist]").NextElementSibling!.TextContent);
        Assert.Empty(cut.FindAll("[data-part=collapse]"));
    }

    [Fact]
    public void Labels_ShouldNameTheCollapseButton_WhenGiven()
    {
        var cut = RenderRibbon(parameters => parameters.Add(ribbon => ribbon.Collapsible, true)
            .Add(ribbon => ribbon.Labels, new RibbonLabels { Collapse = "Tools" }));

        Assert.Equal("Tools", cut.Find("[data-part=collapse]").GetAttribute("aria-label"));
    }

    [Fact]
    public void Collapse_ShouldMarkTheRibbonAndTellTheApp_WhenItsButtonIsClicked()
    {
        var changes = new List<bool>();
        var cut = RenderRibbon(parameters => parameters.Add(ribbon => ribbon.Collapsible, true)
            .Add(ribbon => ribbon.CollapsedChanged, changes.Add));

        cut.Find("[data-part=collapse]").Click();

        Assert.True(cut.Find("[data-part=ribbon]").HasAttribute("data-collapsed"));
        Assert.Equal("false", cut.Find("[data-part=collapse]").GetAttribute("aria-expanded"));
        cut.Find("[data-part=collapse]").Click();
        Assert.False(cut.Find("[data-part=ribbon]").HasAttribute("data-collapsed"));
        Assert.Equal("true", cut.Find("[data-part=collapse]").GetAttribute("aria-expanded"));
        Assert.Equal([true, false], changes);
    }

    [Fact]
    public async Task Collapse_ShouldToggleTheRibbon_WhenItsScriptReportsCtrlF1OrADoubleClick()
    {
        var changes = new List<bool>();
        var cut = RenderRibbon(parameters => parameters.Add(ribbon => ribbon.Collapsible, true)
            .Add(ribbon => ribbon.CollapsedChanged, changes.Add));
        var events = ((DotNetObjectReference<RibbonEvents>)JSInterop.Invocations["attachRibbonRoot"].Single()
            .Arguments[1]!).Value;

        await cut.InvokeAsync(events.OnToggle);

        Assert.True(cut.Find("[data-part=ribbon]").HasAttribute("data-collapsed"));
        Assert.Equal([true], changes);
    }

    [Fact]
    public void Collapsed_ShouldCollapseTheRibbonAndShowItAgain_WhenTheAppSetsIt()
    {
        var cut = RenderRibbon(parameters => parameters.Add(ribbon => ribbon.Collapsed, true));
        Assert.True(cut.Find("[data-part=ribbon]").HasAttribute("data-collapsed"));
        Assert.Empty(cut.FindAll("[data-part=collapse]"));

        cut.Render(parameters => parameters.Add(ribbon => ribbon.Collapsed, false));

        Assert.False(cut.Find("[data-part=ribbon]").HasAttribute("data-collapsed"));
    }

    [Fact]
    public void Collapsed_ShouldLeaveTheCommandsShowingWithNoButton_WhenThePageIsStatic()
    {
        SetRendererInfo(new RendererInfo("Static", isInteractive: false));

        var cut = RenderRibbon(parameters => parameters.Add(ribbon => ribbon.Collapsed, true)
            .Add(ribbon => ribbon.Collapsible, true));

        Assert.False(cut.Find("[data-part=ribbon]").HasAttribute("data-collapsed"));
        Assert.True(cut.Find("[data-part=ribbon]").HasAttribute("data-static"));
        Assert.Empty(cut.FindAll("[data-part=collapse]"));
    }

    [Fact]
    public async Task Driver_ShouldCollapseAndExpandTheRibbon_WhenAsked()
    {
        var cut = RenderRibbon(parameters => parameters.Add(ribbon => ribbon.Collapsible, true));
        var ribbon = cut.Tb().Ribbon();

        await ribbon.CollapseAsync();
        await ribbon.CollapseAsync();
        Assert.True(ribbon.IsCollapsed);
        await ribbon.ExpandAsync();

        Assert.False(ribbon.IsCollapsed);
    }

    [Fact]
    public async Task Driver_ShouldRefuseToCollapse_WhenTheRibbonHasNoCollapseButton()
    {
        var ribbon = RenderRibbon().Tb().Ribbon();

        Assert.Equal("The ribbon has no collapse button; set Collapsible to give it one.",
            (await Assert.ThrowsAsync<InvalidOperationException>(ribbon.CollapseAsync)).Message);
    }

    [Fact]
    public async Task Driver_ShouldRefuseToCollapse_WhenThePageIsStatic()
    {
        SetRendererInfo(new RendererInfo("Static", isInteractive: false));
        var ribbon = RenderRibbon(parameters => parameters.Add(ribbon => ribbon.Collapsible, true)).Tb().Ribbon();

        Assert.Equal("On a static page the ribbon can't collapse; render the page interactively to collapse it.",
            (await Assert.ThrowsAsync<InvalidOperationException>(ribbon.CollapseAsync)).Message);
    }

    [Fact]
    public void Group_ShouldHoldAHiddenButtonThatOpensItsItems_WhenRendered()
    {
        var cut = RenderRibbon();

        var group = cut.Find("[role=tabpanel]:not([hidden]) [data-part=group]");
        var button = group.QuerySelector(":scope > [data-part=group-button]")!;
        var items = group.QuerySelector(":scope > [data-part=items]")!;
        // The script shows the button once the group has collapsed; until then only the items show.
        Assert.True(button.HasAttribute("hidden"));
        Assert.Equal("Write", button.QuerySelector("[data-part=label]")!.TextContent.Trim());
        Assert.Equal(("false", items.Id, items.Id), (button.GetAttribute("aria-expanded"),
            button.GetAttribute("aria-controls"), button.GetAttribute("popovertarget")));
        Assert.False(items.HasAttribute("popover"));
        Assert.False(group.HasAttribute("data-collapse-order"));
    }

    [Fact]
    public void Group_ShouldCarryItsCollapseOrderAndIcon_WhenGiven()
    {
        var cut = Render<TbRibbonGroup>(parameters => parameters.Add(group => group.Label, "Write")
            .Add(group => group.CollapseOrder, 2).Add(group => group.Icon, Square));

        Assert.Equal("2", cut.Find("[data-part=group]").GetAttribute("data-collapse-order"));
        Assert.NotNull(cut.Find("[data-part=group-button] [data-part=start] svg"));
    }

    [Fact]
    public async Task Driver_ShouldClickTheCommand_WhenItsGroupHasTheSameName()
    {
        var clicked = new List<string>();
        var cut = RenderRibbon(tabs: builder => AddTab(builder, 0, "Notes", null,
            group => AddButton(group, 0, "Write", clicked.Add)));

        await cut.Tb().Ribbon().ClickAsync("Write");

        Assert.Equal(["Write"], clicked);
    }

    [Fact]
    public void Render_ShouldThrow_WhenARequiredNameIsMissing()
    {
        Assert.Contains("needs an AriaLabel", Assert.Throws<InvalidOperationException>(() =>
            Render<TbRibbon>(parameters => parameters.Add(ribbon => ribbon.AriaLabel, " "))).Message);
        Assert.Contains("needs a Title", Assert.Throws<InvalidOperationException>(() =>
            RenderRibbon(tabs: builder => AddTab(builder, 0, "", null, _ => { }))).Message);
        Assert.Contains("needs a Label", Assert.Throws<InvalidOperationException>(() =>
            RenderRibbon(tabs: builder => AddTab(builder, 0, "Notes", null, group => AddButton(group, 0, "")))).Message);
        Assert.Contains("TbRibbonGroup needs a Label", Assert.Throws<InvalidOperationException>(() =>
            RenderRibbon(tabs: builder => AddTab(builder, 0, "Notes", null, _ => { }, group: ""))).Message);
    }

    [Fact]
    public void Button_ShouldShowItsIconBesideAboveOrAlone_WhenGivenLargeOrIconOnly()
    {
        var cut = RenderRibbon();

        var large = cut.Find("button.tb-ribbon__button--large:not([data-part=group-button])");
        Assert.Equal("New note", large.QuerySelector("[data-part=label]")!.TextContent.Trim());
        Assert.NotNull(large.QuerySelector("[data-part=start] svg"));
        var small = cut.FindAll("button.tb-ribbon__button:not(.tb-ribbon__button--large):not(.tb-icon-button)");
        Assert.Equal("Find", Assert.Single(small).TextContent.Trim());
        var alone = cut.Find("button.tb-ribbon__button.tb-icon-button");
        Assert.Equal("Mark as done", alone.GetAttribute("aria-label"));
        Assert.Equal("Mark as done", cut.Find("[role=tooltip]").TextContent);
        // Each is a small, quiet button, so a group of them reads as one set.
        Assert.All(cut.FindAll("button.tb-ribbon__button"), button =>
            Assert.Subset(button.ClassList.ToHashSet(), new HashSet<string> { "tb-button--text", "tb-button--neutral",
                "tb-button--small" }));
    }

    [Fact]
    public void Button_ShouldThrow_WhenIconOnlyHasNoIconOrIsLarge()
    {
        Assert.Contains("needs an Icon", Assert.Throws<InvalidOperationException>(() => Render<TbRibbonButton>(
            parameters => parameters.Add(button => button.Label, "Find").Add(button => button.IconOnly, true))).Message);
        Assert.Contains("can't be Large", Assert.Throws<InvalidOperationException>(() => Render<TbRibbonButton>(
            parameters => parameters.Add(button => button.Label, "Find").Add(button => button.Icon, Square)
                .Add(button => button.IconOnly, true).Add(button => button.Large, true))).Message);
    }

    [Fact]
    public async Task Button_ShouldRunItsCommand_WhenClicked()
    {
        var clicked = new List<string>();
        var cut = RenderRibbon(onClick: clicked.Add);

        await cut.Tb().Ribbon("Trail planner").ClickAsync("New note");
        await cut.Tb().Ribbon().ClickAsync("Mark as done");

        Assert.Equal(["New note", "Mark as done"], clicked);
    }

    [Fact]
    public async Task Button_ShouldToggleAndSayWhetherItIsOn_WhenPressedIsBound()
    {
        var pressed = true;
        var cut = Render<TbRibbonButton>(parameters => parameters.Add(button => button.Label, "Mute")
            .Add(button => button.Pressed, pressed).Add(button => button.PressedChanged, value => pressed = value));
        Assert.Equal("true", cut.Find("button").GetAttribute("aria-pressed"));

        await cut.Find("button").ClickAsync(new MouseEventArgs { Detail = 1 });

        Assert.False(pressed);
    }

    [Fact]
    public async Task Button_ShouldToggleTurnOffAndTakeAttributes_WhenItIsIconOnly()
    {
        var pressed = true;
        var cut = Render<TbRibbonButton>(parameters => parameters.Add(button => button.Label, "Mute")
            .Add(button => button.Icon, Square).Add(button => button.IconOnly, true)
            .Add(button => button.Pressed, pressed).Add(button => button.PressedChanged, value => pressed = value)
            .AddUnmatched("data-area", "alerts"));
        Assert.Equal("true", cut.Find("button").GetAttribute("aria-pressed"));
        Assert.Equal("alerts", cut.Find("button").GetAttribute("data-area"));

        await cut.Find("button").ClickAsync(new MouseEventArgs { Detail = 1 });

        Assert.False(pressed);
        cut.Render(parameters => parameters.Add(button => button.Disabled, true));
        Assert.True(cut.Find("button").HasAttribute("disabled"));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Button_ShouldCountItselfShownAndUsed_WhenGivenATelemetryName(bool iconOnly)
    {
        using var shown = new MetricCollector<long>(Services.GetRequiredService<IMeterFactory>(), "Trailblazor",
            "trailblazor.control.shown");
        using var used = new MetricCollector<long>(Services.GetRequiredService<IMeterFactory>(), "Trailblazor",
            "trailblazor.control.used");
        var cut = Render<TbRibbonButton>(parameters => parameters.Add(button => button.Label, "Paste")
            .Add(button => button.Icon, Square).Add(button => button.IconOnly, iconOnly)
            .Add(button => button.TelemetryName, "ribbon-paste"));

        await cut.Find("button").ClickAsync(new MouseEventArgs { Detail = 1 });

        Assert.Equal(["ribbon-paste"],
            shown.GetMeasurementSnapshot().Select(measurement => measurement.Tags["trailblazor.telemetry_name"]));
        Assert.Equal(["ribbon-paste"],
            used.GetMeasurementSnapshot().Select(measurement => measurement.Tags["trailblazor.telemetry_name"]));
    }

    [Fact]
    public void Button_ShouldTakeAMenusTriggerAttributes_WhenUsedAsATrigger()
    {
        var cut = Render<TbMenu>(parameters => parameters
            .Add(menu => menu.Trigger, trigger => builder =>
            {
                builder.OpenComponent<TbRibbonButton>(0);
                builder.AddComponentParameter(1, nameof(TbRibbonButton.Label), "File under");
                builder.AddMultipleAttributes(2, trigger.Attributes);
                builder.CloseComponent();
            })
            .AddChildContent<TbMenuItem>(item => item.AddChildContent("Planned")));

        var button = cut.Find("button.tb-ribbon__button");
        Assert.Equal(cut.Find("[role=menu]").Id, button.GetAttribute("popovertarget"));
        Assert.Equal("menu", button.GetAttribute("aria-haspopup"));
    }

    [Fact]
    public async Task Tabs_ShouldStartOnTheFirstAndFollowTheUser_WhenNoSelectionIsBound()
    {
        var cut = RenderRibbon();
        var ribbon = cut.Tb().Ribbon();
        Assert.Equal(["Notes", "Trails"], ribbon.Tabs);
        Assert.Equal("Notes", ribbon.Selected);
        Assert.Equal(["Write"], ribbon.Groups);

        await ribbon.SelectAsync("Trails");

        Assert.Equal("Trails", ribbon.Selected);
        Assert.Equal(["Plan"], ribbon.Groups);
        // The first tab's commands stay in the markup, hidden, so what its controls hold survives the switch.
        Assert.Equal(["Notes", "Trails"], cut.FindAll("[data-part=commands]").Select(row => row.GetAttribute("aria-label")));
        Assert.Equal("Trails", cut.Find("[role=tabpanel]:not([hidden]) [data-part=commands]").GetAttribute("aria-label"));
    }

    [Fact]
    public async Task Tabs_ShouldSelectByKeyAndReportTheKey_WhenActiveKeyIsBound()
    {
        var keys = new List<string?>();
        var cut = RenderRibbon(parameters => parameters.Add(ribbon => ribbon.ActiveKey, "trails")
            .Add(ribbon => ribbon.ActiveKeyChanged, keys.Add));
        var ribbon = cut.Tb().Ribbon();
        Assert.Equal("Trails", ribbon.Selected);

        await ribbon.SelectAsync("Notes");

        Assert.Equal(["notes"], keys);
        Assert.Equal("Notes", ribbon.Selected);
        // A new key from the parent moves the selection again.
        cut.Render(parameters => parameters.Add(ribbon => ribbon.ActiveKey, "notes"));
        cut.Render(parameters => parameters.Add(ribbon => ribbon.ActiveKey, "trails"));
        Assert.Equal("Trails", ribbon.Selected);
    }

    [Fact]
    public async Task Tabs_ShouldSelectByPositionAndReportIt_WhenActiveIndexIsBound()
    {
        var indexes = new List<int>();
        var cut = RenderRibbon(parameters => parameters.Add(ribbon => ribbon.ActiveIndex, 1)
            .Add(ribbon => ribbon.ActiveIndexChanged, indexes.Add));
        var ribbon = cut.Tb().Ribbon();
        Assert.Equal("Trails", ribbon.Selected);

        await ribbon.SelectAsync("Notes");

        Assert.Equal([0], indexes);
    }

    [Fact]
    public async Task Tabs_ShouldSelectByKey_WhenOnlyAKeyOrOnlyItsCallbackIsGiven()
    {
        var byKey = RenderRibbon(parameters => parameters.Add(ribbon => ribbon.ActiveKey, "trails"));
        Assert.Equal("Trails", byKey.Tb().Ribbon().Selected);

        // A callback alone selects by key too, starting from no key, so no tab.
        var keys = new List<string?>();
        var byCallback = RenderRibbon(parameters => parameters.Add(ribbon => ribbon.ActiveKeyChanged, keys.Add));
        Assert.Null(byCallback.Tb().Ribbon().Selected);
        await byCallback.Tb().Ribbon().SelectAsync("Trails");
        Assert.Equal(["trails"], keys);
    }

    [Fact]
    public async Task Tab_ShouldPassWhatItIsGivenToItsTabAndPanel_WhenGiven()
    {
        using var shown = new MetricCollector<long>(Services.GetRequiredService<IMeterFactory>(), "Trailblazor",
            "trailblazor.control.shown");
        using var used = new MetricCollector<long>(Services.GetRequiredService<IMeterFactory>(), "Trailblazor",
            "trailblazor.control.used");
        var cut = RenderRibbon(tabs: builder =>
        {
            AddTab(builder, 0, "Notes", null, _ => { });
            builder.OpenComponent<TbRibbonTab>(10);
            builder.AddComponentParameter(11, nameof(TbRibbonTab.Title), "Trails");
            builder.AddComponentParameter(12, nameof(TbRibbonTab.Icon), Square);
            builder.AddComponentParameter(13, nameof(TbRibbonTab.TelemetryName), "ribbon-trails");
            builder.AddComponentParameter(14, nameof(TbRibbonTab.Class), "wide");
            builder.AddComponentParameter(15, nameof(TbRibbonTab.Style), "min-height: 5rem");
            builder.AddAttribute(16, "data-area", "trails");
            builder.CloseComponent();
            builder.OpenComponent<TbRibbonTab>(20);
            builder.AddComponentParameter(21, nameof(TbRibbonTab.Title), "Maps");
            builder.AddComponentParameter(22, nameof(TbRibbonTab.Disabled), true);
            builder.CloseComponent();
        });
        var tabs = cut.FindAll("[role=tab]");
        Assert.NotNull(tabs[1].QuerySelector("[data-part=icon]"));
        Assert.Equal([false, false, true], tabs.Select(tab => tab.HasAttribute("disabled")));
        Assert.Equal(["ribbon-trails"],
            shown.GetMeasurementSnapshot().Select(measurement => measurement.Tags["trailblazor.telemetry_name"]));

        await cut.Tb().Ribbon().SelectAsync("Trails");

        var panel = cut.Find("[role=tabpanel]:not([hidden])");
        Assert.Contains("wide", panel.ClassList);
        Assert.Equal("min-height: 5rem", panel.GetAttribute("style"));
        Assert.Equal("trails", panel.GetAttribute("data-area"));
        Assert.Equal(["ribbon-trails"],
            used.GetMeasurementSnapshot().Select(measurement => measurement.Tags["trailblazor.telemetry_name"]));
    }

    [Fact]
    public async Task Dispose_ShouldDetachTheCommandsScript_WhenTheRibbonGoesAway()
    {
        var module = JSInterop.SetupModule("./_content/Trailblazor/ribbon.js");
        module.Mode = JSRuntimeMode.Loose;
        var handle = module.SetupModule("attachRibbon", _ => true);
        handle.SetupVoid("dispose").SetVoidResult();
        RenderRibbon();

        await DisposeComponentsAsync();

        handle.VerifyInvoke("dispose");
    }

    [Fact]
    public async Task Dispose_ShouldDetachTheCollapseScript_WhenTheRibbonGoesAway()
    {
        var module = JSInterop.SetupModule("./_content/Trailblazor/ribbon.js");
        module.Mode = JSRuntimeMode.Loose;
        var handle = module.SetupModule("attachRibbonRoot", _ => true);
        handle.SetupVoid("dispose").SetVoidResult();
        RenderRibbon();

        await DisposeComponentsAsync();

        handle.VerifyInvoke("dispose");
    }

    [Fact]
    public async Task Driver_ShouldRefuseAButtonItCannotClick_WhenItIsDisabledOrNotInTheSelectedTab()
    {
        var cut = RenderRibbon();
        var ribbon = cut.Tb().Ribbon();

        Assert.Equal("The ribbon's selected tab has no button named \"Add a trail\".",
            (await Assert.ThrowsAsync<InvalidOperationException>(() => ribbon.ClickAsync("Add a trail"))).Message);
        await ribbon.SelectAsync("Trails");
        Assert.Equal("The ribbon button \"Close the trail\" is disabled.",
            (await Assert.ThrowsAsync<InvalidOperationException>(() => ribbon.ClickAsync("Close the trail"))).Message);
        Assert.Equal("No tab reads \"Maps\".",
            (await Assert.ThrowsAsync<InvalidOperationException>(() => ribbon.SelectAsync("Maps"))).Message);
        Assert.Equal("No ribbon is named \"Maps\".", Assert.Throws<InvalidOperationException>(() =>
            cut.Tb().Ribbon("Maps").Tabs).Message);
    }

    [Fact]
    public async Task Driver_ShouldLeaveAMenusItemsOut_WhenAGroupHoldsAMenu()
    {
        var cut = RenderRibbon(tabs: builder => AddTab(builder, 0, "Notes", null, group =>
        {
            group.OpenComponent<TbMenu>(0);
            group.AddComponentParameter(1, nameof(TbMenu.Label), "File under");
            group.AddComponentParameter(2, nameof(TbMenu.ChildContent), (RenderFragment)(items =>
            {
                items.OpenComponent<TbMenuItem>(0);
                items.AddComponentParameter(1, nameof(TbMenuItem.ChildContent),
                    (RenderFragment)(text => text.AddContent(0, "Planned")));
                items.CloseComponent();
            }));
            group.CloseComponent();
        }));
        Assert.Equal("Planned", cut.Find("[role=menuitem]").TextContent.Trim());

        // The menu's button is one of the tab's buttons, and its items aren't.
        await cut.Tb().Ribbon().ClickAsync("File under");
        Assert.Equal("The ribbon's selected tab has no button named \"Planned\".",
            (await Assert.ThrowsAsync<InvalidOperationException>(() => cut.Tb().Ribbon().ClickAsync("Planned"))).Message);
    }

    [Fact]
    public void Driver_ShouldReadOnlyTheRibbonsOwnTabs_WhenAGroupHoldsTabs()
    {
        var cut = RenderRibbon(tabs: builder => AddTab(builder, 0, "Notes", null, group =>
        {
            group.OpenComponent<TbTabs>(0);
            group.AddComponentParameter(1, nameof(TbTabs.AriaLabel), "Note views");
            group.AddComponentParameter(2, nameof(TbTabs.ChildContent), (RenderFragment)(views =>
            {
                views.OpenComponent<TbTab>(0);
                views.AddComponentParameter(1, nameof(TbTab.Text), "List");
                views.CloseComponent();
            }));
            group.CloseComponent();
        }));

        Assert.Equal(["Notes"], cut.Tb().Ribbon().Tabs);
    }

    [Fact]
    public async Task Driver_ShouldRefuseToClick_WhenThePageIsStatic()
    {
        SetRendererInfo(new RendererInfo("Static", isInteractive: false));
        var clicked = new List<string>();
        var cut = RenderRibbon(onClick: clicked.Add);

        var still = await Assert.ThrowsAsync<InvalidOperationException>(
            () => cut.Tb().Ribbon().ClickAsync("New note"));

        Assert.Equal("On a static page the ribbon's buttons do nothing; render the page interactively to use them.",
            still.Message);
        Assert.Empty(clicked);
    }

    [Fact]
    public void Tab_ShouldThrow_WhenItIsOutsideARibbon()
    {
        var outside = Assert.Throws<InvalidOperationException>(() => Render<TbTabs>(parameters => parameters
            .Add(tabs => tabs.AriaLabel, "Tools")
            .Add(tabs => tabs.ChildContent, builder => AddTab(builder, 0, "Notes", null, _ => { }))));

        Assert.Equal("TbRibbonTab must be inside a TbRibbon.", outside.Message);
    }

    [Fact]
    public void KeyTip_ShouldMarkTheTabTheButtonsAndTheGroupsButton_WhenGiven()
    {
        var cut = RenderRibbon(tabs: builder => AddKeyTipTab(builder, "N", "W", "B"));

        Assert.Equal("N", cut.Find("[role=tab]").GetAttribute("data-key-tip"));
        Assert.Equal("W", cut.Find("[data-part=group-button]").GetAttribute("data-key-tip"));
        Assert.Equal(["B", "B"], cut.FindAll("[data-part=items] button").Select(b => b.GetAttribute("data-key-tip")));
        // The key reaches the tab's header alone, not its panel.
        Assert.False(cut.Find("[role=tabpanel]").HasAttribute("data-key-tip"));
    }

    [Fact]
    public void KeyTip_ShouldLeaveEveryControlUnmarked_WhenNotGiven()
    {
        var cut = RenderRibbon(tabs: builder => AddKeyTipTab(builder, null, null, null));

        Assert.Empty(cut.FindAll("[data-key-tip]"));
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("-")]
    [InlineData("AB")]
    public void KeyTip_ShouldBeRefused_WhenGivenAnythingButOneLetterOrDigit(string keyTip)
    {
        Assert.Equal("TbRibbonTab's KeyTip must be one letter or digit.", Assert.Throws<InvalidOperationException>(
            () => RenderRibbon(tabs: builder => AddKeyTipTab(builder, keyTip, null, null))).Message);
        Assert.Equal("TbRibbonGroup's KeyTip must be one letter or digit.", Assert.Throws<InvalidOperationException>(
            () => RenderRibbon(tabs: builder => AddKeyTipTab(builder, null, keyTip, null))).Message);
        Assert.Equal("TbRibbonButton's KeyTip must be one letter or digit.", Assert.Throws<InvalidOperationException>(
            () => RenderRibbon(tabs: builder => AddKeyTipTab(builder, null, null, keyTip))).Message);
    }

    [Fact]
    public async Task Driver_ShouldSelectTheTabAndClickItsButton_WhenGivenTheirKeyTips()
    {
        var clicked = new List<string>();
        var ribbon = RenderKeyTips(clicked).Tb().Ribbon();

        await ribbon.PressKeyTipsAsync("T", "b");

        Assert.Equal("Trails", ribbon.Selected);
        Assert.Equal(["Add a trail"], clicked);
    }

    [Fact]
    public async Task Driver_ShouldStartOverOnEachCallAndStopAfterACommand_WhenGivenMoreKeyTips()
    {
        var clicked = new List<string>();
        var ribbon = RenderKeyTips(clicked).Tb().Ribbon();
        await ribbon.PressKeyTipsAsync("T", "B");

        await ribbon.PressKeyTipsAsync("N", "B");

        Assert.Equal(["Add a trail", "Bold"], clicked);
        Assert.Equal("Key tips end once a command is chosen, so \"B\" has nothing to reach.",
            (await Assert.ThrowsAsync<InvalidOperationException>(() => ribbon.PressKeyTipsAsync("T", "B", "B")))
            .Message);
        Assert.Equal(["Add a trail", "Bold", "Add a trail"], clicked);
    }

    [Fact]
    public void KeyTip_ShouldComeFromTheButtonsOwnAttribute_WhenARibbonButtonHasNoKeyTip()
    {
        var cut = Render<TbRibbonButton>(parameters => parameters.Add(button => button.Label, "Bold")
            .AddUnmatched("data-key-tip", "X"));
        Assert.Equal("X", cut.Find("button").GetAttribute("data-key-tip"));

        cut.Render(parameters => parameters.Add(button => button.KeyTip, "B"));

        Assert.Equal("B", cut.Find("button").GetAttribute("data-key-tip"));
    }

    [Fact]
    public async Task Driver_ShouldClickAButtonBesideTheTabs_WhenGivenItsKeyTip()
    {
        var clicked = new List<string>();
        var ribbon = RenderKeyTips(clicked).Tb().Ribbon();

        await ribbon.PressKeyTipsAsync("Q");
        // The element around a field has nothing to click.
        await ribbon.PressKeyTipsAsync("F");

        Assert.Equal(["Quick save"], clicked);
        Assert.Equal("Notes", ribbon.Selected);
    }

    [Fact]
    public async Task Driver_ShouldFollowKeyTips_WhenTheRibbonIsInsideATabsPanel()
    {
        var clicked = new List<string>();
        var cut = Render<TbTabs>(parameters => parameters.Add(tabs => tabs.AriaLabel, "Sheets")
            .AddChildContent<TbTab>(tab => tab.Add(sheet => sheet.Text, "First sheet")
                .Add(sheet => sheet.ChildContent, KeyTipsRibbon(clicked))));

        await cut.Tb().Ribbon("Trail planner").PressKeyTipsAsync("T", "B");

        Assert.Equal(["Add a trail"], clicked);
    }

    [Fact]
    public async Task Driver_ShouldRefuseAKeyTip_WhenNothingHasItThereOrItsControlIsDisabled()
    {
        var clicked = new List<string>();
        var ribbon = RenderKeyTips(clicked).Tb().Ribbon();

        // B is a command's key, which counts only after its tab's.
        Assert.Equal("The ribbon has no key tip \"B\" on its tabs or beside them.",
            (await Assert.ThrowsAsync<InvalidOperationException>(() => ribbon.PressKeyTipsAsync("B"))).Message);
        Assert.Equal("The ribbon's selected tab has no key tip \"Q\".",
            (await Assert.ThrowsAsync<InvalidOperationException>(() => ribbon.PressKeyTipsAsync("N", "Q"))).Message);
        Assert.Equal("The ribbon control with the key tip \"L\" is disabled.",
            (await Assert.ThrowsAsync<InvalidOperationException>(() => ribbon.PressKeyTipsAsync("N", "L"))).Message);
        Assert.Empty(clicked);
    }

    [Fact]
    public async Task Driver_ShouldRefuseKeyTips_WhenThePageIsStatic()
    {
        SetRendererInfo(new RendererInfo("Static", isInteractive: false));
        var ribbon = RenderKeyTips([]).Tb().Ribbon();

        Assert.Equal("On a static page the ribbon has no key tips; render the page interactively to use them.",
            (await Assert.ThrowsAsync<InvalidOperationException>(() => ribbon.PressKeyTipsAsync("N"))).Message);
    }

    private IRenderedComponent<Bunit.Rendering.ContainerFragment> RenderKeyTips(List<string> clicked) =>
        Render(KeyTipsRibbon(clicked));

    // Quick save sits before the tabs with the key Q, beside a field in an element with the key F. Notes (N) has
    // Bold (B) and a disabled Lock (L), and Trails (T) has Add a trail, with B again.
    private RenderFragment KeyTipsRibbon(List<string> clicked) => builder =>
    {
        void AddTab(RenderTreeBuilder tabs, string title, string keyTip, params (string Label, string Key)[] buttons)
        {
            tabs.OpenComponent<TbRibbonTab>(0);
            tabs.SetKey(title);
            tabs.AddComponentParameter(1, nameof(TbRibbonTab.Title), title);
            tabs.AddComponentParameter(2, nameof(TbRibbonTab.KeyTip), keyTip);
            tabs.AddComponentParameter(3, nameof(TbRibbonTab.ChildContent), (RenderFragment)(groups =>
            {
                groups.OpenComponent<TbRibbonGroup>(0);
                groups.AddComponentParameter(1, nameof(TbRibbonGroup.Label), "Write");
                groups.AddComponentParameter(2, nameof(TbRibbonGroup.ChildContent), (RenderFragment)(items =>
                {
                    foreach (var (label, key) in buttons)
                    {
                        items.OpenComponent<TbRibbonButton>(0);
                        items.SetKey(label);
                        items.AddComponentParameter(1, nameof(TbRibbonButton.Label), label);
                        items.AddComponentParameter(2, nameof(TbRibbonButton.KeyTip), key);
                        items.AddComponentParameter(3, nameof(TbRibbonButton.Disabled), label == "Lock");
                        items.AddComponentParameter(4, nameof(TbRibbonButton.OnClick),
                            EventCallback.Factory.Create<MouseEventArgs>(this, () => clicked.Add(label)));
                        items.CloseComponent();
                    }
                }));
                groups.CloseComponent();
            }));
            tabs.CloseComponent();
        }

        builder.OpenComponent<TbRibbon>(0);
        builder.AddComponentParameter(1, nameof(TbRibbon.AriaLabel), "Trail planner");
        builder.AddComponentParameter(2, nameof(TbRibbon.Start), (RenderFragment)(start =>
        {
            start.OpenElement(0, "button");
            start.AddAttribute(1, "type", "button");
            start.AddAttribute(2, "data-key-tip", "q");
            start.AddAttribute(3, "onclick",
                EventCallback.Factory.Create<MouseEventArgs>(this, () => clicked.Add("Quick save")));
            start.AddContent(4, "Quick save");
            start.CloseElement();
            start.AddMarkupContent(5, "<span data-key-tip=\"F\"><input aria-label=\"Find\" /></span>");
        }));
        builder.AddComponentParameter(3, nameof(TbRibbon.ChildContent), (RenderFragment)(tabs =>
        {
            AddTab(tabs, "Notes", "N", ("Bold", "B"), ("Lock", "L"));
            AddTab(tabs, "Trails", "T", ("Add a trail", "B"));
        }));
        builder.CloseComponent();
    };

    // One tab whose group holds a button with text and one with an icon alone, which share the button's key tip.
    private static void AddKeyTipTab(RenderTreeBuilder builder, string? tab, string? group, string? button)
    {
        builder.OpenComponent<TbRibbonTab>(0);
        builder.AddComponentParameter(1, nameof(TbRibbonTab.Title), "Notes");
        builder.AddComponentParameter(2, nameof(TbRibbonTab.KeyTip), tab);
        builder.AddComponentParameter(3, nameof(TbRibbonTab.ChildContent), (RenderFragment)(groups =>
        {
            groups.OpenComponent<TbRibbonGroup>(0);
            groups.AddComponentParameter(1, nameof(TbRibbonGroup.Label), "Write");
            groups.AddComponentParameter(2, nameof(TbRibbonGroup.KeyTip), group);
            groups.AddComponentParameter(3, nameof(TbRibbonGroup.ChildContent), (RenderFragment)(items =>
            {
                foreach (var iconOnly in new[] { false, true })
                {
                    items.OpenComponent<TbRibbonButton>(0);
                    items.AddComponentParameter(1, nameof(TbRibbonButton.Label), "Bold");
                    items.AddComponentParameter(2, nameof(TbRibbonButton.Icon), Square);
                    items.AddComponentParameter(3, nameof(TbRibbonButton.IconOnly), iconOnly);
                    items.AddComponentParameter(4, nameof(TbRibbonButton.KeyTip), button);
                    items.CloseComponent();
                }
            }));
            groups.CloseComponent();
        }));
        builder.CloseComponent();
    }

    private IRenderedComponent<TbRibbon> RenderRibbon(
        Action<ComponentParameterCollectionBuilder<TbRibbon>>? more = null, Action<string>? onClick = null,
        Action<RenderTreeBuilder>? tabs = null) => Render<TbRibbon>(parameters =>
    {
        parameters.Add(ribbon => ribbon.AriaLabel, "Trail planner")
            .Add(ribbon => ribbon.ChildContent, builder => (tabs ?? (all => AddTabs(all, onClick)))(builder));
        more?.Invoke(parameters);
    });

    // Notes has a large button, a small one and an icon alone; Trails has a button that works and one turned off.
    private void AddTabs(RenderTreeBuilder builder, Action<string>? onClick)
    {
        AddTab(builder, 0, "Notes", "notes", group =>
        {
            AddButton(group, 0, "New note", onClick, large: true);
            AddButton(group, 10, "Find", onClick);
            AddButton(group, 20, "Mark as done", onClick, iconOnly: true);
        }, "Write");
        AddTab(builder, 10, "Trails", "trails", group =>
        {
            AddButton(group, 0, "Add a trail", onClick);
            AddButton(group, 10, "Close the trail", onClick, disabled: true);
        }, "Plan");
    }

    private static void AddTab(RenderTreeBuilder builder, int sequence, string title, string? key,
        Action<RenderTreeBuilder> buttons, string group = "Write")
    {
        builder.OpenComponent<TbRibbonTab>(sequence);
        builder.AddComponentParameter(sequence + 1, nameof(TbRibbonTab.Title), title);
        builder.AddComponentParameter(sequence + 2, nameof(TbRibbonTab.Key), key);
        builder.AddComponentParameter(sequence + 3, nameof(TbRibbonTab.ChildContent), (RenderFragment)(groups =>
        {
            groups.OpenComponent<TbRibbonGroup>(0);
            groups.AddComponentParameter(1, nameof(TbRibbonGroup.Label), group);
            groups.AddComponentParameter(2, nameof(TbRibbonGroup.ChildContent), (RenderFragment)(items => buttons(items)));
            groups.CloseComponent();
        }));
        builder.CloseComponent();
    }

    private void AddButton(RenderTreeBuilder builder, int sequence, string label, Action<string>? onClick = null,
        bool large = false, bool iconOnly = false, bool disabled = false)
    {
        builder.OpenComponent<TbRibbonButton>(sequence);
        builder.AddComponentParameter(sequence + 1, nameof(TbRibbonButton.Label), label);
        builder.AddComponentParameter(sequence + 2, nameof(TbRibbonButton.Icon), Square);
        builder.AddComponentParameter(sequence + 3, nameof(TbRibbonButton.Large), large);
        builder.AddComponentParameter(sequence + 4, nameof(TbRibbonButton.IconOnly), iconOnly);
        builder.AddComponentParameter(sequence + 5, nameof(TbRibbonButton.Disabled), disabled);
        builder.AddComponentParameter(sequence + 6, nameof(TbRibbonButton.OnClick),
            EventCallback.Factory.Create<MouseEventArgs>(this, () => onClick?.Invoke(label)));
        builder.CloseComponent();
    }
}
