using Bunit;

namespace Trailblazor.Tests.Components;

public class TbIconButtonTests : BunitContext
{
    private static readonly Icon Square = new("<path d=\"M4 4h16v16H4z\" />");

    [Fact]
    public void Render_ShouldNameTheButtonAndHideTheIcon_WhenCalled()
    {
        var cut = Render<TbIconButton>(parameters => parameters
            .Add(button => button.Icon, Square)
            .Add(button => button.AriaLabel, "Delete"));

        var button = cut.Find("button");
        Assert.Equal("Delete", button.GetAttribute("aria-label"));
        Assert.Equal("true", button.QuerySelector("svg")!.GetAttribute("aria-hidden"));
    }

    [Fact]
    public void Render_ShouldDefaultToANeutralTextButton_WhenNoVariantOrToneIsSet()
    {
        var cut = Render<TbIconButton>(parameters => parameters
            .Add(button => button.Icon, Square)
            .Add(button => button.AriaLabel, "Delete"));

        var button = cut.Find("button");
        Assert.Contains("tb-button--text", button.ClassList);
        Assert.Contains("tb-button--neutral", button.ClassList);
    }

    [Fact]
    public void Render_ShouldShowTheSpinnerInsteadOfTheIcon_WhenLoading()
    {
        var cut = Render<TbIconButton>(parameters => parameters
            .Add(button => button.Icon, Square)
            .Add(button => button.AriaLabel, "Delete")
            .Add(button => button.Loading, true));

        Assert.NotNull(cut.Find("[data-part=spinner]"));
        Assert.Empty(cut.FindAll("svg"));
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public void Render_ShouldThrow_WhenGivenABlankAriaLabel(string ariaLabel)
    {
        Assert.Throws<InvalidOperationException>(() => Render<TbIconButton>(parameters => parameters
            .Add(button => button.Icon, Square)
            .Add(button => button.AriaLabel, ariaLabel)));
    }
}
