using Bunit;

namespace Trailblazor.Tests.Components;

public class TbTextTests : BunitContext
{
    [Fact]
    public void Render_ShouldRenderABodyParagraph_WhenNoParametersAreSet()
    {
        var cut = Render<TbText>(parameters => parameters.AddChildContent("Hello"));

        var paragraph = cut.Find("p");
        Assert.Equal("Hello", paragraph.TextContent);
        Assert.Contains("tb-text--body", paragraph.ClassList);
    }

    [Fact]
    public void Render_ShouldKeepTheElementAndTheLookSeparate_WhenBothAreSet()
    {
        var cut = Render<TbText>(parameters => parameters
            .Add(text => text.TextStyle, TextStyle.Caption)
            .Add(text => text.As, TextElement.H2)
            .AddChildContent("Section"));

        var heading = cut.Find("h2");
        Assert.Contains("tb-text--caption", heading.ClassList);
    }

    [Fact]
    public void Render_ShouldAddTheToneClass_WhenToneIsSet()
    {
        var cut = Render<TbText>(parameters => parameters.Add(text => text.Tone, Tone.Danger));

        Assert.Contains("tb-text--danger", cut.Find("p").ClassList);
    }

    [Fact]
    public void Render_ShouldAddNoToneClass_WhenToneIsNotSet()
    {
        var cut = Render<TbText>();

        Assert.Equal(["tb-text", "tb-text--body"], cut.Find("p").ClassList);
    }
}
