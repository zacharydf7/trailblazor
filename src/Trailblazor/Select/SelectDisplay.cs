using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace Trailblazor;

// The chosen options, or the placeholder, inside the combobox. A separate component, so options can refresh it after
// they update without re-rendering the select and every option again. C#, so it can stay internal.
internal sealed class SelectDisplay : ComponentBase
{
    [Parameter, EditorRequired]
    public Func<IReadOnlyList<ISelectOption>> Selected { get; set; } = default!;

    [Parameter]
    public string? Placeholder { get; set; }

    internal void Refresh() => StateHasChanged();

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        var selected = Selected();
        builder.OpenElement(0, "span");
        builder.AddAttribute(1, "data-part", selected.Count == 0 ? "placeholder" : "value");
        builder.AddAttribute(2, "class", selected.Count == 0 ? "tb-select__placeholder" : "tb-select__value");
        if (selected.Count == 0)
        {
            builder.AddContent(3, Placeholder);
        }

        for (var index = 0; index < selected.Count; index++)
        {
            if (index > 0)
            {
                builder.AddContent(4, ", ");
            }

            builder.AddContent(5, selected[index].Content);
        }

        builder.CloseElement();
    }
}
