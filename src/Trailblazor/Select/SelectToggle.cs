using Microsoft.JSInterop;

namespace Trailblazor;

// Hears the browser open and close a select's list and remove a chip, so the public select needn't expose
// [JSInvokable] methods.
internal sealed class SelectToggle(Func<bool, Task> onToggle, Func<string, string, Task> onRemoveChip)
{
    [JSInvokable]
    public Task OnToggle(bool open) => onToggle(open);

    // The chip's option, by its element id, and the text the chip shows, which is what is announced.
    [JSInvokable]
    public Task OnRemoveChip(string optionId, string text) => onRemoveChip(optionId, text);
}
