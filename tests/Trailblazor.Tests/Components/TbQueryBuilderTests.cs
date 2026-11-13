using System.Text.Json;
using Bunit;
using Microsoft.AspNetCore.Components;
using Trailblazor.Testing;

namespace Trailblazor.Tests.Components;

public class TbQueryBuilderTests : BunitContext
{
    public TbQueryBuilderTests()
    {
        this.AddTrailblazor();
    }

    public enum Difficulty
    {
        Easy,
        Hard,
    }

    public sealed record Trail(string Name, double LengthKm, DateOnly Opened, bool Loop, Difficulty Difficulty);

    private List<QueryGroup> Raised { get; } = [];

    private QueryGroup Last => Raised[^1];

    private IRenderedComponent<TbQueryBuilder> RenderSearch(
        Action<ComponentParameterCollectionBuilder<TbQueryBuilder>>? configure = null) =>
        Render<TbQueryBuilder>(parameters =>
        {
            parameters.Add(builder => builder.Label, "Trail search").Add(builder => builder.Fields, QueryField.For<Trail>())
                .Add(builder => builder.ValueChanged, (QueryGroup query) => Raised.Add(query));
            configure?.Invoke(parameters);
        });

    private static TbQueryBuilderDriver Search(IRenderedComponent<TbQueryBuilder> cut) =>
        cut.Tb().QueryBuilder("Trail search");

    [Fact]
    public void Render_ShouldShowANamedGroupWithItsJoinAndAddButtons_WhenTheQueryIsEmpty()
    {
        var cut = RenderSearch();

        var root = cut.Find("[data-part=query-builder]");
        Assert.Equal(("group", "Trail search"), (root.GetAttribute("role"), root.GetAttribute("aria-label")));
        Assert.Equal("All of these", cut.Tb().Select("Match").Text);
        Assert.Equal(["Add condition", "Add group"],
            cut.FindAll("[data-part=group-actions] button").Select(button => button.TextContent.Trim()));
        Assert.Empty(cut.FindAll("[data-part=condition]"));
    }

    [Fact]
    public async Task AddConditionAsync_ShouldAddAConditionOnTheFirstFieldThatTakesFocus_WhenClicked()
    {
        var cut = RenderSearch();

        await Search(cut).AddConditionAsync();

        var condition = cut.Find("[data-part=condition]");
        Assert.Equal("Condition 1", condition.GetAttribute("aria-label"));
        Assert.Equal("list", cut.Find("[data-part=conditions]").GetAttribute("role"));
        Assert.Equal("Name", Search(cut).Condition(1).Select("Field").Text);
        Assert.Equal(["Contains", "Doesn't contain", "Starts with", "Ends with", "Is", "Isn't", "Is empty", "Isn't empty"],
            Search(cut).Condition(1).Select("Comparison").Options);
        Assert.NotNull(condition.QuerySelector("[data-part=condition-field] [autofocus], [data-part=condition-field][autofocus], "
            + "[data-part=condition-field] [data-tb-autofocus], [data-part=condition-field][data-tb-autofocus]"));
        Assert.Equal([new GridFilter("Name", FilterOperator.Contains)], Last.Conditions);
    }

    [Fact]
    public async Task Condition_ShouldTakeAComparisonAndValueForItsField_WhenTheUserFillsItIn()
    {
        var cut = RenderSearch();
        await Search(cut).AddConditionAsync();

        await Search(cut).Condition(1).Select("Field").ChooseAsync("Length km");
        await Search(cut).Condition(1).Select("Comparison").ChooseAsync("At least");
        await Search(cut).Condition(1).NumberField("Value").EnterAsync("8");

        Assert.Equal([new GridFilter("LengthKm", FilterOperator.GreaterThanOrEqualTo, 8m)], Last.Conditions);
    }

    [Fact]
    public async Task Condition_ShouldOfferDayComparisonsAndAChoiceList_WhenItsFieldIsADateOrAChoice()
    {
        var cut = RenderSearch();
        await Search(cut).AddConditionAsync();
        await Search(cut).AddConditionAsync();

        await Search(cut).Condition(1).Select("Field").ChooseAsync("Opened");
        await Search(cut).Condition(2).Select("Field").ChooseAsync("Difficulty");
        await Search(cut).Condition(2).Select("Comparison").ChooseAsync("Is any of");
        await Search(cut).Condition(2).Select("Value").ChooseAsync("Easy", "Hard");

        Assert.Equal(["Is", "Isn't", "Before", "On or before", "After", "On or after", "Is empty", "Isn't empty"],
            Search(cut).Condition(1).Select("Comparison").Options);
        Assert.Equal(new GridFilter("Difficulty", FilterOperator.IsAnyOf, new object[] { Difficulty.Easy, Difficulty.Hard }),
            Last.Conditions[1]);
    }

