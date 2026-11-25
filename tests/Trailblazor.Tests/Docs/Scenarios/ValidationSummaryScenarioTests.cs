using Bunit;
using Trailblazor.Docs.Examples;
using Trailblazor.Testing;

namespace Trailblazor.Tests.Docs.Scenarios;

// The page of this component shows the body of this test, so it has to read well without the class around it.
public class ValidationSummaryScenarioTests : BunitContext
{
    public ValidationSummaryScenarioTests() => this.AddTrailblazor();

    [Fact]
    public async Task SignUp_ShouldListWhatToFixAndLinkToIt_WhenSubmittedEmpty()
    {
        var cut = Render<ValidationSummaryExample>();

        await cut.Tb().Button("Sign up").ClickAsync();
        var summary = cut.Tb().ValidationSummary();
        Assert.Equal(["Name is required.", "Email is required.", "Emergency contact is required."], summary.Messages);

        await summary.FollowAsync("Email is required.");

        JSInterop.VerifyFocusAsyncInvoke();
    }
}
