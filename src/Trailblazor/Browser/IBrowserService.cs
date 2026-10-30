using Microsoft.AspNetCore.Components;

namespace Trailblazor;

/// <summary>Reaches browser features from C#: the clipboard, file downloads and saves, the eyedropper, scrolling, and
/// the viewport's size. Register it with <c>AddTrailblazor()</c>. It talks to the browser, so call it once the page is
/// interactive, from an event handler or <c>OnAfterRenderAsync</c>, not while prerendering.</summary>
public interface IBrowserService
{
    /// <summary>Copies text to the clipboard. The browser allows it only on a secure page (HTTPS or localhost) and
    /// soon after the user acts, such as from a click handler.</summary>
    /// <returns>Whether the text was copied.</returns>
    Task<bool> CopyTextAsync(string text);

    /// <summary>Downloads the stream as a file, the way a download link does. The stream is read from its current
    /// position and left open.</summary>
    Task DownloadAsync(Stream content, string fileName, string? contentType = null);

    /// <summary>Lets the user choose where to save the stream, in browsers with the File System Access API, and
    /// downloads it in the others. Call it from a click handler, since the browser opens the save dialog only soon
    /// after the user acts. The stream is read from its current position and left open.</summary>
    /// <returns>False when the user cancels the save dialog, and true otherwise.</returns>
    Task<bool> SaveAsync(Stream content, string fileName, string? contentType = null);

    /// <summary>Whether the browser has an eyedropper for <see cref="PickColorAsync"/>, so the button for it can be
    /// left out where it would do nothing.</summary>
    Task<bool> CanPickColorAsync();

    /// <summary>Lets the user pick a color from anywhere on the screen with the browser's eyedropper. Call it from a
    /// click handler, since the browser opens the eyedropper only soon after the user acts.</summary>
    /// <returns>The color as <c>#rrggbb</c>, or null when the user cancels or the browser has no eyedropper.</returns>
    Task<string?> PickColorAsync();

    /// <summary>Scrolls the element's containers until the element is in view. A smooth scroll jumps instead when the
    /// user has asked for reduced motion.</summary>
    Task ScrollIntoViewAsync(ElementReference element, ScrollAlignment alignment = ScrollAlignment.Nearest,
        bool smooth = false);

    /// <summary>The viewport's size now, and the breakpoint it falls in.</summary>
    Task<Viewport> GetViewportAsync();

    /// <summary>Calls <paramref name="onChange"/> with the viewport now, and again each time the user stops resizing
    /// it. Dispose the result to stop.</summary>
    Task<IAsyncDisposable> ObserveViewportAsync(Func<Viewport, Task> onChange);

    /// <summary>Calls <paramref name="onChange"/> with the breakpoint now, and again each time the viewport crosses
    /// into another one. It is cheaper than <see cref="ObserveViewportAsync"/> when only the breakpoint matters.
    /// Dispose the result to stop.</summary>
    Task<IAsyncDisposable> ObserveBreakpointAsync(Func<Breakpoint, Task> onChange);
}
