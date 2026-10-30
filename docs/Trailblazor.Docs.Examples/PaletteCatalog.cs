namespace Trailblazor.Docs.Examples;

public sealed record Palette(string Name, string Title, string Summary);

// The library's palettes, in the order the docs list them.
public static class PaletteCatalog
{
    public static IReadOnlyList<Palette> All { get; } =
    [
        new("graphite", "Graphite", "Near-monochrome grays with ink-colored primary actions. The default."),
        new("cobalt", "Cobalt", "Cool grays with blue primary actions."),
        new("slate", "Slate", "Blue-tinted grays with slate ink for primary actions."),
        new("sand", "Sand", "Warm, earthy grays with brown-black ink for primary actions."),
        new("indigo", "Indigo", "Cool grays with deep indigo."),
        new("violet", "Violet", "Lilac-tinted grays with violet."),
        new("plum", "Plum", "Lilac-tinted grays with a dusky, muted purple."),
        new("fuchsia", "Fuchsia", "Lilac-tinted grays with a vivid, saturated fuchsia."),
        new("rose", "Rose", "Warm grays with rose pink."),
        new("clay", "Clay", "Warm, earthy grays with a muted terracotta."),
        new("moss", "Moss", "Green-tinted grays with an earthy olive green."),
        new("teal", "Teal", "Green-tinted grays with teal."),
    ];
}
