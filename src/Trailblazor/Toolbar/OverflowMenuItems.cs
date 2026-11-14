using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace Trailblazor;

// A toolbar button or separator that moved into the overflow menu; a separator has no button.
internal sealed record OverflowItem(string Id, TbToolbarButton? Button);

// The menu items for the toolbar buttons that don't fit. A separate component, so the toolbar can refresh it without
// re-rendering its content and every button again. C#, so it can stay internal.
internal sealed class OverflowMenuItems : ComponentBase
{
    [Parameter, EditorRequired]
    public Func<IEnumerable<OverflowItem>> Items { get; set; } = default!;

    internal void Refresh() => StateHasChanged();

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        // A separator draws a line between items, so one at either end, or after another, is left out.
        var items = Items().ToList();
        var afterItem = false;
        for (var index = 0; index < items.Count; index++)
        {
            var (id, button) = items[index];
            if (button is null)
            {
                if (afterItem && items.Skip(index + 1).Any(item => item.Button is not null))
                {
                    builder.OpenComponent<TbMenuSeparator>(6);
                    builder.SetKey(id);
                    builder.CloseComponent();
                    afterItem = false;
                }

                continue;
            }

            afterItem = true;

            builder.OpenComponent<TbMenuItem>(0);
            builder.SetKey(id);
            builder.AddComponentParameter(1, nameof(TbMenuItem.Icon), button.Icon);
            builder.AddComponentParameter(2, nameof(TbMenuItem.Disabled), button.Disabled);
            builder.AddComponentParameter(3, nameof(TbMenuItem.OnClick), button.OnClick);
            builder.AddComponentParameter(4, nameof(TbMenuItem.TelemetryName), button.TelemetryName);
            builder.AddComponentParameter(5, nameof(TbMenuItem.ChildContent),
                (RenderFragment)(content => content.AddContent(0, button.Name)));
            builder.CloseComponent();
        }
    }
}
