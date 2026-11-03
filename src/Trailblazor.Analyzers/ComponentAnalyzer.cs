using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace Trailblazor.Analyzers;

/// <summary>Reports Trailblazor controls and fields with no accessible name, dialogs with no title, and inputs given a
/// Value without ValueChanged.</summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class ComponentAnalyzer : DiagnosticAnalyzer
{
    // Each component that needs a name, with the parameters (or attributes it passes to its named element) that can
    // give it one.
    internal static readonly (string Type, string[] Names)[] Named =
    [
        ("Trailblazor.TbIconButton", ["AriaLabel"]),
        ("Trailblazor.TbChipGroup", ["AriaLabel"]),
        ("Trailblazor.TbToolbar", ["AriaLabel"]),
        ("Trailblazor.TbToggleGroupBase`1", ["AriaLabel"]),
        ("Trailblazor.TbToolbarButton", ["Text"]),
        ("Trailblazor.TbProgress", ["Label", "AriaLabel"]),
        ("Trailblazor.TbMenu", ["Label", "Trigger"]),
        ("Trailblazor.TbPopover", ["Label", "Trigger"]),
        ("Trailblazor.TbTab", ["Text", "TextContent"]),
        ("Trailblazor.TbTreeView`2", ["AriaLabel", "aria-labelledby"]),
        ("Trailblazor.TbSubMenu", ["Label"]),
        ("Trailblazor.TbOptionGroup", ["Label"]),
        ("Trailblazor.TbSortable`1", ["Label"]),
        ("Trailblazor.TbInsertDivider", ["Label"]),
    ];

    // An id lets a TbLabel elsewhere name the field's native input. Other attributes written on a field go on its
    // wrapper, not its input, so they don't name it.
    private static readonly string[] FieldNames = ["Label", "AriaLabel", "InputAttributes", "id"];

    // Fields named another way: a checkbox by its content, and a group by attributes on its fieldset. A label's for
    // can't name a select's or list box's div, or a range's wrapper, so an id doesn't name those.
    internal static readonly (string Type, string[] Names)[] OtherFields =
    [
        ("Trailblazor.TbCheckbox`1", [.. FieldNames, "ChildContent"]),
        ("Trailblazor.TbRadioGroup`1", ["Label", "AriaLabel", "aria-label", "aria-labelledby"]),
        ("Trailblazor.TbCheckboxGroup`1", ["Label", "AriaLabel", "aria-label", "aria-labelledby"]),
        ("Trailblazor.TbSelectBase`1", ["Label", "AriaLabel", "InputAttributes"]),
        ("Trailblazor.TbListBoxBase`1", ["Label", "AriaLabel", "InputAttributes"]),
        ("Trailblazor.TbRangeSlider`1", ["Label", "AriaLabel", "InputAttributes"]),
        ("Trailblazor.TbDateRangePicker`1", ["Label", "AriaLabel", "InputAttributes"]),
    ];

    /// <inheritdoc />
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } = ImmutableArray.Create(
        Rules.NoAccessibleName, Rules.NoDialogTitle, Rules.ValueWithoutValueChanged, Rules.NoFieldLabel);

    /// <inheritdoc />
    public override void Initialize(AnalysisContext context)
    {
        // Razor turns markup into generated code, so that is where most components are written.
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze
            | GeneratedCodeAnalysisFlags.ReportDiagnostics);
        context.EnableConcurrentExecution();
        context.RegisterCompilationStartAction(start =>
        {
            var compilation = start.Compilation;
            var fieldBase = compilation.GetTypeByMetadataName("Trailblazor.TbFieldBase`1");
            if (fieldBase is null)
            {
                return;
            }

            var types = new KnownTypes(Resolve(compilation, Named), fieldBase, Resolve(compilation, OtherFields),
                compilation.GetTypeByMetadataName("Trailblazor.TbDialog"),
                compilation.GetTypeByMetadataName("Trailblazor.IDialogService"));
            start.RegisterOperationAction(operation => Analyze(operation, types), OperationKind.Invocation);
        });
    }

    private static void Analyze(OperationAnalysisContext context, KnownTypes types)
    {
        var invocation = (IInvocationOperation)context.Operation;
        if (SymbolEqualityComparer.Default.Equals(invocation.TargetMethod.ContainingType, types.DialogService))
        {
            foreach (var argument in invocation.Arguments.Where(argument => argument.Parameter?.Name == "title"
                && IsBlank(argument.Value)))
            {
                context.ReportDiagnostic(Diagnostic.Create(Rules.NoDialogTitle, argument.Syntax.GetLocation()));
            }

            return;
        }

        if (ComponentUsage.Read(invocation) is not { } usage)
        {
            return;
        }

        var component = usage.Component;
        foreach (var (_, names) in types.Named.Where(entry => InheritsFrom(component, entry.Type)))
        {
            CheckName(context, usage, names, Rules.NoAccessibleName, component.Name,
                string.Join(" or ", names.Where(name => char.IsUpper(name[0]))));
        }

        if (SymbolEqualityComparer.Default.Equals(component, types.Dialog))
        {
            CheckName(context, usage, ["Title"], Rules.NoDialogTitle);
        }

        if (InheritsFrom(component, types.FieldBase))
        {
            var names = types.OtherFields.FirstOrDefault(entry => InheritsFrom(component, entry.Type)).Names
                ?? FieldNames;
            CheckName(context, usage, names, Rules.NoFieldLabel, component.Name);
        }

        if (component.ContainingNamespace.ToDisplayString() == "Trailblazor" && Property(component, "Value") is not null
            && Property(component, "ValueChanged") is not null && usage.Has("Value") && !usage.Has("ValueChanged")
            && !usage.HasSplattedAttributes && !IsOn(usage, "ReadOnly") && !IsOn(usage, "Disabled"))
        {
            var fix = Property(component, "ReadOnly") is null ? "Use @bind-Value or handle ValueChanged"
                : "Use @bind-Value, handle ValueChanged, or set ReadOnly";
            context.ReportDiagnostic(Diagnostic.Create(Rules.ValueWithoutValueChanged,
                usage.Parameters["Value"].Location, component.Name, fix));
        }
    }

    // A name that's missing, or given only as blank text. A missing [EditorRequired] parameter is left to Razor's own
    // warning, so the same mistake isn't reported twice.
    private static void CheckName(OperationAnalysisContext context, ComponentUsage usage, string[] names,
        DiagnosticDescriptor rule, params object[] messageArgs)
    {
        if (usage.HasSplattedAttributes)
        {
            return;
        }

        var given = names.Where(usage.Has).ToArray();
        if (given.Length == 0)
        {
            if (!names.Any(name => Property(usage.Component, name) is { } property && IsEditorRequired(property)))
            {
                context.ReportDiagnostic(Diagnostic.Create(rule, usage.Location, messageArgs));
            }
        }
        else if (given.All(name => IsBlank(usage.Parameters[name].Value)))
        {
            context.ReportDiagnostic(Diagnostic.Create(rule, usage.Parameters[given[0]].Location, messageArgs));
        }
    }

    // A parameter of the component's own that's set, and not to false. An attribute it doesn't take does nothing.
    private static bool IsOn(ComponentUsage usage, string name) =>
        Property(usage.Component, name) is not null && usage.Parameters.TryGetValue(name, out var parameter)
        && !(Unwrap(parameter.Value)?.ConstantValue is { HasValue: true, Value: false });

    private static bool IsBlank(IOperation? value) =>
        Unwrap(value)?.ConstantValue is { HasValue: true } constant
        && (constant.Value is null || constant.Value is string text && string.IsNullOrWhiteSpace(text));

    // The value as written in markup, looking through conversions, Razor's type checks, and the parameters of its
    // type-inference helpers, which receive the markup's values from a call elsewhere in the page.
    private static IOperation? Unwrap(IOperation? value) => value switch
    {
        IConversionOperation conversion => Unwrap(conversion.Operand),
        IInvocationOperation { TargetMethod.Name: "TypeCheck", Arguments.Length: 1 } check =>
            Unwrap(check.Arguments[0].Value),
        IParameterReferenceOperation
        {
            Parameter.ContainingSymbol: IMethodSymbol { ContainingType.Name: "TypeInference" },
        } reference => Unwrap(ArgumentAtCall(reference)),
        _ => value,
    };

    private static IOperation? ArgumentAtCall(IParameterReferenceOperation reference)
    {
        var helper = (IMethodSymbol)reference.Parameter.ContainingSymbol;
        var model = reference.SemanticModel!;
        var calls = reference.Syntax.SyntaxTree.GetRoot().DescendantNodes().OfType<InvocationExpressionSyntax>();
        foreach (var call in calls)
        {
            if (call.Expression is MemberAccessExpressionSyntax access
                && access.Name.Identifier.ValueText == helper.Name
                && model.GetOperation(call) is IInvocationOperation operation
                && SymbolEqualityComparer.Default.Equals(operation.TargetMethod.OriginalDefinition, helper))
            {
                return operation.Arguments.FirstOrDefault(argument =>
                    argument.Parameter?.Ordinal == reference.Parameter.Ordinal)?.Value;
            }
        }

        return null;
    }

    private static (INamedTypeSymbol Type, string[] Names)[] Resolve(Compilation compilation,
        (string Type, string[] Names)[] table) =>
        table.Select(entry => (Type: compilation.GetTypeByMetadataName(entry.Type), entry.Names))
            .Where(entry => entry.Type is not null).Select(entry => (entry.Type!, entry.Names)).ToArray();

    private static bool InheritsFrom(INamedTypeSymbol type, INamedTypeSymbol? baseType)
    {
        for (var current = type; current is not null; current = current.BaseType)
        {
            if (SymbolEqualityComparer.Default.Equals(current.OriginalDefinition, baseType))
            {
                return true;
            }
        }

        return false;
    }

    private static IPropertySymbol? Property(INamedTypeSymbol type, string name)
    {
        for (var current = type; current is not null; current = current.BaseType)
        {
            if (current.GetMembers(name).OfType<IPropertySymbol>().FirstOrDefault() is { } property)
            {
                return property;
            }
        }

        return null;
    }

    private static bool IsEditorRequired(IPropertySymbol property) => property.GetAttributes().Any(attribute =>
        attribute.AttributeClass?.ToDisplayString() == "Microsoft.AspNetCore.Components.EditorRequiredAttribute");

    private sealed class KnownTypes(
        (INamedTypeSymbol Type, string[] Names)[] named, INamedTypeSymbol fieldBase,
        (INamedTypeSymbol Type, string[] Names)[] otherFields, INamedTypeSymbol? dialog,
        INamedTypeSymbol? dialogService)
    {
        public (INamedTypeSymbol Type, string[] Names)[] Named { get; } = named;

        public INamedTypeSymbol FieldBase { get; } = fieldBase;

        public (INamedTypeSymbol Type, string[] Names)[] OtherFields { get; } = otherFields;

        public INamedTypeSymbol? Dialog { get; } = dialog;

        public INamedTypeSymbol? DialogService { get; } = dialogService;
    }
}
