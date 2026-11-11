using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;

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
        var classes = Tab.TabsClasses;
        if (Tab.Icon is { } icon)
        {
            builder.OpenComponent<TbIcon>(0);
            builder.AddComponentParameter(1, nameof(TbIcon.Icon), icon);
            builder.AddComponentParameter(2, nameof(TbIcon.Size), Size.Small);
            builder.AddComponentParameter(3, nameof(TbIcon.Class), classes?.Icon);
            builder.AddAttribute(4, "data-part", "icon");
            builder.CloseComponent();
        }

        if (Tab.TextContent is null)
        {
            builder.AddContent(5, Tab.Text);
        }
        else
        {
            builder.AddContent(6, Tab.TextContent);
        }

        // A tab can't hold a button, so the mark serves the pointer, and the keys in aria-keyshortcuts the keyboard.
        if (Tab.CanClose)
        {
            builder.OpenElement(7, "span");
            builder.AddAttribute(8, "data-part", "close");
            builder.AddAttribute(9, "aria-hidden", "true");
            builder.AddAttribute(10, "class", ClassNames.Join("tb-tabs__close", classes?.Close));
            builder.AddAttribute(11, "onclick", EventCallback.Factory.Create(this, Tab.CloseAsync));
            builder.AddEventStopPropagationAttribute(12, "onclick", true);
            builder.OpenComponent<TbIcon>(13);
            builder.AddComponentParameter(14, nameof(TbIcon.Icon), BuiltInIcons.Close);
            builder.AddComponentParameter(15, nameof(TbIcon.Size), Size.Small);
            builder.CloseComponent();
            builder.CloseElement();
        }
    }
}
