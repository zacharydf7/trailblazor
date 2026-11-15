using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Components;
using Trailblazor.Tests.Docs;

namespace Trailblazor.Tests.Contract;

// A component with two or more built-in strings takes them as a Labels record (#138), so an app can word each one.
public partial class LabelsRuleTests
{
    // Components that keep flat parameters or the app's localizer for their strings, and why.
    private static readonly Dictionary<string, string> Exceptions = new()
    {
        ["TbChip"] = "RemoveLabel words both forms of the remove button's name, with and without the chip's text",
        ["TbPopconfirm"] = "ConfirmText and CancelText are usually set per use, such as \"Delete\", so they stay flat "
            + "parameters",
        ["TbRoot"] = "it holds the app's one toast region, so the app's localizer words it",
    };

    // Keys a part falls back to from a Labels member it's handed under another name, as ItemMover's moved, and why.
    private static readonly Dictionary<string, string> HandedIn = new()
    {
        ["SortableMoved"] = "ItemMover takes SortableLabels.Moved as moved",
    };

    // Strings every component words through the app's localizer alone, and why.
    private static readonly Dictionary<string, string> AppWide = new()
    {
        ["FieldInvalid"] = "a value that doesn't convert reads the same in every field",
        ["DateHintDay"] = "a date format's hint letters, such as DD, read the same everywhere",
        ["DateHintMonth"] = "a date format's hint letters, such as MM, read the same everywhere",
        ["DateHintYear"] = "a date format's hint letters, such as YYYY, read the same everywhere",
    };

    // Components with a Labels record that doesn't yet word every string, and why.
    private static readonly Dictionary<string, string> Unworded = new()
    {
        ["TbDataGrid"] = "its record words a few strings; #451 gives it the rest",
        ["TbQueryBuilder"] = "its comparisons, date words and yes/no choices get members in #518",
    };

    // Internal parts that read strings for the components that render them.
    private static readonly Dictionary<string, string[]> Parts = new()
    {
        ["DialogHeader"] = ["TbDialog"],
        ["DialogMenus"] = ["TbDialog"],
        ["ToastFrame"] = ["TbRoot"],
        ["ToastHost"] = ["TbRoot"],
        ["MoveHandle"] = ["TbSortable", "TbDataGrid"],
        ["ItemMover"] = ["TbSortable", "TbDataGrid"],
        ["CalendarDates"] = ["TbCalendar", "TbDatePicker", "TbDateRangePicker", "TbDateTimePicker"],
        ["RichTextLinkDialog"] = ["TbRichTextEditor"],
        ["Comparisons"] = ["TbDataGrid", "TbQueryBuilder"],
    };

    // Services outside any component, and base classes whose strings every subclass shares.
    private static readonly Dictionary<string, string> Shared = new()
    {
        ["DialogService"] = "its callers pass the title and the buttons' text",
        ["ToastService"] = "its callers pass the toast's text; the hint that an action is available is app-wide",
        ["TbFieldBase"] = "RequiredMessage and InfoButtonLabel replace two of its messages; the invalid-value and "
            + "unnamed-field messages and the spoken \"required\" are app-wide, worded by the app's localizer",
    };

    private static readonly string Source = Path.Combine(Repository.Root, "src", "Trailblazor");

    [Fact]
    public void Components_ShouldTakeALabelsRecord_WhenTheyReadTwoOrMoreBuiltInStrings()
    {
        var files = SourceFiles().ToLookup(Owner);
        var components = typeof(TbButton).Assembly.GetExportedTypes()
            .Where(type => typeof(IComponent).IsAssignableFrom(type) && !type.IsAbstract)
            .Where(type => type.GetProperty("Labels") is null && !Exceptions.ContainsKey(NameOf(type)));

        var breaking = components.Select(type =>
                (Name: NameOf(type), Keys: KeysReadBy(ReadBy(type, files), AriaLabelFallback())))
            .Where(component => component.Keys.Count > 1)
            .Select(component => $"{component.Name} reads {string.Join(", ", component.Keys)}");

        Assert.Empty(breaking);
    }

