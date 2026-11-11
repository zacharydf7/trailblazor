using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;

namespace Trailblazor.Tests.Components;

public class TbTabsTests : BunitContext
{
    public TbTabsTests()
    {
        this.AddTrailblazor();
    }

    [Fact]
    public void Render_ShouldLinkEachTabToItsPanel_WhenCalled()
    {
        var cut = RenderTabs(active: 1);

        var list = cut.Find("[role=tablist]");
        Assert.Equal("Product", list.GetAttribute("aria-label"));
        var tabs = cut.FindAll("[role=tab]");
        Assert.Equal(["Overview", "Specs", "Reviews"], tabs.Select(tab => tab.TextContent));
        Assert.Equal(["false", "true", "false"], tabs.Select(tab => tab.GetAttribute("aria-selected")));
        Assert.Equal(["-1", "0", "-1"], tabs.Select(tab => tab.GetAttribute("tabindex")));
        var panel = cut.Find("[role=tabpanel]");
        Assert.Equal("Specs panel", panel.TextContent.Trim());
        Assert.Equal(panel.Id, tabs[1].GetAttribute("aria-controls"));
        Assert.Equal(tabs[1].Id, panel.GetAttribute("aria-labelledby"));
        Assert.Null(tabs[0].GetAttribute("aria-controls"));
    }

    [Fact]
    public void Render_ShouldListTheTabs_WhenRenderedStatically()
    {
        SetRendererInfo(new RendererInfo("Static", isInteractive: false));

        var cut = RenderTabs();

        Assert.Equal(3, cut.FindAll("[role=tab]").Count);
        Assert.Equal("Overview panel", cut.Find("[role=tabpanel]").TextContent.Trim());
    }

    [Fact]
    public void Click_ShouldSelectTheTabAndRaiseActiveIndexChanged_WhenAnotherTabIsClicked()
    {
        int? selected = null;
        var cut = RenderTabs(changed: index => selected = index);

        cut.FindAll("[role=tab]")[2].Click();

        Assert.Equal(2, selected);
        Assert.Equal("Reviews panel", cut.Find("[role=tabpanel]").TextContent.Trim());
        Assert.Single(cut.FindAll("[role=tabpanel]"));
    }

    [Fact]
    public void Render_ShouldKeepSelectionFromAClick_WhenTheParentRendersAgainWithTheSameIndex()
    {
        var cut = RenderTabs();
        cut.FindAll("[role=tab]")[1].Click();

        cut.Render(parameters => parameters.Add(tabs => tabs.ActiveIndex, 0).Add(tabs => tabs.Centered, true));
        Assert.Equal("Specs panel", cut.Find("[role=tabpanel]").TextContent.Trim());

        cut.Render(parameters => parameters.Add(tabs => tabs.ActiveIndex, 2));
        Assert.Equal("Reviews panel", cut.Find("[role=tabpanel]").TextContent.Trim());
    }

    [Fact]
    public void Render_ShouldKeepVisitedPanelsHidden_WhenKeepAliveIsSet()
    {
        var cut = RenderTabs(keepAlive: true);

        cut.FindAll("[role=tab]")[1].Click();
        cut.FindAll("[role=tab]")[0].Click();

        var panels = cut.FindAll("[role=tabpanel]");
        Assert.Equal(["Overview panel", "Specs panel"], panels.Select(panel => panel.TextContent.Trim()));
        Assert.False(panels[0].HasAttribute("hidden"));
        Assert.True(panels[1].HasAttribute("hidden"));
        Assert.Equal(panels[1].Id, cut.FindAll("[role=tab]")[1].GetAttribute("aria-controls"));
    }

    [Fact]
    public void Render_ShouldRenderEveryPanelHiddenUnlessItsTabIsSelected_WhenRenderAllPanelsIsSet()
    {
        var cut = Render<TbTabs>(parameters => parameters.Add(tabs => tabs.RenderAllPanels, true)
            .Add(tabs => tabs.ChildContent, (RenderFragment)(builder =>
            {
                AddTab(builder, 0, "Overview", "Overview panel");
                AddTab(builder, 10, "Specs", "Specs panel");
            })));

        var panels = cut.FindAll("[role=tabpanel]");
        Assert.Equal(["Overview panel", "Specs panel"], panels.Select(panel => panel.TextContent.Trim()));
        Assert.Equal([false, true], panels.Select(panel => panel.HasAttribute("hidden")));
        Assert.Equal(panels.Select(panel => panel.Id),
            cut.FindAll("[role=tab]").Select(tab => tab.GetAttribute("aria-controls")));
    }

    [Fact]
    public void Render_ShouldRenderEveryPanelHiddenUntilFound_WhenThePageIsStatic()
    {
        SetRendererInfo(new RendererInfo("Static", isInteractive: false));

        var cut = Render<TbTabs>(parameters => parameters.Add(tabs => tabs.ChildContent, (RenderFragment)(builder =>
        {
            AddTab(builder, 0, "Overview", "Overview panel");
            AddTab(builder, 10, "Specs", "Specs panel");
            AddTab(builder, 20, "Pricing", "Pricing panel", disabled: true);
        })));

        var panels = cut.FindAll("[role=tabpanel]");
        Assert.Equal(["Overview panel", "Specs panel", "Pricing panel"], panels.Select(panel => panel.TextContent.Trim()));
        Assert.Equal([null, "until-found", ""], panels.Select(panel => panel.GetAttribute("hidden")));
        Assert.Equal(["0", null, null], panels.Select(panel => panel.GetAttribute("tabindex")));
        Assert.Equal(panels.Select(panel => panel.Id),
            cut.FindAll("[role=tab]").Select(tab => tab.GetAttribute("aria-controls")));
        Assert.All(cut.FindAll("[role=tab]"), tab => Assert.False(tab.HasAttribute("data-ready")));
    }

