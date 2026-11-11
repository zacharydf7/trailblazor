namespace Trailblazor.Tests;

// A localizer whose Plural returns the key and count it was asked for, so a test can see that a component called it.
internal sealed class PluralRecorder : ITbLocalizer
{
    public string this[string key] => TbStrings.English(key);

    public string Plural(string key, int count) => $"{key} for {count}";
}
