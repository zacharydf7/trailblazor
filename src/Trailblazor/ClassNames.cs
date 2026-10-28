namespace Trailblazor;

internal static class ClassNames
{
    public static string Join(params string?[] names) =>
        string.Join(' ', names.Where(name => !string.IsNullOrWhiteSpace(name)));

    // Modifier classes use the enum member's name in lowercase, e.g. Tone.Danger becomes "danger".
    public static string Modifier<TEnum>(string block, TEnum value) where TEnum : struct, Enum =>
        $"{block}--{value.ToString().ToLowerInvariant()}";
}