    [Fact]
    public void Render_ShouldPutTheHeaderClassOnTheTabAndFollowIt_WhenATabHasOne()
    {
        var cut = Render<TbTabs>(parameters => parameters.Add(tabs => tabs.ChildContent, HeaderClassTab("attention")));
        Assert.Equal(["tb-tabs__tab", "attention"], cut.Find("[role=tab]").ClassList);
        Assert.DoesNotContain("attention", cut.Find("[role=tabpanel]").ClassList);

        cut.Render(parameters => parameters.Add(tabs => tabs.ChildContent, HeaderClassTab(null)));

        Assert.Equal(["tb-tabs__tab"], cut.Find("[role=tab]").ClassList);
    }

    [Fact]
    public void Click_ShouldRaiseTheTabsOnClickOnceItIsSelected_WhenItsHeaderIsClickedSelectedOrNot()
    {
        var events = new List<string>();
        var cut = Render<TbTabs>(parameters => parameters
            .Add(tabs => tabs.ActiveIndexChanged, (int index) => events.Add($"selected {index}"))
            .Add(tabs => tabs.ChildContent, (RenderFragment)(builder =>
            {
                AddTab(builder, 0, "Overview", "Overview panel");
                builder.OpenComponent<TbTab>(10);
                builder.AddAttribute(11, nameof(TbTab.Text), "Map");
                builder.AddAttribute(12, nameof(TbTab.OnClick),
                    EventCallback.Factory.Create<Microsoft.AspNetCore.Components.Web.MouseEventArgs>(this,
                        () => events.Add("clicked")));
                builder.CloseComponent();
            })));

        cut.FindAll("[role=tab]")[1].Click();
        cut.FindAll("[role=tab]")[1].Click();

        Assert.Equal(["selected 1", "clicked", "clicked"], events);
    }

    [Fact]
    public async Task Select_ShouldSelectTheTabWithoutRaisingItsOnClick_WhenTheArrowKeysReachIt()
    {
        var order = SetUpOrder();
        var events = new List<string>();
        var cut = Render<TbTabs>(parameters => parameters
            .Add(tabs => tabs.ActiveIndexChanged, (int index) => events.Add($"selected {index}"))
            .Add(tabs => tabs.ChildContent, (RenderFragment)(builder =>
            {
                AddTab(builder, 0, "Overview", "Overview panel");
                builder.OpenComponent<TbTab>(10);
                builder.AddAttribute(11, nameof(TbTab.Text), "Map");
                builder.AddAttribute(12, nameof(TbTab.ChildContent),
                    (RenderFragment)(panel => panel.AddContent(0, "Map panel")));
                builder.AddAttribute(13, nameof(TbTab.OnClick),
                    EventCallback.Factory.Create<Microsoft.AspNetCore.Components.Web.MouseEventArgs>(this,
                        () => events.Add("clicked")));
                builder.CloseComponent();
            })));

        await cut.InvokeAsync(() => order().Value.OnSelect(cut.FindAll("[role=tab]")[1].Id!));

        Assert.Equal(["selected 1"], events);
        Assert.Equal("Map panel", cut.Find("[role=tabpanel]").TextContent.Trim());
    }

    [Fact]
    public void Render_ShouldMakeTheFirstEnabledTabReachable_WhenTheSelectedTabIsDisabled()
    {
        var cut = RenderTabs(active: 0, disabledIndex: 0);

        var tabs = cut.FindAll("[role=tab]");
        Assert.True(tabs[0].HasAttribute("disabled"));
        Assert.Equal(["-1", "0", "-1"], tabs.Select(tab => tab.GetAttribute("tabindex")));
    }

    [Fact]
    public void Render_ShouldRenderNoPanel_WhenATabHasNoContent()
    {
        var cut = Render<TbTabs>(parameters => parameters.Add(tabs => tabs.ChildContent, (RenderFragment)(builder =>
        {
            builder.OpenComponent<TbTab>(0);
            builder.AddAttribute(1, nameof(TbTab.Text), "Compare");
            builder.CloseComponent();
        })));

        Assert.Empty(cut.FindAll("[role=tabpanel]"));
        Assert.Null(cut.Find("[role=tab]").GetAttribute("aria-controls"));
    }

    [Fact]
    public void Render_ShouldUpdateAHeader_WhenItsTabChangesItsText()
    {
        var cut = RenderTabs();

        cut.Render(parameters => parameters.Add(tabs => tabs.ChildContent, (RenderFragment)(builder =>
        {
            AddTab(builder, 0, "Summary", "Overview panel");
            AddTab(builder, 10, "Specs", "Specs panel");
            AddTab(builder, 20, "Reviews", "Reviews panel", disabled: true);
        })));

        Assert.Equal(["Summary", "Specs", "Reviews"], cut.FindAll("[role=tab]").Select(tab => tab.TextContent));
        Assert.True(cut.FindAll("[role=tab]")[2].HasAttribute("disabled"));
    }

