using Bunit;
using Microsoft.AspNetCore.Components.Web;
using Trailblazor.Docs.Examples;

namespace Trailblazor.Tests.Docs;

public class TreeViewExampleTests : DocsTest
{
    private static readonly TimeSpan Load = TimeSpan.FromSeconds(5);

    [Fact]
    public void LoadExample_ShouldShowTheLoadingRowThenTheChildren_WhenARowOpens()
    {
        var cut = Render<TreeViewLoadExample>();
        var tree = cut.Tb().Tree("Regions to open");

        tree.Item("North ridge").QuerySelector("[data-part=toggle]")!.Click();

        cut.WaitForAssertion(() => Assert.NotNull(cut.Find("[data-part=loading]")));
        cut.WaitForAssertion(() => Assert.Contains("North ridge loop", tree.Texts), Load);
    }

    [Fact]
    public void LoadExample_ShouldOfferToTryAgainAndCountTheFailure_WhenALoadFails()
    {
        var cut = Render<TreeViewLoadExample>();
        cut.Find("input[type=checkbox]").Change(true);

        cut.Tb().Tree("Regions to open").Item("River valley").QuerySelector("[data-part=toggle]")!.Click();

        cut.WaitForAssertion(() => Assert.NotNull(cut.Find("[data-part=failed]")), Load);
        Assert.Equal("Failed loads: 1", cut.Find("output").TextContent.Trim());
        Assert.False(cut.Find("input[type=checkbox]").HasAttribute("checked"));

        cut.Find("[data-part=failed] button").Click();

        cut.WaitForAssertion(() => Assert.Contains("River valley spur", cut.Tb().Tree("Regions to open").Texts), Load);
    }

    [Fact]
    public void EmptyExample_ShouldShowTheDefaultMessageAndItsOwnContent_WhenThereAreNoRows()
    {
        var cut = Render<TreeViewEmptyExample>();

        var messages = cut.FindAll("[data-part=empty]");

        Assert.Equal(2, messages.Count);
        Assert.False(string.IsNullOrWhiteSpace(messages[0].TextContent));
        Assert.Contains("Regions you save show here.", messages[1].TextContent);
    }

    [Fact]
    public void EmptyExample_ShouldShowTheRow_WhenARegionIsSaved()
    {
        var cut = Render<TreeViewEmptyExample>();

        Button(cut, "Save a region").Click();

        Assert.Equal(["North ridge"], cut.Tb().Tree("Saved regions").Texts);
    }

    [Fact]
    public void FlatExample_ShouldNestEachPlaceUnderItsParent_WhenRendered()
    {
        var cut = Render<TreeViewFlatExample>();

        var tree = cut.Tb().Tree("Park map");

        Assert.Equal(["North ridge", "Pine Hollow loop", "Eagle Ridge traverse", "River valley", "Cedar Flats"],
            tree.Texts);
        Assert.Equal("2", tree.Item("North ridge", "Pine Hollow loop").GetAttribute("aria-level"));
    }

    [Fact]
    public void MultipleExample_ShouldHoldEveryChosenRow_WhenARowIsAddedWithCtrl()
    {
        var cut = Render<TreeViewMultipleExample>();
        var tree = cut.Tb().Tree("Regions to visit");

        tree.Item("North ridge").Click();
        tree.Item("High peaks").Click(new() { CtrlKey = true });

        Assert.Equal("Chosen: 2", cut.Find("output").TextContent.Trim());
    }

    [Fact]
    public void DisabledExample_ShouldTurnOffOnlyTheClosedRow_WhenRendered()
    {
        var cut = Render<TreeViewDisabledExample>();

        var tree = cut.Tb().Tree("Open and closed regions");

        Assert.Equal("true", tree.Item("River valley").GetAttribute("aria-disabled"));
        Assert.Null(tree.Item("North ridge").GetAttribute("aria-disabled"));
    }

    [Fact]
    public void TemplateExample_ShouldDrawItsOwnContentAndKeepTheText_WhenRendered()
    {
        var cut = Render<TreeViewTemplateExample>();

        var tree = cut.Tb().Tree("Regions and hours");

        Assert.Equal(["North ridge", "River valley"], tree.Texts);
        Assert.Contains("Open 8 to 6", tree.Item("North ridge").TextContent);
    }

    [Theory]
    [InlineData("f"), InlineData("F")]
    public void TypeAheadExample_ShouldLeaveTheLetterToThePage_WhenTheTreesTypeAheadIsOff(string key)
    {
        var cut = Render<TreeViewTypeAheadExample>();

        cut.Find("[aria-label='Regions to flag']").KeyDown(new KeyboardEventArgs { Key = key });

        Assert.Contains("Flags: 1", cut.Find("output").TextContent);
        Assert.True(cut.Find("[aria-label='Regions to flag']").HasAttribute("data-no-typeahead"));
        Assert.False(cut.Find("[aria-label='Regions to find']").HasAttribute("data-no-typeahead"));
    }

    [Fact]
    public async Task IndependentChecksExample_ShouldCheckOnlyTheParent_WhenItIsChecked()
    {
        var cut = Render<TreeViewIndependentChecksExample>();
        var tree = cut.Tb().Tree("Gear to pack");

        await tree.CheckAsync("Kitchen kit");

        Assert.Equal("true", tree.Item("Kitchen kit").GetAttribute("aria-checked"));
        Assert.Equal("false", tree.Item("Kitchen kit", "Stove").GetAttribute("aria-checked"));
    }

    [Fact]
    public void LabelsExample_ShouldUseItsOwnWords_WhenALoadFails()
    {
        var cut = Render<TreeViewLabelsExample>();

        cut.Tb().Tree("Regions on the old map").Item("North ridge").QuerySelector("[data-part=toggle]")!.Click();

        cut.WaitForAssertion(() =>
            Assert.Contains("Reading the old map...", cut.Find("[data-part=loading]").TextContent));
        cut.WaitForAssertion(() =>
            Assert.Contains("The old map wouldn't load.", cut.Find("[data-part=failed]").TextContent), Load);
        Assert.Contains("Ask again", cut.Find("[data-part=failed] button").TextContent);
    }

    [Fact]
    public void ComponentPage_ShouldGroupTheKeysAndKeepFeaturesOutOfAccessibility_WhenShowingTheTreeView()
    {
        var cut = ComponentPage("tree-view");
        var accessibility = cut.Find("ul[aria-label=Accessibility]").TextContent;

        Assert.Contains("Moving rows", cut.Markup);
        Assert.Contains("ExpandAllAsync", cut.Find("ul[aria-label='Testing tips']").TextContent);
        Assert.DoesNotContain("OnDrop", accessibility);
        Assert.DoesNotContain("InsertTemplate", accessibility);
    }
}
