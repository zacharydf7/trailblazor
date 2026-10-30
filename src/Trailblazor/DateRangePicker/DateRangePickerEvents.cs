using Microsoft.JSInterop;

namespace Trailblazor;

// Hears the browser open and close the calendar, and ask for a month that isn't shown, so the public picker needn't
// expose [JSInvokable] methods.
internal sealed class DateRangePickerEvents(Func<bool, Task> onToggle, Func<DateOnly, Task> onShowDate)
{
    [JSInvokable]
    public Task OnToggle(bool open) => onToggle(open);

    [JSInvokable]
    public Task OnShowDate(DateOnly date) => onShowDate(date);
}
