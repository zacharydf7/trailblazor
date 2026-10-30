using AngleSharp.Dom;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.Extensions.DependencyInjection;

namespace Trailblazor.Tests.Contract;

// Rules every public Trailblazor component must follow, checked against all of them by reflection.
public class ComponentContractTests : BunitContext
{
    // TbRoot renders no element of its own, and TbStyles only a <link>, which takes no class or style; TbRadio,
    // TbTab and TbOption only render inside their parent (see their parents' tests).
    private static readonly Type[] NotRenderedAlone =
        [typeof(TbRoot), typeof(TbStyles), typeof(TbRadio<>), typeof(TbTab), typeof(TbOption<>)];

    // Generic components, such as fields, are checked with string as their type argument unless listed here.
    private static readonly Dictionary<Type, Type> TypeArguments = new()
    {
        [typeof(TbNumberField<>)] = typeof(int), [typeof(TbCheckbox<>)] = typeof(bool),
    };

    // Components that render nothing until they are shown are checked shown.
    private static readonly Dictionary<Type, string> ShownBy = new() { [typeof(TbOverlay)] = nameof(TbOverlay.Visible) };

    public ComponentContractTests()
    {
        this.AddTrailblazor();
        SetRendererInfo(new RendererInfo("Static", isInteractive: false));
    }

    public static TheoryData<Type> Components => new(typeof(TbComponentBase).Assembly.GetExportedTypes()
        .Where(type => !type.IsAbstract && typeof(IComponent).IsAssignableFrom(type))
        .Except(NotRenderedAlone)
        .Select(type => type.IsGenericTypeDefinition
            ? type.MakeGenericType(TypeArguments.GetValueOrDefault(type, typeof(string)))
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
            if (ShownBy.TryGetValue(component, out var shown))
            {
                builder.AddAttribute(5, shown, true);
            }

            AddRequiredParameters(builder, component, sequence: 6);
            builder.CloseComponent();
        });

        var root = cut.Nodes.OfType<IElement>().First();
        Assert.Contains("contract-class", root.ClassList);
        Assert.Contains("outline: 1px solid red", root.GetAttribute("style"));
        Assert.Equal("contract-id", root.Id);
        Assert.Equal("yes", root.GetAttribute("data-contract"));
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
                var type when type == typeof(Icon) => new Icon("<path d=\"M4 4h16v16H4z\" />"),
                _ => throw new NotSupportedException($"Add a sample {property.PropertyType.Name} to the contract " +
                    $"kit for {component.Name}.{property.Name}."),
            };
            builder.AddAttribute(sequence++, property.Name, value);
        }
    }
}
