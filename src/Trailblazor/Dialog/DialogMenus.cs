using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;

namespace Trailblazor;

// The Move and Resize menus. They let a single click or tap place and size a dialog, with no dragging (WCAG 2.5.7),
// and the script moves and resizes it with the arrow keys while their buttons have focus.
internal static partial class DialogMenus
{
    // Full is left out: it fills the window, which is what Maximize is for.
    private static readonly DialogSize[] Sizes =
        [DialogSize.Small, DialogSize.Medium, DialogSize.Large, DialogSize.ExtraLarge];

    public static void AddMove(RenderTreeBuilder builder, int sequence, DialogReference dialog) =>
        AddMenu(builder, sequence, dialog, "Move", BuiltInIcons.Move, "Arrow keys move the dialog.", "move", null,
            Enum.GetValues<DialogPosition>(), dialog.MoveTo);

    public static void AddResize(RenderTreeBuilder builder, int sequence, DialogReference dialog) =>
        AddMenu(builder, sequence, dialog, "Resize", BuiltInIcons.ResizeGrip, "Arrow keys resize the dialog.", "resize",
            "tb-dialog__resize", Sizes, dialog.ResizeTo);

    private static void AddMenu<T>(RenderTreeBuilder builder, int sequence, DialogReference dialog, string label,
        Icon icon, string description, string part, string? cssClass, IEnumerable<T> choices, Action<T> choose)
        where T : struct, Enum
    {
        builder.OpenComponent<TbMenu>(sequence);
        builder.AddComponentParameter(sequence + 1, nameof(TbMenu.Label), label);
        builder.AddComponentParameter(sequence + 2, nameof(TbMenu.Icon), icon);
        builder.AddComponentParameter(sequence + 3, nameof(TbMenu.Description), description);
        builder.AddComponentParameter(sequence + 4, nameof(TbMenu.Size), Size.Small);
        builder.AddComponentParameter(sequence + 5, nameof(TbMenu.Class), cssClass);
        builder.AddAttribute(sequence + 6, "data-part", part);
        builder.AddComponentParameter(sequence + 7, nameof(TbMenu.ChildContent), (RenderFragment)(items =>
        {
            foreach (var choice in choices)
            {
                items.OpenComponent<TbMenuItem>(0);
                items.SetKey(choice);
                items.AddComponentParameter(1, nameof(TbMenuItem.OnClick),
                    EventCallback.Factory.Create<MouseEventArgs>(dialog, () => choose(choice)));
                items.AddComponentParameter(2, nameof(TbMenuItem.ChildContent), (RenderFragment)(text =>
                    text.AddContent(0, Words(choice.ToString()))));
                items.CloseComponent();
            }
        }));
        builder.CloseComponent();
    }

    // TopStart reads as "Top start" and ExtraLarge as "Extra large".
    private static string Words(string name) =>
        WordStart().Replace(name, match => $" {match.Value.ToLowerInvariant()}");

    [GeneratedRegex("(?<!^)[A-Z]")]
    private static partial Regex WordStart();
}
