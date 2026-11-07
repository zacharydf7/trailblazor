using Microsoft.JSInterop;

namespace Trailblazor;

// Hears the browser choose several options at once, so the public listbox needn't expose [JSInvokable] methods.
internal sealed class ListBoxEvents(Func<Task> onChooseAll, Func<string[], bool, Task> onChooseRange)
{
    [JSInvokable]
    public Task OnChooseAll() => onChooseAll();

    [JSInvokable]
    public Task OnChooseRange(string[] ids, bool last) => onChooseRange(ids, last);
}
