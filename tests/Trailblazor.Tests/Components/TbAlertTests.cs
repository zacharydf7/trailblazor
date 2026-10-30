using System.Text.RegularExpressions;
using Bunit;

namespace Trailblazor.Tests.Components;

public class TbAlertTests : BunitContext
{
    public TbAlertTests() => this.AddTrailblazor();

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
            .Add(alert => alert.Title, "Heads up").Add(alert => alert.Actions, "<button>Undo</button>")
            .Add(alert => alert.Classes, new AlertClasses
            {
                Icon = "my-icon", Title = "my-title", Message = "my-message", Actions = "my-actions",
                Close = "my-close",
            }));

        Assert.Contains("tb-alert--dense", cut.Find(".tb-alert").ClassList);
        Assert.Contains("my-icon", cut.Find("[data-part=icon]").ClassList);
        Assert.Equal("M4 4h16v16H4z", cut.Find("[data-part=icon] path").GetAttribute("d"));
        Assert.Contains("my-title", cut.Find("[data-part=title]").ClassList);
        Assert.Contains("my-message", cut.Find("[data-part=message]").ClassList);
        Assert.Contains("my-actions", cut.Find("[data-part=actions]").ClassList);
        Assert.Contains("my-close", cut.Find("[data-part=close]").ClassList);
    }

    public static TheoryData<Tone, Icon> ToneIcons => new()
    {
        { Tone.Info, BuiltInIcons.Info }, { Tone.Primary, BuiltInIcons.Info },
        { Tone.Neutral, BuiltInIcons.Info }, { Tone.Success, BuiltInIcons.Success },
        { Tone.Warning, BuiltInIcons.Warning }, { Tone.Danger, BuiltInIcons.Error },
    };

    [Theory, MemberData(nameof(ToneIcons))]
    public void Render_ShouldShowTheTonesIcon_WhenGivenNoIcon(Tone tone, Icon icon)
    {
        var cut = Render<TbAlert>(parameters => parameters.Add(alert => alert.Tone, tone));

        Assert.Equal(Regex.Match(icon.Svg, "d=\"([^\"]+)\"").Groups[1].Value,
            cut.Find("[data-part=icon] path").GetAttribute("d"));
    }

    [Fact]
    public void Render_ShouldShowNoIcon_WhenShowIconIsOff()
    {
        var cut = Render<TbAlert>(parameters => parameters.Add(alert => alert.ShowIcon, false));

        Assert.Empty(cut.FindAll("[data-part=icon]"));
    }

    [Fact]
    public void Render_ShouldShowTheTitleMessageAndActionsInOrder_WhenAllAreGiven()
    {
        var cut = Render<TbAlert>(parameters => parameters.Add(alert => alert.Title, "Couldn't sync")
            .AddChildContent("Saved on this device.")
            .Add(alert => alert.Actions, "<button>Try again</button>"));

        var parts = cut.FindAll("[data-part=title], [data-part=message], [data-part=actions]");
        Assert.Equal(["title", "message", "actions"], parts.Select(part => part.GetAttribute("data-part")));
        Assert.Equal("Couldn't sync", parts[0].TextContent);
    }

    [Theory]
    [InlineData(AlertVariant.Soft, "tb-alert--soft")]
    [InlineData(AlertVariant.Filled, "tb-alert--filled")]
    [InlineData(AlertVariant.Outlined, "tb-alert--outlined")]
    public void Render_ShouldMarkTheVariant_WhenGivenOne(AlertVariant variant, string modifier)
    {
        var cut = Render<TbAlert>(parameters => parameters.Add(alert => alert.Variant, variant));

        Assert.Contains(modifier, cut.Find("[role=status]").ClassList);
    }
}
