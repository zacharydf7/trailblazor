using Bunit;
using Microsoft.AspNetCore.Components;

namespace Trailblazor.Tests.Components;

public class TbLabelTests : BunitContext
{
    public TbLabelTests()
    {
        this.AddTrailblazor();
        SetRendererInfo(new RendererInfo("Server", isInteractive: true));
    }

    [Fact]
    public void Render_ShouldBeALabelForTheControl_WhenForIsSet()
    {
        var cut = Render<TbLabel>(parameters => parameters.Add(label => label.Text, "Project code")
            .Add(label => label.For, "project-code").Add(label => label.Required, true)
            .Add(label => label.TextId, "project-code-label"));

        var text = cut.Find("[data-part=text]");
        Assert.Equal(("LABEL", "project-code", "project-code-label"),
            (text.TagName, text.GetAttribute("for"), text.Id));
        Assert.Equal("true", cut.Find(".tb-label__required").GetAttribute("aria-hidden"));
        Assert.Empty(cut.FindAll("[data-part=info]"));
    }

    [Fact]
    public void Render_ShouldBePlainText_WhenThereIsNoFor()
    {
        var cut = Render<TbLabel>(parameters => parameters.Add(label => label.Text, "Delivery speed"));

        Assert.Equal("SPAN", cut.Find("[data-part=text]").TagName);
        Assert.Empty(cut.FindAll("label"));
    }

    [Fact]
    public void Render_ShouldNameTheInfoButtonAfterTheLabelAndKeepItsLines_WhenInfoIsSet()
    {
        var cut = Render<TbLabel>(parameters => parameters.Add(label => label.Text, "Project code")
            .Add(label => label.Info, "First line.\nSecond line."));

        Assert.Equal("Information about Project code", cut.Find("[data-part=trigger]").GetAttribute("aria-label"));
        Assert.Equal("First line.\nSecond line.", cut.Find(".tb-label__info-text").TextContent.Trim());
    }

    [Fact]
    public void Render_ShouldShowChildContentButNameTheButtonFromText_WhenBothAreSet()
    {
        var cut = Render<TbLabel>(parameters => parameters.Add(label => label.Text, "Delivery speed")
            .Add(label => label.Info, "Overnight costs more.")
            .AddChildContent("<em>Delivery</em> speed"));

        Assert.Equal("Delivery", cut.Find("[data-part=text] em").TextContent);
        Assert.Equal("Information about Delivery speed", cut.Find("[data-part=trigger]").GetAttribute("aria-label"));
    }

    [Fact]
    public void Render_ShouldThrow_WhenAnInfoButtonHasNoTextToNameIt()
    {
        var exception = Assert.Throws<InvalidOperationException>(() => Render<TbLabel>(parameters => parameters
            .Add(label => label.Info, "Overnight costs more.").AddChildContent("Delivery speed")));

        Assert.Contains(nameof(TbLabel.Text), exception.Message);
    }

    [Fact]
    public void Render_ShouldShowRichInfo_WhenInfoContentIsSet()
    {
        var cut = Render<TbLabel>(parameters => parameters.Add(label => label.Text, "Delivery speed")
            .Add(label => label.InfoContent,
                (RenderFragment)(info => info.AddMarkupContent(0, "<a href=\"help\">Help</a>")))
            .Add(label => label.InfoButtonLabel, "More about"));

        Assert.Equal("More about Delivery speed", cut.Find("[data-part=trigger]").GetAttribute("aria-label"));
        Assert.DoesNotContain("tb-label__info-text--lines", cut.Find(".tb-label__info-text").ClassList);
        Assert.Equal("help", cut.Find(".tb-label__info-text a").GetAttribute("href"));
    }
}
