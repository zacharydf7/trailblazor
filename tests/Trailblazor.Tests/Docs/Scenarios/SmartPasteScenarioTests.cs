using Bunit;
using Trailblazor.Docs.Examples;
using Trailblazor.Testing;

namespace Trailblazor.Tests.Docs.Scenarios;

// The page of this component shows the body of this test, so it has to read well without the class around it.
public class SmartPasteScenarioTests : BunitContext
{
    public SmartPasteScenarioTests() => this.AddTrailblazor();

    [Fact]
    public async Task TripLeaderForm_ShouldFillItsFields_WhenTheLeadersDetailsArePasted()
    {
        var cut = Render<SmartPasteExample>();
        var paste = cut.Tb().SmartPaste();

        // PasteAsync presses the button, enters the text in the box that opens, and fills the form from it.
        await paste.PasteAsync("Robin Vale, robin@ridge.example, 555-0100, a party of 4, steady pace");

        Assert.Equal("Filled 5 fields", paste.Status);
        Assert.Equal("Robin Vale", cut.Tb().TextField("Leader's name").Text);
        Assert.Equal("robin@ridge.example", cut.Tb().TextField("Email").Text);
        Assert.Equal("555-0100", cut.Tb().TextField("Phone").Text);
        Assert.Equal("4", cut.Tb().NumberField("Party size").Text);
        Assert.Equal("Steady", cut.Tb().RadioGroup("Pace").Chosen);
    }
}
