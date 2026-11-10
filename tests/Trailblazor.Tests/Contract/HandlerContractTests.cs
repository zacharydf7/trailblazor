using System.Linq.Expressions;
using AngleSharp.Dom;
using Bunit;
using Bunit.Rendering;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace Trailblazor.Tests.Contract;

// What every component promises about the elements a test finds: each keeps its event handlers from render to render,
// so a test can use a part, let the component render again and use the same found part again. The theory finds each
// part of every public component once and raises each of its events twice, including parts that only show once others
// are used, such as an open menu's items. A text box's input and change events are left out: Blazor's @bind gives them
// a new handler on every render, so a test has to find the box again anyway. Other custom events, such as
// ontbfocusleave, aren't raised.
public class HandlerContractTests : BunitContext
{
    private static readonly Dictionary<string, Func<IElement, Task>> Events = new()
    {
        ["onclick"] = part => part.ClickAsync(new MouseEventArgs { Detail = 1 }),
        ["ondblclick"] = part => part.DoubleClickAsync(new MouseEventArgs { Detail = 2 }),
        ["onchange"] = part => part.ChangeAsync(new ChangeEventArgs
            { Value = part.GetAttribute("type") == "checkbox" ? true : part.GetAttribute("value") }),
        ["onkeydown"] = part => part.KeyDownAsync(new KeyboardEventArgs { Key = "Shift" }),
        ["onkeyup"] = part => part.KeyUpAsync(new KeyboardEventArgs { Key = "Shift" }),
        ["ontbrowclick"] = part => part.TriggerEventAsync("ontbrowclick", new RowClickEventArgs()),
        ["ontbrowextend"] = part => part.TriggerEventAsync("ontbrowextend", new RowExtendEventArgs()),
        ["ontbeditstart"] = part => part.TriggerEventAsync("ontbeditstart", EventArgs.Empty),
    };

    private static readonly string Handled = string.Join(", ", Events.Keys.Select(name => $"[blazor\\:{name}]"));

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
    };

    public HandlerContractTests() => this.AddTrailblazor();

    [Theory, MemberData(nameof(DisposeContractTests.Components), MemberType = typeof(DisposeContractTests))]
    public async Task Part_ShouldTakeItsEventsAgain_WhenFoundOnceAndTheComponentRendersAgain(Type component)
    {
        var cut = Render<DynamicComponent>(parameters => DisposeContractTests.Host(parameters, component, Parameters));

        for (var at = 0; at < cut.FindAll(Handled).Count; at++)
        {
            var part = cut.FindAll(Handled)[at];
            await UseAsync(cut, part, at);
            await UseAsync(cut, part, at);
        }
    }

    // Raises each event the part handles, unless an earlier event removed the part. Change is raised only on checkboxes
    // and radio buttons, which a click changes; a text box's change comes from @bind.
    private static async Task UseAsync(IRenderedComponent<DynamicComponent> cut, IElement part, int at)
    {
        foreach (var (name, raise) in Events.Where(pair => part.HasAttribute($"blazor:{pair.Key}")
            && (pair.Key != "onchange" || part.Matches("[type=checkbox], [type=radio]"))))
        {
            if (cut.FindAll(Handled).ElementAtOrDefault(at) is not { } shown || !IsSamePart(part, shown))
            {
                return;
            }

            var error = await Record.ExceptionAsync(() => raise(part));
            Assert.False(error is UnknownEventHandlerIdException, $"{Describe(part)} lost its {name} handler.");
            Assert.True(error is null or MissingEventHandlerException, $"{Describe(part)} failed on {name}: {error}");
        }
    }

    // Label and text aren't compared, since they can change to show the part's new state, as a picked day's does.
    private static bool IsSamePart(IElement before, IElement after) => before.TagName == after.TagName
        && before.Id == after.Id && before.GetAttribute("data-part") == after.GetAttribute("data-part");

    private static string Describe(IElement part) => $"<{part.TagName.ToLowerInvariant()} "
        + $"data-part=\"{part.GetAttribute("data-part")}\" aria-label=\"{part.GetAttribute("aria-label")}\">";

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
