namespace Trailblazor;

// What a TbMenuRadioItem needs from its group, whatever the group's type, since Razor gives an item with a plain
// literal, such as Value="2", the literal's type and not the group's int?.
internal interface IMenuRadioGroup
{
    bool Accepts(object? value);

    bool IsChosen(object? value);

    Task ChooseAsync(object? value);
}
