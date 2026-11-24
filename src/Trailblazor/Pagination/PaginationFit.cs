using Microsoft.JSInterop;
using Trailblazor.Primitives;

namespace Trailblazor;

// Hears how many buttons fit on the pager's row, so the public pagination needn't expose a [JSInvokable] method.
internal sealed class PaginationFit(Func<Exception, Task> onException, Action<int?> onFit) : ScriptListener(onException)
{
    [JSInvokable]
    public Task OnFit(int? slots) => Run(() => onFit(slots));
}
