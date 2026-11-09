using System.Linq.Expressions;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace Trailblazor.Tests.Components;

public class TbRatingTests : BunitContext
{
    private sealed class Review
    {
        public int Stars { get; set; }
    }

    public TbRatingTests()
    {
        this.AddTrailblazor();
    }

    private IRenderedComponent<TbRating> RenderRating(int value, List<int>? changes = null,
        Action<ComponentParameterCollectionBuilder<TbRating>>? configure = null) => Render<TbRating>(parameters =>
    {
        parameters.Add(rating => rating.Label, "Trail rating").Add(rating => rating.Value, value)
            .Add(rating => rating.ValueChanged, (int next) => changes?.Add(next));
        configure?.Invoke(parameters);
    });

    [Fact]
    public void Render_ShouldBeALabeledGroupOfNamedStarsSharingAName_WhenCalled()
    {
        var cut = RenderRating(3);

        var group = cut.Find("fieldset");
        var inputs = cut.FindAll("input[type=radio]");

        Assert.Equal(("radiogroup", "Trail rating"),
            (group.GetAttribute("role"), cut.Find("legend").TextContent.Trim()));
        Assert.Equal(5, inputs.Count);
        Assert.Single(inputs.Select(input => input.GetAttribute("name")).Distinct());
        Assert.Equal(["1 star", "2 stars", "3 stars", "4 stars", "5 stars"],
            cut.FindAll("[data-part=star] .tb-visually-hidden").Select(name => name.TextContent));
        Assert.Equal([false, false, true, false, false], inputs.Select(input => input.HasAttribute("checked")));
    }

    [Fact]
    public void Change_ShouldRaiseValueChangedWithTheStarsNumber_WhenAStarIsChosen()
    {
        var changes = new List<int>();
        var cut = RenderRating(0, changes);

        cut.FindAll("input[type=radio]")[3].Change(true);

        Assert.Equal([4], changes);
    }

    [Theory]
    [InlineData(true, new[] { 0 })]
    [InlineData(false, new int[0])]
    public void Click_ShouldTakeTheRatingBackOnlyWhenClearable_WhenTheChosenStarIsChosenAgain(bool clearable,
        int[] expected)
    {
        var changes = new List<int>();
        var cut = RenderRating(3, changes, parameters => parameters.Add(rating => rating.Clearable, clearable));

        cut.FindAll("input[type=radio]")[2].Click();

        Assert.Equal(expected, changes);
    }

    [Theory]
    [InlineData(2, " ", new[] { 0 })]
    [InlineData(2, "Enter", new int[0])]
    [InlineData(3, " ", new int[0])]
    public void KeyUp_ShouldTakeTheRatingBackOnlyWithSpaceOnTheChosenStar_WhenClearable(int star, string key,
        int[] expected)
    {
        var changes = new List<int>();
        var cut = RenderRating(2, changes, parameters => parameters.Add(rating => rating.Clearable, true));

        cut.FindAll("input[type=radio]")[star - 1].KeyUp(new KeyboardEventArgs { Key = key });

        Assert.Equal(expected, changes);
    }

    [Fact]
    public void Click_ShouldNotTakeTheRatingBack_WhenAnotherStarIsChosen()
    {
        var changes = new List<int>();
        var cut = RenderRating(3, changes, parameters => parameters.Add(rating => rating.Clearable, true));
        cut.FindAll("input[type=radio]")[3].Click();
        cut.FindAll("input[type=radio]")[3].Change(true);

        Assert.Equal([4], changes);
    }

    [Fact]
    public void Render_ShouldGiveEachRatingItsOwnName_WhenInteractiveRatingsBindSameNamedMembers()
    {
        SetRendererInfo(new RendererInfo("Server", isInteractive: true));
        var reviews = new[] { new Review(), new Review() };

        var cut = Render(builder =>
        {
            foreach (var review in reviews)
            {
                builder.OpenComponent<TbRating>(0);
                builder.AddAttribute(1, "Value", review.Stars);
                builder.AddAttribute(2, "ValueExpression", (Expression<Func<int>>)(() => review.Stars));
                builder.CloseComponent();
            }
        });

        var names = cut.FindAll("input[type=radio]").Select(radio => radio.GetAttribute("name"));
        Assert.Equal(2, names.Distinct().Count());
    }

