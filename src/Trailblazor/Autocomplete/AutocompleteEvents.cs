using Microsoft.JSInterop;
using Trailblazor.Primitives;

namespace Trailblazor;

// Hears the browser open and close the list, and ask for suggestions when the user opens it without typing, so the
// public field needn't expose [JSInvokable] methods.
internal sealed class AutocompleteEvents(Func<Exception, Task> onException, Func<bool, Task> onToggle,
    Func<Task> onBrowse)
    : ScriptListener(onException)
{
    [JSInvokable]
    public Task OnToggle(bool open) => Run(() => onToggle(open));

    [JSInvokable]
    public Task OnBrowse() => Run(() => onBrowse());
}
