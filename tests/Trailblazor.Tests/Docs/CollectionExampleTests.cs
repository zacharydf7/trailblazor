using Bunit;
using Trailblazor.Docs.Examples;

namespace Trailblazor.Tests.Docs;

// The table, list and sortable pages' examples, as the docs show them.
public class CollectionExampleTests : DocsTest
{
    [Fact]
    public void TableEmptyExample_ShouldSpanTheColumns_WhenThereAreNoRows()
    {
        var cut = Render<TableEmptyExample>();

        Assert.Equal("3", cut.Find("td").GetAttribute("colspan"));
        Assert.Equal(3, cut.FindAll("th").Count);
    }

    [Fact]
    public void TableWideExample_ShouldScrollInANamedTabStop_WhenRendered()
    {
        var cut = Render<TableWideExample>();

        var area = cut.Find("[role=region]");

        Assert.Equal("Trail details", area.GetAttribute("aria-label"));
        Assert.Equal("0", area.GetAttribute("tabindex"));
    }

    [Fact]
    public void ListLinksExample_ShouldMarkTheCurrentPage_WhenAnItemIsSelected()
    {
        var cut = Render<ListLinksExample>();

        var links = cut.FindAll("a");

        Assert.Equal(["components/list", "components/table", "components/data-grid"],
            links.Select(link => link.GetAttribute("href")));
        Assert.Equal("page", links[0].GetAttribute("aria-current"));
        Assert.Null(links[1].GetAttribute("aria-current"));
    }

    [Fact]
    public void ListOrderedExample_ShouldRenderAnOrderedList_WhenRendered()
    {
        var cut = Render<ListOrderedExample>();

        Assert.Equal(3, cut.FindAll("ol > li").Count);
    }

    [Fact]
    public void ListEmptyExample_ShouldShowTheListInPlaceOfTheMessage_WhenATrailIsSaved()
    {
        var cut = Render<ListEmptyExample>();
        Assert.Empty(cut.FindAll("[role=list], ul"));

        Button(cut, "Save a trail").Click();

        Assert.Contains("Pine Hollow loop", cut.Find("ul").TextContent);
        Assert.DoesNotContain("Trails you save show here.", cut.Markup);
    }

    [Fact]
    public void SortableDisabledExample_ShouldTurnOffTheHandles_WhenItIsDisabled()
    {
        var cut = Render<SortableDisabledExample>();

        var handles = cut.FindAll("button[aria-label^=Move]");

        Assert.Equal(2, handles.Count);
        Assert.All(handles, handle => Assert.True(handle.HasAttribute("disabled")));
    }

    [Fact]
    public void SortableEmptyExample_ShouldShowTheListInPlaceOfTheMessage_WhenStopsAreAdded()
    {
        var cut = Render<SortableEmptyExample>();
        Assert.Empty(cut.FindAll("[data-part=list]"));

        Button(cut, "Add two stops").Click();

        Assert.Equal(2, cut.FindAll("[data-part=item]").Count);
    }

    [Fact]
    public void SortableContentExample_ShouldNameTheHandleByItsItemText_WhenItemsArentStrings()
    {
        var cut = Render<SortableContentExample>();

        Assert.NotNull(cut.Find("button[aria-label='Move Lake camp']"));
    }

    [Theory]
    [InlineData("table", "StickyHeader"), InlineData("list", "Href"), InlineData("sortable", "MoveAsync")]
    public void ComponentPage_ShouldKeepFeaturesOutOfAccessibility_WhenShowingACollection(string example,
        string feature)
    {
        var cut = ComponentPage(example);

        var accessibility = cut.Find("ul[aria-label=Accessibility]").TextContent;

        Assert.DoesNotContain(feature, accessibility);
    }
}
