using System.Collections.Immutable;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace Trailblazor.Analyzers;

/// <summary>Reports Trailblazor controls and fields with no accessible name, dialogs with no title, inputs given a
/// Value without ValueChanged, attributes that look like a misnamed type parameter, components written outside the
/// one they belong in, items whose value isn't their list's type, sliders of something other than a number, tree
/// views and data grids whose parameters don't go together, string parameters given text that looks like code, hotkey
/// keys that can't be used as written, and forms given OnSubmit beside OnValidSubmit or OnInvalidSubmit. It also
/// suggests a filterable select for an autocomplete that opens a known list on focus.</summary>
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
        ("Trailblazor.TbRibbon", ["AriaLabel"]),
        ("Trailblazor.TbRibbonTab", ["Title"]),
        ("Trailblazor.TbRibbonGroup", ["Label"]),
        ("Trailblazor.TbRibbonButton", ["Label"]),
        ("Trailblazor.TbToggleGroupBase`1", ["Label", "AriaLabel"]),
        ("Trailblazor.TbProgress", ["Label", "AriaLabel"]),
        ("Trailblazor.TbTab", ["Text", "TextContent"]),
        ("Trailblazor.TbTreeView`2", ["AriaLabel", "aria-labelledby"]),
        ("Trailblazor.TbSubMenu", ["Label"]),
        ("Trailblazor.TbOptionGroup", ["Label"]),
        ("Trailblazor.TbSortable`1", ["Label"]),
        ("Trailblazor.TbInsertDivider", ["Label"]),
        ("Trailblazor.TbCarousel", ["AriaLabel"]),
        ("Trailblazor.TbVirtualList`1", ["AriaLabel"]),
        ("Trailblazor.TbFab", ["Label"]),
        ("Trailblazor.TbSpeedDial", ["Label"]),
        ("Trailblazor.TbStep", ["Title"]),
        ("Trailblazor.TbAvatar", ["Name"]),
    ];

    // Named by Label, or by AriaLabel when an Icon replaces the text, unless the app's own Trigger replaces their
    // button.
    internal static readonly string[] Labeled =
        ["Trailblazor.TbToolbarButton", "Trailblazor.TbMenu", "Trailblazor.TbPopover", "Trailblazor.TbPopconfirm"];

    // A menu's or popover's trigger shows its Icon beside a Label, so the label still names it.
    private static readonly string[] IconBesideLabel = ["Trailblazor.TbMenu", "Trailblazor.TbPopover"];

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
        ("Trailblazor.TbCalendar`1", ["Label", "AriaLabel", "InputAttributes"]),
    ];

    // Components that work only inside another, paired with each host that can hold them; the warning names each.
    internal static readonly (string Child, string Host)[] Hosted =
    [
        ("Trailblazor.TbRadio`1", "Trailblazor.TbRadioGroup`1"),
        ("Trailblazor.TbCheckboxItem`1", "Trailblazor.TbCheckboxGroup`1"),
        ("Trailblazor.TbOption`1", "Trailblazor.TbSelectBase`1"),
        ("Trailblazor.TbOption`1", "Trailblazor.TbListBoxBase`1"),
        ("Trailblazor.TbToggleItem`1", "Trailblazor.TbToggleGroupBase`1"),
        ("Trailblazor.TbTab", "Trailblazor.TbTabs"),
        ("Trailblazor.TbRibbonTab", "Trailblazor.TbRibbon"),
        ("Trailblazor.TbSplitterPane", "Trailblazor.TbSplitter"),
        ("Trailblazor.TbDockPanel", "Trailblazor.TbDockManager"),
        ("Trailblazor.TbTableOfContentsItem", "Trailblazor.TbTableOfContents"),
        ("Trailblazor.TbTreeItem`1", "Trailblazor.TbTreeView`2"),
        ("Trailblazor.TbMenuRadioItem`1", "Trailblazor.TbMenuRadioGroup`1"),
        ("Trailblazor.TbDialogTitle", "Trailblazor.TbDialog"),
        ("Trailblazor.TbDialogTitle", "Trailblazor.TbDrawer"),
        ("Trailblazor.TbDialogDescription", "Trailblazor.TbDialog"),
        ("Trailblazor.TbDialogDescription", "Trailblazor.TbDrawer"),
        ("Trailblazor.TbBreadcrumb", "Trailblazor.TbBreadcrumbs"),
        ("Trailblazor.TbCarouselSlide", "Trailblazor.TbCarousel"),
        ("Trailblazor.TbNavLink", "Trailblazor.TbNavMenu"),
        ("Trailblazor.TbNavGroup", "Trailblazor.TbNavMenu"),
        ("Trailblazor.TbStep", "Trailblazor.TbStepper"),
    ];

    // Items whose value must be their list's type, paired with each list that can hold them. Each list's only type
    // parameter is the type of its items.
    internal static readonly (string Item, string List)[] Listed =
    [
        ("Trailblazor.TbOption`1", "Trailblazor.TbSelectBase`1"),
        ("Trailblazor.TbOption`1", "Trailblazor.TbListBoxBase`1"),
        ("Trailblazor.TbToggleItem`1", "Trailblazor.TbToggleGroupBase`1"),
        ("Trailblazor.TbRadio`1", "Trailblazor.TbRadioGroup`1"),
        ("Trailblazor.TbCheckboxItem`1", "Trailblazor.TbCheckboxGroup`1"),
        ("Trailblazor.TbMenuRadioItem`1", "Trailblazor.TbMenuRadioGroup`1"),
    ];

    // The trees that keep what is open, selected and checked by each item's key.
    private static readonly string[] KeyedTrees =
        ["Trailblazor.TbTreeView`2", "Trailblazor.TbTreeSelect`2", "Trailblazor.TbMultiTreeSelect`2"];

    /// <inheritdoc />
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } = ImmutableArray.Create(
        Rules.NoAccessibleName, Rules.NoDialogTitle, Rules.ValueWithoutValueChanged, Rules.NoFieldLabel,
        Rules.TypeParameterAsAttribute, Rules.OutsideItsParent, Rules.ItemTypeMismatch, Rules.SliderNotANumber,
        Rules.TreeSetup, Rules.CodeAsText, Rules.KnownListAutocomplete, Rules.ReferenceKeys, Rules.GridSetup,
        Rules.HotkeyKeys, Rules.SubmitHandlers);

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
                compilation.GetTypeByMetadataName("Trailblazor.IDialogService"), Pairs(compilation, Hosted),
                Pairs(compilation, Listed), [.. new[] { "Trailblazor.TbSlider`1", "Trailblazor.TbRangeSlider`1" }
                    .Select(compilation.GetTypeByMetadataName).OfType<INamedTypeSymbol>()],
                compilation.GetTypeByMetadataName("Trailblazor.TbTreeView`2"),
                [.. Labeled.Select(compilation.GetTypeByMetadataName).OfType<INamedTypeSymbol>()],
                compilation.GetTypeByMetadataName("Trailblazor.TbAutocomplete`1"),
                [.. KeyedTrees.Select(compilation.GetTypeByMetadataName).OfType<INamedTypeSymbol>()],
                compilation.GetTypeByMetadataName("Trailblazor.TbDataGrid`1"),
                compilation.GetTypeByMetadataName("Trailblazor.TbHotkey"),
                compilation.GetTypeByMetadataName("Trailblazor.TbForm"));
            // Razor passes each .razor file to analyzers too, which is where a component's tag is found.
            var razorFiles = start.Options.AdditionalFiles
                .Where(file => file.Path.EndsWith(".razor", StringComparison.OrdinalIgnoreCase))
                .ToImmutableDictionary(file => file.Path, StringComparer.OrdinalIgnoreCase);
            start.RegisterOperationAction(operation => Analyze(operation, types, razorFiles), OperationKind.Invocation);
        });
    }

    private static void Analyze(OperationAnalysisContext context, KnownTypes types,
        ImmutableDictionary<string, AdditionalText> razorFiles)
    {
        var invocation = (IInvocationOperation)context.Operation;
        if (SymbolEqualityComparer.Default.Equals(invocation.TargetMethod.ContainingType, types.DialogService))
        {
            foreach (var argument in invocation.Arguments.Where(argument => argument.Parameter?.Name == "title"
                && IsBlank(argument.Value) && !TakesDefaultTitle(invocation, argument)))
            {
                context.ReportDiagnostic(Diagnostic.Create(Rules.NoDialogTitle, argument.Syntax.GetLocation()));
            }

            return;
        }

        if (ComponentUsage.Read(invocation, razorFiles, context.CancellationToken) is not { } usage)
        {
            return;
        }

        var component = usage.Component;
        // A decorative avatar is hidden from screen readers, so it needs no name.
        foreach (var (_, names) in types.Named.Where(entry => InheritsFrom(component, entry.Type)
            && !IsOn(usage, "Decorative")))
        {
            var shown = names.Where(name => char.IsUpper(name[0])).ToList();
            CheckName(context, usage, names, Rules.NoAccessibleName, component.Name, shown.Count > 2
                ? $"{string.Join(", ", shown.Take(shown.Count - 1))} or {shown.Last()}" : string.Join(" or ", shown));
        }

        if (types.Labeled.Any(type => InheritsFrom(component, type)) && !usage.Has("Trigger"))
        {
            CheckLabelOrIcon(context, usage);
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

        CheckTypeParameterNames(context, usage);
        CheckParent(context, invocation, usage, types.Hosted);
        CheckItemType(context, invocation, usage, types.Listed);
        CheckSliderType(context, invocation, usage, types.Sliders);
        CheckTreeSetup(context, invocation, usage, types.TreeView);
        CheckCodeAsText(context, invocation, usage);
        CheckKnownList(context, usage, types.Autocomplete);
        CheckReferenceKeys(context, invocation, usage, types);
        CheckGridSetup(context, usage, types.DataGrid);
        CheckHotkeyKeys(context, usage, types.Hotkey);
        CheckSubmitHandlers(context, usage, types.Form);

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

    // Repeats the component's own check: with an Icon it needs an AriaLabel, and without one a Label; a menu or
    // popover may set a Label beside its Icon instead. When the Icon may be null at run time, either name will do.
    private static void CheckLabelOrIcon(OperationAnalysisContext context, ComponentUsage usage)
    {
        var besideLabel = usage.Has("Label") && IconBesideLabel.Contains(usage.Component.ToDisplayString());
        string[] names = !usage.Has("Icon") || besideLabel ? ["Label"]
            : IsSurelyAnIcon(usage.Parameters["Icon"].Value) ? ["AriaLabel"] : ["Label", "AriaLabel"];
        var shown = Property(usage.Component, "Trigger") is null || names.Length > 1 ? names : [.. names, "Trigger"];
        CheckName(context, usage, names, Rules.NoAccessibleName, usage.Component.Name, string.Join(" or ", shown));
    }

    // An Icon? read from a model or a field may be null; one written in the markup is surely set.
    private static bool IsSurelyAnIcon(IOperation? value) => IsSurelySet(value) && Unwrap(value)?.Type
        is not INamedTypeSymbol { OriginalDefinition.SpecialType: SpecialType.System_Nullable_T };

    // An attribute named like a type parameter, such as T or TItem, on a generic Trailblazor component whose own type
    // parameter has another name.
    private static void CheckTypeParameterNames(OperationAnalysisContext context, ComponentUsage usage)
    {
        var component = usage.Component;
        var typeParameters = component.OriginalDefinition.TypeParameters;
        if (component.ContainingNamespace.ToDisplayString() != "Trailblazor" || typeParameters.Length == 0)
        {
            return;
        }

        foreach (var parameter in usage.Parameters)
        {
            var name = parameter.Key;
            if (IsTypeParameterName(name) && typeParameters.All(type => type.Name != name)
                && Property(component, name) is null)
            {
                var value = Unwrap(parameter.Value.Value)?.ConstantValue.Value as string ?? "...";
                var fix = string.Join(" or ", typeParameters.Select(type => $"{type.Name}=\"{value}\""));
                // Razor maps no unmatched attribute's name back to the page, so this points at the component.
                context.ReportDiagnostic(Diagnostic.Create(Rules.TypeParameterAsAttribute, usage.Location,
                    component.Name, name, fix));
            }
        }
    }

    // Warns when a page writes a component with no host around it. A non-Trailblazor component around it may render
    // it inside a host, and a non-page component may itself be rendered inside one, so neither is checked.
    private static void CheckParent(OperationAnalysisContext context, IInvocationOperation open, ComponentUsage usage,
        (INamedTypeSymbol Child, INamedTypeSymbol Host)[] hosted)
    {
        var hosts = hosted.Where(entry =>
            SymbolEqualityComparer.Default.Equals(usage.Component.OriginalDefinition, entry.Child))
            .Select(entry => entry.Host).ToList();
        if (hosts.Count == 0 || ComponentNesting.Enclosing(open) is not { } enclosing
            || enclosing.Any(owner => hosts.Any(host => InheritsFrom(owner, host))
                || owner.ContainingNamespace.ToDisplayString() != "Trailblazor")
            || !ComponentNesting.IsInPage(open))
        {
            return;
        }

        // The warning names a select, not the TbSelectBase that each kind of select shares.
        var names = hosts.Select(host => host.Name.EndsWith("Base", StringComparison.Ordinal)
            ? host.Name.Substring(0, host.Name.Length - "Base".Length) : host.Name);
        context.ReportDiagnostic(Diagnostic.Create(Rules.OutsideItsParent, usage.Location, usage.Component.Name,
            string.Join(" or ", names)));
    }

    // An item whose value can't be its list's type. The nearest list around it is the one it joins,
    // unless a component from outside Trailblazor comes first, which may put it in another.
    private static void CheckItemType(OperationAnalysisContext context, IInvocationOperation open,
        ComponentUsage usage, (INamedTypeSymbol Item, INamedTypeSymbol List)[] listed)
    {
        var lists = listed.Where(entry =>
            SymbolEqualityComparer.Default.Equals(usage.Component.OriginalDefinition, entry.Item))
            .Select(entry => entry.List).ToList();
        if (lists.Count == 0 || ComponentNesting.Enclosing(open) is not { } enclosing)
        {
            return;
        }

        foreach (var owner in enclosing.TakeWhile(owner =>
            owner.ContainingNamespace.ToDisplayString() == "Trailblazor"))
        {
            if (lists.Any(list => InheritsFrom(owner, list)))
            {
                var item = NonNullable(ComponentNesting.Resolved(usage.Component, open).TypeArguments[0]);
                var held = NonNullable(owner.TypeArguments[0]);
                if (!Fits(context.Compilation, item, held))
                {
                    context.ReportDiagnostic(Diagnostic.Create(Rules.ItemTypeMismatch, usage.Location,
                        usage.Component.Name, Shown(item), owner.Name, Shown(held)));
                }

                return;
            }
        }
    }

    private static void CheckSliderType(OperationAnalysisContext context, IInvocationOperation open,
        ComponentUsage usage, INamedTypeSymbol[] sliders)
    {
        // An app's own slider may take other type parameters, so only the library's sliders are checked.
        if (!sliders.Contains(usage.Component.OriginalDefinition, SymbolEqualityComparer.Default))
        {
            return;
        }

        var type = NonNullable(ComponentNesting.Resolved(usage.Component, open).TypeArguments[0]);
        if (type is not ITypeParameterSymbol and not IErrorTypeSymbol && !IsNumber(type))
        {
            context.ReportDiagnostic(Diagnostic.Create(Rules.SliderNotANumber, usage.Location, usage.Component.Name,
                Shown(type)));
        }
    }

    // A call such as GetColor(), a member path such as _model.Name, or a field such as _title.
    private static readonly Regex CodeShape =
        new(@"^(?<first>[A-Za-z_]\w*)(?<path>(\.[A-Za-z_]\w*)*)(?<call>\(.*\))?$", RegexOptions.CultureInvariant);

    // Markup text given to a string parameter that's almost certainly meant as code, such as Value="GetColor()":
    // shaped like code, and naming a value or a method the page can see. Razor maps a value written in C#, such as
    // @("GetColor()"), back to the .razor file but leaves markup text unmapped, so a C# string literal isn't reported.
    private static void CheckCodeAsText(OperationAnalysisContext context, IInvocationOperation open,
        ComponentUsage usage)
    {
        if (usage.Component.ContainingNamespace.ToDisplayString() != "Trailblazor")
        {
            return;
        }

        var component = ComponentNesting.Resolved(usage.Component, open);
        foreach (var parameter in usage.Parameters)
        {
            if (Property(component, parameter.Key)?.Type.SpecialType == SpecialType.System_String
                && Unwrap(parameter.Value.Value) is { ConstantValue: { HasValue: true, Value: string text } } value
                && !value.Syntax.GetLocation().GetMappedLineSpan().HasMappedPath
                && CodeShape.Match(text) is { Success: true } shape
                && (shape.Groups["path"].Length > 0 || shape.Groups["call"].Success || text[0] == '_')
                && Binds(value, text))
            {
                context.ReportDiagnostic(Diagnostic.Create(Rules.CodeAsText, parameter.Value.Location,
                    usage.Component.Name, parameter.Key, text));
            }
        }
    }

    // Whether the text, read as C# where the page wrote it, names a value or a method. A file name such as Program.cs
    // or App.razor starts with a type the page sees but names nothing, and "Size(cm)" doesn't call the Size enum.
    private static bool Binds(IOperation value, string text)
    {
        var bound = value.SemanticModel!.GetSpeculativeSymbolInfo(value.Syntax.SpanStart,
            SyntaxFactory.ParseExpression(text), SpeculativeBindingOption.BindAsExpression);
        var symbols = bound.Symbol is { } symbol ? [symbol] : bound.CandidateSymbols;
        return symbols.Any(found =>
            found is IMethodSymbol or IFieldSymbol or IPropertySymbol or ILocalSymbol or IParameterSymbol);
    }

    // Repeats the checks the tree view makes as it renders, in the same order, for the parameters set in markup.
    private static void CheckTreeSetup(OperationAnalysisContext context, IInvocationOperation open,
        ComponentUsage usage, INamedTypeSymbol? treeView)
    {
        if (!SymbolEqualityComparer.Default.Equals(usage.Component.OriginalDefinition, treeView))
        {
            return;
        }

        bool Set(string name) => IsGiven(usage, name);
        if (Set("ChildContent") && (Set("Items") || Set("ChildrenSelector") || Set("ParentKeySelector")
            || Set("LoadChildren")))
        {
            context.ReportDiagnostic(Diagnostic.Create(Rules.TreeSetup, usage.Location,
                "takes Items or TbTreeItems, not both"));
            return;
        }

        if (Set("ParentKeySelector") && (Set("ChildrenSelector") || Set("LoadChildren")))
        {
            context.ReportDiagnostic(Diagnostic.Create(Rules.TreeSetup, usage.Parameters["ParentKeySelector"].Location,
                "takes a ParentKeySelector or a ChildrenSelector and LoadChildren, not both"));
            return;
        }

        if (Set("ChildContent") && IsOn(usage, "Virtualize"))
        {
            context.ReportDiagnostic(Diagnostic.Create(Rules.TreeSetup, usage.Parameters["Virtualize"].Location,
                "virtualizes only Items, since TbTreeItems draw their rows where they are written"));
            return;
        }

        // Splatted attributes may hold the KeySelector.
        if (!usage.Has("KeySelector") && !usage.HasSplattedAttributes
            && !AreOwnKeys(context.Compilation, ComponentNesting.Resolved(usage.Component, open)))
        {
            context.ReportDiagnostic(Diagnostic.Create(Rules.TreeSetup, usage.Location,
                "needs a KeySelector when its items aren't their own keys"));
        }
    }

    // Repeats the checks the data grid makes as it renders, in the same order, for the parameters set in markup. A grid
    // with splatted attributes may get its Height from them.
    private static void CheckGridSetup(OperationAnalysisContext context, ComponentUsage usage,
        INamedTypeSymbol? dataGrid)
    {
        if (!SymbolEqualityComparer.Default.Equals(usage.Component.OriginalDefinition, dataGrid))
        {
            return;
        }

        bool Set(string name) => IsGiven(usage, name);
        var virtualizes = IsOn(usage, "Virtualize");
        var (parameter, problem) =
            Set("Items") && Set("DataProvider") ? ("DataProvider", "takes Items or a DataProvider, not both")
            : Set("DataProvider") && Set("Groups")
                ? ("Groups", "groups only rows held in Items, not rows from a DataProvider")
            : Set("ChildrenSelector") && (Set("DataProvider") || Set("Groups") || Set("OnMove") || Set("PageSize"))
                ? ("ChildrenSelector", "with a ChildrenSelector takes its rows from Items, and can't group them, " +
                    "move them or split them into pages; set Virtualize to show many rows")
            : virtualizes && Set("DetailTemplate")
                ? ("DetailTemplate", "that virtualizes its rows can't have a DetailTemplate")
            : virtualizes && (Set("PageSize") || !usage.Has("Height") && !IsOn(usage, "Fill")
                && !usage.HasSplattedAttributes)
                ? ("Virtualize", "that virtualizes its rows needs a Height or Fill, and no PageSize")
            : (null, null);
        if (problem is not null)
        {
            context.ReportDiagnostic(Diagnostic.Create(Rules.GridSetup, usage.Parameters[parameter!].Location,
                problem));
        }
    }

    // Repeats the hotkey's own check of keys written as text. Splatted attributes may hold AllowCharacterKey.
    private static void CheckHotkeyKeys(OperationAnalysisContext context, ComponentUsage usage,
        INamedTypeSymbol? hotkey)
    {
        if (!SymbolEqualityComparer.Default.Equals(usage.Component, hotkey)
            || !usage.Parameters.TryGetValue("Keys", out var keys)
            || Unwrap(keys.Value)?.ConstantValue is not { HasValue: true, Value: string text })
        {
            return;
        }

        var problem = !HotkeyKeys.IsReadable(text) ? "can't be read: end them with one key, after any of Mod, " +
                "Ctrl, Alt, Shift and Meta, as in \"Mod+S\""
            : HotkeyKeys.IsCharacterKey(text) && !IsOn(usage, "AllowCharacterKey") && !usage.HasSplattedAttributes
                ? "are a key people type, with no Mod, Ctrl, Alt or Meta. Set AllowCharacterKey, and let people turn " +
                    "the hotkey off."
            : null;
        if (problem is not null)
        {
            context.ReportDiagnostic(Diagnostic.Create(Rules.HotkeyKeys, keys.Location, text, problem));
        }
    }

    // OnSubmit takes the place of the form's own check, which is what raises OnValidSubmit and OnInvalidSubmit.
    private static void CheckSubmitHandlers(OperationAnalysisContext context, ComponentUsage usage,
        INamedTypeSymbol? form)
    {
        if (!SymbolEqualityComparer.Default.Equals(usage.Component, form) || !IsGiven(usage, "OnSubmit"))
        {
            return;
        }

        var others = new[] { "OnValidSubmit", "OnInvalidSubmit" }.Where(name => IsGiven(usage, name)).ToList();
        if (others.Count > 0)
        {
            context.ReportDiagnostic(Diagnostic.Create(Rules.SubmitHandlers, usage.Parameters["OnSubmit"].Location,
                string.Join(" and ", others)));
        }
    }

    // OpenOnFocus on a list the page already has is there so people can pick without typing, which is a select's
    // job. A select has no Search, FreeText, OnTextChanged, Columns or MinCharacters, and splatted attributes may
    // hold any of them.
    private static void CheckKnownList(OperationAnalysisContext context, ComponentUsage usage,
        INamedTypeSymbol? autocomplete)
    {
        if (SymbolEqualityComparer.Default.Equals(usage.Component.OriginalDefinition, autocomplete)
            && usage.Parameters.TryGetValue("OpenOnFocus", out var opens)
            && Unwrap(opens.Value)?.ConstantValue is { HasValue: true, Value: true }
            && usage.Parameters.TryGetValue("Items", out var items) && IsSurelySet(items.Value)
            && !usage.Has("Search") && !IsOn(usage, "FreeText") && !usage.Has("OnTextChanged")
            && !usage.Has("Columns") && !WaitsForCharacters(usage)
            && !usage.HasSplattedAttributes)
        {
            context.ReportDiagnostic(Diagnostic.Create(Rules.KnownListAutocomplete, opens.Location,
                usage.Component.Name));
        }
    }

    // A tree view or a tree select whose items are their own keys, of a class that compares by reference. A tree
    // written with TbTreeItems is left out, since its values are written in the markup, and so is one with splatted
    // attributes, which may hold the KeySelector. An app's own tree may take other type parameters, so only the
    // library's trees are checked.
    private static void CheckReferenceKeys(OperationAnalysisContext context, IInvocationOperation open,
        ComponentUsage usage, KnownTypes types)
    {
        if (!types.KeyedTrees.Contains(usage.Component.OriginalDefinition, SymbolEqualityComparer.Default)
            || usage.Has("KeySelector") || usage.Has("ChildContent") || usage.HasSplattedAttributes)
        {
            return;
        }

        var tree = ComponentNesting.Resolved(usage.Component, open);
        if (SymbolEqualityComparer.Default.Equals(tree.TypeArguments[0], tree.TypeArguments[1])
            && ComparesByReference(tree.TypeArguments[0]))
        {
            context.ReportDiagnostic(Diagnostic.Create(Rules.ReferenceKeys, usage.Location, usage.Component.Name));
        }
    }

    // A class that keeps object's Equals or its GetHashCode, both of which a record and a string override: a tree
    // finds a key by its hash and then by Equals. What an object holds isn't known, so object isn't counted.
    private static bool ComparesByReference(ITypeSymbol type) =>
        type is INamedTypeSymbol { TypeKind: TypeKind.Class, SpecialType: not SpecialType.System_Object }
        && !(Overrides(type, "Equals") && Overrides(type, "GetHashCode"));

    private static bool Overrides(ITypeSymbol type, string method)
    {
        for (var current = type; current is { SpecialType: not SpecialType.System_Object }; current = current.BaseType)
        {
            if (current.GetMembers(method).OfType<IMethodSymbol>().Any(found => found.IsOverride))
            {
                return true;
            }
        }

        return false;
    }

    private static bool WaitsForCharacters(ComponentUsage usage) =>
        usage.Parameters.TryGetValue("MinCharacters", out var least)
        && !(Unwrap(least.Value)?.ConstantValue is { HasValue: true, Value: 0 });

    private static bool IsGiven(ComponentUsage usage, string name) =>
        usage.Parameters.TryGetValue(name, out var parameter) && IsSurelySet(parameter.Value);

    // Whether markup surely gives the parameter a value. A component's own parameter passed along, or a ?: choice,
    // may be null as the page runs, and the tree view's checks treat null as not given.
    private static bool IsSurelySet(IOperation? value) => Unwrap(value) switch
    {
        null => true,
        { ConstantValue: { HasValue: true, Value: null } } => false,
        IConditionalOperation => false,
        IPropertyReferenceOperation { Property: var property } => !property.GetAttributes().Any(attribute =>
            attribute.AttributeClass?.ToDisplayString() == "Microsoft.AspNetCore.Components.ParameterAttribute"),
        _ => true,
    };

    // Whether each item can be its own key, as the tree view checks with TKey.IsAssignableFrom(TItem).
    private static bool AreOwnKeys(Compilation compilation, INamedTypeSymbol tree)
    {
        var (item, key) = (tree.TypeArguments[0], tree.TypeArguments[1]);
        if (item is ITypeParameterSymbol or IErrorTypeSymbol || key is ITypeParameterSymbol or IErrorTypeSymbol)
        {
            return true;
        }

        var conversion = compilation.ClassifyConversion(item, key);
        return conversion.IsIdentity || (conversion.IsImplicit && (conversion.IsReference || conversion.IsBoxing));
    }

    private static bool IsNumber(ITypeSymbol type) => type.SpecialType is SpecialType.System_SByte
        or SpecialType.System_Byte or SpecialType.System_Int16 or SpecialType.System_UInt16 or SpecialType.System_Int32
        or SpecialType.System_UInt32 or SpecialType.System_Int64 or SpecialType.System_UInt64
        or SpecialType.System_Single or SpecialType.System_Double or SpecialType.System_Decimal;

    // A value of one type can be the other at run time: the same type, or one a cast could reach without changing it.
    // A type known only as the page runs could be anything.
    private static bool Fits(Compilation compilation, ITypeSymbol item, ITypeSymbol held)
    {
        if (item is ITypeParameterSymbol or IErrorTypeSymbol || held is ITypeParameterSymbol or IErrorTypeSymbol)
        {
            return true;
        }

        var conversion = compilation.ClassifyConversion(item, held);
        return conversion.IsIdentity || conversion.IsReference || conversion.IsBoxing || conversion.IsUnboxing;
    }

    private static ITypeSymbol NonNullable(ITypeSymbol type) =>
        type is INamedTypeSymbol { OriginalDefinition.SpecialType: SpecialType.System_Nullable_T } nullable
            ? nullable.TypeArguments[0] : type;

    private static string Shown(ITypeSymbol type) => type.WithNullableAnnotation(NullableAnnotation.NotAnnotated)
        .ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat);

    private static bool IsTypeParameterName(string name) =>
        name.Length > 0 && name[0] == 'T' && (name.Length == 1 || char.IsUpper(name[1]))
        && name.All(char.IsLetterOrDigit);

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

    // A null title gives way to the content's DefaultTitle; a dynamic dialog's content is checked as it opens.
    private static bool TakesDefaultTitle(IInvocationOperation invocation, IArgumentOperation title)
    {
        if (Unwrap(title.Value)?.ConstantValue.Value is not null)
        {
            return false;
        }

        var method = invocation.TargetMethod;
        if (method.TypeParameters.FirstOrDefault(type => type.Name == "TDialog") is not { } dialog)
        {
            return method.Name.StartsWith("ShowDynamic", StringComparison.Ordinal);
        }

        // A dialog type that is itself a type parameter is known only as it opens, so it's checked then.
        if (method.TypeArguments[dialog.Ordinal] is ITypeParameterSymbol)
        {
            return true;
        }

        for (var type = method.TypeArguments[dialog.Ordinal]; type is not null; type = type.BaseType)
        {
            if (type.GetMembers("DefaultTitle").OfType<IPropertySymbol>().Any(property => property.IsOverride))
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsBlank(IOperation? value) =>
        Unwrap(value)?.ConstantValue is { HasValue: true } constant
        && (constant.Value is null || constant.Value is string text && string.IsNullOrWhiteSpace(text));

    // The value as written in markup, looking through conversions, Razor's type checks, the EventCallback.Factory call
    // that wraps an EventCallback passed along, and the parameters of its type-inference helpers, which receive the
    // markup's values from a call elsewhere in the page.
    private static IOperation? Unwrap(IOperation? value) => value switch
    {
        IConversionOperation conversion => Unwrap(conversion.Operand),
        IInvocationOperation { TargetMethod.Name: "TypeCheck", Arguments.Length: 1 } check =>
            Unwrap(check.Arguments[0].Value),
        IInvocationOperation
        {
            TargetMethod: { Name: "Create", ContainingType.Name: "EventCallbackFactory" }, Arguments.Length: 2,
        } create when create.Arguments[1].Parameter?.Type.Name == "EventCallback" => Unwrap(create.Arguments[1].Value),
        IParameterReferenceOperation
        {
            Parameter.ContainingSymbol: IMethodSymbol { ContainingType.Name: "TypeInference" },
        } reference => Unwrap(ArgumentAtCall(reference)),
        _ => value,
    };

    private static IOperation? ArgumentAtCall(IParameterReferenceOperation reference) =>
        ComponentNesting.CallTo(reference)?.Arguments
            .FirstOrDefault(argument => argument.Parameter?.Ordinal == reference.Parameter.Ordinal)?.Value;

    private static (INamedTypeSymbol, INamedTypeSymbol)[] Pairs(Compilation compilation,
        (string, string)[] table) =>
        table.Select(entry => (compilation.GetTypeByMetadataName(entry.Item1),
                compilation.GetTypeByMetadataName(entry.Item2)))
            .Where(entry => entry.Item1 is not null && entry.Item2 is not null)
            .Select(entry => (entry.Item1!, entry.Item2!)).ToArray();

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
        INamedTypeSymbol? dialogService, (INamedTypeSymbol Child, INamedTypeSymbol Host)[] hosted,
        (INamedTypeSymbol Item, INamedTypeSymbol List)[] listed, INamedTypeSymbol[] sliders,
        INamedTypeSymbol? treeView, INamedTypeSymbol[] labeled, INamedTypeSymbol? autocomplete,
        INamedTypeSymbol[] keyedTrees, INamedTypeSymbol? dataGrid, INamedTypeSymbol? hotkey, INamedTypeSymbol? form)
    {
        public (INamedTypeSymbol Type, string[] Names)[] Named { get; } = named;

        public INamedTypeSymbol FieldBase { get; } = fieldBase;

        public (INamedTypeSymbol Type, string[] Names)[] OtherFields { get; } = otherFields;

        public INamedTypeSymbol? Dialog { get; } = dialog;

        public INamedTypeSymbol? DialogService { get; } = dialogService;

        public (INamedTypeSymbol Child, INamedTypeSymbol Host)[] Hosted { get; } = hosted;

        public (INamedTypeSymbol Item, INamedTypeSymbol List)[] Listed { get; } = listed;

        public INamedTypeSymbol[] Sliders { get; } = sliders;

        public INamedTypeSymbol? TreeView { get; } = treeView;

        public INamedTypeSymbol[] Labeled { get; } = labeled;

        public INamedTypeSymbol? Autocomplete { get; } = autocomplete;

        public INamedTypeSymbol[] KeyedTrees { get; } = keyedTrees;

        public INamedTypeSymbol? DataGrid { get; } = dataGrid;

        public INamedTypeSymbol? Hotkey { get; } = hotkey;

        public INamedTypeSymbol? Form { get; } = form;
    }
}
