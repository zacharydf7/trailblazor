using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace Trailblazor;

// The chosen option, or the placeholder, inside the combobox. A separate component, so options can refresh it after
// they update without re-rendering the select and every option again. C#, so it can stay internal.
internal sealed class SelectDisplay : ComponentBase
{
    [Parameter, EditorRequired]
    public Func<ISelectOption?> Selected { get; set; } = default!;

    [Parameter]
    public string? Placeholder { get; set; }

    internal void Refresh() => StateHasChanged();

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        var selected = Selected();
        builder.OpenElement(0, "span");
        builder.AddAttribute(1, "data-part", selected is null ? "placeholder" : "value");
        builder.AddAttribute(2, "class", selected is null ? "tb-select__placeholder" : "tb-select__value");
        if (selected is null)
        {
            builder.AddContent(3, Placeholder);
        }
        else
        {
            builder.AddContent(4, selected.Content);
        }

        builder.CloseElement();
    }
}
