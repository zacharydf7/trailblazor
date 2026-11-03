using System.Reflection;

namespace Trailblazor.Docs.Examples;

// A palette as the docs show it: its attribute value, its name and the summary its XML documentation gives.
public sealed record PaletteInfo(Palette Value)
{
    public string Name => Value.Name;

    public string Title => char.ToUpperInvariant(Name[0]) + Name[1..];

    public string Summary => ApiDocs.Describe(typeof(Palette).GetProperties(BindingFlags.Public | BindingFlags.Static)
        .Single(property => property.PropertyType == typeof(Palette) && Equals(property.GetValue(null), Value)));
}

// The library's palettes, in the order Palette.BuiltIn lists them.
public static class PaletteCatalog
{
    public static IReadOnlyList<PaletteInfo> All { get; } =
        [.. Palette.BuiltIn.Select(value => new PaletteInfo(value))];
}
