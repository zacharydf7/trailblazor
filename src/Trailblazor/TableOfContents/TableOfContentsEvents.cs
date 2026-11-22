using Microsoft.JSInterop;
using Trailblazor.Primitives;

namespace Trailblazor;

// Hears the browser report the section in view, so the public component needn't expose [JSInvokable] methods.
internal sealed class TableOfContentsEvents(Func<Exception, Task> onException, Func<string?, Task> onActive)
    : ScriptListener(onException)
{
    [JSInvokable]
    public Task OnActive(string? targetId) => Run(() => onActive(targetId));
}
