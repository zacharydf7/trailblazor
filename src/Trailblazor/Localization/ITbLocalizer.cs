namespace Trailblazor;

/// <summary>Gives the text of Trailblazor's built-in strings, such as button names and messages, by the keys in
/// <see cref="TbStrings"/>. The default, <see cref="TbLocalizer"/>, answers in English with the app's overrides from
/// <see cref="TrailblazorOptions.Strings"/>; register your own to translate them, for example from resource files for
/// the current culture.</summary>
public interface ITbLocalizer
{
    /// <summary>The text for <paramref name="key"/>. Placeholders such as <c>{0}</c> are filled in by the component
    /// that asks, as each key's summary in <see cref="TbStrings"/> describes.</summary>
    string this[string key] { get; }

    /// <summary>The text for <paramref name="key"/> in the form that fits <paramref name="count"/>, for a message whose
    /// words change with the number, such as "1 file chosen." and "3 files chosen." Keys asked for this way say so in
    /// <see cref="TbStrings"/>. By default, a count of 1 gives the text of the key's one-form if it has one, such as
    /// <see cref="TbStrings.FileUploadChosenOne"/>, and any other count gives the key's own text. Implement it to apply
    /// a language's own plural rules, such as Russian's three forms.</summary>
    string Plural(string key, int count) => this[count == 1 ? TbStrings.OneFormOf(key) : key];
}
