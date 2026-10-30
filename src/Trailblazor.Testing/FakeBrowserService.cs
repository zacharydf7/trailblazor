using System.Text;
using Microsoft.AspNetCore.Components;

namespace Trailblazor.Testing;

/// <summary>Stands in for the browser in bUnit, where there is none. <c>AddTrailblazor()</c> registers it as the
/// <see cref="IBrowserService"/>; get it from <c>Services</c> to see what a component copied, downloaded, saved or
/// scrolled to, and to set what the browser answers.</summary>
public sealed class FakeBrowserService : IBrowserService
{
    private readonly List<string> _copied = [];
    private readonly List<BrowserFile> _downloads = [];
    private readonly List<BrowserFile> _saves = [];
    private readonly List<ScrollRequest> _scrolledTo = [];
    private readonly List<Func<Viewport, Task>> _viewportListeners = [];
    private readonly List<Func<Breakpoint, Task>> _breakpointListeners = [];

    /// <summary>The text copied to the clipboard, oldest first.</summary>
    public IReadOnlyList<string> Copied => _copied;

    /// <summary>Whether copying works, as it doesn't on an insecure page. When false, nothing is copied.</summary>
    public bool CanCopy { get; set; } = true;

    /// <summary>The files downloaded, oldest first.</summary>
    public IReadOnlyList<BrowserFile> Downloads => _downloads;

    /// <summary>The files saved, oldest first.</summary>
    public IReadOnlyList<BrowserFile> Saves => _saves;

    /// <summary>Whether the user cancels the save dialog. When true, nothing is saved.</summary>
    public bool CancelsSave { get; set; }

    /// <summary>The answer to <see cref="CanPickColorAsync"/>.</summary>
    public bool CanPickColor { get; set; } = true;

    /// <summary>The color the user picks with the eyedropper, or null for a user who cancels.</summary>
    public string? PickedColor { get; set; }

    /// <summary>The elements scrolled into view and how, oldest first.</summary>
    public IReadOnlyList<ScrollRequest> ScrolledTo => _scrolledTo;

    /// <summary>The viewport, 1280 by 720 until <see cref="ResizeAsync"/> changes it.</summary>
    public Viewport Viewport { get; private set; } = new(1280, 720);

    /// <summary>Resizes the viewport and tells the observers, as the browser would. Call it through the rendered
    /// component's <c>InvokeAsync</c> when an observer renders.</summary>
    public async Task ResizeAsync(int width, int height)
    {
        var breakpoint = Viewport.Breakpoint;
        Viewport = new Viewport(width, height);
        foreach (var listener in _viewportListeners.ToArray())
        {
            await listener(Viewport);
        }

        if (Viewport.Breakpoint != breakpoint)
        {
            foreach (var listener in _breakpointListeners.ToArray())
            {
                await listener(Viewport.Breakpoint);
            }
        }
    }

    /// <inheritdoc />
    public Task<bool> CopyTextAsync(string text)
    {
        if (CanCopy)
        {
            _copied.Add(text);
        }

        return Task.FromResult(CanCopy);
    }

    /// <inheritdoc />
    public async Task DownloadAsync(Stream content, string fileName, string? contentType = null) =>
        _downloads.Add(await ReadAsync(content, fileName, contentType));

    /// <inheritdoc />
    public async Task<bool> SaveAsync(Stream content, string fileName, string? contentType = null)
    {
        if (CancelsSave)
        {
            return false;
        }

        _saves.Add(await ReadAsync(content, fileName, contentType));
        return true;
    }

    /// <inheritdoc />
    public Task<bool> CanPickColorAsync() => Task.FromResult(CanPickColor);

    /// <inheritdoc />
    public Task<string?> PickColorAsync() => Task.FromResult(CanPickColor ? PickedColor : null);

    /// <inheritdoc />
    public Task ScrollIntoViewAsync(ElementReference element, ScrollAlignment alignment = ScrollAlignment.Nearest,
        bool smooth = false)
    {
        _scrolledTo.Add(new ScrollRequest(element, alignment, smooth));
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task<Viewport> GetViewportAsync() => Task.FromResult(Viewport);

    /// <inheritdoc />
    public async Task<IAsyncDisposable> ObserveViewportAsync(Func<Viewport, Task> onChange)
    {
        _viewportListeners.Add(onChange);
        await onChange(Viewport);
        return new Observation(() => _viewportListeners.Remove(onChange));
    }

    /// <inheritdoc />
    public async Task<IAsyncDisposable> ObserveBreakpointAsync(Func<Breakpoint, Task> onChange)
    {
        _breakpointListeners.Add(onChange);
        await onChange(Viewport.Breakpoint);
        return new Observation(() => _breakpointListeners.Remove(onChange));
    }

    private static async Task<BrowserFile> ReadAsync(Stream content, string fileName, string? contentType)
    {
        using var bytes = new MemoryStream();
        await content.CopyToAsync(bytes);
        return new BrowserFile(fileName, contentType, bytes.ToArray());
    }

    private sealed class Observation(Action stop) : IAsyncDisposable
    {
        public ValueTask DisposeAsync()
        {
            stop();
            return ValueTask.CompletedTask;
        }
    }
}

/// <summary>An element a component scrolled into view through <see cref="FakeBrowserService"/>, and how.</summary>
public sealed record ScrollRequest(ElementReference Element, ScrollAlignment Alignment, bool Smooth);

/// <summary>A file a component downloaded or saved through <see cref="FakeBrowserService"/>.</summary>
public sealed record BrowserFile(string FileName, string? ContentType, byte[] Content)
{
    /// <summary>The content read as UTF-8 text.</summary>
    public string Text => Encoding.UTF8.GetString(Content);
}
