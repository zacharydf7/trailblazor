using Microsoft.JSInterop;
using Trailblazor.Primitives;

namespace Trailblazor;

// Hears the browser choose several options at once, so the public listbox needn't expose [JSInvokable] methods.
internal sealed class ListBoxEvents(Func<Exception, Task> onException, Func<Task> onChooseAll,
    Func<string[], bool, bool, bool, Task> onChooseRange)
    : ScriptListener(onException)
{
    [JSInvokable]
    public Task OnChooseAll() => Run(() => onChooseAll());

    // all marks the range as Ctrl+A over the options a filter shows, and keyboard as chosen by keys, Ctrl+A or
    // Shift+Arrow; with neither, it's a Shift+click.
    [JSInvokable]
    public Task OnChooseRange(string[] ids, bool last, bool all, bool keyboard) =>
        Run(() => onChooseRange(ids, last, all, keyboard));
}
