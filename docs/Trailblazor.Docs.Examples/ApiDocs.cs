using System.Globalization;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using Microsoft.AspNetCore.Components;

namespace Trailblazor.Docs.Examples;

// One of an enum's values, as listed under a parameter of that enum type.
public sealed record ApiValue(string Name, string Description, bool Default);

// Name is how the table shows the member, such as a method's signature, and ShortName its own name, such as
// RefreshAsync. Id is its anchor on its page; a description may link to another table as [text](path#id).
public sealed record ApiMember(string Name, string ShortName, string Type, string Default, string Description,
    bool Required, string Id, IReadOnlyList<ApiValue> Values);

// A method's or a property's Type is what it returns; only a group of settable values has defaults.
public sealed record ApiGroup(string Name, IReadOnlyList<ApiMember> Members, bool Defaults);

public sealed record ApiType(string Name, string Id, string Summary, IReadOnlyList<ApiGroup> Groups)
{
    public IReadOnlyList<ApiMember> Members => [.. Groups.SelectMany(group => group.Members)];
}

// Builds the docs site's API tables from the library's types and its XML documentation.
public static partial class ApiDocs
{
    private static readonly Lazy<IReadOnlyDictionary<string, XElement>> Summaries = new(LoadSummaries);

    // Where each type with a table lives, so a cross-reference to it can link there.
    private static readonly Lazy<IReadOnlyDictionary<Type, string>> Pages = new(() => ExampleCatalog.All
        .SelectMany(example => example.Api.Select(type => (type, path: $"components/{example.Name}/api")))
        .Concat(GuideCatalog.All.SelectMany(guide => guide.Api.Select(type => (type, path: guide.Path))))
        .DistinctBy(page => page.type).ToDictionary(page => page.type, page => page.path));

    // Methods every component has, which a component's Methods table leaves out.
    private static readonly HashSet<string> SharedComponentMethods = ["SetParametersAsync", "Dispose", "DisposeAsync"];

    private static readonly Dictionary<Type, string> Aliases = new()
    {
        [typeof(bool)] = "bool",
        [typeof(int)] = "int",
        [typeof(double)] = "double",
        [typeof(decimal)] = "decimal",
        [typeof(string)] = "string",
        [typeof(object)] = "object",
        [typeof(void)] = "void",
    };

    // A component lists its parameters, apart from Class, Style and attributes, which every component takes, in
    // Parameters, Content and Events groups, then its own methods. An options type lists the properties a caller
    // sets. A service interface, or a handle the library hands out, which has no constructor to call, lists its
    // properties, events and methods, leaving out the accessor methods the compiler makes for the first two.
    public static ApiType Describe(Type type)
    {
        var id = IdOf(type);
        var component = type.IsAssignableTo(typeof(IComponent));
        if (type.IsInterface || (!component && !type.IsValueType && type.GetConstructor(Type.EmptyTypes) is null))
        {
            var context = new NullabilityInfoContext();
            return new(TypeName(type), id, Summary($"T:{type.FullName}"), Groups(
                new("Properties", [.. type.GetProperties().OrderBy(property => Depth(property.DeclaringType!))
                    .ThenBy(property => property.MetadataToken)
                    .Select(property => InterfaceProperty(property, context, id))], false),
                new("Events", [.. type.GetEvents().OrderBy(eventInfo => eventInfo.MetadataToken)
                    .Select(eventInfo => new ApiMember($"event {eventInfo.Name}", eventInfo.Name,
                        TypeName(eventInfo.EventHandlerType!), "",
                        Summary($"E:{DocId(Definition(eventInfo.DeclaringType!))}.{eventInfo.Name}"), false,
                        Anchor(id, eventInfo.Name), []))], false),
                // A record's Equals, ToString and Deconstruct come from the compiler, not the library's API.
                new("Methods", Methods(type.GetMethods().Where(method => method.DeclaringType != typeof(object)
                    && !method.IsDefined(typeof(CompilerGeneratedAttribute))), id), false)));
        }

        var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(property => component
                ? property.IsDefined(typeof(ParameterAttribute)) && property.DeclaringType != typeof(TbComponentBase)
                : property.SetMethod is { IsPublic: true })
            .OrderBy(property => Depth(property.DeclaringType!))
            .ThenBy(property => property.MetadataToken)
            .ToList();
        var defaults = CreateForDefaults(type);
        var members = properties.Select(property => (property, member: Member(property, defaults, id))).ToList();
        if (!component)
        {
            return new(TypeName(type), id, Summary($"T:{type.FullName}"),
                Groups(new ApiGroup("Properties", [.. members.Select(pair => pair.member)], true)));
        }

        return new(TypeName(type), id, Summary($"T:{type.FullName}"), Groups(
            new("Parameters", Kind(members, KindOf.Value), true),
            new("Content", Kind(members, KindOf.Content), false),
            new("Events", Kind(members, KindOf.Event), false),
            new("Methods", Methods(type.GetMethods(BindingFlags.Public | BindingFlags.Instance)
                .Where(method => method.DeclaringType!.IsAssignableTo(typeof(TbComponentBase))
                    && !SharedComponentMethods.Contains(method.Name)), id), false)));
    }