    [Fact]
    public async Task Condition_ShouldDropItsValue_WhenItsFieldChangesOrItsComparisonTakesNone()
    {
        var cut = RenderSearch();
        await Search(cut).AddConditionAsync();
        await Search(cut).Condition(1).TextField("Value").EnterAsync("ridge");

        await Search(cut).Condition(1).Select("Comparison").ChooseAsync("Is empty");
        Assert.Empty(cut.FindAll("[data-part=condition-value]"));
        Assert.Equal([new GridFilter("Name", FilterOperator.IsEmpty)], Last.Conditions);
        await Search(cut).Condition(1).Select("Comparison").ChooseAsync("Is");
        await Search(cut).Condition(1).TextField("Value").EnterAsync("Ridge loop");
        await Search(cut).Condition(1).Select("Field").ChooseAsync("Loop");

        Assert.Equal([new GridFilter("Loop", FilterOperator.EqualTo)], Last.Conditions);
        Assert.Equal(["Yes", "No"], Search(cut).Condition(1).Select("Value").Options);
    }

    [Fact]
    public async Task AddGroupAsync_ShouldNestAGroupWithAConditionAndItsOwnJoin_WhenClicked()
    {
        var cut = RenderSearch();

        await Search(cut).AddGroupAsync();
        await Search(cut).JoinAsync(QueryJoin.Or, 1);
        await Search(cut).AddConditionAsync(1);

        var group = cut.Find("[data-part=groups] > [data-part=group]");
        Assert.Equal(("Group 1", "2"), (group.GetAttribute("aria-label"), group.GetAttribute("data-level")));
        Assert.Equal(QueryJoin.Or, Last.Groups.Single().Join);
        Assert.Equal(2, Last.Groups.Single().Conditions.Count);
        Assert.Equal(QueryJoin.And, Last.Join);
    }

    [Fact]
    public async Task AddGroupAsync_ShouldThrow_WhenTheGroupIsAtTheDeepestLevel()
    {
        var cut = RenderSearch(parameters => parameters.Add(builder => builder.MaxDepth, 2));

        await Search(cut).AddGroupAsync();

        await Assert.ThrowsAsync<InvalidOperationException>(() => Search(cut).AddGroupAsync(1));
    }

    [Fact]
    public async Task Remove_ShouldTakeOutTheConditionOrGroupAndFocusItsGroupsAddButton_WhenClicked()
    {
        var cut = RenderSearch();
        await Search(cut).AddConditionAsync();
        await Search(cut).AddConditionAsync();
        await Search(cut).AddGroupAsync();

        await Search(cut).RemoveConditionAsync(1);
        await Search(cut).RemoveGroupAsync(1);

        Assert.Single(Last.Conditions);
        Assert.Empty(Last.Groups);
        Assert.Equal("Condition 1", cut.Find("[data-part=condition]").GetAttribute("aria-label"));
        Assert.NotEmpty(JSInterop.Invocations["Blazor._internal.domWrapper.focus"]);
    }

    [Fact]
    public void Value_ShouldShowAQueryReadBackFromJson_WhenTheParentSetsIt()
    {
        var saved = JsonSerializer.Deserialize<QueryGroup>(JsonSerializer.Serialize(new QueryGroup
        {
            Join = QueryJoin.Or,
            Conditions = [new("Difficulty", FilterOperator.EqualTo, Difficulty.Hard), new("Opened",
                FilterOperator.LessThan, new DateOnly(2026, 6, 1))],
            Groups = [new() { Conditions = [new("Loop", FilterOperator.EqualTo, true)] }],
        }))!;

        var cut = RenderSearch(parameters => parameters.Add(builder => builder.Value, saved));

        Assert.Equal("Any of these", cut.Tb().Within(() => cut.Find("[data-part=group-header]")).Select("Match").Text);
        Assert.Equal("Hard", Search(cut).Condition(1).Select("Value").Text);
        Assert.Equal("Before", Search(cut).Condition(2).Select("Comparison").Text);
        Assert.Equal("Yes", Search(cut).Condition(1, 1).Select("Value").Text);
        Assert.Empty(Raised);
    }

    [Fact]
    public async Task Value_ShouldKeepTheUsersEdits_WhenTheParentPassesBackTheQueryItHeard()
    {
        QueryGroup? query = null;
        var cut = Render<TbQueryBuilder>(parameters => parameters.Add(builder => builder.Label, "Trail search")
            .Add(builder => builder.Fields, QueryField.For<Trail>())
            .Add(builder => builder.ValueChanged, (QueryGroup changed) => query = changed));
        await Search(cut).AddConditionAsync();

        cut.Render(parameters => parameters.Add(builder => builder.Value, query));
        await Search(cut).AddConditionAsync();

        Assert.Equal(2, query!.Conditions.Count);
        Assert.Equal(2, cut.FindAll("[data-part=condition]").Count);
    }