    [Fact]
    public void Render_ShouldRemoveAHeader_WhenItsTabGoesAway()
    {
        var cut = RenderTabs();

        cut.Render(parameters => parameters.Add(tabs => tabs.ChildContent, (RenderFragment)(builder =>
        {
            AddTab(builder, 0, "Overview", "Overview panel");
            AddTab(builder, 10, "Specs", "Specs panel");
        })));

        Assert.Equal(["Overview", "Specs"], cut.FindAll("[role=tab]").Select(tab => tab.TextContent));
    }

    [Fact]
    public void Remove_ShouldKeepTheSameTabSelected_WhenATabBeforeItGoesAway()
    {
        int? selected = null;
        var cut = RenderTabs(active: 1, changed: index => selected = index);

        cut.Render(parameters => parameters.Add(tabs => tabs.ChildContent, (RenderFragment)(builder =>
        {
            AddTab(builder, 10, "Specs", "Specs panel");
            AddTab(builder, 20, "Reviews", "Reviews panel");
        })));

        cut.WaitForAssertion(() => Assert.Equal(0, selected));
        Assert.Equal("Specs panel", cut.Find("[role=tabpanel]").TextContent.Trim());
    }

    [Fact]
    public void Remove_ShouldSelectThePreviousTab_WhenTheSelectedLastTabGoesAway()
    {
        int? selected = null;
        var cut = RenderTabs(active: 2, changed: index => selected = index);

        cut.Render(parameters => parameters.Add(tabs => tabs.ChildContent, (RenderFragment)(builder =>
        {
            AddTab(builder, 0, "Overview", "Overview panel");
            AddTab(builder, 10, "Specs", "Specs panel");
        })));

        cut.WaitForAssertion(() => Assert.Equal(1, selected));
        Assert.Equal("Specs panel", cut.Find("[role=tabpanel]").TextContent.Trim());
    }

    [Fact]
    public void Remove_ShouldKeepTheParentsIndex_WhenTheParentAdjustsItInTheSameRender()
    {
        int? selected = null;
        var cut = RenderTabs(active: 2, changed: index => selected = index);

        cut.Render(parameters => parameters.Add(tabs => tabs.ActiveIndex, 1).Add(tabs => tabs.ChildContent,
            (RenderFragment)(builder =>
            {
                AddTab(builder, 10, "Specs", "Specs panel");
                AddTab(builder, 20, "Reviews", "Reviews panel");
            })));

        Assert.Equal("Reviews panel", cut.Find("[role=tabpanel]").TextContent.Trim());
        Assert.Null(selected);
    }

    [Fact]
    public void Remove_ShouldKeepTheClickedTabSelected_WhenTheParentEchoesItsIndexAndRemovesAnEarlierTab()
    {
        int? selected = null;
        var cut = RenderTabs(changed: index => selected = index);
        cut.FindAll("[role=tab]")[1].Click();

        cut.Render(parameters => parameters.Add(tabs => tabs.ActiveIndex, 1).Add(tabs => tabs.ChildContent,
            (RenderFragment)(builder =>
            {
                AddTab(builder, 10, "Specs", "Specs panel");
                AddTab(builder, 20, "Reviews", "Reviews panel");
            })));

        cut.WaitForAssertion(() => Assert.Equal(0, selected));
        Assert.Equal("Specs panel", cut.Find("[role=tabpanel]").TextContent.Trim());
    }

    [Fact]
    public void Remove_ShouldReportAFailingCallbackToTheRenderer_WhenTheSelectionMoves()
    {
        var cut = RenderTabs(active: 1, changed: _ => throw new InvalidOperationException("Saving failed."));

        var exception = Assert.Throws<InvalidOperationException>(() => cut.Render(parameters => parameters.Add(
            tabs => tabs.ChildContent, (RenderFragment)(builder => AddTab(builder, 10, "Specs", "Specs panel")))));

        Assert.Equal("Saving failed.", exception.Message);
    }

    [Fact]
    public async Task Dispose_ShouldNotChangeTheSelection_WhenTheWholeListGoesAway()
    {
        int? selected = null;
        RenderTabs(active: 2, changed: index => selected = index);

        await DisposeComponentsAsync();

        Assert.Null(selected);
    }

    [Fact]
    public void Render_ShouldThrow_WhenATabIsOutsideTabs()
    {
        var exception = Assert.Throws<InvalidOperationException>(
            () => Render<TbTab>(parameters => parameters.Add(tab => tab.Text, "Overview")));

        Assert.Contains(nameof(TbTabs), exception.Message);
    }

    [Fact]
    public async Task Dispose_ShouldDetachTheKeyboardScript_WhenTheTabsGoAway()
    {
        var handle = await AttachTabsAsync(JSInterop.SetupModule("./_content/Trailblazor/tabs.js"));
        handle.SetupVoid("dispose").SetVoidResult();
        RenderTabs();

        await DisposeComponentsAsync();

        handle.VerifyInvoke("dispose");
    }

