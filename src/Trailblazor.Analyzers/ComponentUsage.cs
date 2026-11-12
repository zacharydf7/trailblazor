using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Operations;

namespace Trailblazor.Analyzers;

// One component as a render tree builds it: OpenComponent, then its parameters, then CloseComponent. Razor writes every
// component in markup this way, in the page's own code or in a type-inference helper for a generic component.
internal sealed class ComponentUsage
{
    private ComponentUsage(INamedTypeSymbol component, Location location)
    {
        Component = component;
        Location = location;
    }

    public INamedTypeSymbol Component { get; }

    // The parameters and attributes it is given, by name, each with its value and where its name is written.
    public Dictionary<string, (IOperation? Value, Location Location)> Parameters { get; } = new();

    // Attributes splatted with @attributes, which may hold any name.
    public bool HasSplattedAttributes { get; private set; }

    // Where to report a problem with the whole component: the first parameter name Razor maps to the .razor file, or
    // else the OpenComponent call.
    public Location Location { get; private set; }

    public bool Has(string name) => Parameters.ContainsKey(name);

    public static ComponentUsage? Read(IInvocationOperation open)
    {
        if (open.TargetMethod.Name != "OpenComponent" || open.TargetMethod.TypeArguments.Length != 1
            || open.TargetMethod.TypeArguments[0] is not INamedTypeSymbol component
            || open.Parent is not IExpressionStatementOperation statement
            || statement.Parent is not IBlockOperation block)
        {
            return null;
        }

        var usage = new ComponentUsage(component, open.Syntax.GetLocation());
        var builder = Builder(open);
        for (var i = block.Operations.IndexOf(statement) + 1; i < block.Operations.Length; i++)
        {
            if (block.Operations[i] is not IExpressionStatementOperation { Operation: IInvocationOperation call }
                || !SymbolEqualityComparer.Default.Equals(Builder(call), builder))
            {
                continue;
            }

            switch (call.TargetMethod.Name)
            {
                case "CloseComponent":
                    return usage;
                case "AddMultipleAttributes":
                    usage.HasSplattedAttributes = true;
                    break;
                case "AddComponentParameter" or "AddAttribute" when call.Arguments.Length >= 2
                    && call.Arguments[1].Value.ConstantValue.Value is string name:
                    var location = NameLocation(call.Arguments[1].Syntax);
                    usage.Parameters[name] = (call.Arguments.Length > 2 ? call.Arguments[2].Value : null, location);
                    if (!IsMapped(usage.Location) && IsMapped(location))
                    {
                        usage.Location = location;
                    }

                    break;
            }
        }

        return usage;
    }

    private static bool IsMapped(Location location) => location.GetMappedLineSpan().HasMappedPath;

    internal static ISymbol? Builder(IInvocationOperation call) => call.Instance switch
    {
        IParameterReferenceOperation parameter => parameter.Parameter,
        ILocalReferenceOperation local => local.Local,
        _ => null,
    };

    // In nameof(Component.Label), only the parameter's own name is on a line Razor maps to the markup.
    private static Location NameLocation(SyntaxNode argument) =>
        (argument.DescendantNodesAndSelf().OfType<IdentifierNameSyntax>().LastOrDefault() ?? argument).GetLocation();
}
