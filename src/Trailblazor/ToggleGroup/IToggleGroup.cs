namespace Trailblazor;

// A toggle group and its items meet through this, so an item's value may be the group's type or its non-nullable form.
internal interface IToggleGroup
{
    Variant Variant { get; }

    Tone Tone { get; }

    Size Size { get; }

    bool Disabled { get; }

    bool IsPressed(object? value);

    Task ToggleAsync(object? value);

    // Throws when an item's value isn't the group's type, as the item registers.
    void Accept(object? value);
}