    /// <summary>A type's anchor on its page, such as "tbtreeview" for TbTreeView&lt;TItem, TKey&gt;.</summary>
    public static string IdOf(Type type) => type.Name.Split('`')[0].ToLowerInvariant();

    private static IReadOnlyList<ApiGroup> Groups(params ApiGroup[] groups) =>
        [.. groups.Where(group => group.Members.Count > 0)];

    private enum KindOf { Value, Content, Event }

    private static IReadOnlyList<ApiMember> Kind(List<(PropertyInfo Property, ApiMember Member)> members,
        KindOf kind) =>
        [.. members.Where(pair => KindOfType(pair.Property.PropertyType) == kind).Select(pair => pair.Member)];

    private static KindOf KindOfType(Type type) => type switch
    {
        _ when type == typeof(RenderFragment) => KindOf.Content,
        { IsGenericType: true } when type.GetGenericTypeDefinition() == typeof(RenderFragment<>) => KindOf.Content,
        _ when type == typeof(EventCallback) => KindOf.Event,
        { IsGenericType: true } when type.GetGenericTypeDefinition() == typeof(EventCallback<>) => KindOf.Event,
        _ => KindOf.Value,
    };

    private static string Anchor(string type, string member) => $"{type}-{member.ToLowerInvariant()}";

    private static IReadOnlyList<ApiMember> Methods(IEnumerable<MethodInfo> methods, string id)
    {
        var listed = methods.Where(method => !method.IsSpecialName).ToList();
        return [.. listed.OrderBy(method => Depth(method.DeclaringType!)).ThenBy(method => method.MetadataToken)
            .Select(method => Method(method, Anchor(id, MethodKey(method.Name, method.GetGenericArguments().Length,
                listed))))];
    }

    // Overloads that differ in their type parameter count share a name, so their row ids add the count.
    private static string MethodKey(string name, int arity, IEnumerable<MethodInfo> methods) =>
        methods.Any(method => method.Name == name && !method.IsSpecialName
            && method.GetGenericArguments().Length != arity) ? $"{name}-{arity}" : name;

    private static ApiMember Member(PropertyInfo property, object defaults, string type)
    {
        var name = TypeName(property.PropertyType, new NullabilityInfoContext().Create(property));
        // A generic component's defaults come from its instance made with object, so read through that type.
        var value = FormatDefault(defaults.GetType().GetProperty(property.Name)!.GetValue(defaults));
        return new(property.Name, property.Name, name, value, Summary(property),
            property.IsDefined(typeof(EditorRequiredAttribute)), Anchor(type, property.Name),
            Values(property.PropertyType, value));
    }

    private static IReadOnlyList<ApiValue> Values(Type type, string value)
    {
        var options = Nullable.GetUnderlyingType(type) ?? type;
        return options.IsEnum
            ? [.. options.GetFields(BindingFlags.Public | BindingFlags.Static).OrderBy(field => field.MetadataToken)
                .Select(field => new ApiValue(field.Name, Summary($"F:{DocId(options)}.{field.Name}"),
                    value == $"{options.Name}.{field.Name}"))]
            : [];
    }

