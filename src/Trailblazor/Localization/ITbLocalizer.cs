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
}