    [Fact]
    public void Labels_ShouldWordEveryStringTheComponentReads_WhenItHasARecord()
    {
        var files = SourceFiles().ToLookup(Owner);
        var components = typeof(TbButton).Assembly.GetExportedTypes()
            .Where(type => typeof(IComponent).IsAssignableFrom(type) && !type.IsAbstract)
            .Where(type => type.GetProperty("Labels") is not null && !Unworded.ContainsKey(NameOf(type)));

        var unworded = components.SelectMany(type => KeysReadBy(ReadBy(type, files), MemberFallback())
            .Where(key => !AppWide.ContainsKey(key) && !HandedIn.ContainsKey(key))
            .Select(key => $"{NameOf(type)} reads {key}"));

        Assert.Empty(unworded);
    }

    [Fact]
    public void Parts_ShouldBeListed_WhenAFileOutsideAComponentReadsBuiltInStrings()
    {
        var unlisted = SourceFiles().Select(Owner).Distinct()
            .Where(name => !name.StartsWith("Tb", StringComparison.Ordinal) && !name.EndsWith("Labels")
                && !Parts.ContainsKey(name) && !Shared.ContainsKey(name))
            .Where(name => KeysReadBy(SourceFiles().Where(file => Owner(file) == name), AriaLabelFallback()).Count > 0);

        Assert.Empty(unlisted);
    }

    private static IEnumerable<string> SourceFiles() =>
        Directory.GetFiles(Source, "*.*", SearchOption.AllDirectories)
            .Where(file => file.EndsWith(".razor") || file.EndsWith(".cs"))
            .Where(file => !file.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}")
                && !file.Contains($"{Path.DirectorySeparatorChar}Localization{Path.DirectorySeparatorChar}"));

    // The type a file belongs to: TbDataGrid.Filters.cs and TbDataGrid.razor both belong to TbDataGrid.
    private static string Owner(string file) => Path.GetFileName(file).Split('.')[0];

    private static string NameOf(Type type) => type.Name.Split('`')[0];

    // A component's own files, its base classes' unless they're shared, and the internal parts it renders.
    private static IEnumerable<string> ReadBy(Type component, ILookup<string, string> files)
    {
        var names = new List<string>();
        for (var type = component; type is not null && type != typeof(ComponentBase); type = type.BaseType)
        {
            if (!Shared.ContainsKey(NameOf(type)))
            {
                names.Add(NameOf(type));
            }
        }

        names.AddRange(Parts.Where(part => part.Value.Contains(NameOf(component))).Select(part => part.Key));
        return names.SelectMany(name => files[name]);
    }

    // The keys the files read, leaving out doc references, keys that a parameter matched by `replaced` stands in for,
    // and keys on a line that reads a Labels member or on the line after one that reads no key itself.
    private static List<string> KeysReadBy(IEnumerable<string> files, Regex replaced)
    {
        var lines = files.Select(File.ReadAllLines).SelectMany(file => file.Where((line, at) =>
            !LabelsMember().IsMatch(line)
            && (at == 0 || !LabelsMember().IsMatch(file[at - 1]) || KeyRead().IsMatch(file[at - 1])))).ToList();
        var named = lines.SelectMany(line => replaced.Matches(line)).Select(match => match.Groups[1].Value)
            .ToHashSet();
        return lines.SelectMany(line => KeyRead().Matches(line)).Select(match => match.Groups[1].Value)
            .Where(key => !named.Contains(key)).Distinct().Order().ToList();
    }

    [GeneratedRegex("""(?<!cref=")TbStrings\.(\w+)""")]
    private static partial Regex KeyRead();

    [GeneratedRegex("""AriaLabel \?\? Localizer\[TbStrings\.(\w+)\]""")]
    private static partial Regex AriaLabelFallback();

    // A component's own parameter, such as Label or Placeholder, standing in for a key.
    [GeneratedRegex("""(?<![.\w])[A-Z]\w* \?\? Localizer\[TbStrings\.(\w+)\]""")]
    private static partial Regex MemberFallback();

    // A component's own Labels, or the labels a part is handed.
    [GeneratedRegex(@"\b[Ll]abels\.\w")]
    private static partial Regex LabelsMember();
}
