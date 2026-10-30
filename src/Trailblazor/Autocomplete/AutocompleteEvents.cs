using Microsoft.JSInterop;

namespace Trailblazor;

// Hears the browser open and close the list, and ask for suggestions when the user opens it without typing, so the
// public field needn't expose [JSInvokable] methods.
internal sealed class AutocompleteEvents(Func<bool, Task> onToggle, Func<Task> onBrowse)
{
    [JSInvokable]
    public Task OnToggle(bool open) => onToggle(open);

    [JSInvokable]
    public Task OnBrowse() => onBrowse();
}
