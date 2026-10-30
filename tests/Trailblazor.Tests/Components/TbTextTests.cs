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

    [Theory]
    [InlineData(TextStyle.BodySmall, "tb-text--bodysmall")]
    [InlineData(TextStyle.Label, "tb-text--label")]
    public void Render_ShouldAddTheStyleClass_WhenGivenASmallStep(TextStyle style, string modifier)
    {
        var cut = Render<TbText>(parameters => parameters.Add(text => text.TextStyle, style));

        Assert.Contains(modifier, cut.Find("p").ClassList);
    }

    [Fact]
    public void Render_ShouldBeMuted_WhenMutedAndGivenNoTone()
    {
        var cut = Render<TbText>(parameters => parameters.Add(text => text.Muted, true));

        Assert.Contains("tb-text--muted", cut.Find("p").ClassList);
    }

    [Fact]
    public void Render_ShouldLetTheToneWin_WhenMutedAndGivenATone()
    {
        var cut = Render<TbText>(parameters => parameters.Add(text => text.Muted, true)
            .Add(text => text.Tone, Tone.Danger));

        var paragraph = cut.Find("p");
        Assert.Contains("tb-text--danger", paragraph.ClassList);
        Assert.DoesNotContain("tb-text--muted", paragraph.ClassList);
    }

    [Fact]
    public void Render_ShouldKeepToOneLine_WhenMaxLinesIsOne()
    {
        var cut = Render<TbText>(parameters => parameters.Add(text => text.MaxLines, 1));

        var paragraph = cut.Find("p");
        Assert.Contains("tb-text--truncate", paragraph.ClassList);
        Assert.Null(paragraph.GetAttribute("style"));
    }

    [Fact]
    public void Render_ShouldClampToTheLines_WhenMaxLinesIsAboveOne()
    {
        var cut = Render<TbText>(parameters => parameters.Add(text => text.MaxLines, 3)
            .Add(text => text.Style, "color: red"));

        var paragraph = cut.Find("p");
        Assert.Contains("tb-text--clamp", paragraph.ClassList);
        Assert.Equal("--tb-text-lines: 3; color: red", paragraph.GetAttribute("style"));
    }

    [Fact]
    public void Render_ShouldThrow_WhenMaxLinesIsBelowOne()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            Render<TbText>(parameters => parameters.Add(text => text.MaxLines, 0)));
    }
}
