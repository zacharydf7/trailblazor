using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace Trailblazor.Tests.Components;

public class TbCheckboxGroupTests : BunitContext
{
    public TbCheckboxGroupTests() => this.AddTrailblazor();

    [Fact]
    public void Render_ShouldDrawALegendAndACheckboxPerOptionCheckedByTheValue_WhenCalled()
    {
        var cut = RenderGroup(["Tue"], helperText: "Pick at least one.");

        var group = cut.Find("fieldset");
        Assert.Equal(["tb-checkbox-group", "tb-checkbox-group--primary", "tb-checkbox-group--medium"], group.ClassList);
        Assert.Equal("Days", cut.Find("legend").TextContent.Trim());
        Assert.Equal(cut.Find("[data-part=helper]").Id, group.GetAttribute("aria-describedby"));
        Assert.Equal([false, true, false, false],
            cut.FindAll("input[type=checkbox]").Select(box => box.HasAttribute("checked")));
        Assert.Equal("Mon", cut.Find("[data-part=options] [data-part=label]").TextContent.Trim());
    }

    [Fact]
    public void Change_ShouldAddAndRemoveValuesInTheOrderTheyWereChecked_WhenOptionsAreToggled()
    {
        IReadOnlyList<string>? value = null;
        var cut = RenderGroup(["Wed"], onChanged: next => value = next);

        cut.Find("input[value=Mon]").Change(true);
        Assert.Equal(["Wed", "Mon"], value);
        cut.Render(parameters => parameters.Add(group => group.Value, value));
        cut.Find("input[value=Wed]").Change(false);

        Assert.Equal(["Mon"], value);
    }

    [Fact]
    public void Render_ShouldCheckAnOptionThatTheComparerMatches_WhenTheValueIsAnotherInstance()
    {
        var cut = Render<TbCheckboxGroup<Trail>>(parameters => parameters.Add(group => group.Label, "Trails")
            .Add(group => group.Value, [new Trail(2, "Ridge")]).Add(group => group.Comparer, new TrailComparer())
            .Add(group => group.ChildContent, (RenderFragment)(builder =>
            {
                AddOption(builder, 0, new Trail(1, "Lake"));
                AddOption(builder, 10, new Trail(2, "Ridge"));
            })));

        Assert.Equal([false, true], cut.FindAll("input[type=checkbox]").Select(box => box.HasAttribute("checked")));
    }

    [Fact]
    public void Change_ShouldRemoveTheMatchedValue_WhenAnOptionTheComparerMatchesIsUnchecked()
    {
        IReadOnlyList<Trail>? value = null;
        var cut = Render<TbCheckboxGroup<Trail>>(parameters => parameters.Add(group => group.Label, "Trails")
            .Add(group => group.Value, [new Trail(2, "Ridge, renamed"), new Trail(1, "Lake")])
            .Add(group => group.Comparer, new TrailComparer())
            .Add(group => group.ValueChanged, (IReadOnlyList<Trail> next) => value = next)
            .Add(group => group.ChildContent, (RenderFragment)(builder =>
            {
                AddOption(builder, 0, new Trail(1, "Lake"));
                AddOption(builder, 10, new Trail(2, "Ridge"));
            })));

        cut.Find("input[value=Ridge]").Change(false);

        Assert.Equal([new Trail(1, "Lake")], value);
    }

    [Fact]
    public void Render_ShouldPutAnOptionsClassStyleAndAttributesOnItsLabel_WhenTheOptionGetsThem()
    {
        var cut = Render<TbCheckboxGroup<string>>(parameters => parameters.Add(group => group.ChildContent,
            (RenderFragment)(builder =>
            {
                builder.OpenComponent<TbCheckboxItem<string>>(0);
                builder.AddComponentParameter(1, nameof(TbCheckboxItem<string>.Value), "Mon");
                builder.AddComponentParameter(2, nameof(TbCheckboxItem<string>.Class), "option-class");
                builder.AddComponentParameter(3, nameof(TbCheckboxItem<string>.Style), "outline: 1px solid red");
                builder.AddAttribute(4, "data-option", "monday");
                builder.CloseComponent();
            })));

        var label = cut.Find("label.tb-checkbox__label");
        Assert.Contains("option-class", label.ClassList);
        Assert.Equal("outline: 1px solid red", label.GetAttribute("style"));
        Assert.Equal("monday", label.GetAttribute("data-option"));
    }

