using Bunit;
using Trailblazor.Docs.Examples;
using Trailblazor.Testing;

namespace Trailblazor.Tests.Docs.Scenarios;

// The page of this component shows the body of this test, so it has to read well without the class around it.
public class RichTextEditorScenarioTests : BunitContext
{
    public RichTextEditorScenarioTests() => this.AddTrailblazor();

    [Fact]
    public async Task CrewUpdate_ShouldTellTheAppWhomItMentions_WhenAPersonIsMentioned()
    {
        var cut = Render<RichTextEditorMentionsExample>();
        var update = cut.Tb().RichTextEditor("Crew update");

        // MentionAsync replaces the editor's text with this text, which ends with the trigger and the start of a name,
        // then chooses a suggestion.
        await update.MentionAsync("Thanks @da", "Dan Ng");

        Assert.Equal("Mentioned: @Dan Ng (u2)", cut.Find("output").TextContent);
    }
}
