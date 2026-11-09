using Bunit;

namespace Trailblazor.Tests.Components;

public class TbIconTests : BunitContext
{
    private static readonly Icon Square = new("<path d=\"M4 4h16v16H4z\" />");

    [Fact]
    public void FromSvg_ShouldDrawOutlinesWithTheSetsStrokeSettings_WhenGivenAStrokeIcon()
    {
        var icon = Icon.FromSvg("""
            <svg xmlns="http://www.w3.org/2000/svg" width="24" height="24" viewBox="0 0 24 24" fill="none"
                 stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
              <path d="M5 12h14" /><path d="m12 5 7 7-7 7" />
            </svg>
            """);

        var cut = Render<TbIcon>(parameters => parameters.Add(tbIcon => tbIcon.Icon, icon));

        var group = cut.Find("svg > g");
        Assert.Equal(("none", "currentColor", "2", "round"), (group.GetAttribute("fill"), group.GetAttribute("stroke"),
            group.GetAttribute("stroke-width"), group.GetAttribute("stroke-linecap")));
        Assert.Equal(2, group.QuerySelectorAll("path").Length);
        Assert.Equal("0 0 24 24", icon.ViewBox);
        Assert.DoesNotContain("xmlns", icon.Svg, StringComparison.Ordinal);
    }

    [Fact]
    public void FromSvg_ShouldKeepTheMarkupAndViewBox_WhenGivenAFilledIconOnItsOwnGrid()
    {
        var icon = Icon.FromSvg("<svg viewBox=\"0 0 16 16\"><circle cx=\"8\" cy=\"8\" r=\"5\"/></svg>");

        Assert.Equal(("0 0 16 16", "<circle cx=\"8\" cy=\"8\" r=\"5\" />"), (icon.ViewBox, icon.Svg));
    }

    [Fact]
    public void FromSvg_ShouldKeepALinkAsAPlainHref_WhenTheIconUsesXlink()
    {
        var icon = Icon.FromSvg("""
            <svg xmlns="http://www.w3.org/2000/svg" xmlns:xlink="http://www.w3.org/1999/xlink" viewBox="0 0 24 24">
              <defs><path id="bar" d="M4 4h16" /></defs><use xlink:href="#bar" />
            </svg>
            """);

        var cut = Render<TbIcon>(parameters => parameters.Add(tbIcon => tbIcon.Icon, icon));

        Assert.Equal("#bar", cut.Find("use").GetAttribute("href"));
        Assert.DoesNotContain("xlink", icon.Svg, StringComparison.Ordinal);
        Assert.DoesNotContain("xmlns", icon.Svg, StringComparison.Ordinal);
    }

    [Fact]
    public void FromSvg_ShouldDropAnEditorsOwnMarkup_WhenTheIconWasExportedFromOne()
    {
        var icon = Icon.FromSvg("""
            <svg xmlns="http://www.w3.org/2000/svg" xmlns:inkscape="http://www.inkscape.org/namespaces/inkscape"
                 viewBox="0 0 24 24">
              <inkscape:grid spacing="1" /><path inkscape:label="bar" d="M4 4h16" />
            </svg>
            """);

        Assert.Equal("<path d=\"M4 4h16\" />", icon.Svg.Trim());
    }

    [Theory]
    [InlineData("<svg width=\"16px\" height=\"16px\"><rect width=\"4\" height=\"4\"/></svg>", "0 0 16 16")]
    [InlineData("<svg width=\"20\" height=\"18\"><rect width=\"4\" height=\"4\"/></svg>", "0 0 20 18")]
    [InlineData("<svg><rect width=\"4\" height=\"4\"/></svg>", "0 0 24 24")]
    public void FromSvg_ShouldUseTheSizeOrTheUsualGrid_WhenThereIsNoViewBox(string svg, string viewBox) =>
        Assert.Equal(viewBox, Icon.FromSvg(svg).ViewBox);

    [Theory]
    [InlineData("<svg><path d=\"M4 4\"></svg>", "The icon isn't well-formed SVG.")]
    [InlineData("<g><path d=\"M4 4\"/></g>", "The icon is a <g>, not an <svg>.")]
    public void FromSvg_ShouldThrow_WhenGivenSomethingOtherThanOneSvgElement(string svg, string message)
    {
        var error = Assert.Throws<ArgumentException>(() => Icon.FromSvg(svg));

        Assert.StartsWith(message, error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Render_ShouldHideTheIconFromAssistiveTechnology_WhenItHasNoLabel()
    {
        var cut = Render<TbIcon>(parameters => parameters.Add(icon => icon.Icon, Square));

        var svg = cut.Find("svg");
        Assert.Equal("true", svg.GetAttribute("aria-hidden"));
        Assert.False(svg.HasAttribute("role"));
        Assert.NotNull(svg.QuerySelector("path"));
    }

    [Fact]
    public void Render_ShouldExposeTheIconAsAnImage_WhenItHasALabel()
    {
        var cut = Render<TbIcon>(parameters => parameters
            .Add(icon => icon.Icon, Square)
            .Add(icon => icon.Label, "Warning"));

        var svg = cut.Find("svg");
        Assert.Equal("img", svg.GetAttribute("role"));
        Assert.Equal("Warning", svg.GetAttribute("aria-label"));
        Assert.False(svg.HasAttribute("aria-hidden"));
    }

    [Theory]
    [InlineData(Tone.Danger, "tb-icon--danger")]
    [InlineData(Tone.Success, "tb-icon--success")]
    public void Render_ShouldColorTheIconByItsTone_WhenGivenATone(Tone tone, string modifier)
    {
        var cut = Render<TbIcon>(parameters => parameters.Add(icon => icon.Icon, Square).Add(icon => icon.Tone, tone));

        Assert.Contains(modifier, cut.Find("svg").ClassList);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Render_ShouldMarkTheIconToTurnAroundRightToLeft_WhenTheIconSaysSo(bool mirrors)
    {
        var cut = Render<TbIcon>(parameters => parameters
            .Add(icon => icon.Icon, Square with { MirrorsInRightToLeft = mirrors }));

        Assert.Equal(mirrors, cut.Find("svg").ClassList.Contains("tb-icon--mirrored"));
    }

    [Fact]
    public void Render_ShouldTakeNoToneClass_WhenNoToneIsSet()
    {
        var cut = Render<TbIcon>(parameters => parameters.Add(icon => icon.Icon, Square));

        Assert.Equal(["tb-icon", "tb-icon--medium"], cut.Find("svg").ClassList);
    }

    [Fact]
    public void Render_ShouldUseTheIconsViewBox_WhenCalled()
    {
        var circle = new Icon("<circle r=\"8\" />", "0 0 16 16");

        var cut = Render<TbIcon>(parameters => parameters.Add(icon => icon.Icon, circle));

        Assert.Equal("0 0 16 16", cut.Find("svg").GetAttribute("viewBox"));
    }
}
