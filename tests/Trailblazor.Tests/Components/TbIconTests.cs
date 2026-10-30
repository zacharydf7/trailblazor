using Bunit;

namespace Trailblazor.Tests.Components;

public class TbIconTests : BunitContext
{
    private static readonly Icon Square = new("<path d=\"M4 4h16v16H4z\" />");

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
