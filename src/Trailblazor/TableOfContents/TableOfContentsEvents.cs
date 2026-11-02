using Microsoft.JSInterop;

namespace Trailblazor;

// Hears the browser report the section in view, so the public component needn't expose [JSInvokable] methods.
internal sealed class TableOfContentsEvents(Func<string?, Task> onActive)
{
    [JSInvokable]
    public Task OnActive(string? targetId) => onActive(targetId);
}
