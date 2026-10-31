using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace Trailblazor;

// The list renders before its tabs take new parameters, so each tab re-renders its own header when the header changes.
internal sealed class TabHeader : ComponentBase
{
    [Parameter, EditorRequired]
    public TbTab Tab { get; set; } = null!;

    internal void Refresh() => StateHasChanged();

    /// <inheritdoc />
    protected override void OnParametersSet() => Tab.HeaderView = this;

    // Built in code, so a plain header's text has no whitespace around it.
    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        if (Tab.Icon is { } icon)
        {
            builder.OpenComponent<TbIcon>(0);
            builder.AddComponentParameter(1, nameof(TbIcon.Icon), icon);
            builder.AddComponentParameter(2, nameof(TbIcon.Size), Size.Small);
            builder.AddAttribute(3, "data-part", "icon");
            builder.CloseComponent();
        }

        if (Tab.TextContent is null)
        {
            builder.AddContent(4, Tab.Text);
        }
        else
        {
            builder.AddContent(5, Tab.TextContent);
        }
    }
}