    [Fact]
    public async Task Reorder_ShouldMoveTheHeadersAndKeepTheSelectedTab_WhenTheBrowserReportsTheMarkupOrder()
    {
        var order = SetUpOrder();
        int? selected = null;
        var cut = RenderTabs(active: 1, changed: index => selected = index);
        cut.Render(parameters => parameters.Add(tabs => tabs.ChildContent, WithIntroAfterOverview));
        var ids = cut.FindAll("[role=tab]").Select(tab => tab.Id!).ToArray();

        await cut.InvokeAsync(() => order().Value.OnReorder([ids[0], ids[3], ids[1], ids[2]]));

        Assert.Equal(["Overview", "Intro", "Specs", "Reviews"],
            cut.FindAll("[role=tab]").Select(tab => tab.TextContent));
        Assert.Equal(2, selected);
        Assert.Equal("Specs panel", cut.Find("[role=tabpanel]").TextContent.Trim());
    }

    [Fact]
    public async Task Reorder_ShouldKeepTheParentsIndex_WhenTheParentSetsItAlongWithTheNewTab()
    {
        var order = SetUpOrder();
        int? selected = null;
        var cut = RenderTabs(active: 2, changed: index => selected = index);
        cut.Render(parameters => parameters.Add(tabs => tabs.ActiveIndex, 1)
            .Add(tabs => tabs.ChildContent, WithIntroAfterOverview));
        var ids = cut.FindAll("[role=tab]").Select(tab => tab.Id!).ToArray();

        await cut.InvokeAsync(() => order().Value.OnReorder([ids[0], ids[3], ids[1], ids[2]]));

        Assert.Equal("Intro panel", cut.Find("[role=tabpanel]").TextContent.Trim());
        Assert.Null(selected);
    }

    [Fact]
    public async Task Reorder_ShouldKeepTheSelectedTab_WhenTheParentsNewTabWasAlreadyReportedInPlace()
    {
        var order = SetUpOrder();
        int? selected = null;
        var cut = RenderTabs(changed: index => selected = index);
        cut.Render(parameters => parameters.Add(tabs => tabs.ActiveIndex, 3).Add(tabs => tabs.ChildContent,
            (RenderFragment)(builder =>
            {
                AddTab(builder, 0, "Overview", "Overview panel");
                AddTab(builder, 10, "Specs", "Specs panel");
                AddTab(builder, 20, "Reviews", "Reviews panel");
                AddTab(builder, 30, "Intro", "Intro panel");
            })));
        var ids = cut.FindAll("[role=tab]").Select(tab => tab.Id!).ToArray();
        await cut.InvokeAsync(() => order().Value.OnReorder(ids));

        await cut.InvokeAsync(() => order().Value.OnReorder([ids[3], ids[0], ids[1], ids[2]]));

        Assert.Equal(0, selected);
        Assert.Equal("Intro panel", cut.Find("[role=tabpanel]").TextContent.Trim());
    }

    [Fact]
    public void Render_ShouldLeaveATabToFocus_WhenTheParentRemovesTheSelectedTabAndSetsTheIndexAlongWithANewTab()
    {
        SetUpOrder();
        var cut = Render<TbTabs>(parameters => parameters.Add(tabs => tabs.ActiveIndex, 2)
            .Add(tabs => tabs.ChildContent, (RenderFragment)(builder =>
            {
                AddTab(builder, 10, "Overview", "Overview panel");
                AddTab(builder, 20, "Specs", "Specs panel");
                AddTab(builder, 30, "Reviews", "Reviews panel");
            })));

        cut.Render(parameters => parameters.Add(tabs => tabs.ActiveIndex, 0).Add(tabs => tabs.ChildContent,
            (RenderFragment)(builder =>
            {
                AddTab(builder, 5, "Intro", "Intro panel");
                AddTab(builder, 10, "Overview", "Overview panel");
                AddTab(builder, 20, "Specs", "Specs panel");
            })));

        // The order report is still on its way, so nothing is selected yet, but Tab must still reach a tab.
        Assert.Single(cut.FindAll("[role=tab][tabindex='0']"));
        Assert.Empty(cut.FindAll("[role=tab][aria-selected='true']"));
    }

    [Fact]
    public void Render_ShouldApplyTheParentsIndex_WhenTheTabsScriptIsUnavailableAndATabRendersAfterOthers()
    {
        var cut = RenderTabs();

        cut.Render(parameters => parameters.Add(tabs => tabs.ActiveIndex, 1)
            .Add(tabs => tabs.ChildContent, WithIntroAfterOverview));

        Assert.Equal("Specs panel", cut.Find("[role=tabpanel]").TextContent.Trim());
    }

    [Fact]
    public async Task Reorder_ShouldKeepTheHeaders_WhenATabRegisteredAfterTheReportWasSent()
    {
        var order = SetUpOrder();
        var cut = RenderTabs();
        cut.Render(parameters => parameters.Add(tabs => tabs.ChildContent, WithIntroAfterOverview));
        var ids = cut.FindAll("[role=tab]").Select(tab => tab.Id!).ToArray();

        await cut.InvokeAsync(() => order().Value.OnReorder([ids[2], ids[1], ids[0]]));

        Assert.Equal(["Overview", "Specs", "Reviews", "Intro"],
            cut.FindAll("[role=tab]").Select(tab => tab.TextContent));
    }

