using Microsoft.AspNetCore.Components;

namespace Trailblazor.Docs.Examples;

// A demo whose parameters come from the controls beside it on the docs page. Without them it shows its defaults.
public abstract class ControlledExample : ComponentBase
{
    [Parameter]
    public IReadOnlyDictionary<string, object?>? Values { get; set; }
}
