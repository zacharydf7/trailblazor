using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;
using Microsoft.JSInterop;

namespace Trailblazor.Tests.Components;

public class ToastServiceTests : BunitContext
{
    private readonly FakeTimeProvider _time = new();

    public ToastServiceTests()
    {
        Services.AddSingleton<TimeProvider>(_time);
        this.AddTrailblazor();
    }

    private IToastService Toasts => Services.GetRequiredService<IToastService>();

    [Fact]
    public async Task Show_ShouldKeepEveryToast_WhenToastsAreShownFromManyThreadsAtOnce()
    {
        // Resolved before the threads start, since the first resolution builds the test's service provider.
        var toasts = Toasts;
        await Task.WhenAll(Enumerable.Range(0, 8).Select(thread => Task.Run(() =>
        {
            for (var i = 0; i < 500; i++)
            {
                toasts.Show($"Toast {thread}-{i}", Tone.Danger);
            }
        })));

        Assert.Equal(4000, toasts.Toasts.Distinct().Count());
    }

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
        Assert.Empty(Toasts.Toasts);
        // It fades out before it goes.
        host.WaitForAssertion(() => Assert.NotNull(host.Find("[data-part=toast][data-leaving]")));
        _time.Advance(TimeSpan.FromSeconds(0.2));
        host.WaitForAssertion(() => Assert.Empty(host.FindAll("[data-part=toast]")));
    }

    [Fact]
    public void Toast_ShouldStayUntilDismissedAndBeAnnouncedAssertively_WhenGivenAnError()
    {
        var host = Render<ToastHost>();
        var announcer = Render<AnnouncerHost>();

        var toast = Toasts.Show("Upload failed", Tone.Danger);
        Assert.Equal("Upload failed", announcer.Find("[role=alert] p").TextContent);
        _time.Advance(TimeSpan.FromMinutes(10));

        Assert.True(toast.IsOpen);
        host.Find("[data-part=close]").Click();
        Assert.False(toast.IsOpen);
        _time.Advance(TimeSpan.FromSeconds(0.2));
        host.WaitForAssertion(() => Assert.Empty(host.FindAll("[data-part=toast]")));
    }

    [Fact]
    public async Task Toast_ShouldKeepItsRemainingTime_WhenTheToasterPausesOrFocusRestsOnIt()
    {
        var module = JSInterop.SetupModule("./_content/Trailblazor/toaster.js");
        module.SetupModule("attachToaster", _ => true).SetupVoid("sync").SetVoidResult();
        var host = Render<ToastHost>();
        var events = ((DotNetObjectReference<ToasterEvents>)module.Invocations["attachToaster"].Single().Arguments[1]!)
            .Value;
        var toast = Toasts.Show("Saved");
        _time.Advance(TimeSpan.FromSeconds(3));

        await host.InvokeAsync(() => events.OnPause(true));
        host.Find("[data-part=toast]").FocusIn();
        _time.Advance(TimeSpan.FromMinutes(1));
        await host.InvokeAsync(() => events.OnPause(false));
        _time.Advance(TimeSpan.FromMinutes(1));
        Assert.True(toast.IsOpen);

        host.Find("[data-part=toast]").FocusOut();
        _time.Advance(TimeSpan.FromSeconds(1.9));
        Assert.True(toast.IsOpen);
        _time.Advance(TimeSpan.FromSeconds(0.2));
        host.WaitForAssertion(() => Assert.False(toast.IsOpen));
    }

    [Fact]
    public async Task Toast_ShouldStayPaused_WhenThePointerCrossesFromOneToastToAnother()
    {
        var module = JSInterop.SetupModule("./_content/Trailblazor/toaster.js");
        module.SetupModule("attachToaster", _ => true).SetupVoid("sync").SetVoidResult();
        var host = Render<ToastHost>();
        var events = ((DotNetObjectReference<ToasterEvents>)module.Invocations["attachToaster"].Single().Arguments[1]!)
            .Value;
        var toast = Toasts.Show("Saved");
        await host.InvokeAsync(() => events.OnPause(true));

        await host.InvokeAsync(() => events.OnPause(false));
        _time.Advance(TimeSpan.FromMilliseconds(100));
        await host.InvokeAsync(() => events.OnPause(true));
        _time.Advance(TimeSpan.FromMinutes(1));
        await host.InvokeAsync(() => events.OnPause(false));
        _time.Advance(TimeSpan.FromSeconds(0.2));
        _time.Advance(TimeSpan.FromSeconds(4.9));

        // The full five seconds were left, and they start once the pointer has stayed away.
        Assert.True(toast.IsOpen);
        _time.Advance(TimeSpan.FromSeconds(0.2));
        host.WaitForAssertion(() => Assert.False(toast.IsOpen));
    }

    [Fact]
    public async Task Toast_ShouldWaitToStartItsTime_WhenShownWhileTheToasterIsPaused()
    {
        var module = JSInterop.SetupModule("./_content/Trailblazor/toaster.js");
        module.SetupModule("attachToaster", _ => true).SetupVoid("sync").SetVoidResult();
        var host = Render<ToastHost>();
        var events = ((DotNetObjectReference<ToasterEvents>)module.Invocations["attachToaster"].Single().Arguments[1]!)
            .Value;
        await host.InvokeAsync(() => events.OnPause(true));
        var first = Toasts.Show("Saved");
        var second = Toasts.Show("Sent");

        _time.Advance(TimeSpan.FromMinutes(1));
        Assert.True(first.IsOpen && second.IsOpen);
        await host.InvokeAsync(() => events.OnPause(false));
        _time.Advance(TimeSpan.FromSeconds(0.2));
        _time.Advance(TimeSpan.FromSeconds(5.1));

        host.WaitForAssertion(() => Assert.False(first.IsOpen || second.IsOpen));
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
        Assert.Equal(3, host.FindAll("[data-part=toast]:not([data-leaving])").Count);
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
    public void Show_ShouldPutTheTitleAboveTheMessageAndAnnounceBoth_WhenGivenATitle()
    {
        var host = Render<ToastHost>();
        var announcer = Render<AnnouncerHost>();

        Toasts.Show("The file is over 10 MB.", Tone.Danger, new() { Title = "Upload failed" });

        var toast = host.WaitForElement("[data-part=toast]");
        Assert.Equal("Upload failed", toast.QuerySelector("[data-part=title]")!.TextContent);
        Assert.Equal("The file is over 10 MB.", toast.QuerySelector("[data-part=message]")!.TextContent);
        Assert.Equal("Upload failed. The file is over 10 MB.", announcer.Find("[role=alert] p").TextContent);
    }

    [Theory]
    [InlineData("Upload failed!", "Upload failed! The file is over 10 MB.")]
    [InlineData(" ", "The file is over 10 MB.")]
    public void Show_ShouldAnnounceTheTitleWithoutDoublingItsPunctuation_WhenGivenAPunctuatedOrBlankTitle(
        string title, string announced)
    {
        var host = Render<ToastHost>();
        var announcer = Render<AnnouncerHost>();

        Toasts.Show("The file is over 10 MB.", Tone.Danger, new() { Title = title });

        Assert.Equal(announced, announcer.Find("[role=alert] p").TextContent);
        Assert.Equal(string.IsNullOrWhiteSpace(title),
            host.WaitForElement("[data-part=toast]").QuerySelector("[data-part=title]") is null);
    }

    [Theory]
    [InlineData(Tone.Success, false, false, true)]
    [InlineData(Tone.Neutral, false, false, false)]
    [InlineData(Tone.Neutral, true, false, true)]
    [InlineData(Tone.Danger, false, true, false)]
    public void Show_ShouldShowTheTonesIconOrItsOwnOrNone_WhenAsked(Tone tone, bool ownIcon, bool hideIcon, bool shown)
    {
        var host = Render<ToastHost>();

        Toasts.Show("Saved", tone, new()
        {
            Icon = ownIcon ? new Icon("<circle cx=\"12\" cy=\"12\" r=\"6\" />") : null,
            ShowIcon = !hideIcon,
        });

        var icon = host.WaitForElement("[data-part=toast]").QuerySelector(".tb-toast__icon");
        Assert.Equal(shown, icon is not null);
        if (ownIcon)
        {
            Assert.NotNull(icon!.QuerySelector("circle"));
        }
        else if (shown)
        {
            var success = Render<TbIcon>(parameters => parameters.Add(p => p.Icon, BuiltInIcons.Success));
            Assert.Equal(success.Find("svg").InnerHtml, icon!.InnerHtml);
        }
    }

    [Fact]
    public void Show_ShouldShowTheContentButAnnounceTheMessage_WhenGivenContent()
    {
        var host = Render<ToastHost>();
        var announcer = Render<AnnouncerHost>();

        Toasts.Show("Report ready", options: new()
        {
            Content = builder =>
            {
                builder.OpenElement(0, "a");
                builder.AddAttribute(1, "href", "reports/7");
                builder.AddContent(2, "Report ready: open it");
                builder.CloseElement();
            },
        });

        var message = host.WaitForElement("[data-part=message]");
        Assert.Equal("reports/7", message.QuerySelector("a")!.GetAttribute("href"));
        Assert.Equal("Report ready", announcer.Find("[role=status] p").TextContent);
    }

    [Fact]
    public void Toast_ShouldStayOpenAndSayItsActionAndKey_WhenGivenAnAction()
    {
        var host = Render<ToastHost>();
        var announcer = Render<AnnouncerHost>();

        Toasts.Show("Deleted 3 trails", options: new() { Action = new("Undo", EventCallback.Empty) });
        announcer.WaitForAssertion(() => Assert.Equal("Deleted 3 trails. Undo available, press F8.",
            announcer.Find("[role=status] p").TextContent));
        _time.Advance(TimeSpan.FromMinutes(1));

        Assert.Single(host.FindAll("[data-part=toast]"));
    }

    [Fact]
    public void Toast_ShouldCloseOnTime_WhenGivenAnActionAndADuration()
    {
        Render<ToastHost>();

        var toast = Toasts.Show("Deleted", options: new()
        {
            Action = new("Undo", EventCallback.Empty),
            Duration = TimeSpan.FromSeconds(10),
        });
        _time.Advance(TimeSpan.FromSeconds(10));

        Assert.False(toast.IsOpen);
    }

    [Fact]
    public async Task Closed_ShouldSayWhyEachToastClosed_WhenTheyCloseInEachWay()
    {
        var host = Render<ToastHost>();
        var timedOut = Toasts.Show("Saved");
        var closed = Toasts.Show("Copied");
        var acted = Toasts.Show("Deleted", options: new() { Action = new("Undo", EventCallback.Empty) });
        host.WaitForAssertion(() => Assert.Equal(3, host.FindAll("[data-part=toast]").Count));

        host.FindAll("[data-part=close]")[1].Click();
        await host.Find("[data-part=action]").ClickAsync(new());
        _time.Advance(TimeSpan.FromSeconds(5));
        var dismissed = Toasts.Show("Moved");
        dismissed.Dismiss();

        Assert.Equal([ToastCloseReason.TimedOut, ToastCloseReason.CloseButton, ToastCloseReason.Action,
            ToastCloseReason.App],
            await Task.WhenAll(timedOut.Closed, closed.Closed, acted.Closed, dismissed.Closed));
    }

    [Fact]
    public async Task DismissAll_ShouldCloseEveryToastAndTheQueue_WhenCalled()
    {
        var host = Render<ToastHost>();
        var toasts = Enumerable.Range(1, 5).Select(n => Toasts.Show($"Toast {n}", Tone.Danger)).ToList();

        Toasts.DismissAll();

        Assert.Empty(Toasts.Toasts);
        Assert.All(await Task.WhenAll(toasts.Select(toast => toast.Closed)),
            reason => Assert.Equal(ToastCloseReason.App, reason));
        _time.Advance(TimeSpan.FromSeconds(0.2));
        host.WaitForAssertion(() => Assert.Empty(host.FindAll("[data-part=toast]")));
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
    public async Task Toasts_ShouldMoveToThePageAndBack_WhenTheModalHoldingThemIsMinimizedAndRestored()
    {
        var page = Render<ToastHost>();
        var dialogs = Render<DialogHost>();
        _ = Services.GetRequiredService<IDialogService>().ShowAsync<EmptyDialog, bool>("Notes",
            options: new() { Minimizable = true });
        Toasts.Show("Saved");
        dialogs.WaitForElement("dialog [data-part=toast]");
        var dialog = Services.GetRequiredService<IDialogService>().OpenDialogs.Single();

        await dialogs.InvokeAsync(() => dialog.WindowState = DialogWindowState.Minimized);
        page.WaitForAssertion(() => Assert.Single(page.FindAll("[data-part=toast]")));
        await dialogs.InvokeAsync(() => dialog.WindowState = DialogWindowState.Normal);

        dialogs.WaitForElement("dialog [data-part=toast]");
        page.WaitForAssertion(() => Assert.Empty(page.FindAll("[data-part=toast]")));
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

    private sealed class EmptyDialog : TbDialogContent<bool>
    {
    }
}
