using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace Trailblazor.Tests.Components;

// A value loaded again is a new object, so a Comparer that matches by id keeps it matching its option.
public class ComparerTests : BunitContext
{
    private static readonly Trail[] Trails = [new(1, "Ridge loop"), new(2, "Lake shore")];
    private static readonly IEqualityComparer<Trail> ById = EqualityComparer<Trail>.Create(
        (first, second) => first?.Id == second?.Id, trail => trail.Id);

    // Loaded again: the same trail as Trails[1], as a new object with the name it has now.
    private static readonly Trail Reloaded = new(2, "Lake shore trail");

    public ComparerTests() => this.AddTrailblazor();

    [Fact]
    public void RadioGroup_ShouldCheckTheReloadedValuesOption_WhenGivenAComparer()
    {
        var cut = Render<TbRadioGroup<Trail>>(parameters => parameters.Add(group => group.Label, "Trail")
            .Add(group => group.Value, Reloaded).Add(group => group.Comparer, ById)
            .Add(group => group.ChildContent, Radios()));

        Assert.Equal([false, true], cut.FindAll("input").Select(input => input.HasAttribute("checked")));
    }

    [Fact]
    public void RadioGroup_ShouldLeaveTheFormClean_WhenTheReloadedValuesOptionIsCheckedAgain()
    {
        var dirty = false;
        var cut = Render<TbForm>(parameters => parameters.Add(form => form.IsDirtyChanged, (bool now) => dirty = now)
            .Add(form => form.ChildContent, (RenderFragment)(builder =>
            {
                builder.OpenComponent<TbRadioGroup<Trail>>(0);
                builder.AddComponentParameter(1, nameof(TbRadioGroup<Trail>.Label), "Trail");
                builder.AddComponentParameter(2, nameof(TbRadioGroup<Trail>.Value), Reloaded);
                builder.AddComponentParameter(3, nameof(TbRadioGroup<Trail>.Comparer), ById);
                builder.AddComponentParameter(4, nameof(TbRadioGroup<Trail>.ChildContent), Radios());
                builder.CloseComponent();
            })));

        cut.FindAll("input[type=radio]")[0].Change(true);
        Assert.True(dirty);
        cut.FindAll("input[type=radio]")[1].Change(true);

        Assert.False(dirty);
    }

    [Fact]
    public void MenuRadioGroup_ShouldCheckTheReloadedValuesItem_WhenGivenAComparer()
    {
        var cut = Render<TbMenuRadioGroup<Trail>>(parameters => parameters.Add(group => group.Label, "Trail")
            .Add(group => group.Value, Reloaded).Add(group => group.Comparer, ById)
            .Add(group => group.ChildContent, MenuItems()));

        Assert.Equal(["false", "true"],
            cut.FindAll("[role=menuitemradio]").Select(item => item.GetAttribute("aria-checked")));
    }

    [Fact]
    public void ToggleGroup_ShouldPressTheReloadedValuesItem_WhenGivenAComparer()
    {
        var changes = 0;
        var cut = Render<TbToggleGroup<Trail>>(parameters => parameters.Add(group => group.AriaLabel, "Trail")
            .Add(group => group.Value, Reloaded).Add(group => group.Comparer, ById)
            .Add(group => group.ValueChanged, (Trail _) => changes++)
            .Add(group => group.ChildContent, Items()));

        Assert.Equal(["false", "true"],
            cut.FindAll("[data-part=item]").Select(item => item.GetAttribute("aria-pressed")));
        cut.FindAll("[data-part=item]")[1].Click();
        Assert.Equal(0, changes);
    }

    [Fact]
    public void MultiToggleGroup_ShouldPressAndReleaseTheReloadedItem_WhenGivenAComparer()
    {
        IReadOnlyList<Trail> pressed = [Reloaded];
        var cut = Render<TbMultiToggleGroup<Trail>>(parameters => parameters.Add(group => group.AriaLabel, "Trails")
            .Add(group => group.Value, pressed).Add(group => group.Comparer, ById)
            .Add(group => group.ValueChanged, (IReadOnlyList<Trail> next) => pressed = next)
            .Add(group => group.ChildContent, Items()));

        Assert.Equal(["false", "true"],
            cut.FindAll("[data-part=item]").Select(item => item.GetAttribute("aria-pressed")));
        cut.FindAll("[data-part=item]")[1].Click();
        Assert.Empty(pressed);
    }

