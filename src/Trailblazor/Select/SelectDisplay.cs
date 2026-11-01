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

    // Chosen options show as chips, each with a pointer-only remove button the select's script handles; the keyboard
    // removes them with Backspace.
    [Parameter]
    public bool Chips { get; set; }

    // The text for a chosen value that no option has.
    [Parameter]
    public Func<string?>? Unmatched { get; set; }

    // A disabled select's chips, and a disabled option's chip, can't be removed.
    [Parameter]
    public bool Disabled { get; set; }

    internal void Refresh() => StateHasChanged();

    // Content an option names by its Text is hidden from screen readers, which read the Text instead.
    private static void AddContent(RenderTreeBuilder builder, ISelectOption option)
    {
        if (option.Name is not { } name)
        {
            builder.AddContent(0, option.Content);
            return;
        }

        builder.OpenElement(1, "span");
        builder.AddAttribute(2, "aria-hidden", "true");
        builder.AddContent(3, option.Content);
        builder.CloseElement();
        builder.OpenElement(4, "span");
        builder.AddAttribute(5, "class", "tb-visually-hidden");
        builder.AddContent(6, name);
        builder.CloseElement();
    }

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        var selected = Selected();
        if (Chips && selected.Count > 0)
        {
            BuildChips(builder, selected);
            return;
        }

        var unmatched = selected.Count == 0 ? Unmatched?.Invoke() : null;
        var empty = selected.Count == 0 && unmatched is null;
        builder.OpenElement(0, "span");
        builder.AddAttribute(1, "data-part", empty ? "placeholder" : "value");
        builder.AddAttribute(2, "class", empty ? "tb-select__placeholder" : "tb-select__value");
        builder.AddContent(3, empty ? Placeholder : unmatched);

        for (var index = 0; index < selected.Count; index++)
        {
            if (index > 0)
            {
                builder.AddContent(4, ", ");
            }

            var option = selected[index];
            builder.AddContent(5, content => AddContent(content, option));
        }

        builder.CloseElement();
    }

    private void BuildChips(RenderTreeBuilder builder, IReadOnlyList<ISelectOption> selected)
    {
        builder.OpenElement(0, "span");
        builder.AddAttribute(1, "data-part", "chips");
        builder.AddAttribute(2, "class", "tb-select__chips");
        foreach (var option in selected)
        {
            builder.OpenElement(3, "span");
            builder.SetKey(option);
            builder.AddAttribute(4, "data-part", "chip");
            builder.AddAttribute(5, "data-option", option.Id);
            builder.AddAttribute(6, "class", "tb-select__chip");
            builder.AddContent(7, content => AddContent(content, option));
            if (!Disabled && !option.Disabled)
            {
                builder.OpenElement(8, "span");
                builder.AddAttribute(9, "data-part", "chip-remove");
                builder.AddAttribute(10, "aria-hidden", "true");
                builder.AddAttribute(11, "class", "tb-select__chip-remove");
                builder.OpenComponent<TbIcon>(12);
                builder.AddComponentParameter(13, nameof(TbIcon.Icon), BuiltInIcons.Close);
                builder.AddComponentParameter(14, nameof(TbIcon.Size), Size.Small);
                builder.CloseComponent();
                builder.CloseElement();
            }

            builder.CloseElement();
        }

        builder.CloseElement();
    }
}
