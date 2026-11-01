using AngleSharp.Dom;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.Extensions.DependencyInjection;

namespace Trailblazor.Tests.Contract;

// Rules every public Trailblazor component must follow, checked against all of them by reflection.
public class ComponentContractTests : BunitContext
{
    // TbRoot renders no element of its own, TbStyles only a <link> and TbThemeHead only a <script>, which take no class
    // or style; TbRadio, TbCheckboxItem, TbTab, TbOption, TbToggleItem, TbSplitterPane, TbTreeItem, TbMenuRadioItem,
    // TbDialogDescription and TbColumn only render inside their parent (see their parents' tests).
    private static readonly Type[] NotRenderedAlone =
    [
        typeof(TbRoot), typeof(TbStyles), typeof(TbThemeHead), typeof(TbRadio<>), typeof(TbCheckboxItem<>),
        typeof(TbTab), typeof(TbOption<>), typeof(TbToggleItem<>), typeof(TbSplitterPane), typeof(TbTreeItem<>),
        typeof(TbMenuRadioItem<>), typeof(TbDialogDescription), typeof(TbColumn<>),
    ];

    // Generic components, such as fields, are checked with string as their type argument unless listed here.
    private static readonly Dictionary<Type, Type> TypeArguments = new()
    {
        [typeof(TbNumberField<>)] = typeof(int), [typeof(TbCheckbox<>)] = typeof(bool),
        [typeof(TbSlider<>)] = typeof(int), [typeof(TbRangeSlider<>)] = typeof(int),
        [typeof(TbDateRangePicker<>)] = typeof(DateOnly),
    };

    // Components that render nothing until they are shown are checked shown; a popover, a menu and a tooltip get the
    // label or text they need when they are given neither.
    private static readonly Dictionary<Type, (string Name, object Value)> Setup = new()
    {
        [typeof(TbOverlay)] = (nameof(TbOverlay.Visible), true),
        [typeof(TbPopover)] = (nameof(TbPopover.Label), "Sample"),
        [typeof(TbMenu)] = (nameof(TbMenu.Label), "Sample"),
        [typeof(TbTooltip)] = (nameof(TbTooltip.Text), "Sample"),
        [typeof(TbDialog)] = (nameof(TbDialog.Open), true),
    };

    public ComponentContractTests()
    {
        this.AddTrailblazor();
        SetRendererInfo(new RendererInfo("Static", isInteractive: false));
    }

    public static TheoryData<Type> Components => new(typeof(TbComponentBase).Assembly.GetExportedTypes()
        .Where(type => !type.IsAbstract && typeof(IComponent).IsAssignableFrom(type))
        .Except(NotRenderedAlone)
        .Select(type => type.IsGenericTypeDefinition
            ? type.MakeGenericType([.. type.GetGenericArguments()
                .Select(_ => TypeArguments.GetValueOrDefault(type, typeof(string)))])
            : type));

    [Theory, MemberData(nameof(Components))]
    public void Render_ShouldPutClassStyleAndAttributesOnTheRootElement_WhenTheyArePassed(Type component)
    {
        var cut = Render(builder =>
        {
            builder.OpenComponent(0, component);
            builder.AddAttribute(1, nameof(TbComponentBase.Class), "contract-class");
            builder.AddAttribute(2, nameof(TbComponentBase.Style), "outline: 1px solid red");
            builder.AddAttribute(3, "id", "contract-id");
            builder.AddAttribute(4, "data-contract", "yes");
            if (Setup.TryGetValue(component, out var setup))
            {
                builder.AddAttribute(5, setup.Name, setup.Value);
            }

            AddRequiredParameters(builder, component, sequence: 6);
            builder.CloseComponent();
        });

        var root = cut.Nodes.OfType<IElement>().First();
        Assert.Contains("contract-class", root.ClassList);
        Assert.Contains("outline: 1px solid red", root.GetAttribute("style"));
        Assert.Equal("yes", root.GetAttribute("data-contract"));
        // A field's id names its control, apart from one with two controls or a group, which keeps it on its root.
        var named = Assert.Single(cut.FindAll("#contract-id"));
        if (named != root)
        {
            Assert.True(typeof(IFormField).IsAssignableFrom(component), $"{component.Name} moved its id off its root.");
            Assert.Equal("input", named.GetAttribute("data-part"));
        }
    }

