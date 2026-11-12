using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Operations;

namespace Trailblazor.Analyzers;

// The components a component sits inside in markup. Razor passes a component's content to it as a lambda, either to
// AddAttribute right after its OpenComponent or to the type-inference helper that opens a generic component.
internal static class ComponentNesting
{
    // The components around one, nearest first, up to the top of the page's markup; null when the content comes from
    // somewhere the page's code doesn't show, such as a RenderFragment field or parameter.
    public static List<INamedTypeSymbol>? Enclosing(IInvocationOperation open)
    {
        var enclosing = new List<INamedTypeSymbol>();
        IOperation? node = InHelper(open) ? CallTo(open) : open;
        while (node is not null)
        {
            var lambda = Ancestor<IAnonymousFunctionOperation>(node);
            if (lambda is null)
            {
                return IsRenderTree(node) ? enclosing : null;
            }

            if (Ancestor<IArgumentOperation>(lambda) is not { Parent: IInvocationOperation call }
                || Owner(call) is not { } owner)
            {
                return null;
            }

            enclosing.Add(owner);
            node = call;
        }

        return null;
    }

    // The component with the types it's given in markup. One whose types Razor infers is opened in a type-inference
    // helper, with the helper's own type parameters; the call to the helper says what they are.
    public static INamedTypeSymbol Resolved(INamedTypeSymbol component, IOperation open) =>
        InHelper(open) && CallTo(open) is { } call ? AtCall(component, call) : component;

    private static INamedTypeSymbol AtCall(INamedTypeSymbol type, IInvocationOperation call) =>
        type.TypeArguments.Length == 0 ? type
            : type.OriginalDefinition.Construct([.. type.TypeArguments.Select(argument =>
                argument is ITypeParameterSymbol { DeclaringMethod: { } method } parameter
                && SymbolEqualityComparer.Default.Equals(method, call.TargetMethod.OriginalDefinition)
                    ? call.TargetMethod.TypeArguments[parameter.Ordinal] : argument)]);

    // A routable page, which the router renders, so no component of the page's own holds its markup; a layout can,
    // but the analyzer can't see it.
    public static bool IsInPage(IOperation operation) => operation.SemanticModel!.Compilation
        .GetTypeByMetadataName("Microsoft.AspNetCore.Components.RouteAttribute") is { } route
        && PageOf(operation)?.GetAttributes().Any(attribute =>
            SymbolEqualityComparer.Default.Equals(attribute.AttributeClass, route)) == true;

    // A type-inference helper sits in a class of its own, so its page is the one whose code calls it.
    private static INamedTypeSymbol? PageOf(IOperation operation)
    {
        IOperation? place = InHelper(operation) ? CallTo(operation) : operation;
        return place?.SemanticModel!.GetEnclosingSymbol(place.Syntax.SpanStart)?.ContainingType;
    }

    private static bool InHelper(IOperation operation) => operation.SemanticModel!
        .GetEnclosingSymbol(operation.Syntax.SpanStart)?.ContainingType.Name == "TypeInference";

    private static bool IsRenderTree(IOperation operation) =>
        operation.SemanticModel!.GetEnclosingSymbol(operation.Syntax.SpanStart) is IMethodSymbol
        {
            Name: "BuildRenderTree",
        };

    // The component a content lambda is given to: the one whose OpenComponent the AddAttribute call follows, or the one
    // a type-inference helper opens.
    private static INamedTypeSymbol? Owner(IInvocationOperation call)
    {
        if (call.TargetMethod.ContainingType.Name == "TypeInference")
        {
            return call.TargetMethod.OriginalDefinition.DeclaringSyntaxReferences
                .Select(reference => reference.GetSyntax())
                .SelectMany(syntax => syntax.DescendantNodes().OfType<InvocationExpressionSyntax>())
                .Select(syntax => call.SemanticModel!.GetOperation(syntax)).OfType<IInvocationOperation>()
                .Where(open => open.TargetMethod.Name == "OpenComponent" && open.TargetMethod.TypeArguments.Length == 1)
                .Select(open => open.TargetMethod.TypeArguments[0]).OfType<INamedTypeSymbol>()
                .Select(type => AtCall(type, call)).FirstOrDefault();
        }

        if (call.TargetMethod.Name is not ("AddAttribute" or "AddComponentParameter")
            || call.Parent is not IExpressionStatementOperation { Parent: IBlockOperation block } statement)
        {
            return null;
        }

        // Back from the call to its component's OpenComponent, past any whole component written before it.
        var closed = 0;
        for (var i = block.Operations.IndexOf(statement) - 1; i >= 0; i--)
        {
            if (block.Operations[i] is not IExpressionStatementOperation { Operation: IInvocationOperation earlier }
                || !SameBuilder(earlier, call))
            {
                continue;
            }

            if (earlier.TargetMethod.Name == "CloseComponent")
            {
                closed++;
            }
            else if (earlier.TargetMethod.Name == "OpenComponent" && closed-- == 0)
            {
                return earlier.TargetMethod.TypeArguments.FirstOrDefault() as INamedTypeSymbol;
            }
        }

        return null;
    }

    private static bool SameBuilder(IInvocationOperation one, IInvocationOperation other) =>
        SymbolEqualityComparer.Default.Equals(ComponentUsage.Builder(one), ComponentUsage.Builder(other));

    // The call that runs a type-inference helper, which Razor writes once in the page's markup code.
    internal static IInvocationOperation? CallTo(IOperation inHelper)
    {
        var helper = inHelper.SemanticModel!.GetEnclosingSymbol(inHelper.Syntax.SpanStart) as IMethodSymbol;
        var model = inHelper.SemanticModel;
        return inHelper.Syntax.SyntaxTree.GetRoot().DescendantNodes().OfType<InvocationExpressionSyntax>()
            .Where(call => call.Expression is MemberAccessExpressionSyntax access
                && access.Name.Identifier.ValueText == helper?.Name)
            .Select(call => model.GetOperation(call)).OfType<IInvocationOperation>()
            .FirstOrDefault(call =>
                SymbolEqualityComparer.Default.Equals(call.TargetMethod.OriginalDefinition, helper));
    }

    private static T? Ancestor<T>(IOperation operation) where T : class, IOperation
    {
        for (var current = operation.Parent; current is not null; current = current.Parent)
        {
            if (current is T match)
            {
                return match;
            }
        }

        return null;
    }
}