    [Fact]
    public void Change_ShouldMarkEveryCheckboxInvalidAndDescribeTheGroup_WhenValidationFails()
    {
        var cut = RenderGroup(["Mon"], required: true,
            validation: days => days is { Count: > 0 } ? [] : ["Pick at least one day."]);

        cut.Find("input[value=Mon]").Change(false);

        var group = cut.Find("fieldset");
        Assert.Contains("tb-checkbox-group--invalid", group.ClassList);
        Assert.Equal("true", group.GetAttribute("aria-invalid"));
        Assert.StartsWith(cut.Find("[data-part=error]").Id, group.GetAttribute("aria-describedby"));
        Assert.All(cut.FindAll("input[type=checkbox]"), box => Assert.Equal("true", box.GetAttribute("aria-invalid")));
        Assert.Contains("*", cut.Find("legend").TextContent);
    }

    [Fact]
    public void Render_ShouldPutAGivenIdOnTheItemsInput_WhenTheItemHasOne()
    {
        var cut = Render<TbCheckboxGroup<string>>(parameters => parameters.Add(group => group.ChildContent,
            (RenderFragment)(builder =>
            {
                builder.OpenComponent<TbCheckboxItem<string>>(0);
                builder.AddComponentParameter(1, nameof(TbCheckboxItem<string>.Value), "Mon");
                builder.AddAttribute(2, "id", "monday");
                builder.CloseComponent();
            })));

        Assert.Equal("checkbox", cut.Find("#monday").GetAttribute("type"));
        Assert.Null(cut.Find("label.tb-checkbox__label").GetAttribute("id"));
    }

    [Fact]
    public void Render_ShouldMarkEachCheckboxReadOnlyButNotDisabled_WhenReadOnly()
    {
        var cut = Render<TbCheckboxGroup<string>>(parameters => parameters.Add(group => group.ReadOnly, true)
            .Add(group => group.ChildContent, (RenderFragment)(builder => AddOption(builder, 0, "Mon"))));

        var box = cut.Find("input[type=checkbox]");
        Assert.Equal("true", box.GetAttribute("aria-readonly"));
        Assert.False(box.HasAttribute("disabled"));
    }

    [Fact]
    public void Render_ShouldDisableTheGroupOrOneOption_WhenEitherIsDisabled()
    {
        var group = RenderGroup([], disabled: true);
        var option = RenderGroup([]);

        Assert.True(group.Find("fieldset").HasAttribute("disabled"));
        Assert.True(option.Find("input[value=Sun]").HasAttribute("disabled"));
        Assert.Contains("tb-checkbox-group__item--disabled", option.FindAll("label.tb-checkbox__label")[^1].ClassList);
    }

    [Fact]
    public void Render_ShouldNameEveryCheckboxAfterTheBoundProperty_WhenBound()
    {
        var plan = new Plan();

        var cut = Render<TbCheckboxGroup<string>>(parameters => parameters.Add(group => group.Value, plan.Days)
            .Add(group => group.ValueExpression, () => plan.Days)
            .Add(group => group.ChildContent, (RenderFragment)(builder =>
            {
                AddOption(builder, 0, "Mon");
                AddOption(builder, 10, "Tue");
            })));

        Assert.Equal(["plan.Days", "plan.Days"],
            cut.FindAll("input[type=checkbox]").Select(box => box.GetAttribute("name")));
    }

    [Fact]
    public async Task FocusAsync_ShouldAskTheScriptToFocusACheckboxInTheGroup_WhenCalled()
    {
        var module = JSInterop.SetupModule("./_content/Trailblazor/field.js");
        module.SetupVoid("focusCheckbox", _ => true).SetVoidResult();
        var cut = RenderGroup([]);

        await cut.InvokeAsync(() => cut.Instance.FocusAsync(preventScroll: true));

        var call = module.VerifyInvoke("focusCheckbox");
        call.Arguments[0].ShouldBeElementReferenceTo(cut.Find("fieldset"));
        Assert.Equal(true, call.Arguments[1]);
    }

    [Fact]
    public void Render_ShouldThrow_WhenAnOptionIsOutsideAGroup()
    {
        Assert.Throws<InvalidOperationException>(() => Render<TbCheckboxItem<string>>(parameters =>
            parameters.Add(item => item.Value, "Mon")));
    }

