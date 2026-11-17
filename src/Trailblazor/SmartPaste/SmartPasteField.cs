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

    // Whether a field of this value type holds one value that text can say.
    internal static bool Takes(Type type) => Simple.Contains(Nullable.GetUnderlyingType(type) ?? type);
}
