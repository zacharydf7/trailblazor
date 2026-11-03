using System.Text.RegularExpressions;
using Trailblazor.Docs.Examples;

namespace Trailblazor.BrowserTests;

public class ExampleCatalogTests
{
    public static TheoryData<string> Names => new(ExampleCatalog.All.Select(example => example.Name));

    [Theory, MemberData(nameof(Names))]
    public void Example_ShouldHaveTheSourceOfEachOfItsFiles_WhenInTheCatalog(string name)
    {
        var example = ExampleCatalog.Find(name)!;

        Assert.All(example.Sources, source => Assert.Contains("<Tb", source.Text));
    }

    // Each section's id is an anchor on the page, so it must be unique and can't reuse one of the page's fixed ids.
    [Theory, MemberData(nameof(Names))]
    public void Example_ShouldGiveEachSectionAnIdOfItsOwn_WhenInTheCatalog(string name)
    {
        var ids = ExampleCatalog.Find(name)!.Shown.Select(section => section.Id).ToList();

        Assert.Equal(ids.Count, ids.Distinct().Count());
        Assert.DoesNotContain(ids, id => id is "accessibility" or "telemetry" or "design-tokens" or "testing");
    }

    [Theory, MemberData(nameof(Names))]
    public void Example_ShouldListTheTypeOfEachClassesParameter_WhenItsComponentsHaveOne(string name)
    {
        var example = ExampleCatalog.Find(name)!;

        var parts = example.Api.Select(type => type.GetProperty(nameof(TbButton.Classes))?.PropertyType).OfType<Type>();

        Assert.All(parts, type => Assert.Contains(type, example.Api));
    }

    [Fact]
    public void Steps_ShouldHoverTheOpenerFirst_WhenTheTargetOpensOnHover()
    {
        var steps = ExampleCatalog.Find("tooltip")!.Target!.Steps;

        Assert.Equal("await page.GetByRole(AriaRole.Button, new() { Name = \"Add\" }).HoverAsync();\n"
            + "page.GetByRole(AriaRole.Tooltip, new() { Name = \"Add an item\" })", steps);
    }

    [Fact]
    public void Steps_ShouldFilterByText_WhenTheTargetIsFoundByItsText()
    {
        var steps = ExampleCatalog.Find("alert")!.Target!.Steps;

        Assert.Equal("page.GetByRole(AriaRole.Alert).Filter(new() { HasText = \"A danger alert.\" })", steps);
    }

    // With no blocks, the page would list every token the theme has.
    [Theory, MemberData(nameof(Names))]
    public void Example_ShouldNameAtLeastOneBlockAndOnlyBlocksTheStylesheetHas_WhenInTheCatalog(string name)
    {
        var blocks = ExampleCatalog.Find(name)!.Blocks;

        using var stream = typeof(DesignTokens).Assembly.GetManifestResourceStream("Css/trailblazor.css")!;
        using var reader = new StreamReader(stream);
        var css = reader.ReadToEnd();

        Assert.NotEmpty(blocks);
        Assert.All(blocks, block => Assert.Matches($@"\.{block}(?:--|__|[^\w-])", css));
    }

    [Fact]
    public void ExampleCatalog_ShouldShowEveryEmbeddedSource_WhenCalled()
    {
        var shown = ExampleCatalog.All.SelectMany(example => example.Sources).Select(source => $"Source/{source.File}");

        var embedded = typeof(Example).Assembly.GetManifestResourceNames().Where(name => name.StartsWith("Source/"));

        Assert.Equal(embedded.Order(), shown.Order());
    }

    // TbRoot's live regions, which the testing guide names rather than a component's page.
    private static readonly string[] RootParts = ["announcer", "polite", "assertive"];

    // Parts a folder passes in by name rather than writing them as an attribute, found by that quoted name.
    private static readonly Dictionary<string, string[]> NamedParts = new()
    {
        ["Announcer"] = ["polite", "assertive"],
        ["ColorPicker"] = ["hue", "saturation", "brightness", "alpha", "red", "green", "blue"],
        ["Dialog"] = ["minimize", "maximize", "move", "resize"],
        ["Pagination"] = ["first", "previous", "next", "last"],
        ["Select"] = ["placeholder", "value"],
    };

