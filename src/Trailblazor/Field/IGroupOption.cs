namespace Trailblazor;

// What a radio group or a checkbox group reads from each of its options to offer them to smart paste.
internal interface IGroupOption
{
    string? Text { get; }

    object? Value { get; }

    bool Disabled { get; }
}
