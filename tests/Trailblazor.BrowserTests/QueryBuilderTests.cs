using Trailblazor.Testing.Playwright;

namespace Trailblazor.BrowserTests;

public class QueryBuilderTests(BrowserFixture fixture) : BrowserTest(fixture)
{
    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Condition_ShouldFilterTheItems_WhenItsFieldComparisonAndValueAreFilledIn(string browser)
    {
        var page = await OpenAsync(browser, "/query-builder");
        var search = page.Tb().QueryBuilder("Trail search");

        await search.AddConditionAsync();
        await search.Condition(1).Select("Field").ChooseAsync("Length km");
        await search.Condition(1).Select("Comparison").ChooseAsync("At least");
        await search.Condition(1).NumberField("Value").EnterAsync("8");

        await Expect(Readout(page)).ToHaveTextAsync("Eagle Ridge, Ridge Loop");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Group_ShouldJoinItsConditionsItsOwnWay_WhenNestedInTheQuery(string browser)
    {
        var page = await OpenAsync(browser, "/query-builder");
        var search = page.Tb().QueryBuilder("Trail search");

        await search.AddConditionAsync();
        await search.Condition(1).Select("Field").ChooseAsync("Loop");
        await search.Condition(1).Select("Value").ChooseAsync("Yes");
        await search.AddGroupAsync();
        await search.JoinAsync("Any of these", 1);
        var name = page.GetByRole(AriaRole.Group, new() { Name = "Group 1" })
            .GetByRole(AriaRole.Group, new() { Name = "Condition 1" }).GetByLabel("Value");
        await name.FillAsync("pine");
        await name.PressAsync("Enter");
        await search.AddConditionAsync(1);
        await search.Condition(2, 1).Select("Field").ChooseAsync("Difficulty");
        await search.Condition(2, 1).Select("Value").ChooseAsync("Hard");

        await Expect(Readout(page)).ToHaveTextAsync("Pine Hollow, Ridge Loop");
        await search.RemoveGroupAsync(1);
        await search.RemoveConditionAsync(1);
        await Expect(Readout(page)).ToHaveTextAsync("Pine Hollow, Eagle Ridge, Cedar Flats, Ridge Loop");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Focus_ShouldFollowAddedAndRemovedConditions_WhenTheButtonsArePressedWithTheKeyboard(string browser)
    {
        var page = await OpenAsync(browser, "/query-builder");
        var builder = page.GetByRole(AriaRole.Group, new() { Name = "Trail search" });
        var add = builder.GetByRole(AriaRole.Button, new() { Name = "Add condition" });
        await Expect(page.Locator("[data-part=query-builder]")).ToBeVisibleAsync();
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        await add.FocusAsync();
        await page.Keyboard.PressAsync("Enter");
        var field = builder.GetByRole(AriaRole.Group, new() { Name = "Condition 1" }).GetByRole(AriaRole.Combobox,
            new() { Name = "Field" });
        await Expect(field).ToBeFocusedAsync();
        await builder.GetByRole(AriaRole.Button, new() { Name = "Remove condition 1" }).FocusAsync();
        await page.Keyboard.PressAsync("Enter");

        await Expect(builder.GetByRole(AriaRole.Group, new() { Name = "Condition 1" })).ToHaveCountAsync(0);
        await Expect(add).ToBeFocusedAsync();
    }

    private static ILocator Readout(IPage page) =>
        page.GetByRole(AriaRole.Status, new() { Name = "Matches", Exact = true });
}
