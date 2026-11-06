using Bunit;
using Trailblazor.Docs.Examples;

namespace Trailblazor.Tests.Docs;

public class TreeViewContentExampleTests : DocsTest
{
    [Fact]
    public void TreeViewContentExample_ShouldShowEachRowsToggleButNotItsContent_WhenClosed()
    {
        var cut = Render<TreeViewContentExample>();

        Assert.Equal(["Packing list", "Weather"], cut.Tb().Tree("Trip notes").Texts);
        Assert.All(cut.FindAll("[role=treeitem]"), row => Assert.Equal("false", row.GetAttribute("aria-expanded")));
        Assert.DoesNotContain("Boots, water and a paper map.", cut.Markup);
    }

    [Fact]
    public async Task TreeViewContentExample_ShouldRenderTheContentOnlyWhileTheRowIsOpen_WhenToggled()
    {
        var cut = Render<TreeViewContentExample>();

        await cut.Tb().Tree("Trip notes").ExpandAsync("Packing list");

        Assert.Contains("Boots, water and a paper map.", cut.Markup);
        Assert.DoesNotContain("Clear until the afternoon", cut.Markup);

        await cut.Tb().Tree("Trip notes").Item("Packing list").QuerySelector("[data-part=toggle]")!.ClickAsync(new());

        Assert.DoesNotContain("Boots, water and a paper map.", cut.Markup);
    }
}
