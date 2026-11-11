using Bunit;
using Microsoft.AspNetCore.Components;
using Trailblazor.Docs.Examples;

namespace Trailblazor.Tests.Docs;

public class DialogExampleTests : DocsTest
{
    [Fact]
    public void CommandExample_ShouldRenderTheDialogClosedForTheButtonsCommand_WhenThePageIsStatic()
    {
        SetRendererInfo(new RendererInfo("Static", isInteractive: false));

        var cut = Render<DialogCommandExample>();

        var button = cut.Find("button[commandfor]");
        Assert.Equal(("returns-policy", "show-modal"), (button.GetAttribute("commandfor"), button.GetAttribute("command")));
        Assert.False(cut.Find("dialog#returns-policy").HasAttribute("open"));
    }

    [Fact]
    public void CommandExample_ShouldOpenTheDialogFromTheButtonsClick_WhenThePageIsInteractive()
    {
        var cut = Render<DialogCommandExample>();
        Assert.Empty(cut.FindAll("dialog"));
        Assert.Empty(cut.FindAll("button[commandfor]"));

        cut.Find("button").Click();

        Assert.Equal("Returns policy", cut.Find("dialog#returns-policy h2").TextContent);
    }
}
