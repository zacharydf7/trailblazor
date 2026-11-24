using Bunit;
using Trailblazor.Docs.Examples;
using Trailblazor.Testing;

namespace Trailblazor.Tests.Docs.Scenarios;

// The page of this component shows the body of this test, so it has to read well without the class around it.
public class SplitterScenarioTests : BunitContext
{
    public SplitterScenarioTests() => this.AddTrailblazor();

    [Fact]
    public async Task TrailsPane_ShouldGiveTheAppItsSize_WhenResizedCollapsedAndRestored()
    {
        var cut = Render<SplitterExample>();
        // A separator is named after the pane it resizes.
        var trails = cut.Tb().Splitter("Resize Trails");
        Assert.Equal(25, trails.Size);

        // ResizeAsync moves the separator as its keys do, to a percent of the splitter.
        await trails.ResizeAsync(35);
        Assert.Contains(cut.FindAll("p"), text => text.TextContent == "Trails 35%, details 25%");

        await trails.CollapseAsync();
        Assert.True(trails.Collapsed);
        Assert.Contains(cut.FindAll("p"), text => text.TextContent == "Trails hidden, details 25%");

        // The pane comes back at the size it had.
        await trails.RestoreAsync();
        Assert.Contains(cut.FindAll("p"), text => text.TextContent == "Trails 35%, details 25%");
    }
}
