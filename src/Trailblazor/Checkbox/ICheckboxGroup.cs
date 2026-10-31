namespace Trailblazor;

// What a TbCheckboxItem needs from its group, whatever the group's type, since Razor gives an item with a plain
// literal, such as Value="2", the literal's type and not the group's int?.
internal interface ICheckboxGroup
{
    IReadOnlyDictionary<string, object>? InputAttributes { get; }

    ChoiceClasses? Classes { get; }

    bool ReadOnly { get; }

    object? ItemName { get; }

    bool Invalid { get; }

    bool Accepts(object? item);

    string? FormatItem(object? item);

    bool IsChecked(object? item);

    Task SetCheckedAsync(object? item, bool isChecked);
}
