using Bunit;
using Trailblazor.Testing;

namespace Trailblazor.Tests.Components;

public class TbTextTests : BunitContext
{
    private const string Pine = "Pine Hollow loop climbs through old cedar groves";

    public TbTextTests() => this.AddTrailblazor();

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

    [Theory]
    [InlineData("Pine Hollow loop", 16, null)]
    [InlineData("Pine Hollow loop   ", 16, null)]
    [InlineData("Pine Hollow loop", 13, "Pine Hollow…")]
    [InlineData("Pine Hollow loop", 12, "Pine Hollow…")]
    [InlineData("Pine Hollow loop", 11, "Pine Hollow…")]
    [InlineData("Big Cedar Flats trail", 15, "Big Cedar Flats…")]
    [InlineData("Permit TRL-2026-004173-N", 14, "Permit TRL-202…")]
    [InlineData("Lake 🏔️🏔️🏔️", 6, "Lake 🏔️…")]
    public void Shortened_ShouldCutAtASpaceOrElseInsideAWord_WhenGivenText(string text, int most, string? expected)
    {
        Assert.Equal(expected, TbText.Shortened(text, most));
    }

    [Fact]
    public void Render_ShouldShowTheShortenedTextAndHideItFromScreenReaders_WhenMaxLengthCutsIt()
    {
        var cut = Render<TbText>(parameters => parameters.Add(text => text.Text, Pine).Add(text => text.MaxLength, 20));

        var text = cut.Find("p");
        Assert.True(text.HasAttribute("data-cut"));
        Assert.Equal(Pine, text.QuerySelector(".tb-visually-hidden[data-part=full-text]")!.TextContent);
        Assert.Equal("Pine Hollow loop…", text.QuerySelector("[aria-hidden=true]")!.TextContent);
        // Without ShowFullText, it is no tab stop and has no tooltip.
        Assert.Null(text.GetAttribute("tabindex"));
        Assert.Empty(text.QuerySelectorAll("[role=tooltip]"));
    }

    [Fact]
    public void Render_ShouldShowTheWholeTextInATooltipAndBeATabStop_WhenMaxLengthCutsTextWithShowFullText()
    {
        var cut = Render<TbText>(parameters => parameters.Add(text => text.Text, Pine).Add(text => text.MaxLength, 20)
            .Add(text => text.ShowFullText, true).Add(text => text.FullTextPlacement, Placement.Bottom));

        var text = cut.Find("p");
        var tooltip = text.QuerySelector(":scope > [role=tooltip]")!;
        Assert.Equal(("0", Pine, "true", "Bottom"), (text.GetAttribute("tabindex"), tooltip.TextContent,
            tooltip.GetAttribute("aria-hidden"), tooltip.GetAttribute("data-placement")));
        Assert.Contains("tb-text--full-text", text.ClassList);
        Assert.True(cut.Tb().Text(Pine).IsCut);
    }

    [Fact]
    public void Render_ShouldNeitherCutNorShowATooltip_WhenTheTextFitsMaxLength()
    {
        var cut = Render<TbText>(parameters => parameters.Add(text => text.Text, "Cedar Flats")
            .Add(text => text.MaxLength, 20).Add(text => text.ShowFullText, true));

        var text = cut.Find("p");
        Assert.Equal((false, null, "Cedar Flats"), (text.HasAttribute("data-cut"), text.GetAttribute("tabindex"),
            text.TextContent));
        Assert.False(cut.Tb().Text("Cedar Flats").IsCut);
    }

    [Fact]
    public async Task SetCutAsync_ShouldBringAndTakeTheTooltipAndTheTabStop_WhenMaxLinesTextIsMeasured()
    {
        var cut = Render<TbText>(parameters => parameters.Add(text => text.MaxLines, 1)
            .Add(text => text.ShowFullText, true).AddChildContent(Pine));
        var text = cut.Tb().Text(Pine);
        Assert.Equal((false, false), (text.IsCut, cut.Find("p").HasAttribute("data-ready")));

        await text.SetCutAsync(true);

        Assert.Equal((true, Pine), (text.IsCut, text.FullText));
        Assert.Equal(("0", ""), (cut.Find("p").GetAttribute("tabindex"), cut.Find("p").GetAttribute("data-ready")));
        Assert.Equal(Pine, cut.Find("[role=tooltip]").TextContent);
        await text.SetCutAsync(false);
        Assert.False(text.IsCut);
        Assert.Null(cut.Find("p").GetAttribute("tabindex"));
        Assert.Empty(cut.FindAll("[role=tooltip]"));
    }

    [Fact]
    public void Render_ShouldThrow_WhenTheCutOffParametersDontFit()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => Render<TbText>(parameters => parameters
            .Add(text => text.Text, Pine).Add(text => text.MaxLength, 0)));
        Assert.Contains("MaxLength needs Text", Assert.Throws<InvalidOperationException>(() =>
            Render<TbText>(parameters => parameters.Add(text => text.MaxLength, 5).AddChildContent(Pine))).Message);
        Assert.Contains("not both", Assert.Throws<InvalidOperationException>(() => Render<TbText>(parameters =>
            parameters.Add(text => text.Text, Pine).AddChildContent(Pine))).Message);
        Assert.Contains("ShowFullText needs MaxLines or MaxLength", Assert.Throws<InvalidOperationException>(() =>
            Render<TbText>(parameters => parameters.Add(text => text.ShowFullText, true))).Message);
    }
}
