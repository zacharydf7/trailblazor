using Microsoft.JSInterop;
using Trailblazor.Primitives;

namespace Trailblazor;

// Hears whether the text's box cuts it off, so the public text needn't expose a [JSInvokable] method.
internal sealed class TextCut(Func<Exception, Task> onException, Action<bool> onCut) : ScriptListener(onException)
{
    [JSInvokable]
    public Task OnCut(bool cut) => Run(() => onCut(cut));
}
