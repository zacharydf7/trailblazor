using System.Text;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Trailblazor.Testing;

namespace Trailblazor.Tests;

public class FakeBrowserServiceTests : BunitContext
{
    private readonly FakeBrowserService _browser = new();

    [Fact]
    public void AddTrailblazor_ShouldMakeTheFakeTheBrowserService_WhenCalled()
    {
        this.AddTrailblazor();

        Assert.Same(Services.GetRequiredService<FakeBrowserService>(), Services.GetRequiredService<IBrowserService>());
    }

    [Fact]
    public void AddTrailblazor_ShouldKeepTheTestsOwnBrowserService_WhenTheTestRegisteredOneFirst()
    {
        Services.AddSingleton<IBrowserService>(_browser);

        this.AddTrailblazor();

        Assert.Same(_browser, Services.GetRequiredService<IBrowserService>());
    }

    [Fact]
    public async Task CopyText_ShouldCopyNothingAndReturnFalse_WhenCopyingDoesntWork()
    {
        _browser.CanCopy = false;

        Assert.False(await _browser.CopyTextAsync("Trail notes"));

        Assert.Empty(_browser.Copied);
    }

    [Fact]
    public async Task Download_ShouldKeepTheFileFromTheStreamsPosition_WhenCalled()
    {
        using var content = new MemoryStream(Encoding.UTF8.GetBytes("skip:Trail notes"));
        content.Position = 5;

        await _browser.DownloadAsync(content, "notes.txt", "text/plain");

        var file = Assert.Single(_browser.Downloads);
        Assert.Equal(("notes.txt", "text/plain", "Trail notes"), (file.FileName, file.ContentType, file.Text));
    }

    [Fact]
    public async Task Save_ShouldSaveNothingAndReturnFalse_WhenTheUserCancels()
    {
        _browser.CancelsSave = true;
        using var content = new MemoryStream(Encoding.UTF8.GetBytes("Trail notes"));

        Assert.False(await _browser.SaveAsync(content, "notes.txt"));

        Assert.Empty(_browser.Saves);
    }

    [Fact]
    public async Task PickColor_ShouldReturnNull_WhenTheBrowserHasNoEyedropper()
    {
        _browser.PickedColor = "#336699";
        _browser.CanPickColor = false;

        Assert.Null(await _browser.PickColorAsync());
    }

    [Fact]
    public async Task ScrollIntoView_ShouldRecordTheElementAndHow_WhenCalled()
    {
        var element = new ElementReference("newest");

        await _browser.ScrollIntoViewAsync(element, ScrollAlignment.End, smooth: true);

        Assert.Equal([new ScrollRequest(element, ScrollAlignment.End, true)], _browser.ScrolledTo);
    }

    [Fact]
    public async Task Resize_ShouldTellBreakpointObserversOnlyOfANewBreakpoint_WhenTheViewportChanges()
    {
        var viewports = new List<Viewport>();
        var breakpoints = new List<Breakpoint>();
        await _browser.ObserveViewportAsync(viewport => Add(viewports, viewport));
        await _browser.ObserveBreakpointAsync(breakpoint => Add(breakpoints, breakpoint));

        await _browser.ResizeAsync(1300, 700);
        await _browser.ResizeAsync(700, 500);

        Assert.Equal([new(1280, 720), new(1300, 700), new(700, 500)], viewports);
        Assert.Equal([Breakpoint.Xl, Breakpoint.Sm], breakpoints);
    }

    [Fact]
    public async Task Resize_ShouldTellNoOne_WhenTheObservationIsDisposed()
    {
        var viewports = new List<Viewport>();
        var observation = await _browser.ObserveViewportAsync(viewport => Add(viewports, viewport));

        await observation.DisposeAsync();
        await _browser.ResizeAsync(700, 500);

        Assert.Equal([new Viewport(1280, 720)], viewports);
    }

    private static Task Add<T>(List<T> list, T item)
    {
        list.Add(item);
        return Task.CompletedTask;
    }
}
