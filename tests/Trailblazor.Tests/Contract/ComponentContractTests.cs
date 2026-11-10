using AngleSharp.Dom;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;

namespace Trailblazor.Tests.Contract;

// Rules every public Trailblazor component must follow, checked against all of them by reflection.
public class ComponentContractTests : BunitContext
{
    // TbRoot and TbDialogTitle render no element of their own, TbStyles only a <link> and TbThemeHead only a <script>,
    // which take no class or style; TbRadio, TbCheckboxItem, TbTab, TbOption, TbToggleItem, TbSplitterPane,
    // TbTreeItem, TbMenuRadioItem, TbDialogDescription, TbColumn, TbColumnGroup, TbTableOfContentsItem, TbBreadcrumb,
    // TbNavLink, TbNavGroup, TbStep and TbCarouselSlide only render inside their parent (see their parents' tests).
    internal static readonly Type[] NotRenderedAlone =
    [
        typeof(TbRoot), typeof(TbStyles), typeof(TbThemeHead), typeof(TbRadio<>), typeof(TbCheckboxItem<>),
        typeof(TbTab), typeof(TbOption<>), typeof(TbToggleItem<>), typeof(TbSplitterPane), typeof(TbTreeItem<>),
        typeof(TbMenuRadioItem<>), typeof(TbDialogDescription), typeof(TbColumn<>), typeof(TbDialogTitle),
        typeof(TbTableOfContentsItem), typeof(TbBreadcrumb), typeof(TbNavLink), typeof(TbNavGroup),
        typeof(TbStep), typeof(TbCarouselSlide), typeof(TbColumnGroup<>),
    ];

    // Generic components, such as fields, are checked with string as their type argument unless listed here.
    private static readonly Dictionary<Type, Type> TypeArguments = new()
    {
        [typeof(TbNumberField<>)] = typeof(int), [typeof(TbCheckbox<>)] = typeof(bool),
        [typeof(TbSlider<>)] = typeof(int), [typeof(TbRangeSlider<>)] = typeof(int),
        [typeof(TbDateRangePicker<>)] = typeof(DateOnly), [typeof(TbCalendar<>)] = typeof(DateOnly),
        [typeof(TbDatePicker<>)] = typeof(DateOnly), [typeof(TbTimePicker<>)] = typeof(TimeOnly),
        [typeof(TbDateTimePicker<>)] = typeof(DateTime),
    };

