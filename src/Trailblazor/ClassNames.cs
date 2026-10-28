namespace Trailblazor;

internal static class ClassNames
{
    public static string Join(params string?[] names) =>
        string.Join(' ', names.Where(name => !string.IsNullOrWhiteSpace(name)));

    // Modifier classes use the enum member's name in lowercase, e.g. Tone.Danger becomes "danger".
    public static string Modifier<TEnum>(string block, TEnum value) where TEnum : struct, Enum =>
        $"{block}--{value.ToString().ToLowerInvariant()}";

    // Key-value modifiers, e.g. ("tb-stack", "align", Align.Center) becomes "tb-stack--align-center".
    public static string Modifier<TEnum>(string block, string key, TEnum value) where TEnum : struct, Enum =>
        $"{block}--{key}-{value.ToString().ToLowerInvariant()}";
}
