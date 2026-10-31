namespace Trailblazor.Docs.Examples;

// A palette as the docs show it: its attribute value, its name and the summary its XML documentation gives.
public sealed record PaletteInfo(Palette Value)
{
    public string Name => Value.ToString().ToLowerInvariant();

    public string Title => Value.ToString();

    public string Summary => ApiDocs.Describe(Value);
}

// The library's palettes, in the order the enum lists them.
public static class PaletteCatalog
{
    public static IReadOnlyList<PaletteInfo> All { get; } =
        [.. Enum.GetValues<Palette>().Select(value => new PaletteInfo(value))];
}
