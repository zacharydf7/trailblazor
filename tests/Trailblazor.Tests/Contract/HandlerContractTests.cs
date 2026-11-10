using System.Linq.Expressions;
using Bunit;
using Microsoft.AspNetCore.Components;

namespace Trailblazor.Tests.Contract;

// What every component promises about the elements a test finds: each keeps its event handlers from render to render,
// so a test can use a part, let the component render again and use the same found part again. Checked for every public
// component, including the parts that only show once others are used, such as an open menu's items.
public class HandlerContractTests : BunitContext
{
    // Components whose repeated parts come from their items get two, so those parts are used too.
    private static readonly Dictionary<Type, Dictionary<string, object>> Items = new()
    {
        [typeof(TbDataGrid<string>)] = new()
        {
            ["Items"] = new[] { "Pine", "Cedar" }, ["ChildContent"] = NameColumn(),
            ["SelectionMode"] = SelectionMode.Multiple,
        },
        [typeof(TbTreeView<string, string>)] = new() { ["ChildContent"] = TwoTreeItems() },
        [typeof(TbTagInput)] = new() { ["Value"] = new[] { "Pine", "Cedar" } },
        [typeof(TbColorPicker)] = new() { ["Palette"] = new[] { "#336699", "#993366" } },
        [typeof(TbCarousel)] = new() { ["ChildContent"] = TwoSlides() },
        [typeof(TbTransferList<string>)] = new() { ["Items"] = new[] { "Pine", "Cedar" } },
    };

    public HandlerContractTests() => this.AddTrailblazor();

    [Theory, MemberData(nameof(DisposeContractTests.Components), MemberType = typeof(DisposeContractTests))]
    public async Task Part_ShouldTakeItsEventsAgain_WhenFoundOnceAndTheComponentRendersAgain(Type component)
    {
        var cut = Render<DynamicComponent>(parameters => DisposeContractTests.Host(parameters, component, Parameters));

        Assert.Empty(await ContractRules.CheckHandlersAsync(cut));
    }

    private static Dictionary<string, object> Parameters(Type component)
    {
        var values = ValueContractTests.BaseParameters(component);
        if (ValueContractTests.Content.TryGetValue(component, out var content))
        {
            values[content.Name] = content.Value;
        }

        foreach (var (name, value) in Items.GetValueOrDefault(component) ?? [])
        {
            values[name] = value;
        }

        return values;
    }

    private static RenderFragment NameColumn() => builder =>
    {
        builder.OpenComponent<TbColumn<string>>(0);
        builder.AddAttribute(1, nameof(TbColumn<string>.Field), (Expression<Func<string, object?>>)(name => name));
        builder.AddAttribute(2, nameof(TbColumn<string>.Title), "Name");
        builder.AddAttribute(3, nameof(TbColumn<string>.Sortable), true);
        builder.CloseComponent();
    };

    private static RenderFragment TwoTreeItems() => builder =>
    {
        foreach (var item in new[] { "Pine", "Cedar" })
        {
            builder.OpenComponent<TbTreeItem<string>>(0);
            builder.AddAttribute(1, nameof(TbTreeItem<string>.Value), item);
            builder.AddAttribute(2, nameof(TbTreeItem<string>.Text), item);
            builder.CloseComponent();
        }
    };

    private static RenderFragment TwoSlides() => builder =>
    {
        foreach (var slide in new[] { "Pine", "Cedar" })
        {
            builder.OpenComponent<TbCarouselSlide>(0);
            builder.AddAttribute(1, "ChildContent", (RenderFragment)(text => text.AddContent(0, slide)));
            builder.CloseComponent();
        }
    };
}
