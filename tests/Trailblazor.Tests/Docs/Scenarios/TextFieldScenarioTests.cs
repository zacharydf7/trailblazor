using Bunit;
using Trailblazor.Docs.Examples;
using Trailblazor.Testing;

namespace Trailblazor.Tests.Docs.Scenarios;

// The page of this component shows the body of this test, so it has to read well without the class around it.
public class TextFieldScenarioTests : BunitContext
{
    public TextFieldScenarioTests() => this.AddTrailblazor();

    [Fact]
    public async Task Fields_ShouldShowAndClearTheirErrors_WhenAValueIsWrongThenFixed()
    {
        var cut = Render<TextFieldValidationExample>();
        var permit = cut.Tb().TextField("Permit number");
        var handle = cut.Tb().TextField("Handle");

        // The field's own check runs once the value is entered.
        await permit.EnterAsync("2048");
        Assert.True(permit.Invalid);
        Assert.Equal("Enter six digits.", permit.Error);

        await permit.EnterAsync("204815");
        Assert.False(permit.Invalid);

        // The server's answer shows on the field until the handle changes.
        await handle.EnterAsync("robin");
        await cut.Tb().Button("Check the handle").ClickAsync();
        Assert.Equal("That handle is taken.", handle.Error);

        await handle.EnterAsync("wren");
        Assert.Null(handle.Error);
    }
}
