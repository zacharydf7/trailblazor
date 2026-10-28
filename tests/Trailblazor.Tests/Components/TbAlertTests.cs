using Bunit;

namespace Trailblazor.Tests.Components;

public class TbAlertTests : BunitContext
{
    [Fact]
    public void Render_ShouldBeAPoliteInfoAlertWithoutAClose_WhenNoParametersAreSet()
    {
        var cut = Render<TbAlert>(parameters => parameters.AddChildContent("Saved"));

        var alert = cut.Find(".tb-alert");
        Assert.Equal("status", alert.GetAttribute("role"));
        Assert.Contains("tb-alert--info", alert.ClassList);
        Assert.Equal("Saved", cut.Find("[data-part=message]").TextContent);
        Assert.Empty(cut.FindAll("button"));
    }

    [Theory]
    [InlineData(Tone.Danger)]
    [InlineData(Tone.Warning)]
    public void Render_ShouldUseTheAlertRole_WhenGivenAnUrgentTone(Tone tone)
    {
        var cut = Render<TbAlert>(parameters => parameters.Add(alert => alert.Tone, tone));

        Assert.Equal("alert", cut.Find(".tb-alert").GetAttribute("role"));
    }

    [Fact]
    public void Render_ShouldKeepTheConsumerRole_WhenRoleIsPassed()
    {
        var cut = Render<TbAlert>(parameters => parameters
            .Add(alert => alert.Tone, Tone.Danger)
            .AddUnmatched("role", "note"));

        Assert.Equal("note", cut.Find(".tb-alert").GetAttribute("role"));
    }

    [Fact]
    public void Click_ShouldRaiseOnClose_WhenTheCloseButtonIsClicked()
    {
        var closed = false;
        var cut = Render<TbAlert>(parameters => parameters
            .Add(alert => alert.OnClose, () => closed = true)
            .Add(alert => alert.CloseLabel, "Dismiss"));

        var close = cut.Find("[data-part=close]");
        Assert.Equal("Dismiss", close.GetAttribute("aria-label"));
        close.Click();

        Assert.True(closed);
    }

    [Fact]
    public void Render_ShouldAddPartClasses_WhenIconDenseAndClassesAreSet()
    {
        var cut = Render<TbAlert>(parameters => parameters
            .Add(alert => alert.Icon, new Icon("<path d=\"M4 4h16v16H4z\" />"))
            .Add(alert => alert.Dense, true)
            .Add(alert => alert.OnClose, () => { })
            .Add(alert => alert.Classes, new AlertClasses { Icon = "my-icon", Message = "my-message", Close = "my-close" }));

        Assert.Contains("tb-alert--dense", cut.Find(".tb-alert").ClassList);
        Assert.Contains("my-icon", cut.Find("[data-part=icon]").ClassList);
        Assert.Contains("my-message", cut.Find("[data-part=message]").ClassList);
        Assert.Contains("my-close", cut.Find("[data-part=close]").ClassList);
    }
}
