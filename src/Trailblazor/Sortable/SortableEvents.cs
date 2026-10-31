using Microsoft.JSInterop;

namespace Trailblazor;

// Hears drags and move keys from sortable.js, so the public components needn't expose [JSInvokable] methods. Items are
// named by their key's id, not their place, since a key pressed before the last move shows would name a stale place.
internal sealed class SortableEvents(Func<string, string, bool, Task<MoveCheck>> onDragOver,
    Func<string, string, bool, Task> onDrop, Func<string, string, Task> onKey)
{
    [JSInvokable]
    public Task<MoveCheck> OnDragOver(string id, string targetId, bool before) => onDragOver(id, targetId, before);

    [JSInvokable]
    public Task OnDrop(string id, string targetId, bool before) => onDrop(id, targetId, before);

    [JSInvokable]
    public Task OnKey(string id, string key) => onKey(id, key);
}
