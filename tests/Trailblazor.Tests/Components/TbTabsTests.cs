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
        var handle = JSInterop.SetupModule("./_content/Trailblazor/tabs.js").SetupModule("attachTabs", _ => true);
        handle.Setup<bool>("reportsOrder").SetResult(true);
        handle.SetupVoid("dispose").SetVoidResult();
        SetRendererInfo(new RendererInfo("Server", isInteractive: true));
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
        SetRendererInfo(new RendererInfo("Server", isInteractive: true));
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
        module.SetupModule("attachTabs", _ => true).Setup<bool>("reportsOrder").SetResult(true);
        SetRendererInfo(new RendererInfo("Server", isInteractive: true));
        return () => (DotNetObjectReference<TabOrder>)module.Invocations["attachTabs"].Single().Arguments[1]!;
    }

    private static void WithIntroAfterOverview(RenderTreeBuilder builder)
    {
        AddTab(builder, 0, "Overview", "Overview panel");
        AddTab(builder, 5, "Intro", "Intro panel");
        AddTab(builder, 10, "Specs", "Specs panel");
        AddTab(builder, 20, "Reviews", "Reviews panel");
    }

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
