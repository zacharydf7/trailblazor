using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Time.Testing;

namespace Trailblazor.Tests.Components;

public class AnnouncerTests : BunitContext
{
    private readonly FakeTimeProvider _time = new();

    public AnnouncerTests()
    {
        Services.AddSingleton<TimeProvider>(_time);
        this.AddTrailblazor();
    }

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
    public void Clear_ShouldRemoveEveryMessage_WhenCalled()
    {
        var host = Render<AnnouncerHost>();
        Announcer.Polite("Saved");
        Announcer.Assertive("Failed");
        host.WaitForAssertion(() => Assert.Equal(2, host.FindAll("p").Count));

        Announcer.Clear();

        host.WaitForAssertion(() => Assert.Empty(host.FindAll("p")));
    }

    [Fact]
    public void Announcer_ShouldRemoveEachMessage_WhenSevenSecondsHavePassedSinceItWasAnnounced()
    {
        var host = Render<AnnouncerHost>();
        Announcer.Polite("Saved");
        _time.Advance(TimeSpan.FromSeconds(6.9));
        Announcer.Assertive("Failed");

        _time.Advance(TimeSpan.FromSeconds(0.1));

        host.WaitForAssertion(() => Assert.Equal(["Failed"], host.FindAll("p").Select(p => p.TextContent)));
        _time.Advance(TimeSpan.FromSeconds(7));
        host.WaitForAssertion(() => Assert.Empty(host.FindAll("p")));
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
        var dialogs = Render<DialogHost>();

        _ = Services.GetRequiredService<IDialogService>().ShowAsync<EmptyDialog, bool>("Edit");
        Announcer.Polite("Draft restored");

        dialogs.WaitForAssertion(() => Assert.Equal(["Draft restored"],
            dialogs.FindAll("dialog [role=status] p").Select(p => p.TextContent)));
    }

    [Fact]
    public void Announcements_ShouldWaitForTheOpenDialogsNextFrame_WhenMadeAsItOpens()
    {
        var module = JSInterop.SetupModule("./_content/Trailblazor/dialog.js");
        module.SetupModule("openDialog", _ => true);
        var frame = module.SetupVoid("nextFrame");
        var dialogs = Render<DialogHost>();

        _ = Services.GetRequiredService<IDialogService>().ShowAsync<EmptyDialog, bool>("Edit");
        Announcer.Polite("Draft restored");
        dialogs.WaitForAssertion(() => Assert.Single(frame.Invocations));
        Assert.Empty(dialogs.FindAll("dialog [role=status] p"));
        frame.SetVoidResult();

        dialogs.WaitForAssertion(() => Assert.Equal(["Draft restored"],
            dialogs.FindAll("dialog [role=status] p").Select(p => p.TextContent)));
    }

    private sealed class EmptyDialog : TbDialog<bool>
    {
    }
}
