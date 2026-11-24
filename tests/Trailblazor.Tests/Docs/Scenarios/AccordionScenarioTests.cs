using Bunit;
using Trailblazor.Docs.Examples;
using Trailblazor.Testing;

namespace Trailblazor.Tests.Docs.Scenarios;

// The page of this component shows the body of this test, so it has to read well without the class around it.
public class AccordionScenarioTests : BunitContext
{
    public AccordionScenarioTests() => this.AddTrailblazor();

    [Fact]
    public async Task Sizing_ShouldShowItsAnswerAndCloseShipping_WhenExpanded()
    {
        var cut = Render<AccordionExample>();
        var sizing = cut.Tb().AccordionItem("Sizing");
        var shipping = cut.Tb().AccordionItem("Shipping");
        Assert.Equal("true", shipping.Trigger.GetAttribute("aria-expanded"));

        // ExpandAsync gives back the item's panel, to check what it shows.
        var answer = await sizing.ExpandAsync();

        Assert.Equal("Sizes run true, so order your usual one.", answer.TextContent.Trim());
        // One item is open at a time unless the accordion is Multiple.
        Assert.Equal("false", shipping.Trigger.GetAttribute("aria-expanded"));
    }
}
