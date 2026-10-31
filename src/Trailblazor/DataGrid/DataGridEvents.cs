using Microsoft.JSInterop;

namespace Trailblazor;

// Hears the browser report a click on a row, away from the controls in it, so the public grid needn't expose
// [JSInvokable] methods.
internal sealed class DataGridEvents(Func<string, Task> onRowClick)
{
    [JSInvokable]
    public Task OnRowClick(string key) => onRowClick(key);
}
