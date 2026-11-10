using Microsoft.JSInterop;

namespace Trailblazor;

// Hears the browser ask for a cell that isn't shown, and pick a cell the keys reached before it showed, so the public
// calendar needn't expose [JSInvokable] methods.
internal sealed class CalendarEvents(Func<DateOnly, Task> onShowDate, Func<DateOnly, Task> onPick)
{
    [JSInvokable]
    public Task OnShowDate(DateOnly date) => onShowDate(date);

    [JSInvokable]
    public Task OnPick(DateOnly date) => onPick(date);
}
