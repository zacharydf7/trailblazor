namespace Trailblazor.Docs.Examples;

public sealed record Example(string Name, string Title, Type Component);

// Every docs example. The docs site shows them, and the browser tests run axe against each one.
public static class ExampleCatalog
{
    public static IReadOnlyList<Example> All { get; } =
    [
        new("text", "Text", typeof(TextExample)),
        new("divider", "Divider", typeof(DividerExample)),
        new("surface", "Surface", typeof(SurfaceExample)),
    ];

    public static Example? Find(string name) => All.FirstOrDefault(example => example.Name == name);
}
