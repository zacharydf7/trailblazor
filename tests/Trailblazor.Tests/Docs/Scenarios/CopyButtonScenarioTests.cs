using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Trailblazor.Docs.Examples;
using Trailblazor.Testing;

namespace Trailblazor.Tests.Docs.Scenarios;

// The page of this component shows the body of this test, so it has to read well without the class around it.
public class CopyButtonScenarioTests : BunitContext
{
    public CopyButtonScenarioTests() => this.AddTrailblazor();

    [Fact]
    public async Task InstallCommand_ShouldReachTheClipboard_WhenCopied()
    {
        var cut = Render<CopyButtonExample>();
        var browser = Services.GetRequiredService<FakeBrowserService>();

        await cut.Tb().CopyButton().CopyAsync();

        Assert.Equal(["dotnet add package Trailblazor"], browser.Copied);
        Assert.Equal("Copied", cut.Find("button").TextContent.Trim());
    }
}
