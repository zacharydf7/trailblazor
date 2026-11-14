using Microsoft.AspNetCore.Components;

namespace Trailblazor;

// Content a component in the dialog puts in its header. One swapped in sets its content before the one it replaces is
// disposed, so only the component that set the content last clears it.
internal sealed class DialogSlot
{
    private object? _owner;

    public RenderFragment? Content { get; private set; }

    public void Set(object owner, RenderFragment? content)
    {
        _owner = owner;
        Content = content;
    }

    // False when another component has set the content since.
    public bool Clear(object owner)
    {
        if (_owner != owner)
        {
            return false;
        }

        _owner = null;
        Content = null;
        return true;
    }
}