    [Fact]
    public async Task RemoveGroupAsync_ShouldFocusTheOuterGroupsAddButton_WhenClicked()
    {
        var cut = RenderSearch();
        await Search(cut).AddGroupAsync();
        var focused = JSInterop.Invocations["Blazor._internal.domWrapper.focus"].Count;

        await Search(cut).RemoveGroupAsync(1);

        Assert.Equal(focused + 1, JSInterop.Invocations["Blazor._internal.domWrapper.focus"].Count);
        Assert.Empty(cut.FindAll("[data-part=groups]"));
    }

    [Fact]
    public void Value_ShouldShowItsValues_WhenTheParentBuildsTheQueryInCodeOrWithNumberChoices()
    {
        var levels = new QueryField("Level", "Level", QueryFieldKind.Choice) { Choices = [new(1, "One"), new(2, "Two")] };
        var saved = JsonSerializer.Deserialize<QueryGroup>(JsonSerializer.Serialize(new QueryGroup
        {
            Conditions = [new("Level", FilterOperator.EqualTo, 2)],
        }))!;
        var cut = Render<TbQueryBuilder>(parameters => parameters.Add(builder => builder.Label, "Trail search")
            .Add(builder => builder.Fields, [.. QueryField.For<Trail>(), levels]).Add(builder => builder.Value, new QueryGroup
            {
                Conditions = [new("LengthKm", FilterOperator.EqualTo, 8), new("Opened", FilterOperator.EqualTo,
                    new DateTime(2026, 6, 1, 9, 30, 0)), saved.Conditions[0]],
            }));

        Assert.Equal("8", cut.Find("[data-part=condition] [data-part=condition-value] input").GetAttribute("value"));
        Assert.Equal("Two", Search(cut).Condition(3).Select("Value").Text);
        Assert.Contains("2026", cut.FindAll("[data-part=condition]")[1].QuerySelector("[data-part=condition-value] input")!
            .GetAttribute("value"));
    }

    [Fact]
    public async Task Labels_ShouldReplaceTheListsAndButtonsWords_WhenGiven()
    {
        var cut = RenderSearch(parameters => parameters.Add(builder => builder.Labels, new QueryBuilderLabels
        {
            Match = "Mode", All = "Every", Any = "Some", Field = "Column", Comparison = "Test", Value = "Target",
            Condition = "Rule {0}", Group = "Set {0}", AddCondition = "New rule", AddGroup = "New set",
            RemoveCondition = "Drop rule {0}", RemoveGroup = "Drop set {0}",
        }));

        await Search(cut).AddConditionAsync();
        await Search(cut).AddGroupAsync();

        Assert.Equal("Every", cut.Tb().Within(() => cut.Find("[data-part=group-header]")).Select("Mode").Text);
        Assert.Equal(["Rule 1", "Set 1", "Rule 1"],
            cut.FindAll("[data-part=condition], [data-part=group]").Select(part => part.GetAttribute("aria-label")));
        Assert.Equal(("Name", "Contains"), (Search(cut).Condition(1).Select("Column").Text,
            Search(cut).Condition(1).Select("Test").Text));
        Assert.Null(Search(cut).Condition(1).TextField("Target").Text);
        Assert.Equal("Drop rule 1", cut.Find("[data-part=remove-condition]").GetAttribute("aria-label"));
        Assert.Equal("Drop set 1", cut.Find("[data-part=remove-group]").GetAttribute("aria-label"));
        Assert.Contains("New rule", cut.Find("[data-part=add-condition]").TextContent);
        Assert.Contains("New set", cut.Find("[data-part=add-group]").TextContent);
    }

    [Theory]
    [InlineData("", 1, 3)]
    [InlineData("Trail search", 0, 3)]
    [InlineData("Trail search", 1, 0)]
    public void Render_ShouldThrow_WhenTheLabelFieldsOrDepthAreMissing(string label, int fields, int depth)
    {
        Assert.Throws<InvalidOperationException>(() => Render<TbQueryBuilder>(parameters => parameters
            .Add(builder => builder.Label, label).Add(builder => builder.Fields, QueryField.For<Trail>().Take(fields).ToList())
            .Add(builder => builder.MaxDepth, depth)));
    }

    [Fact]
    public void Value_ShouldThrow_WhenAConditionNamesAFieldNotInFields()
    {
        var error = Assert.Throws<InvalidOperationException>(() => RenderSearch(parameters => parameters
            .Add(builder => builder.Value, new QueryGroup { Conditions = [new("Region", FilterOperator.IsEmpty)] })));

        Assert.Contains("Region", error.Message);
    }
}
