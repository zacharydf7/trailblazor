using Bunit;
using Microsoft.Extensions.DependencyInjection;

namespace Trailblazor.Tests.Components;

public class AnnouncerTests : BunitContext
{
    public AnnouncerTests() => Services.AddTrailblazor();

    private IAnnouncer Announcer => Services.GetRequiredService<IAnnouncer>();

    [Fact]
    public void AnnouncerHost_ShouldRenderEmptyHiddenLiveRegions_WhenNothingWasAnnounced()
    {
        var host = Render<AnnouncerHost>();

        Assert.Contains("tb-visually-hidden", host.Find("[data-part=announcer]").ClassList);
        Assert.Equal("polite", host.Find("[role=status]").GetAttribute("aria-live"));
        Assert.Equal("assertive", host.Find("[role=alert]").GetAttribute("aria-live"));
        Assert.Empty(host.FindAll("p"));
    }

    [Fact]
    public void Polite_ShouldAddANewElement_WhenTheSameMessageIsAnnouncedAgain()
    {
        var host = Render<AnnouncerHost>();

        Announcer.Polite("Saved");
        Announcer.Polite("Saved");

        host.WaitForAssertion(() => Assert.Equal(["Saved", "Saved"],
            host.FindAll("[role=status] p").Select(p => p.TextContent)));
    }

    [Fact]
    public void Assertive_ShouldKeepOnlyTheLatestFiveMessages_WhenManyAreAnnounced()
    {
        var host = Render<AnnouncerHost>();

        for (var n = 1; n <= 7; n++)
        {
            Announcer.Assertive($"Error {n}");
        }

        host.WaitForAssertion(() => Assert.Equal(["Error 3", "Error 4", "Error 5", "Error 6", "Error 7"],
            host.FindAll("[role=alert] p").Select(p => p.TextContent)));
        Assert.Empty(host.FindAll("[role=status] p"));
    }

    [Fact]
    public void Announcements_ShouldGoToTheTopmostModalDialog_WhenOneIsOpen()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        var page = Render<AnnouncerHost>();
        var dialogs = Render<DialogHost>();
        var dialogService = Services.GetRequiredService<IDialogService>();
        Announcer.Polite("Before");

        _ = dialogService.ShowAsync<EmptyDialog, bool>("Edit");
        dialogs.WaitForElement("dialog [data-part=announcer]");
        Announcer.Polite("During");

        dialogs.WaitForAssertion(() => Assert.Equal(["During"],
            dialogs.FindAll("dialog [role=status] p").Select(p => p.TextContent)));
        Assert.Equal(["Before"], page.FindAll("[role=status] p").Select(p => p.TextContent));

        _ = dialogService.OpenDialogs.Single().CloseAsync(false, null);
        Announcer.Polite("After");

        page.WaitForAssertion(() => Assert.Equal(["Before", "After"],
            page.FindAll("[role=status] p").Select(p => p.TextContent)));
    }

    [Fact]
    public void Announcements_ShouldReachTheDialog_WhenMadeRightAfterOpeningIt()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        var dialogs = Render<DialogHost>();

        _ = Services.GetRequiredService<IDialogService>().ShowAsync<EmptyDialog, bool>("Edit");
        Announcer.Polite("Draft restored");

        dialogs.WaitForAssertion(() => Assert.Equal(["Draft restored"],
            dialogs.FindAll("dialog [role=status] p").Select(p => p.TextContent)));
    }

    private sealed class EmptyDialog : TbDialog<bool>
    {
    }
}
