using Microsoft.Playwright;

namespace Trailblazor.Testing.Playwright;

/// <summary>A <c>TbQueryBuilder</c>, found by its <c>Label</c>. A group is named by its path of group numbers from
/// the outer group, so no numbers is the outer group itself and <c>2, 1</c> is the first group inside the outer
/// group's second group. Each change waits until the app has drawn it.</summary>
public sealed class TbQueryBuilderLocator(ILocator scope, string label)
{
    private ILocator Root => scope.GetByRole(AriaRole.Group, new() { Name = label, Exact = true })
        .And(scope.Locator("[data-part=query-builder]"));

    /// <summary>The fields of a group's condition with this number, found by their labels, Field, Comparison and
    /// Value: <c>Condition(1).Select("Field").ChooseAsync("Region")</c>.</summary>
    public TrailblazorPage Condition(int number, params int[] group) => new(ConditionOf(number, group));

    /// <summary>Clicks the group's Add condition button and waits for the new condition.</summary>
    public async Task AddConditionAsync(params int[] group)
    {
        var conditions = Conditions(group);
        var count = await conditions.CountAsync();
        await GroupOf(group).Locator(":scope > [data-part=group-actions] [data-part=add-condition]").ClickAsync();
        await Assertions.Expect(conditions).ToHaveCountAsync(count + 1);
    }

    /// <summary>Clicks the group's Add group button and waits for the new group.</summary>
    public async Task AddGroupAsync(params int[] group)
    {
        var groups = Groups(group);
        var count = await groups.CountAsync();
        await GroupOf(group).Locator(":scope > [data-part=group-actions] [data-part=add-group]").ClickAsync();
        await Assertions.Expect(groups).ToHaveCountAsync(count + 1);
    }

    /// <summary>Clicks the remove button of the group's condition with this number and waits for it to go.</summary>
    public async Task RemoveConditionAsync(int number, params int[] group)
    {
        var count = await Conditions(group).CountAsync();
        await ConditionOf(number, group).Locator("[data-part=remove-condition]").ClickAsync();
        await Assertions.Expect(Conditions(group)).ToHaveCountAsync(count - 1);
    }

    /// <summary>Clicks the remove button of the group at this path, which can't be the outer group, and waits for it
    /// to go.</summary>
    public async Task RemoveGroupAsync(params int[] group)
    {
        ArgumentOutOfRangeException.ThrowIfZero(group.Length, nameof(group));
        var siblings = Groups(group[..^1]);
        var count = await siblings.CountAsync();
        await GroupOf(group).Locator(":scope > [data-part=group-header] [data-part=remove-group]").ClickAsync();
        await Assertions.Expect(siblings).ToHaveCountAsync(count - 1);
    }

    /// <summary>Picks the option of the group's Match list with this name, such as "Any of these".</summary>
    public Task JoinAsync(string option, params int[] group) =>
        new TbSelectLocator(scope, GroupOf(group).Locator(":scope > [data-part=group-header] [role=combobox]"))
            .ChooseAsync(option);

    private ILocator Conditions(int[] group) =>
        GroupOf(group).Locator(":scope > [data-part=conditions] > li > [data-part=condition]");

    private ILocator Groups(int[] group) => GroupOf(group).Locator(":scope > [data-part=groups] > [data-part=group]");

    private ILocator ConditionOf(int number, int[] group) => Conditions(group).Nth(number - 1);

    private ILocator GroupOf(int[] path) => path.Aggregate(Root, (group, number) =>
        group.Locator(":scope > [data-part=groups] > [data-part=group]").Nth(number - 1));
}
