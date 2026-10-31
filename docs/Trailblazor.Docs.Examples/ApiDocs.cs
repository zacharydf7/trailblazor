using System.Globalization;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using Microsoft.AspNetCore.Components;

namespace Trailblazor.Docs.Examples;

public sealed record ApiMember(string Name, string Type, string Default, string Description, bool Required);

// A service's or a handle's members are its properties and methods, whose Type is what they return.
public sealed record ApiType(string Name, string Summary, IReadOnlyList<ApiMember> Members, bool Methods = false);

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
    // options type lists the properties a caller sets. A service interface, or a handle the library hands out, which
    // has no constructor to call, lists its properties, events and methods, leaving out the accessor methods the
    // compiler makes for the first two.
    public static ApiType Describe(Type type)
    {
        var component = type.IsAssignableTo(typeof(IComponent));
        if (type.IsInterface || (!component && !type.IsValueType && type.GetConstructor(Type.EmptyTypes) is null))
        {
            var context = new NullabilityInfoContext();
            return new(TypeName(type), Summary($"T:{type.FullName}"),
            [
                .. type.GetProperties().OrderBy(property => Depth(property.DeclaringType!))
                    .ThenBy(property => property.MetadataToken)
                    .Select(property => InterfaceProperty(property, context)),
                .. type.GetEvents().OrderBy(eventInfo => eventInfo.MetadataToken).Select(eventInfo => new ApiMember(
                    $"event {eventInfo.Name}", TypeName(eventInfo.EventHandlerType!), "",
                    Summary($"E:{DocId(type)}.{eventInfo.Name}"), false)),
                .. type.GetMethods()
                    .Where(method => !method.IsSpecialName && method.DeclaringType != typeof(object))
                    .OrderBy(method => Depth(method.DeclaringType!)).ThenBy(method => method.MetadataToken)
                    .Select(Method),
            ], Methods: true);
        }

        var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(property => component
                ? property.IsDefined(typeof(ParameterAttribute)) && property.DeclaringType != typeof(TbComponentBase)
                : property.SetMethod is { IsPublic: true })
            .OrderBy(property => Depth(property.DeclaringType!))
            .ThenBy(property => property.MetadataToken);
        var defaults = CreateForDefaults(type);
        return new(TypeName(type), Summary($"T:{type.FullName}"),
            [.. properties.Select(property => Member(property, defaults))]);
    }

    private static ApiMember Member(PropertyInfo property, object defaults)
    {
        var type = TypeName(property.PropertyType, new NullabilityInfoContext().Create(property));
        // A generic component's defaults come from its instance made with object, so read through that type.
        var value = defaults.GetType().GetProperty(property.Name)!.GetValue(defaults);
        return new(property.Name, type, FormatDefault(value), Summary(property),
            property.IsDefined(typeof(EditorRequiredAttribute)));
    }

    // An indexer shows as this[...], and its summary is found by its parameter types, as a method's is.
    private static ApiMember InterfaceProperty(PropertyInfo property, NullabilityInfoContext context)
    {
        var index = property.GetIndexParameters();
        var type = TypeName(property.PropertyType, context.Create(property));
        if (index.Length == 0)
        {
            var accessors = property.SetMethod is { IsPublic: true } ? "get; set;" : "get;";
            return new($"{property.Name} {{ {accessors} }}", type, "", Summary(property), false);
        }

        var signature = string.Join(", ", index.Select(parameter => Parameter(parameter, context)));
        var types = string.Join(',', index.Select(parameter => DocId(parameter.ParameterType)));
        return new($"this[{signature}] {{ get; }}", type, "",
            Summary($"P:{DocId(property.DeclaringType!)}.{property.Name}({types})"), false);
    }

    /// <summary>The summary the XML documentation gives one value of an enum, such as a palette.</summary>
    public static string Describe(Enum value) => Summary($"F:{value.GetType().FullName}.{value}");

    private static ApiMember Method(MethodInfo method)
    {
        var context = new NullabilityInfoContext();
        var parameters = method.GetParameters();
        var signature = string.Join(", ", parameters.Select(parameter => Parameter(parameter, context)));
        var types = parameters.Length == 0 ? ""
            : $"({string.Join(',', parameters.Select(parameter => DocId(parameter.ParameterType)))})";
        // The XML documentation counts a generic method's type parameters after two backticks, such as Show``2.
        var arguments = method.GetGenericArguments();
        var (shown, id) = arguments.Length == 0 ? (method.Name, method.Name)
            : ($"{method.Name}<{string.Join(", ", arguments.Select(argument => argument.Name))}>",
                $"{method.Name}``{arguments.Length}");
        return new($"{shown}({signature})", TypeName(method.ReturnType, context.Create(method.ReturnParameter)),
            "", Summary($"M:{DocId(method.DeclaringType!)}.{id}{types}"), false);
    }

    private static string Parameter(ParameterInfo parameter, NullabilityInfoContext context)
    {
        var text = $"{TypeName(parameter.ParameterType, context.Create(parameter))} {parameter.Name}";
        return parameter.HasDefaultValue
            ? $"{text} = {(parameter.DefaultValue is null ? "null" : FormatDefault(parameter.DefaultValue))}"
            : text;
    }

    // The type as the XML documentation names it: a generic type itself keeps its arity, as DialogReference`1, a use
    // of one has its type arguments in braces, and a type parameter is its position, after two backticks for a
    // method's and one for a type's.
    private static string DocId(Type type) => type switch
    {
        { IsGenericMethodParameter: true } => $"``{type.GenericParameterPosition}",
        { IsGenericTypeParameter: true } => $"`{type.GenericParameterPosition}",
        { IsGenericTypeDefinition: true } => type.FullName!.Replace('+', '.'),
        { IsGenericType: true } => $"{type.Namespace}.{type.Name[..type.Name.IndexOf('`')]}"
            + $"{{{string.Join(',', type.GetGenericArguments().Select(DocId))}}}",
        _ => type.FullName!.Replace('+', '.'),
    };

    // Bases come first, so a field lists its value and label before the parameters of its own kind.
    private static int Depth(Type type) => type.BaseType is null ? 0 : 1 + Depth(type.BaseType);

    // Generic components are made concrete with object, or int where the type must be a struct, since their defaults
    // don't depend on the type argument.
    private static object CreateForDefaults(Type type)
    {
        var concrete = type.IsGenericTypeDefinition
            ? type.MakeGenericType([.. type.GetGenericArguments().Select(argument =>
                argument.GenericParameterAttributes.HasFlag(GenericParameterAttributes.NotNullableValueTypeConstraint)
                    ? typeof(int) : typeof(object))])
            : type;
        return Activator.CreateInstance(concrete)!;
    }

    private static string FormatDefault(object? value) => value switch
    {
        bool flag => flag ? "true" : "false",
        string text => $"\"{text}\"",
        Enum option => $"{option.GetType().Name}.{option}",
        TimeSpan span => span.ToString(),
        GridSpan span => span.ToString(),
        int or double or decimal => Convert.ToString(value, CultureInfo.InvariantCulture)!,
        // A callback, a fragment or an object has no default worth showing.
        _ => "",
    };

    // With nullability, a reference type marked nullable gets a ?, here or in a type argument.
    public static string TypeName(Type type, NullabilityInfo? nullability = null)
    {
        if (Nullable.GetUnderlyingType(type) is { } underlying)
        {
            return $"{TypeName(underlying)}?";
        }

        var mark = nullability?.ReadState == NullabilityState.Nullable ? "?" : "";
        if (Aliases.TryGetValue(type, out var alias))
        {
            return alias + mark;
        }

        if (!type.IsGenericType)
        {
            return type.Name + mark;
        }

        var name = type.Name[..type.Name.IndexOf('`')];
        var arguments = type.GetGenericArguments().Select((argument, index) =>
            TypeName(argument, nullability?.GenericTypeArguments[index]));
        return $"{name}<{string.Join(", ", arguments)}>{mark}";
    }

    private static string Summary(PropertyInfo property)
    {
        var declaring = property.DeclaringType!;
        var owner = declaring.IsGenericType ? declaring.GetGenericTypeDefinition() : declaring;
        return Summary($"P:{owner.FullName}.{property.Name}");
    }

    private static string Summary(string member) =>
        Summaries.Value.TryGetValue(member, out var summary) ? Text(summary) : "";

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
