namespace Trailblazor;

/// <summary>A form field smart paste can fill, as its <see cref="TbSmartPaste.Extract"/> model sees it.</summary>
/// <param name="Name">The field's label, which the model answers under. A label two fields share gets a number, such
/// as "Phone (2)".</param>
/// <param name="Description">The field's helper text, if any, to tell the model more about what goes in it.</param>
/// <param name="Type">The field's value type, such as <see cref="string"/>, <see cref="int"/> or
/// <see cref="DateOnly"/>.</param>
public sealed record SmartPasteField(string Name, string? Description, Type Type)
{
    private static readonly HashSet<Type> Simple =
    [
        typeof(string), typeof(bool), typeof(byte), typeof(short), typeof(int), typeof(long), typeof(float),
        typeof(double), typeof(decimal), typeof(DateTime), typeof(DateTimeOffset), typeof(DateOnly), typeof(TimeOnly),
        typeof(Guid),
    ];

    /// <summary>The texts of the options to choose from, for a field that picks from options, such as a select, a
    /// radio group or a cascader, whose choices are whole paths; null for any other field. The model answers with one
    /// of them, in any letter case, and an answer that is none of them is skipped.</summary>
    public IReadOnlyList<string>? Choices { get; init; }

    /// <summary>Whether the field takes several values: any number of its <see cref="Choices"/>, as a multiple select
    /// or a checkbox group does, or any number of texts, as a tag input does. The model then answers with each one on
    /// a line of its own.</summary>
    public bool Multiple { get; init; }

    /// <summary>Whether the other field has the same name, description, type, choices and <see cref="Multiple"/>,
    /// with the choices compared item by item, so a test can compare a field with the one it expects.</summary>
    public bool Equals(SmartPasteField? other) => other is not null && Name == other.Name
        && Description == other.Description && Type == other.Type && Multiple == other.Multiple
        && (Choices is null ? other.Choices is null
            : other.Choices is not null && Choices.SequenceEqual(other.Choices));

    /// <summary>A hash code that equal fields share.</summary>
    public override int GetHashCode() => HashCode.Combine(Name, Description, Type, Multiple, Choices?.Count);

    // Whether a field of this value type holds one value that text can say.
    internal static bool Takes(Type type) => Simple.Contains(Nullable.GetUnderlyingType(type) ?? type);
}
