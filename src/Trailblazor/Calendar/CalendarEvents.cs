using Microsoft.JSInterop;
using Trailblazor.Primitives;

namespace Trailblazor;

// Hears the browser ask for a cell that isn't shown, and pick a cell the keys reached before it showed, so the public
// calendar needn't expose [JSInvokable] methods.
internal sealed class CalendarEvents(Func<Exception, Task> onException, Func<DateOnly, Task> onShowDate,
    Func<DateOnly, Task> onPick)
    : ScriptListener(onException)
{
    [JSInvokable]
    public Task OnShowDate(DateOnly date) => Run(() => onShowDate(date));

    [JSInvokable]
    public Task OnPick(DateOnly date) => Run(() => onPick(date));
}
