using System.Collections.Concurrent;

namespace Trailblazor;

// These run on every element of every render, so they allocate only the string returned and build each modifier once.
internal static class ClassNames
{
    private const int MaxStackChars = 512;

    public static string Join(params ReadOnlySpan<string?> names) => Joined(names, " ") ?? "";

    // Modifier classes use the enum member's name in lowercase, e.g. Tone.Danger becomes "danger".
    public static string Modifier<TEnum>(string block, TEnum value) where TEnum : struct, Enum =>
        Modifiers<TEnum>.Plain.GetOrAdd((block, value), static key => $"{key.Block}--{Lower(key.Value)}");

    // Key-value modifiers, e.g. ("tb-stack", "align", Align.Center) becomes "tb-stack--align-center".
    public static string Modifier<TEnum>(string block, string key, TEnum value) where TEnum : struct, Enum =>
        Modifiers<TEnum>.Keyed.GetOrAdd((block, key, value),
            static parts => $"{parts.Block}--{parts.Key}-{Lower(parts.Value)}");

    // Joins the non-blank parts with the separator; null if all are blank, and a lone part comes back as is.
    internal static string? Joined(ReadOnlySpan<string?> parts, string separator)
    {
        var length = 0;
        var count = 0;
        string? only = null;
        foreach (var part in parts)
        {
            if (!string.IsNullOrWhiteSpace(part))
            {
                length += part.Length;
                count++;
                only = part;
            }
        }

        if (count <= 1)
        {
            return only;
        }

        // Class and style lists are short, so they're usually built on the stack and copied into the returned string.
        var total = length + (count - 1) * separator.Length;
        var chars = total <= MaxStackChars ? stackalloc char[total] : new char[total];
        var at = 0;
        foreach (var part in parts)
        {
            if (string.IsNullOrWhiteSpace(part))
            {
                continue;
            }

            if (at > 0)
            {
                separator.AsSpan().CopyTo(chars[at..]);
                at += separator.Length;
            }

            part.AsSpan().CopyTo(chars[at..]);
            at += part.Length;
        }

        return new string(chars);
    }

    private static string Lower<TEnum>(TEnum value) where TEnum : struct, Enum => value.ToString().ToLowerInvariant();

    private static class Modifiers<TEnum> where TEnum : struct, Enum
    {
        public static readonly ConcurrentDictionary<(string Block, TEnum Value), string> Plain = new();

        public static readonly ConcurrentDictionary<(string Block, string Key, TEnum Value), string> Keyed = new();
    }
}
