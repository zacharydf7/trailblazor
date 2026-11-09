using Bunit;
using Microsoft.Extensions.DependencyInjection;

namespace Trailblazor.Tests.Components;

public class TbDrawerTests : BunitContext
{
    public TbDrawerTests() => this.AddTrailblazor();

    private DialogOptions OpenOptions =>
        Assert.Single(Services.GetRequiredService<IDialogService>().OpenDialogs).Options;

    [Fact]
    public void Open_ShouldOpenAModalDialogAtTheEnd_WhenTrue()
    {
        var cut = Render<TbDrawer>(parameters => parameters.Add(drawer => drawer.Open, true)
            .Add(drawer => drawer.Title, "Filters").Add(drawer => drawer.Class, "my-drawer")
            .AddChildContent("Distance"));

        var dialog = cut.Find("dialog");

        Assert.Equal("Filters", dialog.QuerySelector("h2")!.TextContent);
        Assert.Equal(dialog.QuerySelector("h2")!.Id, dialog.GetAttribute("aria-labelledby"));
        Assert.Equal("Distance", cut.Find("[data-part=body]").TextContent.Trim());
        Assert.Contains("tb-drawer", dialog.ClassList);
        Assert.Contains("tb-drawer--end", dialog.ClassList);
        Assert.Contains("my-drawer", dialog.ClassList);
        Assert.Equal((true, DialogPosition.End, DialogSize.Small), (OpenOptions.Modal, OpenOptions.Position,
            OpenOptions.Size));
    }

    [Theory]
    [InlineData(DrawerSide.Start, DialogPosition.Start, "tb-drawer--start")]
    [InlineData(DrawerSide.Top, DialogPosition.Top, "tb-drawer--top")]
    [InlineData(DrawerSide.Bottom, DialogPosition.Bottom, "tb-drawer--bottom")]
    public void Side_ShouldDockTheDrawerToThatEdge_WhenGivenASide(DrawerSide side, DialogPosition position,
        string className)
    {
        var cut = Render<TbDrawer>(parameters => parameters.Add(drawer => drawer.Open, true)
            .Add(drawer => drawer.Title, "Filters").Add(drawer => drawer.Side, side));

        Assert.Contains(className, cut.Find("dialog").ClassList);
        Assert.Equal(position, OpenOptions.Position);
    }

    [Fact]
    public void Open_ShouldPassTheDrawersSettingsToItsDialog_WhenTheyAreSet()
    {
        Render<TbDrawer>(parameters => parameters.Add(drawer => drawer.Open, true)
            .Add(drawer => drawer.Title, "Filters").Add(drawer => drawer.Modal, false)
            .Add(drawer => drawer.Size, DialogSize.Large).Add(drawer => drawer.Width, "20rem")
            .Add(drawer => drawer.TelemetryName, "trail-filters"));

        Assert.Equal((false, DialogSize.Large, "20rem", "trail-filters"), (OpenOptions.Modal, OpenOptions.Size,
            OpenOptions.Width, OpenOptions.TelemetryName));
    }

    [Fact]
    public async Task CloseButton_ShouldCloseTheDrawerAndRaiseOpenChanged_WhenPressed()
    {
        var changes = new List<bool>();
        var cut = Render<TbDrawer>(parameters => parameters.Add(drawer => drawer.Open, true)
            .Add(drawer => drawer.Title, "Filters")
            .Add(drawer => drawer.OpenChanged, (bool open) => changes.Add(open)));

        await cut.Find("[data-part=close]").ClickAsync(new());

        cut.WaitForAssertion(() => Assert.Empty(cut.FindAll("dialog")));
        Assert.Equal([false], changes);
    }

    [Fact]
    public async Task CloseButton_ShouldKeepTheDrawerOpen_WhenCanDismissRefuses()
    {
        var asked = new List<DialogDismissReason>();
        var cut = Render<TbDrawer>(parameters => parameters.Add(drawer => drawer.Open, true)
            .Add(drawer => drawer.Title, "Filters").Add(drawer => drawer.CanDismiss, reason =>
            {
                asked.Add(reason);
                return Task.FromResult(false);
            }));

        await cut.Find("[data-part=close]").ClickAsync(new());

        Assert.Equal([DialogDismissReason.CloseButton], asked);
        Assert.Single(cut.FindAll("dialog"));
    }
}