    // Razor types an item by its literal, so Value="2" in a group of int? is a TbCheckboxItem<int>.
    [Fact]
    public void Change_ShouldCheckTheItemsValue_WhenAnItemOfTheUnderlyingTypeIsInANullableGroup()
    {
        // Numbers need the culture the page renders in, which RendererInfo tells.
        IReadOnlyList<int?>? chosen = null;
        var cut = Render<TbCheckboxGroup<int?>>(parameters => parameters.Add(group => group.Value, [1])
            .Add(group => group.ValueChanged, (IReadOnlyList<int?> next) => chosen = next)
            .Add(group => group.ChildContent, (RenderFragment)(builder =>
            {
                AddOption(builder, 0, 1);
                AddOption(builder, 10, 2);
            })));

        Assert.True(cut.FindAll("input")[0].HasAttribute("checked"));
        cut.FindAll("input")[1].Change(true);

        Assert.Equal([1, 2], chosen);
    }

    [Fact]
    public void Render_ShouldDescribeTheGroupAsRequiredButLeaveItsName_WhenRequired()
    {
        var cut = RenderGroup([], helperText: "Pick at least one.", required: true);

        var fieldset = cut.Find("fieldset");
        Assert.Equal(["Pick at least one.", "required"], fieldset.GetAttribute("aria-describedby")!.Split(' ')
            .Select(id => cut.Find($"#{id}").TextContent.Trim()));
        Assert.Null(fieldset.GetAttribute("aria-required"));
        Assert.Equal("Days *", cut.Find("legend").TextContent.Trim());
    }

    [Fact]
    public void Render_ShouldDescribeTheGroupAsRequired_WhenItHasAnInfoButton()
    {
        var cut = Render<TbCheckboxGroup<string>>(parameters => parameters.Add(group => group.Label, "Days")
            .Add(group => group.Info, "Weekdays only.").Add(group => group.Required, true));

        var fieldset = cut.Find("fieldset");
        Assert.Equal("Days", cut.Find($"#{fieldset.GetAttribute("aria-labelledby")}").TextContent);
        Assert.Equal("required", cut.Find($"#{fieldset.GetAttribute("aria-describedby")}").TextContent);
    }

    private IRenderedComponent<TbCheckboxGroup<string>> RenderGroup(IReadOnlyList<string> value,
        Action<IReadOnlyList<string>>? onChanged = null, string? helperText = null, bool disabled = false,
        bool required = false, Func<IReadOnlyList<string>?, IEnumerable<string>>? validation = null) =>
        Render<TbCheckboxGroup<string>>(parameters => parameters.Add(group => group.Label, "Days")
            .Add(group => group.Value, value).Add(group => group.HelperText, helperText)
            .Add(group => group.Disabled, disabled).Add(group => group.Required, required)
            .Add(group => group.Validation, validation)
            .Add(group => group.ValueChanged, (IReadOnlyList<string> next) => onChanged?.Invoke(next))
            .Add(group => group.ChildContent, (RenderFragment)(builder =>
            {
                AddOption(builder, 0, "Mon");
                AddOption(builder, 10, "Tue");
                AddOption(builder, 20, "Wed");
                AddOption(builder, 30, "Sun", disabled: true);
            })));

    private static void AddOption<TItem>(RenderTreeBuilder builder, int sequence, TItem value, bool disabled = false)
    {
        builder.OpenComponent<TbCheckboxItem<TItem>>(sequence);
        builder.AddComponentParameter(sequence + 1, nameof(TbCheckboxItem<TItem>.Value), value);
        builder.AddComponentParameter(sequence + 2, nameof(TbCheckboxItem<TItem>.Disabled), disabled);
        builder.AddComponentParameter(sequence + 3, nameof(TbCheckboxItem<TItem>.ChildContent),
            (RenderFragment)(content => content.AddContent(0, value?.ToString())));
        builder.CloseComponent();
    }

    private sealed record Trail(int Id, string Name)
    {
        public override string ToString() => Name;
    }

    private sealed class TrailComparer : IEqualityComparer<Trail>
    {
        public bool Equals(Trail? x, Trail? y) => x?.Id == y?.Id;

        public int GetHashCode(Trail obj) => obj.Id;
    }

    private sealed class Plan
    {
        public IReadOnlyList<string> Days { get; set; } = [];
    }
}
