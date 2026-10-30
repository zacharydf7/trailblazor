using System.Reflection;
using System.Xml.Linq;

namespace Trailblazor.Tests.Contract;

// The docs site builds its API tables from these summaries, so every public type and property needs one.
public class DocumentationContractTests
{
    private static readonly Assembly Library = typeof(TbComponentBase).Assembly;

    private static readonly HashSet<string> Documented = XDocument
        .Load(Path.ChangeExtension(Library.Location, ".xml"))
        .Descendants("member")
        .Where(member => member.Element("summary") is not null || member.Element("inheritdoc") is not null)
        .Select(member => (string)member.Attribute("name")!)
        .ToHashSet();

    // Razor makes a public class of _Imports.razor, which isn't part of the library's API.
    public static TheoryData<string> Members => new(Library.GetExportedTypes()
        .Where(type => type.Name != "_Imports")
        .SelectMany(DocumentationIds));

    [Theory, MemberData(nameof(Members))]
    public void Member_ShouldHaveASummary_WhenPublic(string member)
    {
        Assert.Contains(member, Documented);
    }

    private static IEnumerable<string> DocumentationIds(Type type)
    {
        var name = type.FullName!.Replace('+', '.');
        // A .razor component's class summary lives in a partial .cs file beside it.
        yield return $"T:{name}";

        if (type.IsEnum)
        {
            yield break;
        }

        var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static
            | BindingFlags.DeclaredOnly);
        foreach (var property in properties)
        {
            // An indexer's documentation id lists its parameters' types.
            var index = property.GetIndexParameters().Select(parameter => parameter.ParameterType.FullName);
            yield return index.Any()
                ? $"P:{name}.{property.Name}({string.Join(",", index)})"
                : $"P:{name}.{property.Name}";
        }
    }
}
