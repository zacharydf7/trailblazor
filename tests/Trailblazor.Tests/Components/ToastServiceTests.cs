using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;

namespace Trailblazor.Tests.Components;

public class ToastServiceTests : BunitContext
{
    private readonly FakeTimeProvider _time = new();

    public ToastServiceTests()
    {
        Services.AddSingleton<TimeProvider>(_time);
        Services.AddTrailblazor();
        JSInterop.Mode = JSRuntimeMode.Loose;
        SetRendererInfo(new RendererInfo("Server", isInteractive: true));
    }

    private IToastService Toasts => Services.GetRequiredService<IToastService>();

    [Fact]
    public void Show_ShouldRenderTheMessageInTheToasterAndAnnounceIt_WhenCalled()
    {
        var host = Render<ToastHost>();
        var announcer = Render<AnnouncerHost>();

        Toasts.Show("Saved", Tone.Success);

        var toast = host.WaitForElement("[data-part=toast]");
        Assert.Equal("Saved", toast.QuerySelector("[data-part=message]")!.TextContent);
        Assert.Contains("tb-toast--success", toast.ClassList);
        Assert.Equal("Dismiss", toast.QuerySelector("[data-part=close]")!.GetAttribute("aria-label"));
        Assert.Equal("Notifications", host.Find("[data-part=toaster]").GetAttribute("aria-label"));
        Assert.Equal("Saved", announcer.Find("[role=status] p").TextContent);
        Assert.Empty(announcer.FindAll("[role=alert] p"));
    }

    [Fact]
    public void Toast_ShouldClose_WhenItsDurationRunsOut()
    {
        var host = Render<ToastHost>();
        Toasts.Show("Saved");
        host.WaitForElement("[data-part=toast]");

        _time.Advance(TimeSpan.FromSeconds(4.9));
        Assert.Single(host.FindAll("[data-part=toast]"));

        _time.Advance(TimeSpan.FromSeconds(0.2));
        host.WaitForAssertion(() => Assert.Empty(host.FindAll("[data-part=toast]")));
        Assert.Empty(Toasts.Toasts);
    }

    [Fact]
    public void Toast_ShouldStayUntilDismissedAndBeAnnouncedAssertively_WhenGivenAnError()
    {
        var host = Render<ToastHost>();
        var announcer = Render<AnnouncerHost>();

        var toast = Toasts.Show("Upload failed", Tone.Danger);
        _time.Advance(TimeSpan.FromMinutes(10));

        Assert.True(toast.IsOpen);
        Assert.Equal("Upload failed", announcer.Find("[role=alert] p").TextContent);
        host.Find("[data-part=close]").Click();
        Assert.False(toast.IsOpen);
        Assert.Empty(host.FindAll("[data-part=toast]"));
    }

    [Fact]
    public void Toast_ShouldKeepItsRemainingTime_WhenThePointerOrFocusRestsOnIt()
    {
        var host = Render<ToastHost>();
        var toast = Toasts.Show("Saved");
        _time.Advance(TimeSpan.FromSeconds(3));

        host.Find("[data-part=toast]").MouseEnter();
        host.Find("[data-part=toast]").FocusIn();
        _time.Advance(TimeSpan.FromMinutes(1));
        host.Find("[data-part=toast]").MouseLeave();
        _time.Advance(TimeSpan.FromMinutes(1));
        Assert.True(toast.IsOpen);

        host.Find("[data-part=toast]").FocusOut();
        _time.Advance(TimeSpan.FromSeconds(1.9));
        Assert.True(toast.IsOpen);
        _time.Advance(TimeSpan.FromSeconds(0.2));
        host.WaitForAssertion(() => Assert.False(toast.IsOpen));
    }

    [Fact]
    public void Show_ShouldUpdateTheOpenToastAndRestartItsTimer_WhenGivenTheKeyOfOne()
    {
        var host = Render<ToastHost>();
        var first = Toasts.Show("1 file uploaded", Tone.Success, new() { Key = "upload" });
        _time.Advance(TimeSpan.FromSeconds(4));

        var second = Toasts.Show("2 files uploaded", Tone.Success, new() { Key = "upload" });

        Assert.Same(first, second);
        host.WaitForAssertion(() => Assert.Equal("2 files uploaded", host.Find("[data-part=message]").TextContent));
        _time.Advance(TimeSpan.FromSeconds(4));
        Assert.True(first.IsOpen);
        Assert.Single(Toasts.Toasts);
    }

