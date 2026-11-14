using Microsoft.JSInterop;

namespace Trailblazor;

// Hears the browser report that the menu opened or closed, so the public menu needn't expose [JSInvokable] methods.
internal sealed class MenuEvents(Func<bool, Task> onToggle, string? listId = null)
{
    // The test helpers find a menu's events by the id of the list it opens.
    internal string? ListId => listId;

    [JSInvokable]
    public Task OnToggle(bool open) => onToggle(open);
}
