using Bunit;
using Trailblazor.Docs.Examples;
using Trailblazor.Testing;

namespace Trailblazor.Tests.Docs.Scenarios;

// The page of this component shows the body of this test, so it has to read well without the class around it.
public class CodeFieldScenarioTests : BunitContext
{
    public CodeFieldScenarioTests() => this.AddTrailblazor();

    [Fact]
    public async Task Code_ShouldBeTurnedDownThenAccepted_WhenAWrongCodeAndThenTheRightOneAreEntered()
    {
        var cut = Render<CodeFieldExample>();
        // A code field is driven by the text field's helper, found by its label.
        var code = cut.Tb().TextField("Verification code");
        var result = cut.Find("output");

        await code.EnterAsync("135790");
        Assert.Equal("135790 isn't the code.", result.TextContent);

        await code.EnterAsync("246810");
        Assert.Equal("Code accepted.", result.TextContent);
    }
}
