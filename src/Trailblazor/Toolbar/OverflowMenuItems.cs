using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace Trailblazor;

// The menu items for the toolbar buttons with no room. A separate component, so the toolbar can refresh it without
// re-rendering its content and every button again. C#, so it can stay internal.
internal sealed class OverflowMenuItems : ComponentBase
{
    [Parameter, EditorRequired]
    public Func<IEnumerable<TbToolbarButton>> Buttons { get; set; } = default!;

    internal void Refresh() => StateHasChanged();

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        foreach (var button in Buttons())
        {
            builder.OpenComponent<TbMenuItem>(0);
            builder.SetKey(button.Id);
            builder.AddComponentParameter(1, nameof(TbMenuItem.Icon), button.Icon);
            builder.AddComponentParameter(2, nameof(TbMenuItem.Disabled), button.Disabled);
            builder.AddComponentParameter(3, nameof(TbMenuItem.OnClick), button.OnClick);
            builder.AddComponentParameter(4, nameof(TbMenuItem.ChildContent),
                (RenderFragment)(content => content.AddContent(0, button.Text)));
            builder.CloseComponent();
        }
    }
}
