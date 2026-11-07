namespace Trailblazor.Docs.Examples;

// Controls beside a section's demo that change its parameters, and the markup for the values chosen. {controls} in
// Markup is where those values go.
public sealed record ExampleControls(string Markup, params ExampleControl[] Controls)
{
    // Puts the values in a DialogOptions, for a component that takes its settings that way.
    public bool AsOptions { get; init; }

    public IReadOnlyDictionary<string, object?> Initial =>
        Controls.ToDictionary(control => control.Parameter, control => control.Value);

    public string MarkupFor(IReadOnlyDictionary<string, object?> values)
    {
        // A value left as it started stays out of the markup, except text, which has to match what the demo shows.
        var chosen = Controls.Where(control => values[control.Parameter] is string
            || (values[control.Parameter] is { } value && !value.Equals(control.Value)));
        var parts = chosen.Select(control => AsOptions ? Option(control.Parameter, values[control.Parameter]!)
            : Attribute(control.Parameter, values[control.Parameter]!)).ToList();
        var text = !AsOptions ? string.Concat(parts)
            : parts.Count == 0 ? "" : $" Options=\"new() {{ {string.Join(", ", parts)} }}\"";
        return Markup.Replace("{controls}", text);
    }

    private static string Attribute(string name, object value) => value switch
    {
        true => $" {name}",
        false => $" {name}=\"false\"",
        Enum chosen => $" {name}=\"{chosen.GetType().Name}.{chosen}\"",
        _ => $" {name}=\"{((string)value).Replace("\"", "&quot;")}\"",
    };

    private static string Option(string name, object value) => value is Enum chosen
        ? $"{name} = {chosen.GetType().Name}.{chosen}" : $"{name} = {((bool)value ? "true" : "false")}";
}

// One parameter a reader can change, and the value it starts with: a switch, a list of an enum's values, or text,
// where null is no text.
public sealed record ExampleControl(string Parameter, object? Value)
{
    public static ExampleControl Switch(string parameter, bool value = false) => new(parameter, value);

    public static ExampleControl Choice<TEnum>(string parameter, TEnum value) where TEnum : struct, Enum =>
        new(parameter, value);

    public static ExampleControl Text(string parameter, string? value = null) => new(parameter, value);
}