    [Fact]
    public void Autocomplete_ShouldMarkTheReloadedValuesSuggestion_WhenGivenAComparer()
    {
        var cut = Render<TbAutocomplete<Trail>>(parameters => parameters.Add(field => field.Label, "Trail")
            .Add(field => field.Items, Trails).Add(field => field.ItemText, trail => trail.Name)
            .Add(field => field.Value, Reloaded).Add(field => field.Comparer, ById));

        cut.Find("input").Input("l");

        Assert.Equal(["false", "true"],
            cut.FindAll("[role=option]").Select(option => option.GetAttribute("aria-selected")));
    }

    [Fact]
    public void Autocomplete_ShouldLeaveTheFormClean_WhenTheReloadedValuesSuggestionIsChosenAgain()
    {
        var dirty = false;
        var cut = Render<TbForm>(parameters => parameters.Add(form => form.IsDirtyChanged, (bool now) => dirty = now)
            .Add(form => form.ChildContent, (RenderFragment)(builder =>
            {
                builder.OpenComponent<TbAutocomplete<Trail>>(0);
                builder.AddComponentParameter(1, nameof(TbAutocomplete<Trail>.Label), "Trail");
                builder.AddComponentParameter(2, nameof(TbAutocomplete<Trail>.Items), Trails);
                builder.AddComponentParameter(3, nameof(TbAutocomplete<Trail>.ItemText),
                    (Func<Trail, string>)(trail => trail.Name));
                builder.AddComponentParameter(4, nameof(TbAutocomplete<Trail>.Value), Reloaded);
                builder.AddComponentParameter(5, nameof(TbAutocomplete<Trail>.Comparer), ById);
                builder.CloseComponent();
            })));

        cut.Find("input").Input("l");
        cut.FindAll("[role=option]")[0].Click();
        Assert.True(dirty);
        cut.Find("input").Input("l");
        cut.FindAll("[role=option]")[1].Click();

        Assert.False(dirty);
    }

    [Fact]
    public void Autocomplete_ShouldShowTheNewText_WhenTheParentPassesTheValueAgainRenamed()
    {
        var cut = Render<TbAutocomplete<Trail>>(parameters => parameters.Add(field => field.Label, "Trail")
            .Add(field => field.Items, Trails).Add(field => field.ItemText, trail => trail.Name)
            .Add(field => field.Value, Trails[1]).Add(field => field.Comparer, ById));

        cut.Render(parameters => parameters.Add(field => field.Value, Reloaded));

        Assert.Equal("Lake shore trail", cut.Find("input").GetAttribute("value"));
    }

    [Fact]
    public void Select_ShouldShowNoClearButton_WhenItsComparerFindsTheValueEmpty()
    {
        // Any id of 0 or less counts as none chosen.
        var cut = Render<TbSelect<int>>(parameters => parameters.Add(select => select.Label, "Trail")
            .Add(select => select.Clearable, true).Add(select => select.Value, -1)
            .Add(select => select.Comparer, EqualityComparer<int>.Create((first, second) =>
                Math.Max(first, 0) == Math.Max(second, 0), id => Math.Max(id, 0)))
            .Add(select => select.Items, [1, 2]));

        Assert.Empty(cut.FindAll("[data-part=clear]"));
    }

    private static RenderFragment MenuItems() => builder =>
    {
        foreach (var trail in Trails)
        {
            builder.OpenComponent<TbMenuRadioItem<Trail>>(0);
            builder.AddAttribute(1, nameof(TbMenuRadioItem<Trail>.Value), trail);
            builder.AddAttribute(2, nameof(TbMenuRadioItem<Trail>.ChildContent),
                (RenderFragment)(content => content.AddContent(0, trail.Name)));
            builder.CloseComponent();
        }
    };

    private static RenderFragment Radios() => builder =>
    {
        foreach (var trail in Trails)
        {
            builder.OpenComponent<TbRadio<Trail>>(0);
            builder.AddAttribute(1, nameof(TbRadio<Trail>.Value), trail);
            builder.AddAttribute(2, nameof(TbRadio<Trail>.ChildContent),
                (RenderFragment)(content => content.AddContent(0, trail.Name)));
            builder.CloseComponent();
        }
    };

    private static RenderFragment Items() => builder =>
    {
        foreach (var trail in Trails)
        {
            AddItem(builder, trail);
        }
    };

    private static void AddItem(RenderTreeBuilder builder, Trail trail)
    {
        builder.OpenComponent<TbToggleItem<Trail>>(0);
        builder.AddAttribute(1, nameof(TbToggleItem<Trail>.Value), trail);
        builder.AddAttribute(2, nameof(TbToggleItem<Trail>.ChildContent),
            (RenderFragment)(content => content.AddContent(0, trail.Name)));
        builder.CloseComponent();
    }

    private sealed record Trail(int Id, string Name);
}
