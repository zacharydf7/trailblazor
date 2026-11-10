using Bunit;

namespace Trailblazor.Tests.Components;

public class TbFabTests : BunitContext
{
    private static readonly Icon Pencil = new("<path d=\"M3 17v4h4L19 9l-4-4z\" />");

    public TbFabTests()
    {
        this.AddTrailblazor();
    }

    [Fact]
    public void Render_ShouldNameTheButtonByItsLabelAndShowOnlyTheIcon_WhenNotExtended()
    {
        var cut = Render<TbFab>(parameters => parameters.Add(fab => fab.Icon, Pencil)
            .Add(fab => fab.Label, "New message"));

        var button = cut.Find("button");
        Assert.Equal("New message", button.GetAttribute("aria-label"));
        Assert.NotNull(button.QuerySelector("[data-part=icon]"));
        Assert.Null(button.QuerySelector("[data-part=label]"));
        Assert.Contains("tb-fab--bottomend", button.ClassList);
    }

    [Fact]
    public void Render_ShouldShowTheLabelAsTheButtonsText_WhenExtended()
    {
        var cut = Render<TbFab>(parameters => parameters.Add(fab => fab.Icon, Pencil)
            .Add(fab => fab.Label, "New message").Add(fab => fab.Extended, true));

        var button = cut.Find("button");
        Assert.Null(button.GetAttribute("aria-label"));
        Assert.Equal("New message", button.QuerySelector("[data-part=label]")!.TextContent);
        Assert.Contains("tb-fab--extended", button.ClassList);
    }

    [Theory]
    [InlineData(FabPosition.BottomStart, "tb-fab--bottomstart")]
    [InlineData(FabPosition.BottomCenter, "tb-fab--bottomcenter")]
    [InlineData(FabPosition.TopEnd, "tb-fab--topend")]
    [InlineData(FabPosition.Inline, "tb-fab--inline")]
    public void Render_ShouldPlaceTheButton_WhenGivenAPosition(FabPosition position, string expected)
    {
        var cut = Render<TbFab>(parameters => parameters.Add(fab => fab.Icon, Pencil)
            .Add(fab => fab.Label, "New message").Add(fab => fab.Position, position));

        Assert.Contains(expected, cut.Find("button").ClassList);
    }

    [Fact]
    public void Click_ShouldRunTheAction_WhenClicked()
    {
        var clicks = 0;
        var cut = Render<TbFab>(parameters => parameters.Add(fab => fab.Icon, Pencil)
            .Add(fab => fab.Label, "New message").Add(fab => fab.OnClick, () => clicks++));

        cut.Find("button").Click();

        Assert.Equal(1, clicks);
    }

    [Fact]
    public void Render_ShouldThrow_WhenTheLabelIsBlank()
    {
        Assert.Throws<InvalidOperationException>(() => Render<TbFab>(parameters =>
            parameters.Add(fab => fab.Icon, Pencil).Add(fab => fab.Label, " ")));
    }
}