    [Fact]
    public void Render_ShouldSelectTheTabWithTheKey_WhenGivenAnActiveKey()
    {
        var cut = RenderKeyedTabs("specs", includeIntro: false);

        Assert.Equal(["false", "true", "false"], cut.FindAll("[role=tab]").Select(tab =>
            tab.GetAttribute("aria-selected")));
        Assert.Equal("Specs panel", cut.Find("[role=tabpanel]").TextContent.Trim());
    }

    [Fact]
    public void Click_ShouldReportTheTabsKeyButNotItsIndex_WhenSelectingByKey()
    {
        string? key = null;
        var indexReported = false;
        var cut = RenderKeyedTabs("overview", includeIntro: false, value => key = value, () => indexReported = true);

        cut.FindAll("[role=tab]")[2].Click();

        Assert.Equal("reviews", key);
        Assert.False(indexReported);
        Assert.Equal("Reviews panel", cut.Find("[role=tabpanel]").TextContent.Trim());
    }

    [Fact]
    public async Task Reorder_ShouldKeepTheKeyedTabSelected_WhenATabAppearsBeforeIt()
    {
        var order = SetUpOrder();
        string? reported = null;
        var cut = RenderKeyedTabs("specs", includeIntro: false, value => reported = value);
        cut.Render(parameters => parameters.Add(tabs => tabs.ChildContent, KeyedTabs(includeIntro: true)));
        var ids = cut.FindAll("[role=tab]").Select(tab => tab.Id!).ToArray();

        await cut.InvokeAsync(() => order().Value.OnReorder([ids[0], ids[3], ids[1], ids[2]]));

        Assert.Equal(["Overview", "Intro", "Specs", "Reviews"],
            cut.FindAll("[role=tab]").Select(tab => tab.TextContent));
        Assert.Equal("true", cut.FindAll("[role=tab]")[2].GetAttribute("aria-selected"));
        Assert.Equal("Specs panel", cut.Find("[role=tabpanel]").TextContent.Trim());
        Assert.Null(reported);
    }

    [Fact]
    public void Render_ShouldSelectNoTab_WhenTheKeyIsGivenAsNull()
    {
        var cut = Render<TbTabs>(parameters => parameters.Add(tabs => tabs.ActiveKey, null)
            .Add(tabs => tabs.ChildContent, KeyedTabs(includeIntro: false)));

        Assert.All(cut.FindAll("[role=tab]"), tab => Assert.Equal("false", tab.GetAttribute("aria-selected")));
    }

    [Fact]
    public void Render_ShouldUpdateAHeader_WhenItsContentChangesWithoutAnythingElse()
    {
        var count = 1;
        RenderFragment tabs = builder =>
        {
            var shown = count;
            builder.OpenComponent<TbTab>(0);
            builder.AddAttribute(1, nameof(TbTab.TextContent),
                (RenderFragment)(header => header.AddContent(0, $"Inbox {shown}")));
            builder.CloseComponent();
        };
        var cut = Render<TbTabs>(parameters => parameters.Add(t => t.ChildContent, tabs));

        count = 2;
        cut.Render(parameters => parameters.Add(t => t.ChildContent, tabs));

        Assert.Equal("Inbox 2", cut.Find("[role=tab]").TextContent);
    }

    [Fact]
    public void Render_ShouldSelectNoTab_WhenNoTabHasTheKey()
    {
        var cut = RenderKeyedTabs("pricing", includeIntro: false);

        Assert.All(cut.FindAll("[role=tab]"), tab => Assert.Equal("false", tab.GetAttribute("aria-selected")));
        Assert.Empty(cut.FindAll("[role=tabpanel]"));
        Assert.Equal("0", cut.FindAll("[role=tab]")[0].GetAttribute("tabindex"));
    }

    [Fact]
    public void Render_ShouldThrow_WhenATabHasNoKeyAndTheTabsSelectByKey()
    {
        var failure = Assert.Throws<InvalidOperationException>(() => Render<TbTabs>(parameters => parameters
            .Add(tabs => tabs.ActiveKey, "overview")
            .Add(tabs => tabs.ChildContent, (RenderFragment)(builder => AddTab(builder, 0, "Overview", "Panel")))));

        Assert.Equal("Every TbTab needs a Key when its TbTabs has an ActiveKey.", failure.Message);
    }

    [Fact]
    public void Render_ShouldThrow_WhenATabHasNoTextOrTextContent()
    {
        var failure = Assert.Throws<InvalidOperationException>(() => Render<TbTabs>(parameters => parameters
            .Add(tabs => tabs.ChildContent, (RenderFragment)(builder =>
            {
                builder.OpenComponent<TbTab>(0);
                builder.CloseComponent();
            }))));

        Assert.Equal("TbTab needs a Text or a TextContent.", failure.Message);
    }

    [Fact]
    public void Render_ShouldShowTheIconAndHeaderInTheTab_WhenGivenThem()
    {
        var cut = Render<TbTabs>(parameters => parameters.Add(tabs => tabs.ChildContent, (RenderFragment)(builder =>
        {
            builder.OpenComponent<TbTab>(0);
            builder.AddAttribute(1, nameof(TbTab.Icon), new Icon("<path d=\"M4 4h16v16H4z\" />"));
            builder.AddAttribute(2, nameof(TbTab.TextContent), (RenderFragment)(header =>
            {
                header.AddContent(0, "Comments ");
                header.OpenElement(1, "strong");
                header.AddContent(2, "3");
                header.CloseElement();
            }));
            builder.CloseComponent();
        })));

        var tab = cut.Find("[role=tab]");
        Assert.NotNull(tab.QuerySelector("[data-part=icon]"));
        Assert.Equal("3", tab.QuerySelector("strong")!.TextContent);
        Assert.Equal("Comments 3", tab.TextContent.Trim());
    }

