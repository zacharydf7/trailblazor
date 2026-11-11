using Microsoft.AspNetCore.Components;

namespace Trailblazor;

// An option made from one of a select's Items, so a long list keeps no component per option. Text names it, and
// Content shows in the list and, once chosen, in the select.
internal sealed class ItemOption(string id, object? value, string text, bool disabled, RenderFragment content,
    string? name) : ISelectOption
{
    public string Id => id;

    public object? Value => value;

    public string Text => text;

    public bool Disabled => disabled;

    public RenderFragment Content => content;

    public string? Name => name;
}
