using Bunit;
using Trailblazor.Docs.Examples;
using Trailblazor.Testing;

namespace Trailblazor.Tests.Docs.Scenarios;

// The page of this component shows the body of this test, so it has to read well without the class around it.
public class CheckboxGroupScenarioTests : BunitContext
{
    public CheckboxGroupScenarioTests() => this.AddTrailblazor();

    [Fact]
    public async Task TrailDays_ShouldGiveTheAppTheTickedDays_WhenDaysAreTickedAndUnticked()
    {
        var cut = Render<CheckboxGroupExample>();
        var days = cut.Tb().CheckboxGroup("Trail days");
        var value = cut.Find("output");
        Assert.Equal(["Saturday"], days.Checked);

        // Each box is named by its text. Checked reads the boxes in the page's order, and the app's value keeps the
        // order they were ticked in.
        await days.CheckAsync("Monday", "Friday");
        Assert.Equal(["Monday", "Friday", "Saturday"], days.Checked);
        Assert.Equal("Trail days value: Saturday, Monday, Friday", value.TextContent);

        await days.UncheckAsync("Saturday");
        Assert.Equal("Trail days value: Monday, Friday", value.TextContent);
    }
}
