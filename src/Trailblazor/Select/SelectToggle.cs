using Microsoft.JSInterop;
using Trailblazor.Primitives;

namespace Trailblazor;

// Hears the browser open and close a select's list and remove a chip, so the public select needn't expose
// [JSInvokable] methods.
internal sealed class SelectToggle(Func<Exception, Task> onException, Func<bool, Task> onToggle,
    Func<string, string, Task> onRemoveChip, Func<string, Task> onFilter, Func<int, string, Task<int>> onFind,
    string comboboxId)
    : ScriptListener(onException)
{
    // The test helpers find a select's toggle by the id of its combobox.
    internal string ComboboxId => comboboxId;

    [JSInvokable]
    public Task OnToggle(bool open) => Run(() => onToggle(open));

    // The chip's option, by its element id, and the text the chip shows, which is what is announced.
    [JSInvokable]
    public Task OnRemoveChip(string optionId, string text) => Run(() => onRemoveChip(optionId, text));

    // A virtualized list filters, and finds typed text, on the server, where all its options are.
    [JSInvokable]
    public Task OnFilter(string query) => Run(() => onFilter(query));

    [JSInvokable]
    public Task<int> OnFind(int from, string typed) => Run(() => onFind(from, typed), -1);
}