    // Components that render nothing until they are shown are checked shown; a popover, a popconfirm, a menu, a
    // tooltip, a progress, an avatar, a kbd, a virtual list, a tab and a column get the label, text, name, keys, items
    // or title they need when they are given none.
    internal static readonly Dictionary<Type, (string Name, object Value)> Setup = new()
    {
        [typeof(TbOverlay)] = (nameof(TbOverlay.Visible), true),
        [typeof(TbPopover)] = (nameof(TbPopover.Label), "Sample"),
        [typeof(TbPopconfirm)] = (nameof(TbPopconfirm.Label), "Sample"),
        [typeof(TbMenu)] = (nameof(TbMenu.Label), "Sample"),
        [typeof(TbTooltip)] = (nameof(TbTooltip.Text), "Sample"),
        [typeof(TbProgress)] = (nameof(TbProgress.Label), "Sample"),
        [typeof(TbDialog)] = (nameof(TbDialog.Open), true),
        [typeof(TbDrawer)] = (nameof(TbDrawer.Open), true),
        [typeof(TbAvatar)] = (nameof(TbAvatar.Name), "Sample"),
        [typeof(TbKbd)] = (nameof(TbKbd.Keys), "Ctrl+K"),
        [typeof(TbVirtualList<string>)] = (nameof(TbVirtualList<string>.Items), Array.Empty<string>()),
        [typeof(TbTab)] = (nameof(TbTab.Text), "Sample"),
        [typeof(TbColumn<string>)] = (nameof(TbColumn<string>.Title), "Sample"),
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
        // An id names the part users act on: a field's control, unless it has two or is a group, or a trigger.
        var named = Assert.Single(cut.FindAll("#contract-id"));
        var field = typeof(IFormField).IsAssignableFrom(component);
        if (named != root)
        {
            Assert.True(field || component == typeof(TbMenu) || component == typeof(TbSpeedDial)
                || component == typeof(TbPopover) || component == typeof(TbPopconfirm),
                $"{component.Name} moved its id off its root.");
            Assert.Equal(field ? "input" : "trigger", named.GetAttribute("data-part"));
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

    [Theory, MemberData(nameof(Fields))]
    public void Render_ShouldLookInvalidWithoutAMessage_WhenTheParentSetsInvalid(Type field)
    {
        var cut = Render(builder =>
        {
            builder.OpenComponent(0, field);
            builder.AddAttribute(1, nameof(TbTextField<string>.Invalid), true);
            AddRequiredParameters(builder, field, sequence: 2);
            builder.CloseComponent();
        });

        Assert.Empty(cut.FindAll("[data-part=error]"));
        Assert.NotEmpty(cut.FindAll("[aria-invalid=true]"));
        Assert.Contains(cut.Nodes.OfType<IElement>().First().ClassList, name => name.EndsWith("--invalid"));
    }

    public static TheoryData<Type, bool> FieldsStaticAndInteractive =>
        new(from row in Fields from interactive in new[] { false, true } select (row.Data, interactive));

    [Theory, MemberData(nameof(FieldsStaticAndInteractive))]
    public void Render_ShouldNameTheControlWithAHiddenLabel_WhenGivenOnlyAnAriaLabel(Type field, bool interactive)
    {
        if (interactive)
        {
            SetRendererInfo(new RendererInfo("Server", isInteractive: true));
        }

        var cut = Render(builder =>
        {
            builder.OpenComponent(0, field);
            builder.AddAttribute(1, nameof(TbTextField<string>.AriaLabel), "Search trails");
            AddRequiredParameters(builder, field, sequence: 2);
            builder.CloseComponent();
        });

        var hidden = Assert.Single(cut.FindAll(".tb-visually-hidden"),
            part => part.TextContent.Trim().StartsWith("Search trails"));
        var names = hidden.TagName == "LEGEND"
            || (hidden.GetAttribute("for") is { } target && cut.FindAll($"[id='{target}']").Count > 0)
            || (hidden.Id is { Length: > 0 } id && cut.FindAll($"[aria-labelledby~='{id}']").Count > 0)
            || hidden.Closest("label")?.QuerySelector("input") is not null;
        Assert.True(names, $"The hidden label of {field.Name} names no control.");
        // A label with an id names each of the field's own parts, such as a range's thumbs and a popup list.
        if (hidden.Id is { Length: > 0 } labelId)
        {
            Assert.All(cut.FindAll("input[aria-labelledby], [role=combobox][aria-labelledby], "
                    + "[role=listbox][aria-labelledby], [role=group][aria-labelledby]"),
                part => Assert.Contains(labelId, part.GetAttribute("aria-labelledby")!.Split(' ')));
        }
    }

    [Theory, MemberData(nameof(Fields))]
    public async Task Focus_ShouldRaiseOnFocusAndOnBlurOnce_WhenFocusCrossesTheField(Type field)
    {
        var events = new List<string>();
        var cut = Render(builder =>
        {
            builder.OpenComponent(0, field);
            builder.AddAttribute(1, nameof(TbTextField<string>.OnFocus),
                EventCallback.Factory.Create<FocusEventArgs>(this, () => events.Add("focus")));
            builder.AddAttribute(2, nameof(TbTextField<string>.OnBlur),
                EventCallback.Factory.Create<FocusEventArgs>(this, () => events.Add("blur")));
            AddRequiredParameters(builder, field, sequence: 3);
            builder.CloseComponent();
        });

        var root = cut.Find("[data-tb-focus-root]");
        await root.TriggerEventAsync("ontbfocusenter", new FieldFocusEventArgs { CrossesField = true });
        await root.TriggerEventAsync("ontbfocusleave", new FieldFocusEventArgs { CrossesField = false });
        await root.TriggerEventAsync("ontbfocusleave", new FieldFocusEventArgs { CrossesField = true });

        Assert.Equal(["focus", "blur"], events);
    }

    [Theory, MemberData(nameof(Fields))]
    public void Render_ShouldMarkOneControlForFocus_WhenAutoFocusIsSet(Type field)
    {
        // Fields are marked only on an interactive page; the browser focuses the mark as it appears.
        SetRendererInfo(new RendererInfo("Server", isInteractive: true));
        var cut = Render(builder =>
        {
            builder.OpenComponent(0, field);
            builder.AddAttribute(1, nameof(TbTextField<string>.Label), "Trail");
            builder.AddAttribute(2, nameof(TbTextField<string>.AutoFocus), true);
            AddRequiredParameters(builder, field, sequence: 3);
            builder.CloseComponent();
        });

        Assert.Single(cut.FindAll("[data-tb-autofocus]"));
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
        foreach (var (name, value) in RequiredParameters(component))
        {
            builder.AddAttribute(sequence++, name, value);
        }
    }

    // A sample for each parameter a component requires, so it renders.
    internal static IEnumerable<(string Name, object Value)> RequiredParameters(Type component)
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
                var type when type == typeof(Func<string, string>) => (Func<string, string>)(text => text),
                _ => throw new NotSupportedException($"Add a sample {property.PropertyType.Name} to the contract " +
                    $"kit for {component.Name}.{property.Name}."),
            };
            yield return (property.Name, value);
        }
    }
}
