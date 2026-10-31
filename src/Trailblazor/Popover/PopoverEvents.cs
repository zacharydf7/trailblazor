using Microsoft.JSInterop;

namespace Trailblazor;

// Hears the browser open and close a popover's panel, so the public popover needn't expose [JSInvokable] methods.
internal sealed class PopoverEvents(Func<bool, Task> onToggle)
{
    [JSInvokable]
    public Task OnToggle(bool open) => onToggle(open);
}
