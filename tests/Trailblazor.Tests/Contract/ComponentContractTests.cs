using AngleSharp.Dom;
using Bunit;
using Microsoft.AspNetCore.Components;

namespace Trailblazor.Tests.Contract;

// Rules every public Trailblazor component must follow, checked against all of them by reflection.
public class ComponentContractTests : BunitContext
{
    // TbRoot only adds the stylesheet to the page head; it renders no element of its own.
    private static readonly Type[] RenderNoElement = [typeof(TbRoot)];

    public static TheoryData<Type> Components => new(typeof(TbComponentBase).Assembly.GetExportedTypes()
        .Where(type => !type.IsAbstract && typeof(IComponent).IsAssignableFrom(type))
        .Except(RenderNoElement));

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
            builder.CloseComponent();
        });

        var root = cut.Nodes.OfType<IElement>().First();
        Assert.Contains("contract-class", root.ClassList);
        Assert.Contains("outline: 1px solid red", root.GetAttribute("style"));
        Assert.Equal("contract-id", root.Id);
        Assert.Equal("yes", root.GetAttribute("data-contract"));
    }
}
