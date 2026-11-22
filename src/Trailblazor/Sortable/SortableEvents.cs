using Microsoft.JSInterop;
using Trailblazor.Primitives;

namespace Trailblazor;

// Hears drags and move keys from sortable.js, so the public components needn't expose [JSInvokable] methods. Items are
// named by their key's id, not their place, since a key pressed before the last move shows would name a stale place.
// Only a sortable list in a group hears moves to another list, named by its ListId.
internal sealed class SortableEvents(Func<Exception, Task> onException,
    Func<string, string, bool, Task<MoveCheck>> onDragOver, Func<string, string, bool, Task> onDrop,
    Func<string, string, Task> onKey, Func<string, string, string?, bool, Task<MoveCheck>>? onDragOverList = null,
    Func<string, string, string?, bool, Task>? onDropList = null, Func<string, string, Task>? onListKey = null)
    : ScriptListener(onException)
{
    // The list's or grid's accessible name, by which the bUnit drivers tell whose drags these are.
    public Func<string?> Name { get; init; } = () => null;

    [JSInvokable]
    public Task<MoveCheck> OnDragOver(string id, string targetId, bool before) =>
        Run(() => onDragOver(id, targetId, before), new MoveCheck(false));

    [JSInvokable]
    public Task OnDrop(string id, string targetId, bool before) => Run(() => onDrop(id, targetId, before));

    [JSInvokable]
    public Task OnKey(string id, string key) => Run(() => onKey(id, key));

    // The target is null for a drop past the other list's last item, or on an empty list.
    [JSInvokable]
    public Task<MoveCheck> OnDragOverList(string id, string listId, string? targetId, bool before) =>
        Run(() => onDragOverList?.Invoke(id, listId, targetId, before) ?? Task.FromResult(new MoveCheck(false)),
            new MoveCheck(false));

    [JSInvokable]
    public Task OnDropList(string id, string listId, string? targetId, bool before) =>
        Run(() => onDropList?.Invoke(id, listId, targetId, before) ?? Task.CompletedTask);

    [JSInvokable]
    public Task OnListKey(string id, string listId) => Run(() => onListKey?.Invoke(id, listId) ?? Task.CompletedTask);
}