    [Theory]
    [InlineData(false, "horizontal")]
    [InlineData(true, "vertical")]
    public void Render_ShouldMarkTheListVertical_WhenVertical(bool vertical, string orientation)
    {
        var cut = Render<TbTabs>(parameters => parameters.Add(tabs => tabs.Vertical, vertical)
            .Add(tabs => tabs.ChildContent, (RenderFragment)(builder => AddTab(builder, 0, "Overview", "Panel"))));

        Assert.Equal(orientation, cut.Find("[role=tablist]").GetAttribute("aria-orientation"));
        Assert.Equal(vertical, cut.Find(".tb-tabs").ClassList.Contains("tb-tabs--vertical"));
    }

    [Theory]
    [InlineData(Variant.Text, "tb-tabs--text")]
    [InlineData(Variant.Soft, "tb-tabs--soft")]
    [InlineData(Variant.Filled, "tb-tabs--filled")]
    [InlineData(Variant.Outlined, "tb-tabs--outlined")]
    public void Render_ShouldMarkTheTabsWithTheirVariant_WhenGivenOne(Variant variant, string modifier)
    {
        var cut = Render<TbTabs>(parameters => parameters.Add(tabs => tabs.Variant, variant)
            .Add(tabs => tabs.ChildContent, (RenderFragment)(builder => AddTab(builder, 0, "Overview", "Panel"))));

        Assert.Contains(modifier, cut.Find(".tb-tabs").ClassList);
    }

    [Fact]
    public void Render_ShouldMarkTheTabsAsText_WhenNoVariantIsGiven()
    {
        var cut = Render<TbTabs>(parameters => parameters
            .Add(tabs => tabs.ChildContent, (RenderFragment)(builder => AddTab(builder, 0, "Overview", "Panel"))));

        Assert.Contains("tb-tabs--text", cut.Find(".tb-tabs").ClassList);
    }

    [Theory]
    [InlineData(Size.Small, "tb-tabs--small")]
    [InlineData(Size.Large, "tb-tabs--large")]
    public void Render_ShouldMarkTheSize_WhenGivenASize(Size size, string modifier)
    {
        var cut = RenderTabs();

        cut.Render(parameters => parameters.Add(tabs => tabs.Size, size));

        Assert.Contains(modifier, cut.Find(".tb-tabs").ClassList);
    }

    private IRenderedComponent<TbTabs> RenderKeyedTabs(string active, bool includeIntro,
        Action<string?>? keyChanged = null, Action? indexChanged = null) =>
        Render<TbTabs>(parameters => parameters.Add(tabs => tabs.ActiveKey, active)
            .Add(tabs => tabs.ActiveKeyChanged, (string? key) => keyChanged?.Invoke(key))
            .Add(tabs => tabs.ActiveIndexChanged, (int _) => indexChanged?.Invoke())
            .Add(tabs => tabs.ChildContent, KeyedTabs(includeIntro)));

    private static RenderFragment KeyedTabs(bool includeIntro) => builder =>
    {
        AddTab(builder, 0, "Overview", "Overview panel", key: "overview");
        if (includeIntro)
        {
            AddTab(builder, 10, "Intro", "Intro panel", key: "intro");
        }

        AddTab(builder, 20, "Specs", "Specs panel", key: "specs");
        AddTab(builder, 30, "Reviews", "Reviews panel", key: "reviews");
    };

    // The tabs script's callback, once the tabs have attached it.
    private Func<DotNetObjectReference<TabOrder>> SetUpOrder()
    {
        var module = JSInterop.SetupModule("./_content/Trailblazor/tabs.js");
        AttachTabsAsync(module).GetAwaiter().GetResult();
        return () => (DotNetObjectReference<TabOrder>)module.Invocations["attachTabs"].Single().Arguments[1]!;
    }

    // The script hands back its handle inside an object; here a module of its own stands in for the handle.
    private async Task<BunitJSModuleInterop> AttachTabsAsync(BunitJSModuleInterop module)
    {
        var handle = JSInterop.SetupModule("tabs-handle");
        var reference = await JSInterop.JSRuntime.InvokeAsync<IJSObjectReference>("import", "tabs-handle");
        module.Setup<TabsAttachment?>("attachTabs", _ => true).SetResult(new TabsAttachment(reference));
        return handle;
    }

    private static void WithIntroAfterOverview(RenderTreeBuilder builder)
    {
        AddTab(builder, 0, "Overview", "Overview panel");
        AddTab(builder, 5, "Intro", "Intro panel");
        AddTab(builder, 10, "Specs", "Specs panel");
        AddTab(builder, 20, "Reviews", "Reviews panel");
    }