    [Fact]
    public void Render_ShouldPutInputAttributesAndClassesOnEachStar_WhenTheRatingHasThem()
    {
        SetRendererInfo(new RendererInfo("Server", isInteractive: true));

        var cut = RenderRating(0, configure: parameters => parameters
            .Add(rating => rating.InputAttributes, new Dictionary<string, object> { ["name"] = "stars" })
            .Add(rating => rating.Classes, new ChoiceClasses { Input = "my-input", Label = "my-label" }));

        Assert.All(cut.FindAll("input[type=radio]"), radio =>
        {
            Assert.Equal("stars", radio.GetAttribute("name"));
            Assert.Contains("my-input", radio.ClassList);
        });
        Assert.Contains("my-label", cut.Find("legend").ClassList);
    }

    [Fact]
    public async Task FocusAsync_ShouldAskTheScriptToFocusAStar_WhenCalled()
    {
        var module = JSInterop.SetupModule("./_content/Trailblazor/field.js");
        module.SetupVoid("focusRadio", _ => true).SetVoidResult();
        var cut = RenderRating(2);

        await cut.InvokeAsync(() => cut.Instance.FocusAsync(preventScroll: true));

        var call = module.VerifyInvoke("focusRadio");
        call.Arguments[0].ShouldBeElementReferenceTo(cut.Find("fieldset"));
        Assert.Equal(true, call.Arguments[1]);
    }

    [Fact]
    public void Render_ShouldTurnOffEveryStarButTheChosenOne_WhenReadOnly()
    {
        var changes = new List<int>();
        var cut = RenderRating(2, changes, parameters => parameters.Add(rating => rating.ReadOnly, true)
            .Add(rating => rating.Clearable, true));

        cut.FindAll("input[type=radio]")[1].Click();

        Assert.Equal("true", cut.Find("fieldset").GetAttribute("aria-readonly"));
        Assert.Equal([true, false, true, true, true], cut.FindAll("input[type=radio]")
            .Select(input => input.HasAttribute("disabled")));
        Assert.Empty(changes);
    }

    [Fact]
    public void Render_ShouldShowTheGivenNumberOfStarsWithTheirNames_WhenMaxAndStarLabelAreSet()
    {
        var cut = RenderRating(0, configure: parameters => parameters.Add(rating => rating.Max, 3)
            .Add(rating => rating.StarLabel, star => $"{star} of 3").Add(rating => rating.Size, Size.Large)
            .Add(rating => rating.Tone, Tone.Danger));

        Assert.Equal(["1 of 3", "2 of 3", "3 of 3"],
            cut.FindAll("[data-part=star] .tb-visually-hidden").Select(name => name.TextContent));
        Assert.Contains("tb-rating--large", cut.Find("fieldset").ClassList);
        Assert.Contains("tb-rating--danger", cut.Find("fieldset").ClassList);
    }

    [Fact]
    public void Render_ShouldThrow_WhenMaxIsBelowOne()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => RenderRating(0, configure: parameters => parameters
            .Add(rating => rating.Max, 0)));
    }

    [Fact]
    public void Click_ShouldMarkTheRatingInvalid_WhenARequiredRatingIsTakenBack()
    {
        var cut = RenderRating(3, configure: parameters => parameters.Add(rating => rating.Required, true)
            .Add(rating => rating.Clearable, true));

        cut.FindAll("input[type=radio]")[2].Click();

        Assert.Equal("true", cut.Find("fieldset").GetAttribute("aria-invalid"));
        Assert.Contains("tb-rating--invalid", cut.Find("fieldset").ClassList);
    }
}
