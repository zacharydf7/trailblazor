using Bunit;
using Trailblazor.Docs.Examples;
using Trailblazor.Testing;

namespace Trailblazor.Tests.Docs.Scenarios;

// The page of this component shows the body of this test, so it has to read well without the class around it.
public class FormScenarioTests : BunitContext
{
    public FormScenarioTests() => this.AddTrailblazor();

    [Fact]
    public async Task SignUp_ShouldListEveryErrorThenSave_WhenSubmittedEmptyAndThenFilledIn()
    {
        var cut = Render<FormExample>();
        var form = cut.Tb().Form("Trip sign-up");
        var signUp = cut.Tb().Button("Sign up");
        var status = cut.Find("output");

        // Clicking Sign up waits until the form has handled the submit, so each field shows its message, in the
        // fields' order.
        await signUp.ClickAsync();
        Assert.Equal(["Enter the leader's name.", "Enter how many are going.", "Read the safety notes first."],
            form.Errors);
        Assert.Equal("3 to fix.", status.TextContent);

        // Each field is found by its label and filled in with the helper from its own page.
        await cut.Tb().TextField("Leader's name").EnterAsync("Ada");
        await cut.Tb().NumberField("Party size").EnterAsync("4");
        await cut.Tb().Checkbox("I've read the safety notes").CheckAsync();
        Assert.Empty(form.Errors);
        Assert.Equal("Ready to sign up.", status.TextContent);

        await signUp.ClickAsync();
        Assert.Equal("Signed up Ada's party of 4.", status.TextContent);
    }
}
