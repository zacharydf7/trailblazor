using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;

namespace Trailblazor;

// The buttons of dialog content written in C#, as a <TbButton> would be in markup.
internal static class DialogButtons
{
    public static void Add(RenderTreeBuilder builder, int sequence, object receiver, string text, Variant variant,
        Tone tone, Func<Task> onClick)
    {
        builder.OpenComponent<TbButton>(sequence);
        builder.AddComponentParameter(sequence + 1, nameof(TbButton.Variant), variant);
        builder.AddComponentParameter(sequence + 2, nameof(TbButton.Tone), tone);
        builder.AddComponentParameter(sequence + 3, nameof(TbButton.OnClick),
            EventCallback.Factory.Create<MouseEventArgs>(receiver, onClick));
        builder.AddComponentParameter(sequence + 4, nameof(TbButton.ChildContent),
            (RenderFragment)(content => content.AddContent(0, text)));
        builder.CloseComponent();
    }
}
