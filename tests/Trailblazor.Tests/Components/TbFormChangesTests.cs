using Bunit;
using Microsoft.AspNetCore.Components;
using Trailblazor.Testing;

namespace Trailblazor.Tests.Components;

public class TbFormChangesTests : BunitContext
{
    private bool _touched;
    private bool _dirty;

    public TbFormChangesTests() => this.AddTrailblazor();

    [Fact]
    public void Change_ShouldMarkTheFormTouchedAndDirty_WhenAFieldChangesAndUntilItsChangedBack()
    {
        var cut = RenderForm("Ridge loop");
        Assert.Equal((false, false), (_touched, _dirty));

        cut.Find("input").Change("Lake shore");
        Assert.Equal((true, true), (_touched, _dirty));

        cut.Find("input").Change("Ridge loop");
        Assert.Equal((true, false), (_touched, _dirty));
    }

    [Fact]
    public async Task Reset_ShouldMarkTheFormUntouchedAndClean_WhenItPutsTheFieldsBack()
    {
        var cut = RenderForm("Ridge loop");
        cut.Find("input").Change("Lake shore");

        await cut.InvokeAsync(() => cut.Instance.ResetAsync());

        Assert.Equal((false, false), (_touched, _dirty));
    }

    [Fact]
    public void Render_ShouldMakeTheFormClean_WhenTheParentGivesADirtyFieldANewValue()
    {
        var cut = RenderForm("Ridge loop");
        cut.Find("input").Change("Lake shore");
        Assert.True(_dirty);

        cut.Render(parameters => parameters.Add(form => form.ChildContent, Field("Cedar flats")));

        Assert.False(_dirty);
    }

    [Fact]
    public async Task MarkAsSaved_ShouldMarkTheFormUntouchedAndClean_WhenTheValuesAreSaved()
    {
        var cut = RenderForm("Ridge loop");
        cut.Find("input").Change("Lake shore");

        await cut.InvokeAsync(() => cut.Instance.MarkAsSavedAsync());

        Assert.Equal((false, false), (_touched, _dirty));
        await cut.InvokeAsync(() => cut.Instance.ResetAsync());
        Assert.Equal("Lake shore", cut.Find("input").GetAttribute("value"));
    }

    [Fact]
    public void Change_ShouldLeaveTheFormClean_WhenACheckboxIsCheckedAndUncheckedAgain()
    {
        var cut = RenderDays((IReadOnlyList<string>)["Mon"], ["Mon", "Tue"], comparer: null);

        cut.FindAll("input[type=checkbox]")[1].Change(true);
        Assert.True(_dirty);
        cut.FindAll("input[type=checkbox]")[1].Change(false);

        Assert.False(_dirty);
    }

    [Fact]
    public void Input_ShouldMarkTheFormTouchedAndDirty_WhenAFieldHoldsTextItCantRead()
    {
        var cut = Render<TbForm>(parameters => parameters.Add(form => form.IsTouchedChanged, (bool touched) => _touched = touched)
            .Add(form => form.IsDirtyChanged, (bool dirty) => _dirty = dirty)
            .Add(form => form.ChildContent, (RenderFragment)(builder =>
            {
                builder.OpenComponent<TbNumberField<int?>>(0);
                builder.AddComponentParameter(1, nameof(TbNumberField<int?>.Label), "Hikers");
                builder.AddComponentParameter(2, nameof(TbNumberField<int?>.Value), (int?)5);
                builder.CloseComponent();
            })));

        cut.Find("input").TriggerEvent("ontbnumberchange", new NumberChangeEventArgs { Value = "", BadInput = true });

        Assert.Equal((true, true), (_touched, _dirty));
    }

    private IRenderedComponent<TbForm> RenderForm(string trail) =>
        Render<TbForm>(parameters => parameters.Add(form => form.IsTouchedChanged, (bool touched) => _touched = touched)
            .Add(form => form.IsDirtyChanged, (bool dirty) => _dirty = dirty)
            .Add(form => form.ChildContent, Field(trail)));

