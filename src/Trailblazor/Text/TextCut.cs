using Microsoft.JSInterop;
using Trailblazor.Primitives;

namespace Trailblazor;

// Hears whether the text's box cuts it off, and whether it sits in something that takes focus, so the public text
// needn't expose a [JSInvokable] method.
internal sealed class TextCut(Func<Exception, Task> onException, Action<bool, bool> onCut) : ScriptListener(onException)
{
    [JSInvokable]
    public Task OnCut(bool cut, bool held) => Run(() => onCut(cut, held));
}
