using Microsoft.JSInterop;

namespace Trailblazor;

// Hears the browser open and close a popover's panel, so the public popover needn't expose [JSInvokable] methods.
internal sealed class PopoverEvents(Func<bool, Task> onToggle, string panelId)
{
    // The id of the panel these events are for, by which the bUnit driver finds a popover's events.
    public string PanelId => panelId;

    [JSInvokable]
    public Task OnToggle(bool open) => onToggle(open);
}
