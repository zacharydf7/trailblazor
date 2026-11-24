using Bunit;
using Trailblazor.Docs.Examples;
using Trailblazor.Testing;

namespace Trailblazor.Tests.Docs.Scenarios;

// The page of this component shows the body of this test, so it has to read well without the class around it.
public class TagInputScenarioTests : BunitContext
{
    public TagInputScenarioTests() => this.AddTrailblazor();

    [Fact]
    public async Task TripReport_ShouldBeSentOnlyWithAnAnimal_WhenAnimalsAreAddedAndRemoved()
    {
        var cut = Render<TagInputFormExample>();
        var seen = cut.Tb().TagInput("Wildlife seen");
        var send = cut.Tb().Button("Send report");
        var sent = cut.Find("output");

        // A report with no animals isn't sent, and the field says why.
        await send.ClickAsync();
        Assert.Equal("Add at least one animal.", seen.Error);
        Assert.Equal("", sent.TextContent);

        // AddAsync adds the text as pressing Enter does.
        await seen.AddAsync("Marmot");
        await seen.AddAsync("Deer");
        await seen.AddAsync("Heron");
        await seen.RemoveAsync("Deer");
        Assert.Equal(["Marmot", "Heron"], seen.Tags);

        await send.ClickAsync();
        Assert.Null(seen.Error);
        Assert.Equal("Sent: Marmot, Heron.", sent.TextContent);
    }
}
