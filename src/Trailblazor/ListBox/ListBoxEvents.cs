using Microsoft.JSInterop;

namespace Trailblazor;

// Hears the browser choose several options at once, so the public listbox needn't expose [JSInvokable] methods.
internal sealed class ListBoxEvents(Func<Task> onChooseAll, Func<string[], bool, bool, Task> onChooseRange)
{
    [JSInvokable]
    public Task OnChooseAll() => onChooseAll();

    // all marks the range as Ctrl+A over the options a filter shows, rather than a Shift+click.
    [JSInvokable]
    public Task OnChooseRange(string[] ids, bool last, bool all) => onChooseRange(ids, last, all);
}
