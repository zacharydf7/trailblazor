using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace Trailblazor;

/// <summary>An on/off switch for a setting that takes effect at once. It is a checkbox that screen readers announce as
/// a switch.</summary>
public sealed class TbSwitch : TbCheckbox<bool>
{
    private protected override string Block => "tb-switch";

    private protected override string? Role => "switch";

    /// <summary>An icon in the thumb while the switch is on, such as a check, so the state shows by more than color
    /// and position. Screen readers skip it, since the switch already says whether it's on.</summary>
    [Parameter]
    public Icon? ThumbIcon { get; set; }

    /// <summary>An icon in the thumb while the switch is off, such as a cross.</summary>
    [Parameter]
    public Icon? OffThumbIcon { get; set; }

    // Both render, and the input's checked state picks which shows, so the icon changes as soon as the thumb moves.
    private protected override RenderFragment? OverInput => ThumbIcon is null && OffThumbIcon is null ? null
        : builder =>
        {
            AddThumbIcon(builder, 0, ThumbIcon, "on");
            AddThumbIcon(builder, 10, OffThumbIcon, "off");
        };

    private static void AddThumbIcon(RenderTreeBuilder builder, int sequence, Icon? icon, string state)
    {
        if (icon is null)
        {
            return;
        }

        builder.OpenElement(sequence, "span");
        builder.AddAttribute(sequence + 1, "data-part", "thumb-icon");
        builder.AddAttribute(sequence + 2, "data-state", state);
        builder.AddAttribute(sequence + 3, "aria-hidden", "true");
        builder.AddAttribute(sequence + 4, "class", $"tb-switch__thumb-icon tb-switch__thumb-icon--{state}");
        builder.OpenComponent<TbIcon>(sequence + 5);
        builder.AddComponentParameter(sequence + 6, nameof(TbIcon.Icon), icon);
        builder.CloseComponent();
        builder.CloseElement();
    }
}