    // An indexer shows as this[...], and its summary is found by its parameter types, as a method's is.
    private static ApiMember InterfaceProperty(PropertyInfo property, NullabilityInfoContext context, string owner)
    {
        var index = property.GetIndexParameters();
        var type = TypeName(property.PropertyType, context.Create(property));
        if (index.Length == 0)
        {
            var accessors = property.SetMethod is { IsPublic: true } ? "get; set;" : "get;";
            return new($"{property.Name} {{ {accessors} }}", property.Name, type, "", Summary(property), false,
                Anchor(owner, property.Name), []);
        }

        var signature = string.Join(", ", index.Select(parameter => Parameter(parameter, context)));
        var types = string.Join(',', index.Select(parameter => DocId(parameter.ParameterType)));
        return new($"this[{signature}] {{ get; }}", "this", type, "",
            Summary($"P:{DocId(property.DeclaringType!)}.{property.Name}({types})"), false, Anchor(owner, "this"), []);
    }

    /// <summary>The plain-text summary the XML documentation gives a property, such as a built-in palette.</summary>
    public static string Describe(PropertyInfo property) => Summary(property, links: false);

    private static ApiMember Method(MethodInfo method, string id)
    {
        var context = new NullabilityInfoContext();
        var parameters = method.GetParameters();
        var signature = string.Join(", ", parameters.Select(parameter => Parameter(parameter, context)));
        var types = parameters.Length == 0 ? ""
            : $"({string.Join(',', parameters.Select(parameter => DocId(parameter.ParameterType)))})";
        // The XML documentation counts a generic method's type parameters after two backticks, such as Show``2.
        var arguments = method.GetGenericArguments();
        var (shown, name) = arguments.Length == 0 ? (method.Name, method.Name)
            : ($"{method.Name}<{string.Join(", ", arguments.Select(argument => argument.Name))}>",
                $"{method.Name}``{arguments.Length}");
        var summary = Summary($"M:{DocId(Definition(method.DeclaringType!))}.{name}{types}");
        if (summary.Length == 0)
        {
            // An override with no summary of its own, such as a field's FocusAsync, has its base's.
            summary = Summary($"M:{DocId(Definition(method.GetBaseDefinition().DeclaringType!))}.{name}{types}");
        }

        return new($"{shown}({signature})", method.Name,
            TypeName(method.ReturnType, context.Create(method.ReturnParameter)), "", summary, false, id, []);
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

    private static string Summary(PropertyInfo property, bool links = true) =>
        Summary($"P:{Definition(property.DeclaringType!).FullName}.{property.Name}", links);

    // A member a generic base declares is documented on the base's definition, such as TbFieldBase`1.
    private static Type Definition(Type type) => type.IsGenericType ? type.GetGenericTypeDefinition() : type;

    private static string Summary(string member, bool links = true) =>
        Summaries.Value.TryGetValue(member, out var summary) ? Text(summary, links) : "";

    // Turns a summary into text: a cref keeps its last two names, such as Variant.Outlined, and links to its table
    // when it has one.
    private static string Text(XElement summary, bool links)
    {
        var parts = summary.Nodes().Select(node => node switch
        {
            XText text => text.Value,
            XElement { Name.LocalName: "see" } see when see.Attribute("cref") is { } cref =>
                links && LinkOf(cref.Value) is { } link ? $"[{ShortName(cref.Value)}]({link})" : ShortName(cref.Value),
            XElement { Name.LocalName: "see" } see => see.Attribute("langword")?.Value ?? see.Value,
            XElement { Name.LocalName: "paramref" or "typeparamref" } reference =>
                reference.Attribute("name")?.Value ?? "",
            XElement element => element.Value,
            _ => "",
        });
        return Whitespace().Replace(string.Concat(parts), " ").Trim();
    }

    // A type's table, or a member's row in it, on the API page that lists the type.
    private static string? LinkOf(string cref)
    {
        var name = cref[2..].Split('(')[0];
        var member = cref.StartsWith("T:", StringComparison.Ordinal) ? null : name[(name.LastIndexOf('.') + 1)..];
        var owner = typeof(TbComponentBase).Assembly.GetType(member is null ? name : name[..name.LastIndexOf('.')]);
        if (owner is null || !Pages.Value.TryGetValue(owner, out var page))
        {
            return null;
        }

        if (member is null)
        {
            return $"{page}#{IdOf(owner)}";
        }

        var arity = member.Contains('`', StringComparison.Ordinal) ? int.Parse(member[(member.LastIndexOf('`') + 1)..],
            CultureInfo.InvariantCulture) : 0;
        var key = MethodKey(Arity().Replace(member, ""), arity, owner.GetMethods());
        return $"{page}#{IdOf(owner)}-{key.ToLowerInvariant()}";
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

    [GeneratedRegex(@"`+\d+")]
    private static partial Regex Arity();
}