    [Fact]
    public void Update_ShouldReplaceTheContentAndStartTheNewTimer_WhenAStayOpenToastFinishes()
    {
        var host = Render<ToastHost>();
        var toast = Toasts.Show("Uploading…", options: new() { StayOpen = true });
        _time.Advance(TimeSpan.FromMinutes(1));
        Assert.True(toast.IsOpen);

        toast.Update("Uploaded", Tone.Success, new() { Duration = TimeSpan.FromSeconds(2) });

        host.WaitForAssertion(() => Assert.Contains("tb-toast--success", host.Find("[data-part=toast]").ClassList));
        Assert.Equal("Uploaded", host.Find("[data-part=message]").TextContent);
        _time.Advance(TimeSpan.FromSeconds(2.1));
        host.WaitForAssertion(() => Assert.False(toast.IsOpen));
    }

    [Fact]
    public void Show_ShouldQueueToastsBeyondTheMaximum_WhenMoreAreOpen()
    {
        var host = Render<ToastHost>();
        var toasts = Enumerable.Range(1, 4).Select(n => Toasts.Show($"Toast {n}", Tone.Danger)).ToList();

        host.WaitForAssertion(() => Assert.Equal(3, host.FindAll("[data-part=toast]").Count));
        Assert.DoesNotContain("Toast 4", host.Markup);

        toasts[0].Dismiss();

        host.WaitForAssertion(() => Assert.Contains("Toast 4", host.Markup));
        Assert.Equal(3, host.FindAll("[data-part=toast]").Count);
    }

    [Fact]
    public async Task Action_ShouldRunAndCloseTheToast_WhenClicked()
    {
        var host = Render<ToastHost>();
        var undone = false;
        var toast = Toasts.Show("Deleted",
            options: new() { Action = new("Undo", EventCallback.Factory.Create(this, () => undone = true)) });

        await host.WaitForElement("[data-part=action]").ClickAsync(new());

        Assert.True(undone);
        Assert.False(toast.IsOpen);
    }

    [Fact]
    public void Toasts_ShouldRenderInTheTopmostModalDialog_WhenOneIsOpen()
    {
        var page = Render<ToastHost>();
        var dialogs = Render<DialogHost>();
        Toasts.Show("Saved");
        var dialogService = Services.GetRequiredService<IDialogService>();

        _ = dialogService.ShowAsync<EmptyDialog, bool>("Outer");
        _ = dialogService.ShowAsync<EmptyDialog, bool>("Inner");
        _ = dialogService.ShowAsync<EmptyDialog, bool>("Panel", options: new() { Modal = false });

        dialogs.WaitForAssertion(() => Assert.Equal(3, dialogs.FindAll("dialog").Count));
        Assert.Empty(page.FindAll("[data-part=toast]"));
        var toaster = dialogs.WaitForElement("[data-part=toast]").Closest("dialog")!;
        Assert.Equal("Inner", toaster.QuerySelector("h2")!.TextContent);
        Assert.Single(dialogs.FindAll("[data-part=toast]"));

        foreach (var dialog in dialogService.OpenDialogs.ToList())
        {
            _ = dialog.CloseAsync(false, null);
        }

        page.WaitForAssertion(() => Assert.Single(page.FindAll("[data-part=toast]")));
    }

    [Fact]
    public void Toast_ShouldNotStartATimer_WhenRenderedStatically()
    {
        SetRendererInfo(new RendererInfo("Static", isInteractive: false));
        var host = Render<ToastHost>();
        var toast = Toasts.Show("Saved");
        host.WaitForElement("[data-part=toast]");

        _time.Advance(TimeSpan.FromMinutes(1));

        Assert.True(toast.IsOpen);
    }

    [Fact]
    public void Dismiss_ShouldDoNothing_WhenTheToastHasAlreadyClosed()
    {
        var toast = Toasts.Show("Saved");
        var other = Toasts.Show("Other");
        toast.Dismiss();

        toast.Dismiss();
        toast.Update("Again", Tone.Neutral);

        Assert.Equal([other], Toasts.Toasts);
    }

    [Fact]
    public void Toaster_ShouldUseTheAppWidePosition_WhenConfigured()
    {
        Services.GetRequiredService<IOptions<TrailblazorOptions>>().Value.Toasts.Position = ToastPosition.BottomCenter;

        var host = Render<ToastHost>();

        Assert.Contains("tb-toaster--bottomcenter", host.Find("[data-part=toaster]").ClassList);
    }

    [Fact]
    public async Task Dispose_ShouldDetachTheToasterScript_WhenTheHostGoesAway()
    {
        var module = JSInterop.SetupModule("./_content/Trailblazor/toaster.js");
        var handle = module.SetupModule("attachToaster", _ => true);
        handle.SetupVoid("sync").SetVoidResult();
        handle.SetupVoid("dispose").SetVoidResult();
        var host = Render<ToastHost>();
        host.WaitForAssertion(() => handle.VerifyInvoke("sync"));

        await DisposeComponentsAsync();

        handle.VerifyInvoke("dispose");
    }

    private sealed class EmptyDialog : TbDialog<bool>
    {
    }
}
