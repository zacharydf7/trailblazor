using Microsoft.JSInterop;
using Trailblazor.Primitives;

namespace Trailblazor;

// Hears the browser open and close the calendar, ask for a month that isn't shown, and pick a day the keys reached
// before its month showed, so the public picker needn't expose [JSInvokable] methods.
internal sealed class DateRangePickerEvents(Func<Exception, Task> onException, Func<bool, Task> onToggle,
    Func<DateOnly, Task> onShowDate, Func<DateOnly, Task> onPick)
    : ScriptListener(onException)
{
    [JSInvokable]
    public Task OnToggle(bool open) => Run(() => onToggle(open));

    [JSInvokable]
    public Task OnShowDate(DateOnly date) => Run(() => onShowDate(date));

    [JSInvokable]
    public Task OnPick(DateOnly date) => Run(() => onPick(date));
}