    public static TheoryData<Type> Fields =>
        new(Components.Select(row => row.Data).Where(typeof(IFormField).IsAssignableFrom));

    [Theory, MemberData(nameof(Fields))]
    public void Render_ShouldShowTheParentsErrorAndMarkTheFieldInvalid_WhenTheParentSetsOne(Type field)
    {
        var cut = Render(builder =>
        {
            builder.OpenComponent(0, field);
            builder.AddAttribute(1, nameof(TbTextField<string>.Error), "That name is taken.");
            AddRequiredParameters(builder, field, sequence: 2);
            builder.CloseComponent();
        });

        Assert.Equal("That name is taken.", cut.Find("[data-part=error]").TextContent.Trim());
        Assert.NotEmpty(cut.FindAll("[aria-invalid=true]"));
    }

    public static TheoryData<Type> BoxedFields => new(Components.Select(row => row.Data).Where(type =>
    {
        for (var parent = type.BaseType; parent is not null; parent = parent.BaseType)
        {
            if (parent.IsGenericType && parent.GetGenericTypeDefinition() == typeof(TbBoxedFieldBase<>))
            {
                return true;
            }
        }

        return false;
    }));

    [Theory, MemberData(nameof(BoxedFields))]
    public void Render_ShouldDrawTheBoxInItsVariantAndSize_WhenGivenThem(Type field)
    {
        var cut = Render(builder =>
        {
            builder.OpenComponent(0, field);
            builder.AddAttribute(1, nameof(TbTextField<string>.Variant), FieldVariant.Filled);
            builder.AddAttribute(2, nameof(TbTextField<string>.Size), Size.Large);
            AddRequiredParameters(builder, field, sequence: 3);
            builder.CloseComponent();
        });
        var plain = Render(builder =>
        {
            builder.OpenComponent(0, field);
            AddRequiredParameters(builder, field, sequence: 1);
            builder.CloseComponent();
        });

        Assert.Superset(new HashSet<string> { "tb-field", "tb-field--filled", "tb-field--large" },
            cut.Find(".tb-field").ClassList.ToHashSet());
        Assert.Superset(new HashSet<string> { "tb-field--outlined", "tb-field--medium" },
            plain.Find(".tb-field").ClassList.ToHashSet());
    }

    // Supplies a sample value for each [EditorRequired] parameter, so every component renders without setup.
    private static void AddRequiredParameters(RenderTreeBuilder builder, Type component, int sequence)
    {
        var required = component.GetProperties()
            .Where(property => property.IsDefined(typeof(EditorRequiredAttribute), inherit: true));
        foreach (var property in required)
        {
            object value = property.PropertyType switch
            {
                var type when type == typeof(string) => "Sample",
                var type when type == typeof(int) => 3,
                var type when type == typeof(Icon) => new Icon("<path d=\"M4 4h16v16H4z\" />"),
                var type when type == typeof(IEnumerable<string>) => Array.Empty<string>(),
                var type when type == typeof(IReadOnlyList<string>) => Array.Empty<string>(),
                var type when type == typeof(RenderFragment<string>) =>
                    (RenderFragment<string>)(text => item => item.AddContent(0, text)),
                var type when type == typeof(Func<string, object>) => (Func<string, object>)(text => text),
                _ => throw new NotSupportedException($"Add a sample {property.PropertyType.Name} to the contract " +
                    $"kit for {component.Name}.{property.Name}."),
            };
            builder.AddAttribute(sequence++, property.Name, value);
        }
    }
}
