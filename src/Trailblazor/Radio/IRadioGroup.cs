namespace Trailblazor;

// What a TbRadio needs from its group, whatever the group's type, since Razor gives a radio with a plain literal, such
// as Value="2", the literal's type and not the group's int?.
internal interface IRadioGroup
{
    IReadOnlyDictionary<string, object>? InputAttributes { get; }

    ChoiceClasses? Classes { get; }

    bool ReadOnly { get; }

    string GroupName { get; }

    bool Accepts(object? value);

    string? FormatOption(object? value);

    bool IsSelected(object? value);

    Task SelectAsync(object? value);
}
