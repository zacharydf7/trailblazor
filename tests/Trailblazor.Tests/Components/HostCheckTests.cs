using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;
using Microsoft.Extensions.Time.Testing;

namespace Trailblazor.Tests.Components;

public class HostCheckTests : BunitContext
{
    private readonly FakeTimeProvider _time = new();
    private readonly FakeLogCollector _logs = new();

    public HostCheckTests()
    {
        Services.AddSingleton<TimeProvider>(_time);
        Services.AddSingleton<ILogger<ToastService>>(new FakeLogger<ToastService>(_logs));
        Services.AddSingleton<ILogger<DialogService>>(new FakeLogger<DialogService>(_logs));
        Services.AddSingleton<ILogger<Announcer>>(new FakeLogger<Announcer>(_logs));
        this.AddTrailblazor();
    }

    private IToastService Toasts => Services.GetRequiredService<IToastService>();

    private IDialogService Dialogs => Services.GetRequiredService<IDialogService>();

    private IReadOnlyList<string> Warnings => _logs.GetSnapshot().Where(log => log.Level == LogLevel.Warning)
        .Select(log => log.Message).ToList();

    [Fact]
    public void Show_ShouldWarnOnceThatNoTbRootIsRendering_WhenNoToastHostAttachesSoonAfter()
    {
        Render<AnnouncerHost>();
        Toasts.Show("Saved");
        Toasts.Show("Sent");

        _time.Advance(TimeSpan.FromSeconds(0.9));
        Assert.Empty(Warnings);
        _time.Advance(TimeSpan.FromSeconds(0.1));

        var warning = Assert.Single(Warnings);
        Assert.StartsWith("A toast was shown", warning);
        Assert.Contains("TbRoot", warning);
        Assert.Contains("HostRenderMode", warning);
    }

    [Fact]
    public void Show_ShouldShowTheToastWithoutWarning_WhenTheHostInitializesAfterIt()
    {
        Toasts.Show("Saved");
        var host = Render<ToastHost>();
        Render<AnnouncerHost>();

        _time.Advance(TimeSpan.FromSeconds(1));

        Assert.Equal("Saved", host.WaitForElement("[data-part=toast] [data-part=message]").TextContent);
        Assert.Empty(Warnings);
    }

    [Fact]
    public async Task Show_ShouldWarn_WhenTheOnlyToastHostHasGone()
    {
        Render<ToastHost>();
        await DisposeComponentsAsync();
        Render<AnnouncerHost>();

        Toasts.Show("Saved");
        _time.Advance(TimeSpan.FromSeconds(1));

        Assert.StartsWith("A toast was shown", Assert.Single(Warnings));
    }

    [Fact]
    public async Task Show_ShouldNotWarn_WhenTheHostsAttachAndGoBeforeTheCheckEnds()
    {
        // A prerendered page shows the toast, its hosts render it, and the request ends, all within a moment.
        Toasts.Show("Saved");
        Render<ToastHost>();
        Render<AnnouncerHost>();
        await DisposeComponentsAsync();

        _time.Advance(TimeSpan.FromSeconds(1));

        Assert.Empty(Warnings);
    }

    [Fact]
    public void ConfirmAsync_ShouldWarnThatNoTbRootIsRendering_WhenNoDialogHostAttachesSoonAfter()
    {
        _ = Dialogs.ConfirmAsync("Delete trail", "Delete it?");

        _time.Advance(TimeSpan.FromSeconds(1));

        Assert.StartsWith("A dialog was shown", Assert.Single(Warnings));
    }

    [Fact]
    public void OpenInPlace_ShouldNotWarn_WhenNoDialogHostIsRendering()
    {
        Services.GetRequiredService<DialogService>().OpenInPlace("Settings", new());

        _time.Advance(TimeSpan.FromSeconds(1));

        Assert.Empty(Warnings);
    }

    [Fact]
    public void Polite_ShouldWarnThatNoTbRootIsRendering_WhenNoAnnouncerHostAttachesSoonAfter()
    {
        Services.GetRequiredService<IAnnouncer>().Polite("Saved");

        _time.Advance(TimeSpan.FromSeconds(1));

        Assert.StartsWith("An announcement was shown", Assert.Single(Warnings));
    }
}
