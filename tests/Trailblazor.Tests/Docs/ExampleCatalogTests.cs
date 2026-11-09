using System.Reflection;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Components;
using Trailblazor.Docs.Examples;

namespace Trailblazor.Tests.Docs;

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
        var ids = ExampleCatalog.Find(name)!.Sections.Select(section => section.Id).ToList();

        Assert.Equal(ids.Count, ids.Distinct().Count());
        Assert.Empty(ids.Intersect(["usage", "when-to-use", "when-not-to-use", "accessibility", "keyboard", "labeling",
            "announcements", "what-it-does", "app-duties", "before-interactive", "notes", "telemetry", "design-tokens",
            "testing", "related"]));
    }

    [Theory, MemberData(nameof(Names))]
    public void Example_ShouldNameOnlyComponentsInTheCatalog_WhenItListsRelatedOnes(string name)
    {
        var example = ExampleCatalog.Find(name)!;

        Assert.All(example.Related, related => Assert.NotNull(ExampleCatalog.Find(related)));
        Assert.DoesNotContain(name, example.Related);
    }

    // A link in catalog text, written [text](components/name) or [text](guides/name), must reach a page the site has.
    [Theory, MemberData(nameof(Names))]
    public void Example_ShouldLinkOnlyToPagesTheSiteHas_WhenItsTextHasLinks(string name)
    {
        var example = ExampleCatalog.Find(name)!;
        IEnumerable<string?> texts = [.. example.Accessibility, .. example.WhenToUse, .. example.WhenNotToUse,
            .. example.LabelingNotes, .. example.Announcements, .. example.AppDuties, .. example.BeforeInteractive,
            .. example.NotesShown.Select(note => note.Text), .. example.Keyboard.Select(row => row.Action),
            .. example.Keyboard.Select(row => row.When), .. example.TelemetryNotes,
            .. example.Sections.Select(section => section.Description), example.TestingTip, .. example.TestingTips,
            example.RightToLeftKeys];

        // Matches the path of every link DocsText renders, and anything else written like one.
        var paths = texts.OfType<string>().SelectMany(text => Regex.Matches(text, @"\]\(([^)\s]+)\)"))
            .Select(match => match.Groups[1].Value.Split('#')[0]);

        Assert.All(paths, path => Assert.True(
            path.StartsWith("components/") && ExampleCatalog.Find(path["components/".Length..]) is not null
                || GuideCatalog.All.Any(guide => guide.Path == path), $"{path} isn't a page the site has."));
    }

    [Theory, MemberData(nameof(Names))]
    public void Example_ShouldLinkAnAuthoringPracticesPattern_WhenItNamesOne(string name)
    {
        if (ExampleCatalog.Find(name)!.Pattern is { } pattern)
        {
            Assert.StartsWith("https://www.w3.org/WAI/ARIA/apg/patterns/", pattern.Url);
        }
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

        Assert.Equal("page.GetByRole(AriaRole.Alert).Filter(new() { HasText = \"Danger alert.\" })", steps);
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
    public void ExampleCatalog_ShouldGiveEveryPageItsUsageSectionsAndRelated_WhenCalled()
    {
        var incomplete = ExampleCatalog.All
            .Where(example => example.WhenToUse.Count == 0 || example.Sections.Count == 0 || example.Related.Count == 0);

        Assert.Empty(incomplete.Select(example => example.Name));
    }

    [Fact]
    public void ExampleCatalog_ShouldShowEveryEmbeddedSource_WhenCalled()
    {
        // Sections can share a file, such as a dialog that two of them open.
        var shown = ExampleCatalog.All.SelectMany(example => example.Sources).Select(source => $"Source/{source.File}")
            .Distinct().Concat(GuideSources());

        var embedded = typeof(Example).Assembly.GetManifestResourceNames().Where(name => name.StartsWith("Source/"));

        Assert.Equal(embedded.Order(), shown.Order());
    }

    // The example components a guide or recipe page shows the source of, which it names as typeof(RecipeEditExample).
    private static IEnumerable<string> GuideSources()
    {
        var assembly = typeof(Example).Assembly;
        var pages = assembly.GetManifestResourceNames().Where(name => name.StartsWith("Guides/")).Select(name =>
        {
            using var reader = new StreamReader(assembly.GetManifestResourceStream(name)!);
            return reader.ReadToEnd();
        });
        var shown = ExampleCatalog.All.SelectMany(example => example.Sources).Select(source => source.File).ToHashSet();

        return pages.SelectMany(page => Regex.Matches(page, @"ExampleSource\.Of\(typeof\((\w+)\)\)"))
            .Select(match => $"{match.Groups[1].Value}.razor").Distinct().Where(file => !shown.Contains(file))
            .Select(file => $"Source/{file}");
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
        ["Dialog"] = ["drawer"],
        ["Field"] =
        [
            "text-field", "number-field", "checkbox", "checkbox-group", "switch", "slider", "radio-group", "select",
            "autocomplete", "color-picker", "file-upload", "date-range-picker", "listbox", "rich-text-editor",
        ],
        ["Select"] = ["select", "listbox"],
        ["Sortable"] = ["data-grid"],
    };

    [Fact]
    public void DataParts_ShouldDescribeEachPartOnceInASentence_WhenAComponentHasParts()
    {
        Assert.All(ExampleCatalog.All, example =>
        {
            Assert.Equal(example.DataParts.Count, example.DataParts.DistinctBy(part => part.Name).Count());
            Assert.All(example.DataParts, part => Assert.Matches(@"^[A-Z].*\.$", part.Description));
        });
    }

    [Fact]
    public void Anatomy_ShouldBeGiven_WhenTheComponentIsMadeOfParts()
    {
        string[] composed = ["dialog", "select", "listbox", "tabs", "menu", "accordion", "toolbar", "data-grid",
            "tree-view", "radio-group", "checkbox-group", "splitter", "toggle-group", "grid", "chip", "list",
            "table-of-contents", "button-group", "breadcrumbs", "card", "drawer"];

        Assert.All(composed, name => Assert.NotNull(ExampleCatalog.Find(name)!.Anatomy));
    }

    [Fact]
    public void Anatomy_ShouldNameOnlyComponentsTheLibraryHas_WhenGiven()
    {
        var components = typeof(TbButton).Assembly.GetTypes().Select(type => type.Name.Split('`')[0]).ToHashSet();

        var named = ExampleCatalog.All.Where(example => example.Anatomy is not null)
            .SelectMany(example => Regex.Matches(example.Anatomy!, @"(?<=<|@inherits )(Tb\w+)"))
            .Select(match => match.Value);

        Assert.All(named, name => Assert.Contains(name, components));
    }

    [Fact]
    public void Anatomy_ShouldUseOnlyParametersTheComponentsHave_WhenGiven()
    {
        var components = typeof(TbButton).Assembly.GetTypes().Where(type => type.IsAssignableTo(typeof(IComponent)))
            .ToLookup(type => type.Name.Split('`')[0]);
        HashSet<string> Parameters(string component) => [.. components[component].SelectMany(type => type
            .GetProperties().Where(property => property.IsDefined(typeof(ParameterAttribute)))
            .Select(property => property.Name))];
        string[] typeArguments = ["TItem", "TKey", "TValue"];
        // An aria-* attribute passes through to the component's outer element, but if the component has the matching
        // parameter (AriaLabel for aria-label), the anatomy has to use the parameter instead.
        bool Allowed(string component, string attribute) => Parameters(component).Contains(attribute)
            || (attribute.StartsWith("aria-") && !Parameters(component).Any(parameter =>
                parameter.Equals(attribute.Replace("-", ""), StringComparison.OrdinalIgnoreCase)));

        var wrong = ExampleCatalog.All.Where(example => example.Anatomy is not null).SelectMany(example =>
        {
            // A < right after a name, as in TbDialogContent<TResult>, doesn't start a tag; a quoted value can hold >.
            var tags = Regex.Matches(example.Anatomy!,
                @"(?<!\w)<(?<name>[A-Z]\w*)(?<attributes>(?:""[^""]*""|[^>""])*)>").ToList();
            var named = tags.Select(tag => tag.Groups["name"].Value).Where(name => name.StartsWith("Tb")).ToList();
            return tags.SelectMany(tag =>
            {
                var name = tag.Groups["name"].Value;
                // A tag that isn't a component, such as <Cell>, is a fragment parameter of a component around it.
                if (!name.StartsWith("Tb"))
                {
                    return named.Any(component => Parameters(component).Contains(name))
                        ? [] : [$"{example.Name}: {name}"];
                }

                var attributes = Regex.Replace(tag.Groups["attributes"].Value, "\"[^\"]*\"", "")
                    .Split([' ', '/'], StringSplitOptions.RemoveEmptyEntries)
                    .Select(attribute => attribute.TrimEnd('=').Replace("@bind-", ""));
                return attributes.Except(typeArguments).Where(attribute => !Allowed(name, attribute))
                    .Select(attribute => $"{example.Name}: {name}.{attribute}");
            });
        });

        Assert.Empty(wrong);
    }

    [Fact]
    public void DataParts_ShouldNameEveryPartTheLibraryHasAndNoOthers_WhenComparedWithItsSource()
    {
        var documented = ExampleCatalog.All.SelectMany(example => example.DataParts).Select(part => part.Name)
            .Concat(RootParts).ToHashSet();
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

            var unlisted = folder.Value
                .Except(examples.SelectMany(example => example.DataParts).Select(part => part.Name))
                .Select(part => $"{folder.Key} has {part}");
            var gone = examples.SelectMany(example => example.DataParts.Select(part => part.Name).Except(folder.Value)
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
