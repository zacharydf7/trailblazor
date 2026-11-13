using Microsoft.JSInterop;

namespace Trailblazor;

// Hears the page's shortcut for the palette, so the public component needn't expose a [JSInvokable] method.
internal sealed class CommandPaletteEvents(Func<Task> onShortcut)
{
    [JSInvokable]
    public Task OnShortcut() => onShortcut();
}
