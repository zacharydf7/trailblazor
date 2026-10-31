using Microsoft.AspNetCore.Components;

namespace Trailblazor;

// TbSelect and TbOption meet through these, so an option's value may be the select's type or its non-nullable form,
// as an enum option is in a select bound to a nullable enum.
internal interface ISelect
{
    bool IsSelected(object? value);

    string? Format(object? value);

    Task<bool> ChooseAsync(object? value, string input);

    void Add(ISelectOption option);

    void Remove(ISelectOption option);

    // The chosen option's content shows in the combobox, so options refresh it after they update.
    void Refresh();
}

internal interface ISelectOption
{
    // The option's element id, which its chip carries so the script can name it.
    string Id { get; }

    object? Value { get; }

    bool Disabled { get; }

    RenderFragment Content { get; }
}