    [Fact]
    public void Render_ShouldPutAGivenIdOnTheTabAndNameItsPanelByIt_WhenTheTabHasOne()
    {
        var cut = Render<TbTabs>(parameters => parameters.Add(tabs => tabs.AriaLabel, "Product")
            .Add(tabs => tabs.ChildContent, SpecsTab("specs")));

        Assert.Equal("tab", Assert.Single(cut.FindAll("#specs")).GetAttribute("role"));
        Assert.Equal("specs", cut.Find("[role=tabpanel]").GetAttribute("aria-labelledby"));
    }

    [Fact]
    public void Render_ShouldMoveTheTabsIdAndItsPanelsName_WhenTheGivenIdChanges()
    {
        var cut = Render<TbTabs>(parameters => parameters.Add(tabs => tabs.AriaLabel, "Product")
            .Add(tabs => tabs.ChildContent, SpecsTab("specs")));

        cut.Render(parameters => parameters.Add(tabs => tabs.ChildContent, SpecsTab("details")));

        Assert.Equal("details", cut.Find("[role=tab]").Id);
        Assert.Equal("details", cut.Find("[role=tabpanel]").GetAttribute("aria-labelledby"));
    }

    private static RenderFragment HeaderClassTab(string? headerClass) => builder =>
    {
        builder.OpenComponent<TbTab>(0);
        builder.AddAttribute(1, nameof(TbTab.Text), "Leader");
        builder.AddAttribute(2, nameof(TbTab.HeaderClass), headerClass);
        builder.AddAttribute(3, nameof(TbTab.ChildContent), (RenderFragment)(panel => panel.AddContent(0, "Name")));
        builder.CloseComponent();
    };

    private static RenderFragment SpecsTab(string id) => builder =>
    {
        builder.OpenComponent<TbTab>(0);
        builder.AddAttribute(1, nameof(TbTab.Text), "Specs");
        builder.AddAttribute(2, "id", id);
        builder.AddAttribute(3, nameof(TbTab.ChildContent),
            (RenderFragment)(panel => panel.AddContent(0, "Specs panel")));
        builder.CloseComponent();
    };

    private IRenderedComponent<TbTabs> RenderTabs(int active = 0, Action<int>? changed = null, bool keepAlive = false,
        int disabledIndex = -1) =>
        Render<TbTabs>(parameters => parameters
            .Add(tabs => tabs.ActiveIndex, active)
            .Add(tabs => tabs.ActiveIndexChanged, (int index) => changed?.Invoke(index))
            .Add(tabs => tabs.KeepAlive, keepAlive)
            .Add(tabs => tabs.AriaLabel, "Product")
            .Add(tabs => tabs.ChildContent, (RenderFragment)(builder =>
            {
                AddTab(builder, 0, "Overview", "Overview panel", disabledIndex == 0);
                AddTab(builder, 10, "Specs", "Specs panel", disabledIndex == 1);
                AddTab(builder, 20, "Reviews", "Reviews panel", disabledIndex == 2);
            })));

    [Fact]
    public void Render_ShouldShowACloseMarkAndNameItsKeys_WhenATabCanClose()
    {
        var cut = Render<TbTabs>(parameters => parameters.Add(tabs => tabs.ChildContent, ClosableTabs(() => { })));

        var tabs = cut.FindAll("[role=tab]");
        Assert.Equal(["Overview", "Specs", "Reviews"], tabs.Select(tab => tab.TextContent));
        Assert.Equal([null, "Delete Backspace", null], tabs.Select(tab => tab.GetAttribute("aria-keyshortcuts")));
        var mark = Assert.Single(cut.FindAll("[data-part=close]"));
        Assert.Equal("true", mark.GetAttribute("aria-hidden"));
        Assert.Same(tabs[1], mark.Closest("[role=tab]"));
    }

    [Fact]
    public void Render_ShouldShowNoCloseMark_WhenThePageIsStatic()
    {
        SetRendererInfo(new RendererInfo("Static", isInteractive: false));

        var cut = Render<TbTabs>(parameters => parameters.Add(tabs => tabs.ChildContent, ClosableTabs(() => { })));

        Assert.Empty(cut.FindAll("[data-part=close]"));
        Assert.Null(cut.FindAll("[role=tab]")[1].GetAttribute("aria-keyshortcuts"));
    }

    [Fact]
    public void Close_ShouldRaiseOnCloseWithoutSelectingTheTab_WhenItsMarkIsClicked()
    {
        var events = new List<string>();
        var cut = Render<TbTabs>(parameters => parameters
            .Add(tabs => tabs.ActiveIndexChanged, (int index) => events.Add($"selected {index}"))
            .Add(tabs => tabs.ChildContent, ClosableTabs(() => events.Add("closed"))));

        cut.Find("[data-part=close]").Click();

        Assert.Equal(["closed"], events);
        Assert.Equal("Overview panel", cut.Find("[role=tabpanel]").TextContent.Trim());
    }

    [Fact]
    public async Task Delete_ShouldRaiseOnCloseOnlyForAClosableTab_WhenTheScriptReportsIt()
    {
        var order = SetUpOrder();
        var closed = 0;
        var cut = Render<TbTabs>(parameters => parameters.Add(tabs => tabs.ChildContent, ClosableTabs(() => closed++)));
        var tabs = cut.FindAll("[role=tab]");

        foreach (var tab in tabs)
        {
            await cut.InvokeAsync(() => order().Value.OnClose(tab.Id!));
        }

        Assert.Equal(1, closed);
    }

