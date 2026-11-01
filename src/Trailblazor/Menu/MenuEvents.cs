using Microsoft.JSInterop;

namespace Trailblazor;

// Hears the browser report that the menu opened or closed, so the public menu needn't expose [JSInvokable] methods.
internal sealed class MenuEvents(Func<bool, Task> onToggle)
{
    [JSInvokable]
    public Task OnToggle(bool open) => onToggle(open);
}