    private static RenderFragment Field(string trail) => builder =>
    {
        builder.OpenComponent<TbTextField<string>>(0);
        builder.AddAttribute(1, nameof(TbTextField<string>.Label), "Trail");
        builder.AddAttribute(2, nameof(TbTextField<string>.Value), trail);
        builder.CloseComponent();
    };

    [Fact]
    public void Change_ShouldLeaveTheFormClean_WhenTheFirstOfTwoCheckedItemsIsUncheckedAndCheckedAgain()
    {
        var cut = RenderDays((IReadOnlyList<string>)["Mon", "Tue"], ["Mon", "Tue"], comparer: null);

        cut.FindAll("input[type=checkbox]")[0].Change(false);
        Assert.True(_dirty);
        cut.FindAll("input[type=checkbox]")[0].Change(true);

        Assert.False(_dirty);
    }

    [Fact]
    public void Change_ShouldLeaveTheFormClean_WhenAComparerMatchesARecheckedItemToTheLoadedOne()
    {
        var loaded = new Day(1);
        var cut = RenderDays((IReadOnlyList<Day>)[loaded], [new Day(1), new Day(2)], new SameId());

        cut.FindAll("input[type=checkbox]")[0].Change(false);
        cut.FindAll("input[type=checkbox]")[0].Change(true);

        Assert.False(_dirty);
    }

    private IRenderedComponent<TbForm> RenderDays<TItem>(IReadOnlyList<TItem> value, IReadOnlyList<TItem> options,
        IEqualityComparer<TItem>? comparer) =>
        Render<TbForm>(parameters => parameters.Add(form => form.IsDirtyChanged, (bool dirty) => _dirty = dirty)
            .Add(form => form.ChildContent, (RenderFragment)(builder =>
            {
                builder.OpenComponent<TbCheckboxGroup<TItem>>(0);
                builder.AddComponentParameter(1, nameof(TbCheckboxGroup<TItem>.Label), "Days");
                builder.AddComponentParameter(2, nameof(TbCheckboxGroup<TItem>.Value), value);
                builder.AddComponentParameter(3, nameof(TbCheckboxGroup<TItem>.Comparer), comparer);
                builder.AddComponentParameter(4, nameof(TbCheckboxGroup<TItem>.ChildContent), (RenderFragment)(items =>
                {
                    foreach (var option in options)
                    {
                        items.OpenComponent<TbCheckboxItem<TItem>>(0);
                        items.AddComponentParameter(1, nameof(TbCheckboxItem<TItem>.Value), option);
                        items.AddComponentParameter(2, nameof(TbCheckboxItem<TItem>.ChildContent),
                            (RenderFragment)(text => text.AddContent(0, $"{option}")));
                        items.CloseComponent();
                    }
                }));
                builder.CloseComponent();
            })));

    // A class, not a record, so a day loaded again is a different instance that only a comparer matches.
    private sealed class Day(int id)
    {
        public int Id { get; } = id;

        public override string ToString() => $"Day {Id}";
    }

    private sealed class SameId : IEqualityComparer<Day>
    {
        public bool Equals(Day? x, Day? y) => x?.Id == y?.Id;

        public int GetHashCode(Day day) => day.Id;
    }

    [Fact]
    public async Task Change_ShouldLeaveTheFormClean_WhenATagIsRemovedAndAddedAgainButNotInAnotherCase()
    {
        var cut = Render<TbForm>(parameters => parameters.Add(form => form.IsDirtyChanged, (bool dirty) => _dirty = dirty)
            .Add(form => form.ChildContent, (RenderFragment)(builder =>
            {
                builder.OpenComponent<TbTagInput>(0);
                builder.AddComponentParameter(1, nameof(TbTagInput.Label), "Features");
                builder.AddComponentParameter(2, nameof(TbTagInput.Value), (IReadOnlyList<string>)["Shade"]);
                builder.AddComponentParameter(3, nameof(TbTagInput.Comparer), StringComparer.OrdinalIgnoreCase);
                builder.CloseComponent();
            })));
        var tags = cut.Tb().TagInput("Features");

        await tags.RemoveAsync("Shade");
        await tags.AddAsync("Shade");
        Assert.False(_dirty);
        await tags.RemoveAsync("Shade");
        await tags.AddAsync("shade");

        Assert.True(_dirty);
    }
}