    // Folders whose bases and children other components render, and the components whose pages may list their parts.
    private static readonly Dictionary<string, string[]> SharedFolders = new()
    {
        ["Button"] = ["icon-button"],
        ["Field"] =
        [
            "text-field", "number-field", "checkbox", "checkbox-group", "switch", "slider", "radio-group", "select",
            "autocomplete", "color-picker", "file-upload", "date-range-picker", "listbox",
        ],
        ["Select"] = ["select", "listbox"],
        ["Sortable"] = ["data-grid"],
    };

    [Fact]
    public void DataParts_ShouldNameEveryPartTheLibraryHasAndNoOthers_WhenComparedWithItsSource()
    {
        var documented = ExampleCatalog.All.SelectMany(example => example.DataParts).Concat(RootParts).ToHashSet();
        var inSource = SourceParts().SelectMany(folder => folder.Value).ToHashSet();

        Assert.Empty(inSource.Except(documented).Order());
        Assert.Empty(documented.Except(inSource).Order());
    }

    // A folder's parts belong to the components named after it, such as Checkbox's to checkbox and checkbox-group.
    [Fact]
    public void DataParts_ShouldListEachComponentsPartsAndNoOthers_WhenComparedWithItsFolder()
    {
        var folders = SourceParts();
        // Each component goes to the longest folder name its own starts with, so text-field is TextField's, not Text's.
        var owners = ExampleCatalog.All.ToLookup(example => folders.Keys
            .Where(folder => example.Name.Replace("-", "").StartsWith(folder, StringComparison.OrdinalIgnoreCase))
            .MaxBy(folder => folder.Length));
        var wrong = folders.SelectMany(folder =>
        {
            var examples = owners[folder.Key].ToList();
            if (examples.Count == 0)
            {
                return [];
            }

            var unlisted = folder.Value.Except(examples.SelectMany(example => example.DataParts))
                .Select(part => $"{folder.Key} has {part}");
            var gone = examples.SelectMany(example => example.DataParts.Except(folder.Value)
                .Except(SharedFolders.Where(shared => shared.Value.Contains(example.Name))
                    .SelectMany(shared => folders[shared.Key]))
                .Select(part => $"{example.Name} lists {part}"));
            return unlisted.Concat(gone);
        });

        Assert.Empty(wrong);
    }

    private static Dictionary<string, HashSet<string>> SourceParts()
    {
        var library = Path.Combine(Repository.Root, "src", "Trailblazor");
        // Markup, a render tree attribute and an attribute dictionary entry.
        var part = new Regex(@"data-part=""([a-z0-9-]+)""|""data-part"",\s*""([a-z0-9-]+)""|"
            + @"\[""data-part""\]\s*=\s*""([a-z0-9-]+)""");
        // Comments name parts too, as the Classes records' summaries do, without rendering them.
        var comments = new Regex(@"^\s*//[^\n]*|@\*[\s\S]*?\*@", RegexOptions.Multiline);
        return Directory.EnumerateFiles(library, "*.*", SearchOption.AllDirectories)
            .Where(file => file.EndsWith(".razor") || file.EndsWith(".cs"))
            .Select(file => Path.GetRelativePath(library, file).Split(Path.DirectorySeparatorChar))
            .Where(path => path.Length > 1 && path[0] is not ("obj" or "bin"))
            .GroupBy(path => path[0], path => comments.Replace(File.ReadAllText(Path.Combine([library, .. path])), ""))
            .ToDictionary(folder => folder.Key, folder => folder
                .SelectMany(text => part.Matches(text))
                .Select(match => match.Groups.Values.Skip(1).First(group => group.Success).Value)
                .Concat(NamedParts.GetValueOrDefault(folder.Key, [])
                    .Where(name => folder.Any(text => text.Contains($"\"{name}\""))))
                .ToHashSet());
    }
}
