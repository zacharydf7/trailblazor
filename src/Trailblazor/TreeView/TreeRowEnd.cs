using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;

namespace Trailblazor;

// A tree row's end slot. It redraws itself when a TbTreeItemEnd in the row fills it; redrawing the tree instead would
// redraw the row's template and the TbTreeItemEnd in it, which would fill the slot again. C#, so it can stay internal.
internal sealed class TreeRowEnd : ComponentBase, IDisposable
{
    // The row's ItemEndTemplate or TbTreeItem.End, shown before what a TbTreeItemEnd fills in.
    [Parameter]
    public RenderFragment? Fixed { get; set; }

    [Parameter, EditorRequired]
    public ContentSlot Slot { get; set; } = default!;

    [Parameter]
    public string? Id { get; set; }

    [Parameter]
    public string? Class { get; set; }

    public static RenderFragment Render(RenderFragment? end, ContentSlot slot, string? id, string? className) =>
        builder =>
        {
            builder.OpenComponent<TreeRowEnd>(0);
            builder.AddComponentParameter(1, nameof(Fixed), end);
            builder.AddComponentParameter(2, nameof(Slot), slot);
            builder.AddComponentParameter(3, nameof(Id), id);
            builder.AddComponentParameter(4, nameof(Class), className);
            builder.CloseComponent();
        };

    protected override void OnInitialized() => Slot.Changed += StateHasChanged;

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        if (Fixed is null && Slot.Content is null)
        {
            return;
        }

        // The end slot's own controls keep their clicks and keys from the row.
        builder.OpenElement(0, "span");
        builder.AddAttribute(1, "data-part", "end");
        builder.AddAttribute(2, "id", Id);
        builder.AddAttribute(3, "class", Class);
        builder.AddEventStopPropagationAttribute(4, "onclick", true);
        builder.AddEventStopPropagationAttribute(5, "ondblclick", true);
        builder.AddEventStopPropagationAttribute(6, "onkeydown", true);
        builder.AddContent(7, Fixed);
        builder.AddContent(8, Slot.Content);
        builder.CloseElement();
    }

    public void Dispose() => Slot.Changed -= StateHasChanged;
}
