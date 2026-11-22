using Microsoft.JSInterop;
using Trailblazor.Primitives;

namespace Trailblazor;

// Hears the script's save on Enter and cancel on Escape, so the public component needn't expose [JSInvokable] methods.
internal sealed class InPlaceEditorEvents(Func<Exception, Task> onException, Func<Task> onSave, Func<Task> onCancel)
    : ScriptListener(onException)
{
    [JSInvokable]
    public Task OnSave() => Run(() => onSave());

    [JSInvokable]
    public Task OnCancel() => Run(() => onCancel());
}