    [Fact]
    public void Close_ShouldLeaveFocusAlone_WhenTheParentKeepsTheTabAndRemovesItLater()
    {
        var cut = Render<TbTabs>(parameters => parameters.Add(tabs => tabs.ChildContent, ClosableTabs(() => { })));

        cut.Find("[data-part=close]").Click();
        cut.Render(parameters => parameters.Add(tabs => tabs.ChildContent, (RenderFragment)(builder =>
        {
            AddTab(builder, 0, "Overview", "Overview panel");
            AddTab(builder, 20, "Reviews", "Reviews panel");
        })));

        Assert.Equal(2, cut.FindAll("[role=tab]").Count);
        Assert.Empty(JSInterop.Invocations["Blazor._internal.domWrapper.focus"]);
    }

    [Fact]
    public void Close_ShouldFocusTheEnabledTabBefore_WhenTheNextTabIsDisabled()
    {
        var cut = Render<ClosingHost>(parameters => parameters.Add(host => host.DisabledText, "Reviews"));

        cut.FindAll("[data-part=close]")[1].Click();

        Assert.Equal(["Overview", "Reviews"], cut.FindAll("[role=tab]").Select(tab => tab.TextContent));
        Assert.Equal(Header(cut, "Overview").Id, FocusedReference().Id);
    }

    [Fact]
    public async Task Close_ShouldFocusTheNextTab_WhenOnCloseRemovesTheTabAfterAwaiting()
    {
        var answer = new TaskCompletionSource();
        var cut = Render<ClosingHost>(parameters => parameters.Add(host => host.Answer, answer.Task));
        cut.FindAll("[data-part=close]")[1].Click();
        Assert.Equal(3, cut.FindAll("[role=tab]").Count);

        await cut.InvokeAsync(answer.SetResult);

        Assert.Equal(["Overview", "Reviews"], cut.FindAll("[role=tab]").Select(tab => tab.TextContent));
        Assert.Equal(Header(cut, "Reviews").Id, FocusedReference().Id);
    }

    // bUnit's markup loses a re-rendered header's reference id, so the tab's own reference is compared instead.
    private static ElementReference Header(IRenderedComponent<ClosingHost> cut, string text) =>
        cut.FindComponents<TbTab>().Single(tab => tab.Instance.Text == text).Instance.Header;

    private ElementReference FocusedReference() => (ElementReference)JSInterop.VerifyFocusAsyncInvoke().Arguments[0]!;

    // Every tab can be closed, and is removed once Answer completes, when one is given.
    private sealed class ClosingHost : ComponentBase
    {
        private readonly List<string> _tabs = ["Overview", "Specs", "Reviews"];

        [Parameter]
        public string? DisabledText { get; set; }

        [Parameter]
        public Task? Answer { get; set; }

        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            builder.OpenComponent<TbTabs>(0);
            builder.AddAttribute(1, nameof(TbTabs.ChildContent), (RenderFragment)(content =>
            {
                foreach (var text in _tabs)
                {
                    content.OpenComponent<TbTab>(2);
                    content.SetKey(text);
                    content.AddAttribute(3, nameof(TbTab.Text), text);
                    content.AddAttribute(4, nameof(TbTab.Disabled), text == DisabledText);
                    content.AddAttribute(5, nameof(TbTab.OnClose), EventCallback.Factory.Create(this, async () =>
                    {
                        await (Answer ?? Task.CompletedTask);
                        _tabs.Remove(text);
                    }));
                    content.AddAttribute(6, nameof(TbTab.ChildContent),
                        (RenderFragment)(panel => panel.AddContent(0, $"{text} panel")));
                    content.CloseComponent();
                }
            }));
            builder.CloseComponent();
        }
    }

    // Specs and Reviews hear OnClose, but only Specs can be closed, since Reviews is disabled.
    private RenderFragment ClosableTabs(Action onClose) => builder =>
    {
        AddTab(builder, 0, "Overview", "Overview panel");
        Add(10, "Specs", disabled: false);
        Add(20, "Reviews", disabled: true);

        void Add(int sequence, string text, bool disabled)
        {
            builder.OpenComponent<TbTab>(sequence);
            builder.AddAttribute(sequence + 1, nameof(TbTab.Text), text);
            builder.AddAttribute(sequence + 2, nameof(TbTab.Disabled), disabled);
            builder.AddAttribute(sequence + 3, nameof(TbTab.OnClose), EventCallback.Factory.Create(this, onClose));
            builder.AddAttribute(sequence + 4, nameof(TbTab.ChildContent),
                (RenderFragment)(panel => panel.AddContent(0, $"{text} panel")));
            builder.CloseComponent();
        }
    };

    private static void AddTab(RenderTreeBuilder builder, int sequence, string text, string content,
        bool disabled = false, string? key = null)
    {
        builder.OpenComponent<TbTab>(sequence);
        builder.AddAttribute(sequence + 1, nameof(TbTab.Text), text);
        builder.AddAttribute(sequence + 2, nameof(TbTab.Disabled), disabled);
        builder.AddAttribute(sequence + 3, nameof(TbTab.ChildContent),
            (RenderFragment)(panel => panel.AddContent(0, content)));
        builder.AddAttribute(sequence + 4, nameof(TbTab.Key), key);
        builder.CloseComponent();
    }
}
