using Bunit;
using Trailblazor.Docs.Examples;
using Trailblazor.Testing;

namespace Trailblazor.Tests.Docs.Scenarios;

// The page of this component shows the body of this test, so it has to read well without the class around it.
public class LabelScenarioTests : BunitContext
{
    public LabelScenarioTests() => this.AddTrailblazor();

    [Fact]
    public async Task BagSize_ShouldExplainItselfAndNameItsField_WhenItsInfoIsOpenedAndItsFieldFilled()
    {
        var cut = Render<LabelInfoExample>();

        // The info button is named "Information about" and the label, and OpenAsync returns the panel it opens.
        var info = await cut.Tb().Popover("Information about Bag size").OpenAsync();
        Assert.Contains("A day pack is about 20.", info.TextContent);

        // The label names its field, so the field's helper finds it by the label's text.
        await cut.Tb().TextField("Bag size").EnterAsync("35");
        Assert.Equal("35", cut.Tb().TextField("Bag size").Text);
    }
}
