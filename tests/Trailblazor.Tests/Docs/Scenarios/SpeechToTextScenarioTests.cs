using Bunit;
using Trailblazor.Docs.Examples;
using Trailblazor.Testing;

namespace Trailblazor.Tests.Docs.Scenarios;

// The page of this component shows the body of this test, so it has to read well without the class around it.
public class SpeechToTextScenarioTests : BunitContext
{
    public SpeechToTextScenarioTests() => this.AddTrailblazor();

    [Fact]
    public async Task TrailNotes_ShouldTakeWhatIsSaid_WhenDictated()
    {
        var cut = Render<SpeechToTextExample>();
        var dictate = cut.Tb().SpeechToText("Dictate notes");

        // StartAsync presses the button, and SayAsync hands the app a phrase as the browser does on hearing one.
        await dictate.StartAsync();
        Assert.True(dictate.IsListening);
        await dictate.SayAsync("Bridge out at mile three.");

        Assert.Equal("Bridge out at mile three.", cut.Tb().TextField("Trail notes").Text);
    }
}
