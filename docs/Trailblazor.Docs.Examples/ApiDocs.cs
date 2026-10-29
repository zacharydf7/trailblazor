using System.Globalization;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using Microsoft.AspNetCore.Components;

namespace Trailblazor.Docs.Examples;

public sealed record ApiMember(string Name, string Type, string Default, string Description, bool Required);

public sealed record ApiType(string Name, IReadOnlyList<ApiMember> Members);

// Builds the docs site's API tables from the library's types and its XML documentation.
public static partial class ApiDocs
{
    private static readonly Lazy<IReadOnlyDictionary<string, XElement>> Summaries = new(LoadSummaries);

    private static readonly Dictionary<Type, string> Aliases = new()
    {
        [typeof(bool)] = "bool",
        [typeof(int)] = "int",
        [typeof(double)] = "double",
        [typeof(decimal)] = "decimal",
        [typeof(string)] = "string",
        [typeof(object)] = "object",
    };

    // A component lists its parameters, apart from Class, Style and attributes, which every component takes; an
    // options type lists the properties a caller sets.
    public static ApiType Describe(Type type)
    {
        var component = type.IsAssignableTo(typeof(IComponent));
        var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(property => component
                ? property.IsDefined(typeof(ParameterAttribute)) && property.DeclaringType != typeof(TbComponentBase)
                : property.SetMethod is { IsPublic: true })
            .OrderBy(property => Depth(property.DeclaringType!))
            .ThenBy(property => property.MetadataToken);
        var defaults = CreateForDefaults(type);
        return new(TypeName(type), [.. properties.Select(property => Member(property, defaults))]);
    }

    private static ApiMember Member(PropertyInfo property, object defaults)
    {
        var nullable = new NullabilityInfoContext().Create(property).ReadState == NullabilityState.Nullable;
        var type = TypeName(property.PropertyType);
        if (nullable && !type.EndsWith('?'))
        {
            type += "?";
        }

        // A generic component's defaults come from its instance made with object, so read through that type.
        var value = defaults.GetType().GetProperty(property.Name)!.GetValue(defaults);
        return new(property.Name, type, FormatDefault(value), Summary(property),
            property.IsDefined(typeof(EditorRequiredAttribute)));
    }

    // Bases come first, so a field lists its value and label before the parameters of its own kind.
    private static int Depth(Type type) => type.BaseType is null ? 0 : 1 + Depth(type.BaseType);

    // Generic components are made concrete with object, since their defaults don't depend on the type argument.
    private static object CreateForDefaults(Type type)
    {
        var concrete = type.IsGenericTypeDefinition
            ? type.MakeGenericType([.. type.GetGenericArguments().Select(_ => typeof(object))])
            : type;
        return Activator.CreateInstance(concrete)!;
    }

    private static string FormatDefault(object? value) => value switch
    {
        bool flag => flag ? "true" : "false",
        string text => $"\"{text}\"",
        Enum option => $"{option.GetType().Name}.{option}",
        TimeSpan span => span.ToString(),
        int or double or decimal => Convert.ToString(value, CultureInfo.InvariantCulture)!,
        // A callback, a fragment or an object has no default worth showing.
        _ => "",
    };

    public static string TypeName(Type type)
    {
        if (Nullable.GetUnderlyingType(type) is { } underlying)
        {
            return $"{TypeName(underlying)}?";
        }

        if (Aliases.TryGetValue(type, out var alias))
        {
            return alias;
        }

        if (!type.IsGenericType)
        {
            return type.Name;
        }

        var name = type.Name[..type.Name.IndexOf('`')];
        return $"{name}<{string.Join(", ", type.GetGenericArguments().Select(TypeName))}>";
    }

    private static string Summary(PropertyInfo property)
    {
        var declaring = property.DeclaringType!;
        var owner = declaring.IsGenericType ? declaring.GetGenericTypeDefinition() : declaring;
        return Summaries.Value.TryGetValue($"P:{owner.FullName}.{property.Name}", out var summary)
            ? Text(summary)
            : "";
    }

    // Turns a summary into plain text: a cref keeps its last two names, such as Variant.Outlined.
    private static string Text(XElement summary)
    {
        var parts = summary.Nodes().Select(node => node switch
        {
            XText text => text.Value,
            XElement { Name.LocalName: "see" } see when see.Attribute("cref") is { } cref => ShortName(cref.Value),
            XElement { Name.LocalName: "see" } see => see.Attribute("langword")?.Value ?? see.Value,
            XElement { Name.LocalName: "paramref" or "typeparamref" } reference =>
                reference.Attribute("name")?.Value ?? "",
            XElement element => element.Value,
            _ => "",
        });
        return Whitespace().Replace(string.Concat(parts), " ").Trim();
    }

    private static string ShortName(string cref)
    {
        var names = cref[(cref.IndexOf(':') + 1)..].Split('(')[0].Split('.');
        var name = cref.StartsWith("T:", StringComparison.Ordinal) ? names[^1] : string.Join('.', names[^2..]);
        return Arity().Replace(name, "");
    }

    private static IReadOnlyDictionary<string, XElement> LoadSummaries()
    {
        using var stream = typeof(ApiDocs).Assembly.GetManifestResourceStream("Api/Trailblazor.xml")
            ?? throw new InvalidOperationException("The library's XML documentation isn't embedded.");
        return XDocument.Load(stream).Descendants("member")
            .Where(member => member.Element("summary") is not null)
            .ToDictionary(member => (string)member.Attribute("name")!, member => member.Element("summary")!);
    }

    [GeneratedRegex(@"\s+")]
    private static partial Regex Whitespace();

    [GeneratedRegex(@"`\d+")]
    private static partial Regex Arity();
}
