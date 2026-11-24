using Bunit;
using Trailblazor.Docs.Examples;
using Trailblazor.Testing;

namespace Trailblazor.Tests.Docs.Scenarios;

// The page of this component shows the body of this test, so it has to read well without the class around it.
public class NumberFieldScenarioTests : BunitContext
{
    public NumberFieldScenarioTests() => this.AddTrailblazor();

    [Fact]
    public async Task Quantity_ShouldKeepTheAppsValueWithinItsLimits_WhenSteppedAndTyped()
    {
        var cut = Render<NumberFieldExample>();
        var quantity = cut.Tb().NumberField("Quantity");
        var value = cut.Find("output");

        // StepAsync presses the field's step buttons, up for a positive count and down for a negative one.
        await quantity.StepAsync(2);
        Assert.Equal("Quantity value: 3", value.TextContent);

        // A number past the limit stays in the field with an error, and the app keeps the value it had.
        await quantity.EnterAsync("12");
        Assert.True(quantity.Invalid);
        Assert.Equal("12", quantity.Text);
        Assert.Equal("Quantity value: 3", value.TextContent);

        await quantity.EnterAsync("7");
        Assert.False(quantity.Invalid);
        Assert.Equal("Quantity value: 7", value.TextContent);
    }
}
